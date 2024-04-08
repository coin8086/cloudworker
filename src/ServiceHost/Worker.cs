using CloudWorker.MessageQueue;
using CloudWorker.ServiceInterface;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWorker.ServiceHost;

class WorkerOptions
{
    public int? Concurrency { get; set; } = 1;

    public static WorkerOptions Default = new WorkerOptions();
}

class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IUserService _userService;
    private readonly IMessageQueue _requests;
    private readonly IMessageQueue _responses;
    private readonly WorkerOptions _workerOptions;
    private readonly TelemetryClient _telemetryClient;

    public Worker(
        IOptions<WorkerOptions> options, ILogger<Worker> logger, IUserService userService,
        [FromKeyedServices(Queues.RequestQueue)] IMessageQueue requests,
        [FromKeyedServices(Queues.ResponseQueue)] IMessageQueue responses,
        TelemetryClient telemetryClient)
    {
        _logger = logger;
        _userService = userService;
        _requests = requests;
        _responses = responses;
        _workerOptions = options.Value;
        _telemetryClient = telemetryClient;
        SetTelemetryProperties();
    }

    private void SetTelemetryProperties()
    {
        string? queueType = null;
        if (_responses is ServiceBusQueue)
        {
            queueType = ServiceBusQueue.QueueType;
        }
        else if (_responses is StorageQueue)
        {
            queueType = StorageQueue.QueueType;
        }
        else
        {
            queueType = "unknown";
        }
        _telemetryClient.Context.GlobalProperties.Add("QueueType", queueType);
        _telemetryClient.Context.GlobalProperties.Add("ServiceType", _userService.GetType().FullName);
    }

    private async Task ProcessMessageAsync(CancellationToken stoppingToken)
    {
        var lease = _requests.MessageLease.TotalMilliseconds;
        var interval = (int)(lease * 3 / 4);
        while (!stoppingToken.IsCancellationRequested)
        {
            IMessage? request = null;
            try
            {
                request = await _requests.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Waiting for request is cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when waiting for request");
                throw;
            }

            using (_telemetryClient.StartOperation<RequestTelemetry>("ProcessMessage"))
            {
                _logger.LogTrace("Received request {id}", request.Id);

                try
                {
                    using var timer = new Timer(async _ => {
                        try
                        {
                            await request.RenewLeaseAsync();
                        }
                        catch (Exception ex)
                        {
                            //TODO: Is the exception correlated by the operation in app insights? If not, how to?
                            //TODO: Retry when failed
                            _logger.LogWarning("Failed renewing lease for request {id}. Error: {error}", request.Id, ex);
                        }
                    }, null, interval, interval);

                    string? result = null;

                    using (_telemetryClient.StartOperation<DependencyTelemetry>("InvokeService"))
                    {
                        try
                        {
                            //InvokeAsync of a user service should catch all application exceptions and handle them properly.
                            //The only exception is the OperationCanceledException, which can be thrown when stoppingToken is
                            //set to canceled.
                            result = await _userService.InvokeAsync(request.Content, stoppingToken);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Invoking user service is canceled.");
                            timer.Change(Timeout.Infinite, Timeout.Infinite);
                            await request.ReturnAsync();
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error when invoking user service");
                            timer.Change(Timeout.Infinite, Timeout.Infinite);
                            await request.ReturnAsync();
                            throw;
                        }
                    }

                    await _responses.SendAsync(result);

                    //Until result is succesfully sent, then request can be removed from queue.
                    await request.DeleteAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ProcessMessage error");
                    throw;
                }
            }
        }

        if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Cancellation is requested. Quit.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _userService.InitializeAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ExecuteAsync: Canceled when initializing.");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExecuteAsync: Failed initializing user service.");
            throw;
        }

        int concurrency = _workerOptions.Concurrency ?? 1;
        if (concurrency <= 0)
        {
            concurrency = 1;
        }

        _logger.LogInformation("ExecuteAsync: Concurrency = {concurrency}", concurrency);

        using var semaphore = new SemaphoreSlim(concurrency);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await semaphore.WaitAsync();
                _ = Task.Run(() => ProcessMessageAsync(stoppingToken), stoppingToken).ContinueWith(_ => semaphore.Release());
            }
            catch (Exception ex)
            {
                //Log error and continue...
                _logger.LogError(ex, "ExecuteAsync: Error");
            }
        }
        _logger.LogInformation("ExecuteAsync: Stopping...");

        //Wait for all running tasks to finish or timeout.
        var tasks = new Task[concurrency];
        for (int i = 0; i < concurrency; i++)
        {
            tasks[i] = semaphore.WaitAsync(3000);
        }
        await Task.WhenAll(tasks);
        _logger.LogInformation("ExecuteAsync: Stopped.");
    }

    public override void Dispose()
    {
        base.Dispose();
        _telemetryClient.FlushAsync(CancellationToken.None).Wait();
        _logger.LogInformation("Telemetry flushed.");
    }
}

static class ServiceCollectionWorkerServiceExtensions
{
    public static IServiceCollection AddWorkerService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<Worker>();
        services.Configure<WorkerOptions>(configuration.GetSection("Worker"));
        return services;
    }
}

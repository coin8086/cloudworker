using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud.Soa;

class WorkerOptions
{
    public int? Concurrency { get; set; } = 1;

    public static WorkerOptions Default = new WorkerOptions();
}

class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISoaService _userService;
    private readonly IRequestQueue _requests;
    private readonly IResponseQueue _responses;
    private readonly WorkerOptions _workerOptions;

    public Worker(ILogger<Worker> logger, ISoaService userService, IRequestQueue requests, IResponseQueue responses,
        IOptions<WorkerOptions> options)
    {
        _logger = logger;
        _userService = userService;
        _requests = requests;
        _responses = responses;
        _workerOptions = options.Value;
    }

    private async Task ProcessMessageAsync(CancellationToken stoppingToken)
    {
        try
        {
            var lease = _requests.MessageLease.TotalMilliseconds;
            var interval = (int)(lease * 3 / 4);
            while (!stoppingToken.IsCancellationRequested)
            {
                IMessage? request = null;
                try
                {
                    request = await _requests.WaitAsync(cancel: stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("ProcessMessageAsync: Waiting for request is cancelled.");
                    break;
                }

                _logger.LogTrace("ProcessMessageAsync: Received request {id}", request.Id);

                using var timer = new Timer(async _ => {
                    try
                    {
                        await request.RenewLeaseAsync();
                    }
                    catch (Exception ex)
                    {
                        //TODO: Retry when failed
                        _logger.LogWarning("ProcessMessageAsync: Failed renewing lease for request {id}. Error: {error}", request.Id, ex);
                    }
                }, null, interval, interval);


                string? result = null;
                try
                {
                    //InvokeAsync of a user service should catch all application exceptions and handle them properly.
                    //This means it could return error message to client in the result, or throw an exception out,
                    //which all depend on the user service. The only exception is the OperationCanceledException, which
                    //should be thrown when stoppingToken is set to cancelled.
                    result = await _userService.InvokeAsync(request.Content, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("ProcessMessageAsync: User service call is cancelled. Return current request back to the queue.");
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                    await request.ReturnAsync();
                    break;
                }

                //TODO: Retry when throttled
                await _responses.SendAsync(result);

                //Until result is succesfully sent, then request can be removed from queue.
                await request.DeleteAsync();
            }

            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("ProcessMessageAsync: Cancellation is requested. Quit.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("ProcessMessageAsync: Error: {error}", ex);
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                _logger.LogError("ExecuteAsync: Error: {error}", ex);
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

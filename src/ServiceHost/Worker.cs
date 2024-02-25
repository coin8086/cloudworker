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
}

class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IUserService _userService;
    private readonly IRequestQueue _requests;
    private readonly IResponseQueue _responses;
    private readonly WorkerOptions _workerOptions;

    public Worker(ILogger<Worker> logger, IUserService userService, IRequestQueue requests, IResponseQueue responses,
        IOptions<WorkerOptions> options)
    {
        _logger = logger;
        _userService = userService;
        _requests = requests;
        _responses = responses;
        _workerOptions = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                IMessage? request = null;
                try
                {
                    request = await _requests.WaitAsync(cancel: stoppingToken, lease: TimeSpan.FromSeconds(48));
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Waiting for request is cancelled.");
                    break;
                }

                using var timer = new Timer(async _ => {
                    try
                    {
                        await request.RenewLeaseAsync(TimeSpan.FromSeconds(48));
                    }
                    catch (Exception ex)
                    {
                        //TODO: Retry when failed?
                        _logger.LogWarning("Failed renewing lease for request {id}. Error: {error}", request.Id, ex);
                    }
                }, null, 15 * 1000, 15 * 1000);


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
                    _logger.LogInformation("User service call is cancelled. Return current request back to the queue.");
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                    await request.RenewLeaseAsync(TimeSpan.Zero);
                    break;
                }

                await _responses.SendAsync(result);

                //Until result is succesfully sent, then request can be removed from queue.
                await request.DeleteAsync();
            }

            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation is requested. Quit.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in ExecuteAsync: {error}", ex);
            throw;
        }
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

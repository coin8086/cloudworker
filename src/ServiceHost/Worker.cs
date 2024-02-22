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
    private readonly IQueueRequests _requests;
    private readonly IQueueResponses _responses;
    private readonly WorkerOptions _workerOptions;

    public Worker(ILogger<Worker> logger, IUserService userService, IQueueRequests requests, IQueueResponses responses,
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
                var request = await _requests.ReceiveAsync(cancel: stoppingToken, lease: TimeSpan.FromSeconds(48));
                if (request.IsEnding)
                {
                    _logger.LogInformation("Received ending message at {time}. Quit.", DateTimeOffset.Now);
                    await request.AckEndingAsync();
                    break;
                }
                else
                {
                    using var timer = new Timer(_ => {
                        try
                        {
                            request.RenewLeaseAsync(TimeSpan.FromSeconds(48));
                        }
                        catch (Exception ex)
                        {
                            //TODO: Retry when failed?
                            _logger.LogWarning("Failed renewing lease for request {id}. Error: {error}", request.Id, ex);
                        }
                    }, null, 15 * 1000, 15 * 1000);
                    //TODO: Support cancel for InvokeAsync
                    var result = await _userService.InvokeAsync(request.Message);
                    await _responses.SendAsync(result);

                    //Until result is succesfully sent, then request can be removed from queue.
                    await request.RemoveAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in executing: {error}", ex);
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

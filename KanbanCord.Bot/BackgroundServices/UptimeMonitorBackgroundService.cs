using KanbanCord.Core.Options;
using Microsoft.Extensions.Options;

namespace KanbanCord.Bot.BackgroundServices;

public class UptimeMonitorBackgroundService : BackgroundService
{
    private readonly UptimeMonitorOptions _uptimeMonitorOptions;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UptimeMonitorBackgroundService> _logger;
    
    public UptimeMonitorBackgroundService(IHttpClientFactory httpClientFactory, IOptions<UptimeMonitorOptions> uptimeMonitorOptions, ILogger<UptimeMonitorBackgroundService> logger)
    {
        _logger = logger;
        _uptimeMonitorOptions = uptimeMonitorOptions.Value;
        _httpClient = httpClientFactory.CreateClient();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_uptimeMonitorOptions.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(_uptimeMonitorOptions.PushUrl))
        {
            _logger.LogError("An error occurred in {service}, you have to provide a PushUrl when UptimeMonitor is enabled", nameof(UptimeMonitorBackgroundService));
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _httpClient.GetAsync(_uptimeMonitorOptions.PushUrl, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in {service}", nameof(UptimeMonitorBackgroundService));
            }
            finally
            {
                await Task.Delay(_uptimeMonitorOptions.PushInterval, stoppingToken);
            }
        }
    }
}
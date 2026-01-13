namespace FidelityCard.Srv.Services;

/// <summary>
/// Hosted service che sincronizza la cache email all'avvio dell'applicazione
/// caricando tutti gli utenti dalla Sede API
/// </summary>
public class CacheSyncHostedService : IHostedService
{
    private readonly ILogger<CacheSyncHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CacheSyncHostedService(
        ILogger<CacheSyncHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CacheSyncHostedService: Avvio sincronizzazione cache...");
        
        try
        {
            // Crea uno scope per ottenere i servizi scoped
            using var scope = _serviceProvider.CreateScope();
            
            var emailCacheService = scope.ServiceProvider.GetRequiredService<IEmailCacheService>();
            var sedeApiService = scope.ServiceProvider.GetRequiredService<ISedeApiService>();

            // Esegui la sincronizzazione
            await emailCacheService.SyncFromSedeAsync(sedeApiService);

            _logger.LogInformation("CacheSyncHostedService: Sincronizzazione cache completata con successo");
        }
        catch (Exception ex)
        {
            // Non bloccare l'avvio dell'applicazione se la sincronizzazione fallisce
            _logger.LogError(ex, "CacheSyncHostedService: Errore durante la sincronizzazione cache. L'applicazione continuerà con cache vuota.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CacheSyncHostedService: Arresto servizio");
        return Task.CompletedTask;
    }
}

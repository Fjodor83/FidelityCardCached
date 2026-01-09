using FidelityCard.Lib.Services;

namespace FidelityCard.Srv.Services;

public class TokenService : ITokenService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IWebHostEnvironment env, ILogger<TokenService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public string GenerateToken(string email, string store)
    {
        var token = TokenManager.Generate();
        var pathName = Path.Combine(_env.ContentRootPath, "Token");
        
        if (!Directory.Exists(pathName))
        {
            Directory.CreateDirectory(pathName);
        }

        var fileName = Path.Combine(pathName, token);
        File.WriteAllText(fileName, $"{store}\r\n{email}");
        
        _logger.LogInformation("GenerateToken: Token creato per email={Email}, store={Store}, path={Path}", email, store, fileName);
        
        return token;
    }

    /// <summary>
    /// Genera un token per il profilo che include anche il CdFidelity
    /// Formato: store\r\nemail\r\ncdFidelity
    /// </summary>
    public string GenerateProfileToken(string email, string store, string cdFidelity)
    {
        var token = TokenManager.Generate();
        var pathName = Path.Combine(_env.ContentRootPath, "Token");
        
        if (!Directory.Exists(pathName))
        {
            Directory.CreateDirectory(pathName);
        }

        var fileName = Path.Combine(pathName, token);
        var content = $"{store}\r\n{email}\r\n{cdFidelity}";
        File.WriteAllText(fileName, content);
        
        _logger.LogInformation("GenerateProfileToken: Token profilo creato - token={Token}, email={Email}, cdFidelity={CdFidelity}, path={Path}", 
            token, email, cdFidelity, fileName);
        
        return token;
    }

    public async Task<string> ValidateTokenAsync(string token)
    {
        return await GetTokenDataAsync(token);
    }

    public async Task<string> GetTokenDataAsync(string token)
    {
        string pathName = Path.Combine(_env.ContentRootPath, "Token");
        string fileName = Path.Combine(pathName, token);

        _logger.LogInformation("GetTokenDataAsync: Cercando token={Token}, path={Path}", token, fileName);

        // Prima verifichiamo se il file esiste PRIMA del cleanup
        bool existsBeforeCleanup = File.Exists(fileName);
        _logger.LogInformation("GetTokenDataAsync: File esiste prima del cleanup: {Exists}", existsBeforeCleanup);

        // Cleanup dei token vecchi (ma NON quello che stiamo cercando)
        CleanupTokens(token);

        if (File.Exists(fileName))
        {
            var content = await File.ReadAllTextAsync(fileName);
            _logger.LogInformation("GetTokenDataAsync: Token trovato, contenuto={Content}", content);
            return content;
        }

        _logger.LogWarning("GetTokenDataAsync: Token NON trovato - token={Token}", token);
        return string.Empty;
    }

    // Helper per cleanup - esclude il token corrente
    private void CleanupTokens(string? excludeToken = null)
    {
        string pathName = Path.Combine(_env.ContentRootPath, "Token");
        if (!Directory.Exists(pathName)) return;

        var files = Directory.EnumerateFiles(pathName);
        foreach (var file in files)
        {
            // Non cancellare il token che stiamo cercando
            if (!string.IsNullOrEmpty(excludeToken) && file.EndsWith(excludeToken))
                continue;

            FileInfo fileInfo = new(file);
            if (fileInfo.CreationTime < DateTime.Now.AddMinutes(-15))
            {
                try
                {
                    File.Delete(file);
                    _logger.LogDebug("CleanupTokens: Eliminato token vecchio - {File}", file);
                }
                catch { }
            }
        }
    }

    public void BackgroundCleanup(TimeSpan maxAge)
    {
        string pathName = Path.Combine(_env.ContentRootPath, "Token");
        if (!Directory.Exists(pathName)) return;

        var files = Directory.EnumerateFiles(pathName);
        foreach (var file in files)
        {
            FileInfo fileInfo = new(file);
            if (fileInfo.CreationTime < DateTime.Now.Subtract(maxAge))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }
    }
}

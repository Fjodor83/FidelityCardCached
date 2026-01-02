using FidelityCard.Lib.Services;

namespace FidelityCard.Srv.Services;

public class TokenService : ITokenService
{
    private readonly IWebHostEnvironment _env;

    public TokenService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string GenerateToken(string email, string store)
    {
        var token = TokenManager.Generate(); // Assuming TokenManager is still static or available
        var pathName = Path.Combine(_env.ContentRootPath, "Token");
        
        if (!Directory.Exists(pathName))
        {
            Directory.CreateDirectory(pathName);
        }

        var fileName = Path.Combine(pathName, token);
        File.WriteAllText(fileName, $"{store}\r\n{email}");
        
        return token;
    }

    public async Task<string> ValidateTokenAsync(string token)
    {
        // For compatibility with current logic, validation often means just reading it or checking existence.
        // We will return the content if valid, or empty if not.
        return await GetTokenDataAsync(token);
    }

    public async Task<string> GetTokenDataAsync(string token)
    {
        CleanupTokens(); // Opportunistic cleanup, similar to previous logic but can be moved to background

        string pathName = Path.Combine(_env.ContentRootPath, "Token");
        string fileName = Path.Combine(pathName, token);

        if (File.Exists(fileName))
        {
            return await File.ReadAllTextAsync(fileName);
        }

        return string.Empty;
    }

    // Helper for opportunistic cleanup during operations, maintaining original behavior for now
    private void CleanupTokens()
    {
         string pathName = Path.Combine(_env.ContentRootPath, "Token");
         if (!Directory.Exists(pathName)) return;

         var files = Directory.EnumerateFiles(pathName);
         foreach (var file in files)
         {
             FileInfo fileInfo = new(file);
             if (fileInfo.CreationTime < DateTime.Now.AddMinutes(-15))
             {
                 try
                 {
                     File.Delete(file);
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

namespace FidelityCard.Srv.Services;

public interface ITokenService
{
    string GenerateToken(string email, string store);
    Task<string> ValidateTokenAsync(string token);
    Task<string> GetTokenDataAsync(string token);
    void BackgroundCleanup(TimeSpan maxAge);
}

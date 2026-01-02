using FidelityCard.Srv.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

namespace FidelityCard.Tests.Services;

public class TokenServiceTests : IDisposable
{
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly TokenService _tokenService;
    private readonly string _testPath;

    public TokenServiceTests()
    {
        _mockEnv = new Mock<IWebHostEnvironment>();
        
        // Setup a temporary directory for tests
        _testPath = Path.Combine(Path.GetTempPath(), "FidelityCardTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testPath);
        
        _mockEnv.Setup(e => e.ContentRootPath).Returns(_testPath);

        _tokenService = new TokenService(_mockEnv.Object);
    }

    [Fact]
    public void GenerateToken_ShouldCreateTokenFile()
    {
        // Arrange
        string email = "test@example.com";
        string store = "TestStore";

        // Act
        string token = _tokenService.GenerateToken(email, store);

        // Assert
        Assert.False(string.IsNullOrEmpty(token));
        
        string tokenPath = Path.Combine(_testPath, "Token", token);
        Assert.True(File.Exists(tokenPath));
        
        string content = File.ReadAllText(tokenPath);
        Assert.Contains(email, content);
        Assert.Contains(store, content);
    }

    [Fact]
    public async Task GetTokenDataAsync_ShouldReturnContent_WhenTokenExists()
    {
        // Arrange
        string email = "test@example.com";
        string store = "TestStore";
        string token = "test-token";
        
        string tokenDir = Path.Combine(_testPath, "Token");
        Directory.CreateDirectory(tokenDir);
        string tokenPath = Path.Combine(tokenDir, token);
        await File.WriteAllTextAsync(tokenPath, $"{store}\r\n{email}");

        // Act
        string content = await _tokenService.GetTokenDataAsync(token);

        // Assert
        Assert.Contains(email, content);
    }

    [Fact]
    public async Task GetTokenDataAsync_ShouldReturnEmpty_WhenTokenDoesNotExist()
    {
        // Act
        string content = await _tokenService.GetTokenDataAsync("non-existent-token");

        // Assert
        Assert.Equal(string.Empty, content);
    }

    // Cleanup
    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }
}

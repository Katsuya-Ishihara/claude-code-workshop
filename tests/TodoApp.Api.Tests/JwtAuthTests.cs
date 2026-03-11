using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace TodoApp.Api.Tests;

public class JwtAuthTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task 認証なしでAuthorizeエンドポイントにアクセスすると401が返る()
    {
        var response = await _client.GetAsync("/test/auth");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task 有効なJWTトークンでAuthorizeエンドポイントにアクセスすると200が返る()
    {
        var token = GenerateTestToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/test/auth");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Authenticated!", content);
    }

    [Fact]
    public async Task 無効なトークンでアクセスすると401が返る()
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await _client.GetAsync("/test/auth");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string GenerateTestToken()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("dev-only-jwt-secret-key-minimum-32-characters-long!!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };

        var token = new JwtSecurityToken(
            issuer: "TodoApp",
            audience: "TodoApp",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

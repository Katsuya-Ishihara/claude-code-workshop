using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace TodoApp.Client.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "authToken";

    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;

    public AuthStateProvider(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);

        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = ParseClaimsFromJwt(token);

        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim != null && long.TryParse(expClaim.Value, out var expUnix))
        {
            var expTime = DateTimeOffset.FromUnixTimeSeconds(expUnix);
            if (expTime <= DateTimeOffset.UtcNow)
            {
                await MarkUserAsLoggedOutAsync();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public virtual async Task MarkUserAsAuthenticatedAsync(string token)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public virtual async Task MarkUserAsLoggedOutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);

        _httpClient.DefaultRequestHeaders.Authorization = null;

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims;
        }
        catch
        {
            return Enumerable.Empty<Claim>();
        }
    }
}

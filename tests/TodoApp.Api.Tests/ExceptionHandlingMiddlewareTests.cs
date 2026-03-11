using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApp.Api.Tests;

public class ExceptionHandlingMiddlewareTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task NotFoundExceptionが発生した場合_404ProblemDetailsを返す()
    {
        var response = await _client.GetAsync("/test/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonDocument.Parse(content);
        Assert.Equal(404, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("テストリソースが見つかりません", problemDetails.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task BusinessRuleExceptionが発生した場合_400ProblemDetailsを返す()
    {
        var response = await _client.GetAsync("/test/business-rule");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonDocument.Parse(content);
        Assert.Equal(400, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("ビジネスルールに違反しています", problemDetails.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task 未処理の例外が発生した場合_500ProblemDetailsを返しスタックトレースを含まない()
    {
        var response = await _client.GetAsync("/test/unhandled");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonDocument.Parse(content);
        Assert.Equal(500, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("サーバーエラーが発生しました", problemDetails.RootElement.GetProperty("title").GetString());

        // スタックトレースが露出していないことを確認
        Assert.DoesNotContain("at ", content);
        Assert.DoesNotContain("StackTrace", content);
        Assert.DoesNotContain("InvalidOperationException", content);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoApp.Client.Services;
using TodoApp.Shared.Models;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Services;

public class TodoApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static TodoResponse CreateSampleTodoResponse(int id = 1, string title = "テストTodo")
    {
        return new TodoResponse
        {
            Id = id,
            Title = title,
            Description = "テスト説明",
            Status = TodoStatus.NotStarted,
            Priority = Priority.Medium,
            ProgressRate = 0,
            DueDate = new DateTime(2026, 12, 31),
            CreatedAt = new DateTime(2026, 1, 1),
            UpdatedAt = new DateTime(2026, 1, 1),
            CreatedByUserId = 1,
            AssignedToUserId = null
        };
    }

    private static (TodoApiClient client, SimpleMockHttpMessageHandler handler) CreateClient(
        HttpStatusCode statusCode,
        object? responseBody = null)
    {
        var handler = new SimpleMockHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var client = new TodoApiClient(httpClient);
        return (client, handler);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_正常時_TodoリストをHttpStatusCode200で返す()
    {
        var expectedTodos = new List<TodoResponse>
        {
            CreateSampleTodoResponse(1, "Todo1"),
            CreateSampleTodoResponse(2, "Todo2")
        };
        var pagedResponse = new PaginatedResponse<TodoResponse>
        {
            Items = expectedTodos, TotalCount = 2, Page = 1, PageSize = 100
        };
        var (client, handler) = CreateClient(HttpStatusCode.OK, pagedResponse);

        var result = await client.GetAllAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Todo1", result[0].Title);
        Assert.Equal("Todo2", result[1].Title);
        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Contains("api/todos", handler.LastRequest?.RequestUri?.PathAndQuery ?? "");
    }

    [Fact]
    public async Task GetAllAsync_空リスト_空のリストを返す()
    {
        var pagedResponse = new PaginatedResponse<TodoResponse>
        {
            Items = new List<TodoResponse>(), TotalCount = 0, Page = 1, PageSize = 100
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, pagedResponse);

        var result = await client.GetAllAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_存在するTodo_TodoResponseを返す()
    {
        var expected = CreateSampleTodoResponse(1, "テストTodo");
        var (client, handler) = CreateClient(HttpStatusCode.OK, expected);

        var result = await client.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("テストTodo", result.Title);
        Assert.Equal("api/todos/1", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task GetByIdAsync_存在しないTodo_nullを返す()
    {
        var (client, _) = CreateClient(HttpStatusCode.NotFound);

        var result = await client.GetByIdAsync(999);

        Assert.Null(result);
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_正常時_作成されたTodoResponseを返す()
    {
        var expected = CreateSampleTodoResponse(1, "新しいTodo");
        var (client, handler) = CreateClient(HttpStatusCode.Created, expected);
        var request = new CreateTodoRequest
        {
            Title = "新しいTodo",
            Description = "説明",
            Priority = Priority.High
        };

        var result = await client.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("新しいTodo", result.Title);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal("api/todos", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task CreateAsync_バリデーションエラー_nullを返す()
    {
        var (client, _) = CreateClient(HttpStatusCode.BadRequest);
        var request = new CreateTodoRequest { Title = "" };

        var result = await client.CreateAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_サーバーエラー_HttpRequestExceptionをスローする()
    {
        var (client, _) = CreateClient(HttpStatusCode.InternalServerError);
        var request = new CreateTodoRequest { Title = "テスト" };

        await Assert.ThrowsAsync<HttpRequestException>(() => client.CreateAsync(request));
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_正常時_更新されたTodoResponseを返す()
    {
        var expected = CreateSampleTodoResponse(1, "更新済みTodo");
        var (client, handler) = CreateClient(HttpStatusCode.OK, expected);
        var request = new UpdateTodoRequest
        {
            Title = "更新済みTodo",
            Description = "更新された説明"
        };

        var result = await client.UpdateAsync(1, request);

        Assert.NotNull(result);
        Assert.Equal("更新済みTodo", result.Title);
        Assert.Equal(HttpMethod.Put, handler.LastRequest?.Method);
        Assert.Equal("api/todos/1", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task UpdateAsync_存在しないTodo_nullを返す()
    {
        var (client, _) = CreateClient(HttpStatusCode.NotFound);
        var request = new UpdateTodoRequest { Title = "更新済み" };

        var result = await client.UpdateAsync(999, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_サーバーエラー_HttpRequestExceptionをスローする()
    {
        var (client, _) = CreateClient(HttpStatusCode.InternalServerError);
        var request = new UpdateTodoRequest { Title = "更新済み" };

        await Assert.ThrowsAsync<HttpRequestException>(() => client.UpdateAsync(1, request));
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_正常時_trueを返す()
    {
        var (client, handler) = CreateClient(HttpStatusCode.NoContent);

        var result = await client.DeleteAsync(1);

        Assert.True(result);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest?.Method);
        Assert.Equal("api/todos/1", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task DeleteAsync_存在しないTodo_falseを返す()
    {
        var (client, _) = CreateClient(HttpStatusCode.NotFound);

        var result = await client.DeleteAsync(999);

        Assert.False(result);
    }

    #endregion

    #region UpdateStatusAsync

    [Fact]
    public async Task UpdateStatusAsync_正常時_更新されたTodoResponseを返す()
    {
        var expected = CreateSampleTodoResponse(1, "テストTodo");
        expected.Status = TodoStatus.InProgress;
        var (client, handler) = CreateClient(HttpStatusCode.OK, expected);
        var request = new UpdateTodoStatusRequest { Status = TodoStatus.InProgress };

        var result = await client.UpdateStatusAsync(1, request);

        Assert.NotNull(result);
        Assert.Equal(TodoStatus.InProgress, result.Status);
        Assert.Equal(HttpMethod.Patch, handler.LastRequest?.Method);
        Assert.Equal("api/todos/1/status", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task UpdateStatusAsync_存在しないTodo_nullを返す()
    {
        var (client, _) = CreateClient(HttpStatusCode.NotFound);
        var request = new UpdateTodoStatusRequest { Status = TodoStatus.Completed };

        var result = await client.UpdateStatusAsync(999, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateStatusAsync_サーバーエラー_HttpRequestExceptionをスローする()
    {
        var (client, _) = CreateClient(HttpStatusCode.InternalServerError);
        var request = new UpdateTodoStatusRequest { Status = TodoStatus.InProgress };

        await Assert.ThrowsAsync<HttpRequestException>(() => client.UpdateStatusAsync(1, request));
    }

    #endregion

    #region UpdateAssigneeAsync

    [Fact]
    public async Task UpdateAssigneeAsync_正常時_更新されたTodoResponseを返す()
    {
        var expected = CreateSampleTodoResponse(1, "テストTodo");
        expected.AssignedToUserId = 5;
        var (client, handler) = CreateClient(HttpStatusCode.OK, expected);
        var request = new UpdateTodoAssigneeRequest { AssignedToUserId = 5 };

        var result = await client.UpdateAssigneeAsync(1, request);

        Assert.NotNull(result);
        Assert.Equal(5, result.AssignedToUserId);
        Assert.Equal(HttpMethod.Patch, handler.LastRequest?.Method);
        Assert.Equal("api/todos/1/assign", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task UpdateAssigneeAsync_担当者をnullに変更_TodoResponseを返す()
    {
        var expected = CreateSampleTodoResponse(1, "テストTodo");
        expected.AssignedToUserId = null;
        var (client, handler) = CreateClient(HttpStatusCode.OK, expected);
        var request = new UpdateTodoAssigneeRequest { AssignedToUserId = null };

        var result = await client.UpdateAssigneeAsync(1, request);

        Assert.NotNull(result);
        Assert.Null(result.AssignedToUserId);
    }

    [Fact]
    public async Task UpdateAssigneeAsync_存在しないTodo_nullを返す()
    {
        var (client, _) = CreateClient(HttpStatusCode.NotFound);
        var request = new UpdateTodoAssigneeRequest { AssignedToUserId = 5 };

        var result = await client.UpdateAssigneeAsync(999, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAssigneeAsync_サーバーエラー_HttpRequestExceptionをスローする()
    {
        var (client, _) = CreateClient(HttpStatusCode.InternalServerError);
        var request = new UpdateTodoAssigneeRequest { AssignedToUserId = 5 };

        await Assert.ThrowsAsync<HttpRequestException>(() => client.UpdateAssigneeAsync(1, request));
    }

    #endregion
}

/// <summary>
/// テスト用の HttpMessageHandler モック。
/// </summary>
public class SimpleMockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly object? _responseBody;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SimpleMockHttpMessageHandler(HttpStatusCode statusCode, object? responseBody = null)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;

        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var response = new HttpResponseMessage(_statusCode);

        if (_responseBody is not null)
        {
            var json = JsonSerializer.Serialize(_responseBody, JsonOptions);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        return response;
    }
}

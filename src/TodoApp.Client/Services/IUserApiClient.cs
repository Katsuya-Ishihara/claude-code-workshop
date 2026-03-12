using TodoApp.Shared.Responses;

namespace TodoApp.Client.Services;

/// <summary>
/// ユーザーAPI呼び出しクライアントのインターフェース
/// </summary>
public interface IUserApiClient
{
    /// <summary>
    /// ユーザー一覧を取得する
    /// </summary>
    /// <returns>ユーザー一覧</returns>
    Task<List<UserResponse>> GetUsersAsync(CancellationToken cancellationToken = default);
}

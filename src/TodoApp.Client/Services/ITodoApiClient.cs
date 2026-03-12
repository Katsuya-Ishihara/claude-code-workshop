using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Services;

/// <summary>
/// Todo API を呼び出すクライアントのインターフェース。
/// </summary>
public interface ITodoApiClient
{
    /// <summary>
    /// Todo 一覧を取得する。
    /// </summary>
    Task<List<TodoResponse>> GetAllAsync();

    /// <summary>
    /// 指定された ID の Todo を取得する。
    /// </summary>
    /// <param name="id">Todo の ID。</param>
    Task<TodoResponse?> GetByIdAsync(int id);

    /// <summary>
    /// 新しい Todo を作成する。
    /// </summary>
    /// <param name="request">作成リクエスト。</param>
    Task<TodoResponse?> CreateAsync(CreateTodoRequest request);

    /// <summary>
    /// 指定された ID の Todo を更新する。
    /// </summary>
    /// <param name="id">Todo の ID。</param>
    /// <param name="request">更新リクエスト。</param>
    Task<TodoResponse?> UpdateAsync(int id, UpdateTodoRequest request);

    /// <summary>
    /// 指定された ID の Todo を削除する。
    /// </summary>
    /// <param name="id">Todo の ID。</param>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// 指定された ID の Todo のステータスを変更する。
    /// </summary>
    /// <param name="id">Todo の ID。</param>
    /// <param name="request">ステータス変更リクエスト。</param>
    Task<TodoResponse?> UpdateStatusAsync(int id, UpdateTodoStatusRequest request);

    /// <summary>
    /// 指定された ID の Todo の担当者を変更する。
    /// </summary>
    /// <param name="id">Todo の ID。</param>
    /// <param name="request">担当者変更リクエスト。</param>
    Task<TodoResponse?> UpdateAssigneeAsync(int id, UpdateTodoAssigneeRequest request);
}

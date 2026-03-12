using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Data;
using TodoApp.Api.Data.Entities;
using TodoApp.Api.Exceptions;
using TodoApp.Shared.Models;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Services;

public class TodoService(TodoAppDbContext dbContext) : ITodoService
{
    public async Task<TodoResponse> CreateAsync(CreateTodoRequest request, int createdByUserId, CancellationToken cancellationToken = default)
    {
        // 担当者が指定されている場合、存在チェック
        if (request.AssignedToUserId.HasValue)
        {
            var assigneeExists = await dbContext.Users
                .AnyAsync(u => u.Id == request.AssignedToUserId.Value, cancellationToken);
            if (!assigneeExists)
            {
                throw new NotFoundException("指定された担当者が見つかりません");
            }
        }

        var todoItem = new TodoItem
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority ?? Priority.Medium,
            DueDate = request.DueDate,
            CreatedByUserId = createdByUserId,
            AssignedToUserId = request.AssignedToUserId,
            Status = TodoStatus.NotStarted,
            ProgressRate = 0
        };

        dbContext.TodoItems.Add(todoItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        // ナビゲーションプロパティを読み込む
        await dbContext.Entry(todoItem).Reference(t => t.CreatedBy).LoadAsync(cancellationToken);
        if (todoItem.AssignedToUserId.HasValue)
        {
            await dbContext.Entry(todoItem).Reference(t => t.AssignedTo).LoadAsync(cancellationToken);
        }

        return new TodoResponse
        {
            Id = todoItem.Id,
            Title = todoItem.Title,
            Description = todoItem.Description,
            Status = todoItem.Status,
            Priority = todoItem.Priority,
            ProgressRate = todoItem.ProgressRate,
            DueDate = todoItem.DueDate,
            CompletedAt = todoItem.CompletedAt,
            CreatedAt = todoItem.CreatedAt,
            UpdatedAt = todoItem.UpdatedAt,
            CreatedByUserId = todoItem.CreatedByUserId,
            CreatedByDisplayName = todoItem.CreatedBy.DisplayName,
            AssignedToUserId = todoItem.AssignedToUserId,
            AssignedToDisplayName = todoItem.AssignedTo?.DisplayName,
            CategoryId = todoItem.CategoryId
        };
    }
}

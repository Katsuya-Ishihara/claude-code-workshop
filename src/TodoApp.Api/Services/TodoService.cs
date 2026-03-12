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

        return MapToResponse(todoItem);
    }

    public async Task<TodoResponse> UpdateAsync(int id, UpdateTodoRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var todoItem = await dbContext.TodoItems
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("指定されたTodoが見つかりません");

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

        todoItem.Title = request.Title;
        todoItem.Description = request.Description;
        todoItem.Priority = request.Priority ?? Priority.Medium;
        todoItem.DueDate = request.DueDate;
        todoItem.AssignedToUserId = request.AssignedToUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(todoItem);
    }

    private static TodoResponse MapToResponse(TodoItem todoItem)
    {
        return new TodoResponse
        {
            Id = todoItem.Id,
            Title = todoItem.Title,
            Description = todoItem.Description,
            Status = todoItem.Status,
            Priority = todoItem.Priority,
            ProgressRate = todoItem.ProgressRate,
            DueDate = todoItem.DueDate,
            CreatedAt = todoItem.CreatedAt,
            UpdatedAt = todoItem.UpdatedAt,
            CompletedAt = todoItem.CompletedAt,
            CreatedByUserId = todoItem.CreatedByUserId,
            CreatedByDisplayName = todoItem.CreatedBy?.DisplayName ?? string.Empty,
            AssignedToUserId = todoItem.AssignedToUserId,
            AssignedToDisplayName = todoItem.AssignedTo?.DisplayName,
            CategoryId = todoItem.CategoryId
        };
    }
}

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

        return MapToResponse(todoItem);
    }

    public async Task<TodoResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var todo = await dbContext.TodoItems
            .AsNoTracking()
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (todo is null)
        {
            throw new NotFoundException("指定されたTodoが見つかりません");
        }

        return new TodoResponse
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            Status = todo.Status,
            Priority = todo.Priority,
            ProgressRate = todo.ProgressRate,
            DueDate = todo.DueDate,
            CompletedAt = todo.CompletedAt,
            CreatedByUserId = todo.CreatedByUserId,
            AssignedToUserId = todo.AssignedToUserId,
            CreatedByName = todo.CreatedBy.DisplayName,
            AssignedToName = todo.AssignedTo?.DisplayName,
            CreatedAt = todo.CreatedAt,
            UpdatedAt = todo.UpdatedAt
        };
    }

    public async Task<TodoResponse> UpdateStatusAsync(int id, UpdateTodoStatusRequest request, CancellationToken cancellationToken = default)
    {
        var todoItem = await dbContext.TodoItems.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("指定されたTodoが見つかりません");

        todoItem.Status = request.Status;

        if (request.Status == TodoStatus.Completed)
        {
            todoItem.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            todoItem.CompletedAt = null;
        }

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
            CreatedByUserId = todoItem.CreatedByUserId,
            AssignedToUserId = todoItem.AssignedToUserId
        };
    }
}

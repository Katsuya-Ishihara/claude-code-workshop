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

    public async Task<TodoResponse> UpdateAssigneeAsync(int id, UpdateTodoAssigneeRequest request, CancellationToken cancellationToken = default)
    {
        var todoItem = await dbContext.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("指定されたTodoが見つかりません");

        if (request.AssignedToUserId.HasValue)
        {
            var assigneeExists = await dbContext.Users
                .AnyAsync(u => u.Id == request.AssignedToUserId.Value, cancellationToken);
            if (!assigneeExists)
            {
                throw new NotFoundException("指定された担当者が見つかりません");
            }
        }

        todoItem.AssignedToUserId = request.AssignedToUserId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(todoItem);
    }

    private static TodoResponse MapToResponse(Data.Entities.TodoItem todoItem)
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

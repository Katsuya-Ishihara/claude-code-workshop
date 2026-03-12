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
    public async Task<PaginatedResponse<TodoResponse>> GetAllAsync(GetTodosRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.TodoItems
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);
        if (request.Priority.HasValue)
            query = query.Where(t => t.Priority == request.Priority.Value);
        if (request.AssignedToUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == request.AssignedToUserId.Value);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(t => t.Title.Contains(keyword) || (t.Description != null && t.Description.Contains(keyword)));
        }

        query = ApplySort(query, request.SortBy, request.SortDesc);
        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TodoResponse
            {
                Id = t.Id, Title = t.Title, Description = t.Description,
                Status = t.Status, Priority = t.Priority, ProgressRate = t.ProgressRate,
                DueDate = t.DueDate, CompletedAt = t.CompletedAt,
                CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt,
                CreatedByUserId = t.CreatedByUserId,
                CreatedByDisplayName = t.CreatedBy.DisplayName,
                AssignedToUserId = t.AssignedToUserId,
                AssignedToDisplayName = t.AssignedTo != null ? t.AssignedTo.DisplayName : null,
                CategoryId = t.CategoryId
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<TodoResponse> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<TodoResponse> CreateAsync(CreateTodoRequest request, int createdByUserId, CancellationToken cancellationToken = default)
    {
        if (request.AssignedToUserId.HasValue)
        {
            var assigneeExists = await dbContext.Users.AnyAsync(u => u.Id == request.AssignedToUserId.Value, cancellationToken);
            if (!assigneeExists) throw new NotFoundException("指定された担当者が見つかりません");
        }

        var todoItem = new TodoItem
        {
            Title = request.Title, Description = request.Description,
            Priority = request.Priority ?? Priority.Medium, DueDate = request.DueDate,
            CreatedByUserId = createdByUserId, AssignedToUserId = request.AssignedToUserId,
            Status = TodoStatus.NotStarted, ProgressRate = 0
        };

        dbContext.TodoItems.Add(todoItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(todoItem).Reference(t => t.CreatedBy).LoadAsync(cancellationToken);
        if (todoItem.AssignedToUserId.HasValue)
            await dbContext.Entry(todoItem).Reference(t => t.AssignedTo).LoadAsync(cancellationToken);

        return MapToResponse(todoItem);
    }

    public async Task<TodoResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var todo = await dbContext.TodoItems
            .AsNoTracking()
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("指定されたTodoが見つかりません");

        return MapToResponse(todo);
    }

    public async Task<TodoResponse> UpdateAsync(int id, UpdateTodoRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var todoItem = await dbContext.TodoItems
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("指定されたTodoが見つかりません");

        if (request.AssignedToUserId.HasValue)
        {
            var assigneeExists = await dbContext.Users.AnyAsync(u => u.Id == request.AssignedToUserId.Value, cancellationToken);
            if (!assigneeExists) throw new NotFoundException("指定された担当者が見つかりません");
        }

        todoItem.Title = request.Title;
        todoItem.Description = request.Description;
        todoItem.Priority = request.Priority ?? Priority.Medium;
        todoItem.DueDate = request.DueDate;
        todoItem.AssignedToUserId = request.AssignedToUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(todoItem);
    }

    public async Task<TodoResponse> UpdateStatusAsync(int id, UpdateTodoStatusRequest request, CancellationToken cancellationToken = default)
    {
        var todoItem = await dbContext.TodoItems
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("指定されたTodoが見つかりません");

        todoItem.Status = request.Status;
        todoItem.CompletedAt = request.Status == TodoStatus.Completed ? DateTime.UtcNow : null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(todoItem);
    }

    public async Task DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var todoItem = await dbContext.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("指定されたTodoが見つかりません");

        todoItem.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<TodoItem> ApplySort(IQueryable<TodoItem> query, string? sortBy, bool sortDesc)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "title" => sortDesc ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "status" => sortDesc ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "priority" => sortDesc ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "duedate" => sortDesc ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            "createdat" => sortDesc ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            "updatedat" => sortDesc ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };
    }

    private static TodoResponse MapToResponse(TodoItem todoItem)
    {
        return new TodoResponse
        {
            Id = todoItem.Id, Title = todoItem.Title, Description = todoItem.Description,
            Status = todoItem.Status, Priority = todoItem.Priority, ProgressRate = todoItem.ProgressRate,
            DueDate = todoItem.DueDate, CompletedAt = todoItem.CompletedAt,
            CreatedAt = todoItem.CreatedAt, UpdatedAt = todoItem.UpdatedAt,
            CreatedByUserId = todoItem.CreatedByUserId,
            CreatedByDisplayName = todoItem.CreatedBy?.DisplayName ?? string.Empty,
            AssignedToUserId = todoItem.AssignedToUserId,
            AssignedToDisplayName = todoItem.AssignedTo?.DisplayName,
            CategoryId = todoItem.CategoryId
        };
    }
}

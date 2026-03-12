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

        // ステータスフィルタ
        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        // 優先度フィルタ
        if (request.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == request.Priority.Value);
        }

        // 担当者フィルタ
        if (request.AssignedToUserId.HasValue)
        {
            query = query.Where(t => t.AssignedToUserId == request.AssignedToUserId.Value);
        }

        // キーワード検索（タイトルまたは説明）
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(t =>
                t.Title.Contains(keyword) ||
                (t.Description != null && t.Description.Contains(keyword)));
        }

        // ソート
        query = ApplySort(query, request.SortBy, request.SortDesc);

        // 総件数取得
        var totalCount = await query.CountAsync(cancellationToken);

        // ページネーション
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TodoResponse
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                ProgressRate = t.ProgressRate,
                DueDate = t.DueDate,
                CompletedAt = t.CompletedAt,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                CreatedByUserId = t.CreatedByUserId,
                CreatedByDisplayName = t.CreatedBy.DisplayName,
                AssignedToUserId = t.AssignedToUserId,
                AssignedToDisplayName = t.AssignedTo != null ? t.AssignedTo.DisplayName : null,
                CategoryId = t.CategoryId
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<TodoResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

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
            CreatedByDisplayName = todo.CreatedBy.DisplayName,
            AssignedToDisplayName = todo.AssignedTo?.DisplayName,
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
            AssignedToUserId = todoItem.AssignedToUserId
        };
    }
}

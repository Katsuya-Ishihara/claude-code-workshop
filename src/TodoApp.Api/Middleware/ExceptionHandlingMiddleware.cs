using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Exceptions;

namespace TodoApp.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "リソースが見つかりません");
            await WriteProblemDetailsAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            logger.LogWarning(ex, "リソース競合");
            await WriteProblemDetailsAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning(ex, "ビジネスルール違反");
            await WriteProblemDetailsAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "予期しないエラーが発生しました");
            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError,
                "サーバーエラーが発生しました", "しばらく時間をおいて再度お試しください");
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string title, string? detail = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        });
    }
}

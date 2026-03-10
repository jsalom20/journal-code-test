using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Middleware;

public sealed class DatabaseExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public DatabaseExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateException exception) when (TryMap(exception, out var title, out var detail))
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = title,
                Detail = detail
            });
        }
    }

    private static bool TryMap(DbUpdateException exception, out string title, out string detail)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        title = "Database conflict";
        detail = "The requested change conflicts with existing data.";

        if (!message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) &&
            !message.Contains("constraint failed", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (message.Contains("Books.Isbn13", StringComparison.OrdinalIgnoreCase))
        {
            title = "Duplicate ISBN";
            detail = "A book with this ISBN already exists.";
            return true;
        }

        if (message.Contains("Borrowers.CardNumber", StringComparison.OrdinalIgnoreCase))
        {
            title = "Duplicate card number";
            detail = "A borrower with this card number already exists.";
            return true;
        }

        if (message.Contains("Borrowers.Email", StringComparison.OrdinalIgnoreCase))
        {
            title = "Duplicate email";
            detail = "A borrower with this email already exists.";
            return true;
        }

        if (message.Contains("BookCopies.Barcode", StringComparison.OrdinalIgnoreCase))
        {
            title = "Duplicate barcode";
            detail = "A copy with this barcode already exists.";
            return true;
        }

        if (message.Contains("BookCopies.InventoryNumber", StringComparison.OrdinalIgnoreCase))
        {
            title = "Duplicate inventory number";
            detail = "A copy with this inventory number already exists.";
            return true;
        }

        if (message.Contains("Authors.Name", StringComparison.OrdinalIgnoreCase))
        {
            title = "Duplicate author";
            detail = "An author with this name already exists.";
            return true;
        }

        if (message.Contains("Reservations.AssignedCopyId", StringComparison.OrdinalIgnoreCase))
        {
            title = "Copy already assigned";
            detail = "That copy was just assigned to another reservation.";
            return true;
        }

        if (message.Contains("Reservations.BookId, BorrowerId", StringComparison.OrdinalIgnoreCase))
        {
            title = "Duplicate reservation";
            detail = "This borrower already has an active reservation for that title.";
            return true;
        }

        return true;
    }
}

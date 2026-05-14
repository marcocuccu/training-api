using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Training.Extensions;

// 400 Bad Request	            ArgumentException	                Invalid input: empty string, out of range value, invalid DTO...
// 400 Bad Request	            InvalidOperationException	        Invalid operation in the current state i.e. user already disabled
// 401 Unauthorized	            UnauthorizedAccessException	        Failed to login, missing/not valid token, ...
// 403 Forbidden	            AccessViolationException*	        User authenticated but not authorized for this operation
// 404 Not Found	            KeyNotFoundException	            Resource not found: id not found, missing record, ...
// 409 Conflict	                ConflictException,
//                              DuplicateNameException	            Data conflict i.e. email already registered
// 422 Unprocessable Entity	    custom ValidationException	        Valid data but conflict against business rules
// 500 Internal Server Error	Exception	                        Generic error
// 500 Internal Server Error	SqlException	                    Generic SQL error
// 503 Service Unavailable	    custom ServiceUnavailableException	External service (i.e. DB) not available at the moment

public static class AddExceptionHandlerExtension
{
    public static void AddExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(app =>
        {
            app.Run(async context =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                context.Response.ContentType = "application/json";

                // STATUS CODE MAPPING
                context.Response.StatusCode = exception switch
                {
                    BadRequestException => StatusCodes.Status400BadRequest,
                    ArgumentException => StatusCodes.Status400BadRequest,
                    InvalidOperationException => StatusCodes.Status400BadRequest,

                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,

                    ForbiddenException => StatusCodes.Status403Forbidden,

                    NotFoundException => StatusCodes.Status404NotFound,
                    KeyNotFoundException => StatusCodes.Status404NotFound,

                    ConflictException => StatusCodes.Status409Conflict,

                    _ => StatusCodes.Status500InternalServerError
                };

                // LOGGING
                if(context.Response.StatusCode >= 500)
                {
                    logger.LogError(
                        exception,
                        "Unhandled exception occurred while processing {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                }
                else
                {
                    logger.LogWarning(
                        "Exception {ExceptionType} handled with status {StatusCode} while processing {Method} {Path}",
                        exception?.GetType().Name,
                        context.Response.StatusCode,
                        context.Request.Method,
                        context.Request.Path
                    );
                }

                // RESPONSE PREPARATION
                var response = new
                {
                    statusCode = context.Response.StatusCode,
                    message = exception switch
                    {
                        ConflictException ex => ex.Message,
                        UnauthorizedAccessException ex => ex.Message,
                        ArgumentException ex => ex.Message,
                        KeyNotFoundException => "Resource not found",
                        _ => "An unexpected error occurred"
                    }
                };

                await context.Response.WriteAsJsonAsync(response);
            });
        });
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
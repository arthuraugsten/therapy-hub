using Microsoft.AspNetCore.Http.HttpResults;

using TherapyHub.Domain.Abstractions;

namespace TherapyHub.Api.Endpoints;

public static class ErrorExtensions
{
    /// <summary>ProblemDetails extension that carries the stable error code (e.g. "Patient.NotFound").</summary>
    public const string ErrorCodeExtension = "code";

    /// <summary>Maps a domain/application error to an RFC 7807 ProblemDetails response.</summary>
    public static ProblemHttpResult ToProblem(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        return TypedResults.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description,
            extensions: new Dictionary<string, object?> { [ErrorCodeExtension] = error.Code });
    }
}
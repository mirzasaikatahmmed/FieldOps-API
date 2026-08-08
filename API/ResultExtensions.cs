using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Models;
using Microsoft.AspNetCore.Http;

namespace FieldOps.API;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
        => result.IsSuccess
            ? Results.NoContent()
            : Results.Json(new { error = result.Error, statusCode = result.StatusCode }, statusCode: result.StatusCode);

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (!result.IsSuccess)
            return Results.Json(new { error = result.Error, statusCode = result.StatusCode }, statusCode: result.StatusCode);

        return result.StatusCode switch
        {
            201 => Results.Json(result.Data, statusCode: 201),
            _ => Results.Ok(result.Data)
        };
    }
}

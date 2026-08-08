using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace FieldOps.API;

public static class AuthorizationExtensions
{
    public static RouteGroupBuilder RequireRoles(this RouteGroupBuilder group, params string[] roles)
    {
        group.RequireAuthorization(new AuthorizeAttribute { Roles = string.Join(',', roles) });
        return group;
    }

    public static RouteHandlerBuilder RequireRoles(this RouteHandlerBuilder builder, params string[] roles)
    {
        return builder.RequireAuthorization(new AuthorizeAttribute { Roles = string.Join(',', roles) });
    }
}

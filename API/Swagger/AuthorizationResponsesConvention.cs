using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace API.Swagger;

/// <summary>
/// Documents 401/403 (as <see cref="ProblemDetails"/>) only on actions that actually require
/// authentication — via <c>[Authorize]</c> on the action or its controller, and not
/// <c>[AllowAnonymous]</c> — so public endpoints don't advertise responses they can't return.
/// </summary>
public class AuthorizationResponsesConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        var requiresAuth = action.Attributes.OfType<IAuthorizeData>().Any()
                           || action.Controller.Attributes.OfType<IAuthorizeData>().Any();
        if (!requiresAuth || action.Attributes.OfType<IAllowAnonymous>().Any())
            return;

        action.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status401Unauthorized));
        action.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status403Forbidden));
    }
}

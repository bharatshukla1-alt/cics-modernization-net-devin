using SignOnService.Api.Auth;
using SignOnService.Api.Menus;

namespace SignOnService.Api.Endpoints;

/// <summary>Role menus (COMEN01C main menu, COADM01C admin menu).</summary>
public static class MenuEndpoints
{
    public static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        var menus = app.MapGroup("/api/v1/menus").WithTags("Menus");
        menus.MapGet("/main", (MenuService service) => service.Main())
            .RequireAuthorization(Roles.User)
            .WithName("GetMainMenu")
            .WithSummary("Customer services menu (COMEN02Y options) for regular users.")
            .Produces<MenuResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        menus.MapGet("/admin", (MenuService service) => service.Admin())
            .RequireAuthorization(Roles.Admin)
            .WithName("GetAdminMenu")
            .WithSummary("Administration menu (COADM02Y options) for administrators.")
            .Produces<MenuResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        return app;
    }
}

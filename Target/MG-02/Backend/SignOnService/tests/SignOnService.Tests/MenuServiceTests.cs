using SignOnService.Api.Menus;

namespace SignOnService.Tests;

public class MenuServiceTests
{
    private static MenuService Service(params (string Program, string Route)[] routes)
    {
        var options = new MenuRouteOptions();
        foreach (var (program, route) in routes) options.Routes[program] = route;
        return new MenuService(options);
    }

    [Fact]
    public void Main_menu_lists_eleven_options_in_source_order()
    {
        var menu = Service().Main();
        Assert.Equal("Customer services", menu.Title);
        Assert.Equal(Enumerable.Range(1, 11), menu.Options.Select(o => o.Number));
        Assert.Equal("Account View", menu.Options[0].Name);
        Assert.Equal("Pending Authorization View", menu.Options[10].Name);
    }

    [Fact]
    public void Admin_menu_lists_six_options_and_routes_migrated_ones()
    {
        var menu = Service(("COTRTLIC", "/transaction-types"), ("COTRTUPC", "/transaction-types/new")).Admin();
        Assert.Equal(6, menu.Options.Count);
        Assert.Equal(MenuStatus.NotInstalled, menu.Options[0].Status);
        Assert.Null(menu.Options[0].Route);
        Assert.Equal((MenuStatus.Available, "/transaction-types"), (menu.Options[4].Status, menu.Options[4].Route));
        Assert.Equal((MenuStatus.Available, "/transaction-types/new"), (menu.Options[5].Status, menu.Options[5].Route));
    }

    [Fact]
    public void Unmigrated_main_options_are_not_installed()
    {
        Assert.All(Service().Main().Options, o => Assert.Equal(MenuStatus.NotInstalled, o.Status));
    }

    [Fact]
    public void Admin_only_option_is_flagged_for_regular_users_before_dummy_check()
    {
        var menu = Service(("X", "/x")).Build("t", [new MenuEntry(1, "Secret", "DUMMY001", 'A'), new MenuEntry(2, "Routed", "X", 'A')], callerIsAdmin: false);
        Assert.Equal(MenuStatus.AdminOnly, menu.Options[0].Status);
        Assert.Equal(MenuStatus.AdminOnly, menu.Options[1].Status);
        Assert.Null(menu.Options[1].Route);
    }

    [Fact]
    public void Dummy_program_is_coming_soon()
    {
        var menu = Service(("DUMMY001", "/x")).Build("t", [new MenuEntry(1, "Later", "DUMMY001", 'U')], callerIsAdmin: true);
        Assert.Equal(MenuStatus.ComingSoon, menu.Options[0].Status);
    }

    [Fact]
    public void Blank_route_counts_as_not_installed()
    {
        var menu = Service(("COUSR00C", " ")).Admin();
        Assert.Equal(MenuStatus.NotInstalled, menu.Options[0].Status);
    }
}

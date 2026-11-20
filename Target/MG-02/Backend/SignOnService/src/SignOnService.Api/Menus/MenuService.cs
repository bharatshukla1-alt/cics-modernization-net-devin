namespace SignOnService.Api.Menus;

public sealed record MenuEntry(int Number, string Name, string Program, char UserType);

public sealed record MenuOption(int Number, string Name, string? Route, string Status);

public sealed record MenuResponse(string Title, IReadOnlyList<MenuOption> Options);

public static class MenuStatus
{
    public const string Available = "AVAILABLE";
    public const string NotInstalled = "NOT_INSTALLED";
    public const string ComingSoon = "COMING_SOON";
    public const string AdminOnly = "ADMIN_ONLY";
}

/// <summary>Menu tables from COMEN02Y (main) and COADM02Y (admin), in source order.</summary>
public static class MenuCatalog
{
    public static readonly IReadOnlyList<MenuEntry> Main =
    [
        new(1, "Account View", "COACTVWC", 'U'),
        new(2, "Account Update", "COACTUPC", 'U'),
        new(3, "Credit Card List", "COCRDLIC", 'U'),
        new(4, "Credit Card View", "COCRDSLC", 'U'),
        new(5, "Credit Card Update", "COCRDUPC", 'U'),
        new(6, "Transaction List", "COTRN00C", 'U'),
        new(7, "Transaction View", "COTRN01C", 'U'),
        new(8, "Transaction Add", "COTRN02C", 'U'),
        new(9, "Transaction Reports", "CORPT00C", 'U'),
        new(10, "Bill Payment", "COBIL00C", 'U'),
        new(11, "Pending Authorization View", "COPAUS0C", 'U'),
    ];

    public static readonly IReadOnlyList<MenuEntry> Admin =
    [
        new(1, "User List", "COUSR00C", 'A'),
        new(2, "User Add", "COUSR01C", 'A'),
        new(3, "User Update", "COUSR02C", 'A'),
        new(4, "User Delete", "COUSR03C", 'A'),
        new(5, "Transaction Type List/Update", "COTRTLIC", 'A'),
        new(6, "Transaction Type Maintenance", "COTRTUPC", 'A'),
    ];
}

public sealed class MenuRouteOptions
{
    /// <summary>Legacy program name → SPA route for groups already migrated.</summary>
    public Dictionary<string, string> Routes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>COMEN01C/COADM01C PROCESS-ENTER-KEY outcomes, precomputed per option.</summary>
public sealed class MenuService(MenuRouteOptions options)
{
    public const string MainTitle = "Customer services";
    public const string AdminTitle = "Administration";

    public MenuResponse Main() => Build(MainTitle, MenuCatalog.Main, callerIsAdmin: false);

    public MenuResponse Admin() => Build(AdminTitle, MenuCatalog.Admin, callerIsAdmin: true);

    public MenuResponse Build(string title, IEnumerable<MenuEntry> entries, bool callerIsAdmin) =>
        new(title, entries.Select(e => ToOption(e, callerIsAdmin)).ToList());

    private MenuOption ToOption(MenuEntry entry, bool callerIsAdmin)
    {
        if (entry.UserType == 'A' && !callerIsAdmin)
            return new(entry.Number, entry.Name, null, MenuStatus.AdminOnly);
        if (entry.Program.StartsWith("DUMMY", StringComparison.OrdinalIgnoreCase))
            return new(entry.Number, entry.Name, null, MenuStatus.ComingSoon);
        return options.Routes.TryGetValue(entry.Program, out var route) && !string.IsNullOrWhiteSpace(route)
            ? new(entry.Number, entry.Name, route, MenuStatus.Available)
            : new(entry.Number, entry.Name, null, MenuStatus.NotInstalled);
    }
}

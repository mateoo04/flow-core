namespace FlowCore.Data;

/// <summary>Stable IDs for root seed entities so URLs and bookmarks stay consistent across runs.</summary>
internal static class DemoSeedIds
{
    public static readonly Guid UserAlex = Guid.Parse("a1000001-0000-4000-8000-000000000001");
    public static readonly Guid UserSam = Guid.Parse("a1000001-0000-4000-8000-000000000002");

    public static readonly Guid TagUi = Guid.Parse("a2000002-0000-4000-8000-000000000001");
    public static readonly Guid TagBug = Guid.Parse("a2000002-0000-4000-8000-000000000002");

    public static readonly Guid WorkspaceNorth = Guid.Parse("a3000003-0000-4000-8000-000000000001");
    public static readonly Guid WorkspaceSouth = Guid.Parse("a3000003-0000-4000-8000-000000000002");
    public static readonly Guid WorkspacePlatform = Guid.Parse("a3000003-0000-4000-8000-000000000003");
}

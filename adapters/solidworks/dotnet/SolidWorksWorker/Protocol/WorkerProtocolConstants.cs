namespace SolidWorksMcp.SolidWorksWorker.Protocol;

internal static class WorkerProtocolConstants
{
    public const string ProtocolVersion = "0.1.0";
    public const string WorkerName = "solidworks-worker";
    public const string WorkerVersion = "0.1.0";
    public const string BackendName = "solidworks-dotnet-worker";
    public const string SliceName = "level1-real-modeling-v1";
    public const int BaselineSolidWorksMajorVersion = 2022;

    public static readonly int[] SupportedSolidWorksMajorVersions =
    [
        BaselineSolidWorksMajorVersion
    ];

    public static readonly string[] SupportedCommandKinds =
    [
        "new_part",
        "select_plane",
        "start_sketch",
        "draw_line",
        "draw_circle",
        "draw_centered_rectangle",
        "close_sketch",
        "extrude_boss",
        "save_part",
        "export_step"
    ];
}

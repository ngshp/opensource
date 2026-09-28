namespace NGPB.Launcher.Models;

public sealed class AppConfig
{
    public string LauncherVersion { get; set; } = "";

    public string GameName { get; set; } = "";

    public string GameExe { get; set; } = "";

    public string ApiUrl { get; set; } = "";

    public string PatchUrl { get; set; } = "";

    public string Manifest { get; set; } = "";

    public bool AutoUpdate { get; set; }

    public bool RequireSHA256 { get; set; }
}

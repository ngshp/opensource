namespace NGPB.Launcher.Models;

public class AppConfig
{
    public string LauncherVersion { get; set; } = "1.0.0";

    public string GameName { get; set; } = "NGPB";

    public string GameExe { get; set; } = "NGPB.exe";

    public string ApiUrl { get; set; } = "";

    public string PatchUrl { get; set; } = "";

    public string Manifest { get; set; } = "manifest.json";

    public bool AutoUpdate { get; set; } = true;

    public bool RequireSHA256 { get; set; } = true;
}

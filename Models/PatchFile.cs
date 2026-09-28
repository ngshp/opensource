namespace NGPB.Launcher.Models;

public sealed class PatchFile
{
    public string Name { get; set; } = "";

    public string Url { get; set; } = "";

    public long Size { get; set; }

    public string Sha256 { get; set; } = "";
}

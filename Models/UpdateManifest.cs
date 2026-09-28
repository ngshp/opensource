using System.Collections.Generic;

namespace NGPB.Launcher.Models;

public sealed class UpdateManifest
{
    public string Version { get; set; } = "";

    public string Channel { get; set; } = "stable";

    public List<PatchFile> Files { get; set; } = new();
}

using System.Collections.Generic;

namespace NGPB.Launcher.Models;

public sealed class PatchManifest
{
    public string Version { get; set; } = "";

    public List<PatchFile> Files { get; set; } = new();
}

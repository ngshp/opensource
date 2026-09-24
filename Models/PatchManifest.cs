namespace NGPB.Launcher.Models;


public class PatchManifest
{

public string Version {get;set;}="";


public int Build {get;set;}


public List<PatchFile> Files {get;set;}
=
new();


}

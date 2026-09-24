namespace NGPB.Launcher.Services;


public class BackupManager
{


private readonly string backupFolder =
"Backup";



public void Backup(string file)

{


if(!File.Exists(file))

return;



Directory.CreateDirectory(
backupFolder);



string name =
Path.GetFileName(file);



string destination =
Path.Combine(

backupFolder,

name+".backup"

);



File.Copy(

file,

destination,

true

);


}



}

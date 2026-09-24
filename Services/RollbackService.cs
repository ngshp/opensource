namespace NGPB.Launcher.Services;


public class RollbackService
{


private readonly string backupFolder =
"Backup";



public bool Restore(string file)

{


string backup =
Path.Combine(

backupFolder,

Path.GetFileName(file)+".backup"

);



if(!File.Exists(backup))

return false;



File.Copy(

backup,

file,

true

);



return true;


}


}

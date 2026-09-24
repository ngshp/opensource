using NGPB.Launcher.Security;


namespace NGPB.Launcher.Services;


public class RepairService
{


private readonly BackupManager backup =
new();



public async Task<bool> Repair(

string file,

string hash)

{


var verify =
await SHA256Verifier.Verify(

file,

hash

);



if(verify.Success)

return true;



if(File.Exists(file))

{

File.Delete(file);

}



return false;


}



}

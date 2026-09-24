using System.Security.Cryptography;


namespace NGPB.Launcher.Security;


public static class IntegrityMonitor
{


public static bool VerifyLauncher()

{


string? launcher =
Environment.ProcessPath;



if(string.IsNullOrEmpty(launcher))

return false;



string hash =
CalculateSHA256(launcher);



string serverHash =
"SERVER_SHA256_HASH";



return hash.Equals(

serverHash,

StringComparison.OrdinalIgnoreCase

);


}




private static string CalculateSHA256(
string file)

{


using FileStream stream =
File.OpenRead(file);



using SHA256 sha =
SHA256.Create();



byte[] result =
sha.ComputeHash(stream);



return BitConverter

.ToString(result)

.Replace("-","")

.ToLower();


}


}

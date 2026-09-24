using System.Security.Cryptography;
using System.Text;


namespace NGPB.Launcher.Security;


public static class ConfigProtector
{


public static byte[] Encrypt(
string text)

{


return ProtectedData.Protect(

Encoding.UTF8.GetBytes(text),

null,

DataProtectionScope.CurrentUser

);


}



public static string Decrypt(
byte[] data)

{


return Encoding.UTF8.GetString(

ProtectedData.Unprotect(

data,

null,

DataProtectionScope.CurrentUser

)

);


}


}

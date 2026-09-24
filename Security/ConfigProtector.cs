using System.Security.Cryptography;
using System.Text;


namespace NGPB.Launcher.Security;


public static class ConfigProtector
{


    public static byte[] Encrypt(string text)
    {


        byte[] data =
            Encoding.UTF8.GetBytes(text);



        return ProtectedData.Protect(

            data,

            null,

            DataProtectionScope.CurrentUser

        );


    }





    public static string Decrypt(byte[] data)
    {


        byte[] result =
            ProtectedData.Unprotect(

                data,

                null,

                DataProtectionScope.CurrentUser

            );



        return Encoding.UTF8.GetString(result);


    }


}

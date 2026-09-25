using System.IO;
using System.Security.Cryptography;


namespace NGPB.Launcher.Security;


public class LauncherShield
{


    public bool RunSecurityCheck()
    {


        try
        {


            string exe =
            Environment.ProcessPath!;



            if(!File.Exists(exe))
                return false;




            string hash =
                GetHash(exe);



            SecurityLogger.Security(
                "Launcher Hash: "
                + hash
            );



            return true;


        }

        catch(Exception ex)
        {


            SecurityLogger.Error(
                ex.Message
            );


            return false;


        }


    }





    private string GetHash(string file)
    {


        using SHA256 sha =
            SHA256.Create();



        using FileStream stream =
            File.OpenRead(file);



        byte[] hash =
            sha.ComputeHash(stream);



        return Convert.ToHexString(hash);


    }


}

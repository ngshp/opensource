using System;
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
            {

                return false;

            }





            string hash =
                CalculateHash(exe);





            SecurityLogger.Security(

                "Launcher SHA256 : "
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







    private string CalculateHash(
        string file)
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

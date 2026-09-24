using System.IO;
using System.Security.Cryptography;


namespace NGPB.Launcher.Security;


public class MainSecure
{

    private readonly string securePath =
        "Secure/main.secure";



    public bool Validate()
    {

        try
        {

            if(!File.Exists(securePath))
            {

                CreateSecureFile();

            }



            byte[] data =
                File.ReadAllBytes(
                    securePath
                );



            if(data.Length < 64)
            {

                return false;

            }



            SecurityLogger.Security(
                "main.secure validation OK"
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





    private void CreateSecureFile()
    {


        Directory.CreateDirectory(
            "Secure"
        );



        byte[] secureData =
            RandomNumberGenerator.GetBytes(
                64
            );



        File.WriteAllBytes(

            securePath,

            secureData

        );


        SecurityLogger.Info(
            "main.secure created"
        );


    }


}

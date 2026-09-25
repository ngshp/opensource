using System.IO;
using System.Security.Cryptography;


namespace NGPB.Launcher.Security;


public class MainSecure
{


    private readonly string path =
        "Secure/main.secure";



    public bool Validate()
    {


        try
        {


            if(!File.Exists(path))
            {

                Create();

            }



            byte[] data =
                File.ReadAllBytes(path);



            return data.Length >= 64;


        }

        catch(Exception ex)
        {


            SecurityLogger.Error(
                ex.Message
            );


            return false;

        }


    }





    private void Create()
    {


        Directory.CreateDirectory(
            "Secure"
        );



        byte[] data =
            RandomNumberGenerator.GetBytes(
                64
            );



        File.WriteAllBytes(
            path,
            data
        );



        SecurityLogger.Info(
            "main.secure created"
        );


    }


}

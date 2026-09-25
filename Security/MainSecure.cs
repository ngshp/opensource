using System.IO;


namespace NGPB.Launcher.Security;


public class MainSecure
{


    private readonly string secureFile =
        "main.secure";




    public bool Validate()
    {


        if(!File.Exists(secureFile))
        {


            SecurityLogger.Error(
                "main.secure missing"
            );


            return false;


        }




        string data =
            File.ReadAllText(
                secureFile
            );




        if(string.IsNullOrWhiteSpace(data))
        {


            SecurityLogger.Error(
                "main.secure invalid"
            );


            return false;


        }




        return true;


    }



}

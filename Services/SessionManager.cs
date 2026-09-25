using System.IO;
using NGPB.Launcher.Security;


namespace NGPB.Launcher.Services;


public class SessionManager
{


    private readonly string file =
        "session.secure";




    public void SaveSession(string token)
    {


        byte[] data =
            System.Text.Encoding.UTF8
            .GetBytes(token);



        File.WriteAllBytes(
            file,
            data
        );


        SecurityLogger.Security(
            "Session Saved"
        );


    }





    public string? LoadSession()
    {


        if(!File.Exists(file))
            return null;



        byte[] data =
            File.ReadAllBytes(file);



        string token =
            System.Text.Encoding.UTF8
            .GetString(data);



        SecurityLogger.Security(
            "Session Loaded"
        );


        return token;


    }





    public void ClearSession()
    {


        if(File.Exists(file))
        {

            File.Delete(file);

        }


        SecurityLogger.Security(
            "Session Cleared"
        );


    }


}

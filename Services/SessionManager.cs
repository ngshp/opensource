using System.IO;
using NGPB.Launcher.Security;


namespace NGPB.Launcher.Services;


public class SessionManager
{


    private readonly string sessionFile =
        "session.secure";



    // ===============================
    // SAVE SESSION
    // ===============================

    public bool SaveSession(
        string token)
    {


        try
        {


            byte[] encrypted =
                ConfigProtector.Encrypt(

                    token

                );



            File.WriteAllBytes(

                sessionFile,

                encrypted

            );



            SecurityLogger.Security(
                "Session saved"
            );



            return true;


        }

        catch(Exception ex)
        {


            SecurityLogger.Error(

                "Save session failed: "
                + ex.Message

            );


            return false;

        }


    }





    // ===============================
    // LOAD SESSION
    // ===============================

    public string? LoadSession()
    {


        try
        {


            if(!File.Exists(sessionFile))
            {

                SecurityLogger.Info(
                    "No session found"
                );


                return null;

            }




            byte[] encrypted =
                File.ReadAllBytes(

                    sessionFile

                );



            string token =
                ConfigProtector.Decrypt(

                    encrypted

                );




            SecurityLogger.Security(
                "Session loaded"
            );



            return token;


        }

        catch(Exception ex)
        {


            SecurityLogger.Error(

                "Load session failed: "
                + ex.Message

            );


            return null;

        }


    }





    // ===============================
    // DELETE SESSION
    // ===============================

    public void ClearSession()
    {


        try
        {


            if(File.Exists(sessionFile))
            {

                File.Delete(
                    sessionFile
                );

            }



            SecurityLogger.Security(
                "Session cleared"
            );


        }

        catch(Exception ex)
        {


            SecurityLogger.Error(

                "Clear session failed: "
                + ex.Message

            );


        }


    }





    // ===============================
    // CHECK SESSION
    // ===============================

    public bool HasSession()
    {


        return File.Exists(
            sessionFile
        );


    }


}

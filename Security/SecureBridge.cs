using NGPB.Launcher.Services;


namespace NGPB.Launcher.Security;


public class SecureBridge
{


    private readonly AntiCheatService antiCheat;


    private readonly LauncherShield shield;



    private const string BUILD =
        "NGPB-Launcher-115-FINAL";





    public SecureBridge()
    {

        antiCheat =
            new AntiCheatService();


        shield =
            new LauncherShield();

    }





    public bool IsSecureContext()
    {

        SecurityLogger.Security(
            "Secure Context Checked"
        );


        return true;

    }






    public string GetBuild()
    {

        return BUILD;

    }







    public string GetHWID()
    {

        return
        Environment.MachineName;


    }







    public bool VerifyIntegrity()
    {


        bool result =
            shield.RunSecurityCheck();



        if(result)
        {

            SecurityLogger.Security(
                "Integrity Check OK"
            );

        }
        else
        {

            SecurityLogger.Error(
                "Integrity Check Failed"
            );

        }



        return result;


    }








    public bool AntiCheatHeartbeat()
    {


        bool safe =
            antiCheat.CheckSafe();



        if(safe)
        {

            SecurityLogger.Security(
                "AntiCheat Heartbeat OK"
            );

        }
        else
        {

            SecurityLogger.Error(
                "AntiCheat Violation"
            );

        }



        return safe;


    }






    public void Log(
        string level,
        string message)
    {



        if(string.IsNullOrWhiteSpace(message))

            return;



        if(message.Length > 500)

            return;




        switch(level.ToLower())
        {


            case "info":

                SecurityLogger.Info(message);

                break;



            case "security":

                SecurityLogger.Security(message);

                break;



            case "error":

                SecurityLogger.Error(message);

                break;



            case "warning":

                SecurityLogger.Warning(message);

                break;


        }



    }



}

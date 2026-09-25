using System.Diagnostics;


namespace NGPB.Launcher.Security;


public class AntiCheatService
{


    private readonly string[] blocked =
    {

        "cheatengine",
        "processhacker",
        "x64dbg",
        "ollydbg"

    };






    public void Initialize()
    {


        SecurityLogger.Security(
            "Anti Cheat Started"
        );


    }







    public bool CheckSafe()
    {


        foreach(Process p in Process.GetProcesses())
        {


            string name =
                p.ProcessName
                .ToLower();




            foreach(string bad in blocked)
            {


                if(name.Contains(bad))
                {


                    SecurityLogger.Error(

                        "Blocked process : "
                        + name

                    );


                    return false;


                }


            }


        }




        return true;


    }



}

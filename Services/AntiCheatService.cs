using System.Diagnostics;
using NGPB.Launcher.Security;


namespace NGPB.Launcher.Services;


public class AntiCheatService
{


    private readonly string[] blocked =
    {

        "cheatengine",
        "x64dbg",
        "ollydbg",
        "dnspy",
        "processhacker"

    };





    public void Initialize()
    {


        SecurityLogger.Security(
            "AntiCheat Initialized"
        );


        Scan();


    }





    public bool CheckSafe()
    {

        return Scan();

    }





    private bool Scan()
    {


        foreach(Process p in Process.GetProcesses())
        {


            try
            {


                string name =
                    p.ProcessName.ToLower();



                foreach(string item in blocked)
                {


                    if(name.Contains(item))
                    {


                        SecurityLogger.Error(
                            $"Blocked process {name}"
                        );


                        return false;


                    }


                }


            }

            catch
            {

            }


        }


        return true;


    }


}

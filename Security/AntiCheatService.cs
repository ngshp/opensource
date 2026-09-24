using System.Diagnostics;
using NGPB.Launcher.Security;


namespace NGPB.Launcher.Services;


public class AntiCheatService
{


    private readonly string[] blockedProcess =
    {

        "cheatengine",

        "dnspy",

        "ollydbg",

        "x64dbg",

        "ida",

        "processhacker"

    };




    public void Initialize()
    {


        SecurityLogger.Security(
            "Anti Cheat Started"
        );


        ScanProcess();


    }






    private void ScanProcess()
    {


        foreach(Process process 
        in Process.GetProcesses())
        {


            try
            {


                string name =
                    process.ProcessName
                    .ToLower();



                foreach(string block 
                in blockedProcess)
                {


                    if(name.Contains(block))
                    {


                        SecurityLogger.Error(

                            $"Blocked process detected : {name}"

                        );



                        process.Kill();



                    }


                }


            }

            catch
            {

            }


        }


    }





    public bool CheckSafe()
    {

        foreach(Process process 
        in Process.GetProcesses())
        {


            try
            {


                string name =
                process.ProcessName
                .ToLower();



                foreach(string block 
                in blockedProcess)
                {


                    if(name.Contains(block))
                    {

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

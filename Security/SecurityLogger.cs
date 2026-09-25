using System;
using System.IO;


namespace NGPB.Launcher.Security;


public static class SecurityLogger
{

    private static readonly object locker = new();


    private static readonly string folder =
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Logs"
        );


    private static readonly string file =
        Path.Combine(
            folder,
            "security.log"
        );



    public static void Info(string msg)
    {
        Write("INFO", msg);
    }


    public static void Security(string msg)
    {
        Write("SECURITY", msg);
    }


    public static void Warning(string msg)
    {
        Write("WARNING", msg);
    }


    public static void Error(string msg)
    {
        Write("ERROR", msg);
    }





    private static void Write(
        string level,
        string msg)
    {

        try
        {

            lock(locker)
            {

                Directory.CreateDirectory(folder);


                string line =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {msg}"
                + Environment.NewLine;



                File.AppendAllText(
                    file,
                    line
                );

            }

        }

        catch
        {

        }

    }


}

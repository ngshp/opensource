using System;
using System.IO;


namespace NGPB.Launcher.Security;


public static class SecurityLogger
{


    private static readonly string folder =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "NGPB",
            "logs"
        );



    private static readonly string file =
        Path.Combine(
            folder,
            "security.log"
        );




    static SecurityLogger()
    {

        Directory.CreateDirectory(folder);

    }







    public static void Info(string message)
    {

        Write(
            "INFO",
            message
        );

    }






    public static void Security(string message)
    {

        Write(
            "SECURITY",
            message
        );

    }






    public static void Error(string message)
    {

        Write(
            "ERROR",
            message
        );

    }







    private static void Write(
        string level,
        string message)
    {


        try
        {

            File.AppendAllText(

                file,

                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}\n"

            );


        }

        catch
        {

        }


    }



}

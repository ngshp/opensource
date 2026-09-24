using System;
using System.IO;


namespace NGPB.Launcher.Security;


public static class SecurityLogger
{


    private static readonly object lockObject =
        new();



    private static readonly string logFolder =
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Logs"
        );



    private static readonly string logFile =
        Path.Combine(
            logFolder,
            "security.log"
        );



    private const long MaxFileSize =
        5 * 1024 * 1024; // 5 MB




    // =====================================
    // INFO LOG
    // =====================================

    public static void Info(
        string message)
    {

        Write(
            "INFO",
            message
        );

    }




    // =====================================
    // SECURITY LOG
    // =====================================

    public static void Security(
        string message)
    {

        Write(
            "SECURITY",
            message
        );

    }




    // =====================================
    // WARNING LOG
    // =====================================

    public static void Warning(
        string message)
    {

        Write(
            "WARNING",
            message
        );

    }




    // =====================================
    // ERROR LOG
    // =====================================

    public static void Error(
        string message)
    {

        Write(
            "ERROR",
            message
        );

    }





    // =====================================
    // MAIN WRITE FUNCTION
    // =====================================

    private static void Write(
        string level,
        string message)
    {


        try
        {


            lock(lockObject)
            {


                Directory.CreateDirectory(
                    logFolder
                );



                RotateLog();




                string log =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                + $" [{level}] "
                + message
                + Environment.NewLine;




                File.AppendAllText(

                    logFile,

                    log

                );


            }


        }

        catch
        {

            // Jangan membuat launcher crash
            // jika logging gagal

        }


    }





    // =====================================
    // LOG ROTATION
    // =====================================

    private static void RotateLog()
    {


        try
        {


            if(!File.Exists(logFile))

                return;




            FileInfo info =
                new FileInfo(
                    logFile
                );



            if(info.Length < MaxFileSize)

                return;




            string backup =
                Path.Combine(

                    logFolder,

                    $"security_{DateTime.Now:yyyyMMdd_HHmmss}.log"

                );




            File.Move(

                logFile,

                backup

            );


        }

        catch
        {

        }


    }



}

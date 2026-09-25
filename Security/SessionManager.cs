using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;


namespace NGPB.Launcher.Security;


public class SessionManager
{


    private readonly string sessionFile;


    private readonly string folder;





    public SessionManager()
    {


        folder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData
                ),
                "NGPB"
            );



        if(!Directory.Exists(folder))
        {

            Directory.CreateDirectory(folder);

        }




        sessionFile =
            Path.Combine(
                folder,
                "session.secure"
            );


    }







    // =================================
    // SAVE SESSION
    // =================================


    public void SaveSession(
        string token)
    {


        try
        {


            byte[] encrypted =
                Encrypt(
                    token
                );



            File.WriteAllBytes(
                sessionFile,
                encrypted
            );



        }

        catch(Exception)
        {


            ClearSession();


        }


    }








    // =================================
    // LOAD SESSION AUTO LOGIN
    // =================================


    public string? LoadSession()
    {


        try
        {


            if(!File.Exists(sessionFile))
            {

                return null;

            }





            byte[] encrypted =
                File.ReadAllBytes(
                    sessionFile
                );





            string token =
                Decrypt(
                    encrypted
                );




            if(string.IsNullOrWhiteSpace(token))
            {

                return null;

            }





            return token;


        }

        catch
        {


            ClearSession();

            return null;


        }


    }










    // =================================
    // CLEAR SESSION
    // =================================


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


        }

        catch
        {


        }


    }









    // =================================
    // AES ENCRYPTION
    // =================================


    private byte[] Encrypt(
        string text)
    {


        byte[] data =
            Encoding.UTF8.GetBytes(
                text
            );



        using Aes aes =
            Aes.Create();




        aes.Key =
            GetKey();




        aes.GenerateIV();




        using MemoryStream ms =
            new MemoryStream();




        // simpan IV dulu

        ms.Write(
            aes.IV,
            0,
            aes.IV.Length
        );




        using CryptoStream cs =
            new CryptoStream(
                ms,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write
            );



        cs.Write(
            data,
            0,
            data.Length
        );


        cs.FlushFinalBlock();




        return ms.ToArray();


    }










    // =================================
    // AES DECRYPT
    // =================================


    private string Decrypt(
        byte[] encrypted)
    {



        using Aes aes =
            Aes.Create();




        aes.Key =
            GetKey();




        byte[] iv =
            new byte[16];



        Array.Copy(
            encrypted,
            iv,
            16
        );



        aes.IV =
            iv;





        using MemoryStream ms =
            new MemoryStream(
                encrypted,
                16,
                encrypted.Length - 16
            );





        using CryptoStream cs =
            new CryptoStream(
                ms,
                aes.CreateDecryptor(),
                CryptoStreamMode.Read
            );





        using StreamReader reader =
            new StreamReader(
                cs
            );



        return reader.ReadToEnd();


    }









    // =================================
    // WINDOWS PROTECTED KEY
    // =================================


    private byte[] GetKey()
    {


        byte[] machineKey =
            Encoding.UTF8.GetBytes(
                "NGPB_LAUNCHER_SECURITY_KEY_2026"
            );





        return ProtectedData.Protect(

            machineKey,

            null,

            DataProtectionScope.CurrentUser

        );


    }



}

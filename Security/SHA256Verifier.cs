using System.Security.Cryptography;
using NGPB.Launcher.Models;


namespace NGPB.Launcher.Security;


public static class SHA256Verifier
{


    public static async Task<VerificationResult> Verify(

        string filePath,

        string expectedHash)

    {


        if(!File.Exists(filePath))
        {

            return new VerificationResult

            {

                Success = false,

                Message = "File tidak ditemukan"

            };

        }



        using FileStream stream =
            File.OpenRead(filePath);



        using SHA256 sha =
            SHA256.Create();



        byte[] hash =
            await sha.ComputeHashAsync(stream);



        string actual =
            BitConverter
            .ToString(hash)
            .Replace("-", "")
            .ToLower();



        bool valid =
            actual.Equals(

                expectedHash.ToLower(),

                StringComparison.OrdinalIgnoreCase

            );



        return new VerificationResult

        {

            Success = valid,

            ExpectedHash = expectedHash,

            ActualHash = actual,

            Message =
                valid ?

                "SHA256 Valid"

                :

                "SHA256 Invalid"

        };


    }


}

using System.Text.Json;

using NGPB.Launcher.Models;
using NGPB.Launcher.Security;


namespace NGPB.Launcher.Services;


public class AppConfigService
{


    private readonly string configPath =
        "Config/app.secure";





    public void Save(AppConfig config)
    {


        Directory.CreateDirectory(
            "Config"
        );



        string json =
            JsonSerializer.Serialize(

                config,

                new JsonSerializerOptions

                {

                    WriteIndented = true

                }

            );




        byte[] encrypted =
            ConfigProtector.Encrypt(json);




        File.WriteAllBytes(

            configPath,

            encrypted

        );


    }






    public AppConfig? Load()
    {


        if(!File.Exists(configPath))

            return null;




        byte[] encrypted =
            File.ReadAllBytes(

                configPath

            );




        string json =
            ConfigProtector.Decrypt(

                encrypted

            );




        return JsonSerializer.Deserialize<AppConfig>(json);


    }



}

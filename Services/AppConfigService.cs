using System.Text.Json;
using NGPB.Launcher.Models;


namespace NGPB.Launcher.Services;


public class AppConfigService
{


    private readonly string file =
        "Secure/app.secure";



    public AppConfig? Load()
    {


        try
        {


            if(!File.Exists(file))
                return null;



            string json =
                File.ReadAllText(file);



            return JsonSerializer
                .Deserialize<AppConfig>(
                    json
                );


        }

        catch
        {

            return null;

        }


    }


}

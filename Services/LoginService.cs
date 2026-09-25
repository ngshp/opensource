using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using NGPB.Launcher.Security;



namespace NGPB.Launcher.Services;


public class LoginService
{


    private readonly HttpClient client;



    private readonly AppConfigService configService;



    private readonly TimeSpan timeout =
        TimeSpan.FromSeconds(15);





    public LoginService()
    {


        client =
            new HttpClient();



        client.Timeout =
            timeout;



        configService =
            new AppConfigService();


    }







    public async Task<string?> Login(
        string username,
        string password)
    {


        try
        {


            AppConfig? config =
                configService.Load();





            if(config == null)
            {


                SecurityLogger.Error(
                    "app.secure missing"
                );


                return null;


            }






            var loginData =
                new
                {

                    username,

                    password,

                    device =
                    Environment.MachineName

                };







            string json =
                JsonSerializer.Serialize(
                    loginData
                );







            using StringContent content =
                new StringContent(

                    json,

                    Encoding.UTF8,

                    "application/json"

                );







            string url =
                config.ApiUrl
                +
                config.LoginEndpoint;








            SecurityLogger.Info(
                "Connecting login server"
            );







            HttpResponseMessage response =
                await client.PostAsync(
                    url,
                    content
                );







            if(!response.IsSuccessStatusCode)
            {


                SecurityLogger.Error(

                    "Login HTTP Failed : "
                    +
                    response.StatusCode

                );


                return null;


            }








            string result =
                await response.Content.ReadAsStringAsync();








            using JsonDocument doc =
                JsonDocument.Parse(
                    result
                );







            if(doc.RootElement
                .TryGetProperty(
                    "token",
                    out JsonElement token))
            {



                SecurityLogger.Security(
                    "Login Token Received"
                );



                return token.GetString();


            }







            SecurityLogger.Error(
                "Token missing"
            );



            return null;


        }

        catch(Exception ex)
        {


            SecurityLogger.Error(
                "Login Error : "
                +
                ex.Message
            );



            return null;


        }


    }



}

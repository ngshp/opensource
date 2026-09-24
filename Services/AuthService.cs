using NGPB.Launcher.Models;


namespace NGPB.Launcher.Services;


public class AuthService
{


private readonly ApiClient api;



public AuthService()

{

api = new ApiClient();

}



public async Task<bool> Login(

string username,

string password)

{


var result =
await api.Post<LoginResponse>(

"auth/login",

new LoginRequest

{

Username=username,

Password=password

}

);



if(result == null)

return false;



if(result.Success)

{


UserSession.Username =
result.Username;


UserSession.Token =
result.Token;



SessionManager.Save();



return true;


}



return false;


}



}

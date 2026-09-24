namespace NGPB.Launcher.Models;


public static class UserSession
{


public static string Username { get; set; } = "";


public static string Token { get; set; } = "";


public static bool IsLogin

{

get

{

return !string.IsNullOrEmpty(Token);

}

}


}

using System.Net.Http;


namespace NGPB.Launcher.Services;


public class ClientValidator
{


private readonly HttpClient client =
new();



public async Task<bool> Validate()

{


try

{


string response =
await client.GetStringAsync(

"https://api.ngpb.com/client/check"

);



return response=="OK";


}

catch

{


return false;


}


}


}

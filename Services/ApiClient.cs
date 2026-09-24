using System.Net.Http.Headers;
using System.Net.Http.Json;


namespace NGPB.Launcher.Services;


public class ApiClient
{


private readonly HttpClient client;



public ApiClient()

{


client = new HttpClient();


client.BaseAddress =
new Uri(

"https://api.ngpb.com/"

);


client.Timeout =
TimeSpan.FromSeconds(30);


}



public void SetToken(string token)

{


client.DefaultRequestHeaders.Authorization =

new AuthenticationHeaderValue(

"Bearer",

token

);


}



public async Task<T?> Post<T>(

string endpoint,

object data)

{


var response =
await client.PostAsJsonAsync(

endpoint,

data

);



return await response.Content
.ReadFromJsonAsync<T>();



}



public async Task<T?> Get<T>(

string endpoint)

{


return await client
.GetFromJsonAsync<T>(endpoint);


}



}

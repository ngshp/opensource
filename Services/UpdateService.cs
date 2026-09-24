using System.Net.Http.Json;
using NGPB.Launcher.Models;


namespace NGPB.Launcher.Services;


public class UpdateService
{


private readonly HttpClient client =
new();



public async Task<PatchManifest?> GetManifest()

{


return await client
.GetFromJsonAsync<PatchManifest>(

"https://cdn.ngpb.com/manifest.json"

);


}


}

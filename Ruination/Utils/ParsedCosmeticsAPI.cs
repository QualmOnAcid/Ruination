using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ruination_v2.Models;

namespace Ruination_v2.Utils;

public class ParsedCosmeticsAPI
{
    private static ParsedCosmeticsAPIObject _api;

    public static async Task Load()
    {
        Logger.Log("Downloading API");
        try
        {
            using(HttpClient _client = new HttpClient())
            {
                var resp = await _client.GetAsync("https://raw.githubusercontent.com/QualmOnAcid/Ruination/main/ParsedCosmetics.json").ConfigureAwait(false);
                string apitext = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                _api = JsonConvert.DeserializeObject<ParsedCosmeticsAPIObject>(Encoding.UTF8.GetString(Convert.FromBase64String(apitext)));
            }
            Logger.Log("Downloaded API");
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message, e);
            Environment.Exit(0);
        }
    }

    public static ParsedCosmeticsAPIObject GetApi() => _api;

    public class ParsedCosmeticsAPIObject
    {
        public List<CacheItem> CachedItems = new();
    }
}
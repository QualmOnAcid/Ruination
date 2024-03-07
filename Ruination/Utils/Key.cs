using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ruination_v2.Utils;

public class BstlarKey
{
    public static async Task<bool> CheckKey(string key)
    {
        Logger.Log("Validating Key: " + key);
        try
        {
            if (string.IsNullOrEmpty(key)) return false;
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("bstk", "Pr3y7jXMR6yuBvgp3RKAC0C9a78awJMwjHUe");
                string url = "https://bstlar.com/keys/validate/" + key;
                var downloaded = await wc.DownloadStringTaskAsync(url);
                var keyObj = JsonConvert.DeserializeObject<BoostellarKey>(downloaded);
                return keyObj.valid;
            }
        } catch(Exception e)
        {
            Logger.LogError(e.Message, e);
            return false;
        }
    }

    public static async Task<bool> CheckConfigKey()
    {
        var configKey = Config.GetConfig().Key;
        return await CheckKey(configKey);
    }
    
    public class BoostellarKey
    {
        public bool valid;
    }
    
}
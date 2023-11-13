using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebviewAppShared.Utils
{
    public class Mappings
    {
        private static string _mappingsPath;
        private static string localMappingPath = Utils.AppDataFolder + "\\mappings.usmap";

        public static async Task DownloadMappings()
        {
            _mappingsPath = "";
            Logger.Log("Downloading Mappings");
            try
            {

                JArray mappings = JArray.Parse(await new System.Net.WebClient().DownloadStringTaskAsync("https://fortnitecentral.genxgames.gg/api/v1/mappings"));
                Logger.Log("Downloaded FC Mappings");
                foreach (dynamic mapping in mappings)
                {
                    if (mapping["meta"]["compressionMethod"].ToString().ToLower().Equals("oodle"))
                    {
                        await new System.Net.WebClient().DownloadFileTaskAsync(mapping["url"].ToString(), localMappingPath);
                        _mappingsPath = localMappingPath;
                        return;
                    }
                }

                Logger.Log("No oodle mappings in FC. Using ApiMappings");

                await UseApiMappings();
            } catch(Exception ex)
            {
                Logger.LogError(ex.Message, ex);
                try
                {
                    await UseApiMappings();
                } catch(Exception e)
                {
                    Logger.LogError(e.Message, e);
                    Utils.MessageBox("Mappings could not be loaded: " + ex.Message + " | " + e.Message);
                    Environment.Exit(0);
                }
            }
        }

        private static async Task UseApiMappings()
        {
            Logger.Log("Downloading Api Mappings");
            await new System.Net.WebClient().DownloadFileTaskAsync(API.GetApi().Other.MappingsUrl, localMappingPath);
            _mappingsPath = localMappingPath;
            Logger.Log("Downloaded Api Mappings");
        }

        public static string GetMappingsPath() => _mappingsPath;

    }
}

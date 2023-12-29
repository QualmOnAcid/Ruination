
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebviewAppShared.Utils;

namespace WebviewAppShared.Swapper
{
    public class LobbyBackground
    {

        private static async Task<List<string>> GetLobbyBackgroundFilePaths()
        {
            var paths = new List<string>();

            string fortniteFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\FortniteGame\\Saved\\PersistentDownloadDir\\CMS\\DownloadCache.json";

            if (!System.IO.File.Exists(fortniteFile)) return paths;

            var cacheObject = JsonConvert.DeserializeObject<DownloadCacheObject>(System.IO.File.ReadAllText(fortniteFile));

            Logger.Log($"Found {cacheObject.Cache.Count} CacheObjects");

            var lobbyObjects = cacheObject.Cache.Where(x => x.Key.ToLower().Contains("lobby"));

            Logger.Log(lobbyObjects.Count() + " are LobbyObjects");

            foreach (var lobbyobj in lobbyObjects)
                paths.Add(lobbyobj.Value.filePath);

            return paths;
        }

        public class CacheItemObject
        {
            public string filePath { get; set; }
            public string sourceUrl { get; set; }
        }

        public class DownloadCacheObject
        {
            public int Version { get; set; }
            public Dictionary<string, CacheItemObject> Cache { get; set; }
        }

        public static async Task ConvertLobbyBG(string path)
        {
            var filepaths = await GetLobbyBackgroundFilePaths();

            if(!System.IO.File.Exists(path))
            {
                await Utils.Utils.MessageBox("Filepath doesn't exist");
                return;
            }

            foreach(var fp in filepaths)
            {
                if (!System.IO.File.Exists(fp)) continue;

                File.Copy(path, fp, true);
            }

            Config.GetConfig().ConvertedItems.Add(new()
            {
                Type = "LOBBYBACKGROUND",
                Assets = new(),
                ID = "LOBBYBACKGROUND",
                isPlugin = false,
                Name = "Lobby Background",
                OptionID = "LOBBYBACKGROUND"
            });

            Config.Save();

            await Utils.Utils.MessageBox("Converted Lobby Background");
        }

        public static async Task RevertLobbyBG(bool showMsgBox = true)
        {
            try
            {
                foreach (var fp in await GetLobbyBackgroundFilePaths())
                {
                    File.Delete(fp);
                }

                if(showMsgBox)
                {
                    Config.GetConfig().ConvertedItems.RemoveAll(x => x.Type == "LOBBYBACKGROUND");
                    Config.Save();
                    Utils.Utils.MessageBox("Reverted Lobby Background");
                }

            } catch(Exception e)
            {
                Utils.Utils.MessageBox("Error: " + e.Message);
                Logger.LogError(e.Message, e);
            }
        }

    }
}

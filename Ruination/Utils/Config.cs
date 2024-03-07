using Newtonsoft.Json;
using Ruination_v2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ruination_v2.Utils
{
    public class Config
    {

        private static ConfigObject _config;
        private static string ConfigFilePath = Utils.AppDataFolder + "\\Config.json";

        public static void Load()
        {
            Logger.Log("Loading Config");
            try
            {
                LoadDefault();
                Directory.CreateDirectory(Utils.AppDataFolder);
                if (!File.Exists(ConfigFilePath)) return;
                string read = File.ReadAllText(ConfigFilePath);
                read = Encoding.UTF8.GetString(Convert.FromBase64String(read));
                read = Encoding.UTF8.GetString(Convert.FromBase64String(read));
                _config = JsonConvert.DeserializeObject<ConfigObject>(read);
            } catch(Exception e)
            {
                LoadDefault();
                Logger.LogError(e.Message, e);
            }
            Logger.Log("Loaded Config");
        }

        public static void LoadDefault()
        {
            _config = new();
            _config.CachedItems = new();
            _config.Key = "";
            _config.ConvertedItems = new();
            _config.ShowKickableOptions = false;
        }

        public static void Save()
        {
            Directory.CreateDirectory(Utils.AppDataFolder);
            if(_config != null)
            {
                Logger.Log("Saving Config");
                string serialized = JsonConvert.SerializeObject(_config);
                serialized = Convert.ToBase64String(Encoding.UTF8.GetBytes(serialized));
                serialized = Convert.ToBase64String(Encoding.UTF8.GetBytes(serialized));
                File.WriteAllText(ConfigFilePath, serialized);
                Logger.Log("Saved Config");
            }
        }

        public static ConfigObject GetConfig() => _config;

    }

    public class ConfigObject
    {
        public List<CacheItem> CachedItems = new();
        public string Key = "";
        public DateTime KeyValidTime = DateTime.Now;
        public List<ConvertedItem> ConvertedItems = new();
        public bool ShowKickableOptions = false;
    }

}

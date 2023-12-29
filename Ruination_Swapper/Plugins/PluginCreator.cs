using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebviewAppShared.Models;

namespace WebviewAppShared.Plugins
{
    public class PluginCreator
    {
        public static PluginModel _Plugin = new();

        public static List<string> Materials = new List<string>() {  };
        public static List<PluginAsset> Assets = new List<PluginAsset>() { };

        public static async Task AddMaterial()
        {
            Materials.Add("");
            Utils.Utils.MainWindow.UpdateUI();
        }

        public static async Task DeleteMaterial(int index)
        {
            Materials.RemoveAt(index);
            Utils.Utils.MainWindow.UpdateUI();
        }

        public static async Task AddAsset()
        {
            Assets.Add(new());
            Utils.Utils.MainWindow.UpdateUI();
        }

        public static async Task DeleteAsset(int index)
        {
            Assets.RemoveAt(index);
            Utils.Utils.MainWindow.UpdateUI();
        }

        public class PluginAsset
        {
            public string Asset = "";
            public string ToAsset = "";
        }

        public static async Task WriteCurrentPlugin()
        {
            string Type = _Plugin.Type.ToLower();

            PluginModel Plugin = new PluginModel()
            {
                Name = _Plugin.Name,
                Description = _Plugin.Description,
                Icon = _Plugin.Icon,
                Rarity = _Plugin.Rarity,
                ID = GenerateRandomID(),
                Type = Type
            };

            if(Assets.Count > 0)
            {
                Plugin.Swaps = new();
                foreach(var asset in Assets)
                {
                    Plugin.Swaps.Add(new()
                    {
                        Asset = asset.Asset,
                        ToAsset = asset.ToAsset
                    });
                }
            }

            if(Type != "uefnskin")
            {
                Plugin.Option.Name = _Plugin.Option.Name;
                Plugin.Option.Rarity = _Plugin.Option.Rarity;
                Plugin.Option.Description = _Plugin.Option.Description;
                Plugin.Option.Icon = _Plugin.Option.Icon;
            }

            if(Type != "normal")
            {
                Plugin.Files = _Plugin.Files;
            }

            if(Type == "uefnskin")
            {
                Plugin.Option = null;
                if(Materials != null && Materials.Count > 0)
                {
                    Plugin.Materials = Materials;
                }
                Plugin.Skin = _Plugin.Skin;
            }

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            System.IO.File.WriteAllText(Plugin.Name + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(Plugin, settings));
            File.Copy(Plugin.Name + ".json", Plugin.Name + "_" + ".json");
            await PluginUtils.AddPlugin(Plugin.Name + "_" + ".json");

            _Plugin = new();

            Utils.Utils.MainWindow.CurrentState = State.PLUGIN_VIEW;
            Utils.Utils.MainWindow.UpdateUI();
        }

        private static string GenerateRandomID(int length = 32)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";

            string s = "";

            for (int i = 0; i < length; i++)
                s += chars[new Random().Next(chars.Length)];

            return s;
        }

    }
}

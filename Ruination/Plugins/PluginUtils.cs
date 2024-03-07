using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ruination_v2.Models;
using Ruination_v2.Utils;

namespace Ruination_v2.Plugins;

public class PluginUtils
{
    public static List<PluginModel> GetInstalledPlugins()
    {
        string pluginFolder = Utils.Utils.AppDataFolder + "\\Plugins\\";
        if(!Directory.Exists(pluginFolder)) Directory.CreateDirectory(pluginFolder);

        var plugins = new List<PluginModel>();

        foreach(var file in Directory.GetFiles(pluginFolder))
        {
            Logger.Log("Loading Plugin " + file);
            try
            {
                string readfile = File.ReadAllText(file);
                var pl = JsonConvert.DeserializeObject<PluginModel>(readfile);
                pl.FilePath = file;
                plugins.Add(pl);
            } catch(Exception ex)
            {
                Logger.LogError(ex.Message, ex);
            }
        }

        return plugins;
    }

    public static async Task AddPlugin(string file)
    {
        Logger.Log("Adding Plugin");
        string pluginFolder = Utils.Utils.AppDataFolder + "\\Plugins\\";
        if (!Directory.Exists(pluginFolder)) Directory.CreateDirectory(pluginFolder);

        //Using this to display the plugins in the order in which they were added
        long pluginIndex = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

        string newpath = pluginFolder + $"{pluginIndex}_" + Path.GetFileName(file);

        Logger.Log("Created Path: " + newpath);

        while(File.Exists(newpath))
        {
            newpath = newpath + "_";
            Logger.Log("Created Path: " + newpath);
        }

        File.Move(file, newpath);
    }
}
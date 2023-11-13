using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebviewAppShared.Utils
{
    public class Epicgames
    {
        private static string paksPath = "";
        private static string fortniteVersion = "";

        public static void Load()
        {
            Logger.Log("Loading Epicgames paths");
            string launcherInstalledPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\Epic\\UnrealEngineLauncher\\LauncherInstalled.dat";
            if (File.Exists(launcherInstalledPath))
            {
                var installed = JsonConvert.DeserializeObject<LauncherInstalled>(File.ReadAllText(launcherInstalledPath));
                var fortniteGame = installed.InstallationList.FirstOrDefault(x => x.AppName == "Fortnite");
                if (fortniteGame == null) return;
                paksPath = fortniteGame.InstallLocation + "\\FortniteGame\\Content\\Paks";
                fortniteVersion = fortniteGame.AppVersion;
                Logger.Log("Found Fortnite at " + paksPath + " with version " + fortniteVersion);
            } else
            {
                Logger.LogError("Epicgames path could not be found");
            }
        }

        public static string GetPaksPath() => paksPath;
        public static string GetInstalledFortniteVersion() => fortniteVersion;

        public class LauncherInstalled
        {
            public List<InstallListObject> InstallationList { get; set; }
        }

        public class InstallListObject
        {
            public string InstallLocation { get; set; }
            public string AppName { get; set; }
            public string AppVersion { get; set; }
        }

    }
}

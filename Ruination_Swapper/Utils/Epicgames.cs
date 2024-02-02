using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace WebviewAppShared.Utils
{
    public class Epicgames
    {
        private static string paksPath = "";
        private static string fortniteVersion = "";
        private static string apiFnVersion = "";    

        public static async Task<bool> Load()
        {
            Logger.Log("Loading Epicgames paths");

            if(!Directory.Exists(Utils.AppDataFolder))
                Directory.CreateDirectory(Utils.AppDataFolder);

            if(File.Exists(Utils.AppDataFolder + "\\Paksfolder.txt"))
            {
                string _paksfolder = File.ReadAllText(Utils.AppDataFolder + "\\Paksfolder.txt");
                if(Directory.Exists(_paksfolder))
                {
                    if (File.Exists(_paksfolder + "\\global.ucas") && File.Exists(_paksfolder + "\\global.utoc"))
                    {
                        Logger.Log("Using temp file paks folder");
                        paksPath = _paksfolder;
                        return true;
                    }
                }
            }

            string launcherInstalledPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\Epic\\UnrealEngineLauncher\\LauncherInstalled.dat";
            if (File.Exists(launcherInstalledPath))
            {
                var installed = JsonConvert.DeserializeObject<LauncherInstalled>(File.ReadAllText(launcherInstalledPath));
                var fortniteGame = installed.InstallationList.FirstOrDefault(x => x.AppName == "Fortnite");
                if (fortniteGame == null)
                {
                    Logger.Log("Fortnite was not found as an App. User has to put in Paks folder");
                    return false;
                }
                paksPath = fortniteGame.InstallLocation + "\\FortniteGame\\Content\\Paks";
                fortniteVersion = fortniteGame.AppVersion;
                Logger.Log("Found Fortnite at " + paksPath + " with version " + fortniteVersion);
                return true;
            } else
            {
                Logger.LogError("Epicgames path could not be found");
                return false;
            }
        }

        private static async Task ShowInvalidPaksBox()
        {
            await Utils.MessageBox("The path you entered is not a valid Fortnite Directory");
        }

        public static async Task CheckInstallation(string install)
        {
            Logger.Log("Checking Installation: " + install);

            bool isValidFortniteInstallation = false;

            if (string.IsNullOrEmpty(install) || string.IsNullOrWhiteSpace(install) || !Directory.Exists(install))
            {
                await ShowInvalidPaksBox();
                return;
            }

            install = install.Replace("\\", "/");

            if (!Directory.Exists(install))
            {
                await ShowInvalidPaksBox();
                return;
            }

            if (install.ToLower().EndsWith("paks") || install.ToLower().EndsWith("paks/"))
            {
                if (File.Exists(install + "\\global.ucas") && File.Exists(install + "\\global.utoc"))
                    isValidFortniteInstallation = true;
            } else
            {
                string paksDir = install + "\\FortniteGame\\Content\\Paks";

                if(Directory.Exists(paksDir))
                {
                    if (File.Exists(paksDir + "\\global.ucas") && File.Exists(paksDir + "\\global.utoc"))
                    {
                        install = paksDir;
                        isValidFortniteInstallation = true;
                    }
                }
            }

            if(isValidFortniteInstallation)
            {

                if (!Directory.Exists(Utils.AppDataFolder))
                    Directory.CreateDirectory(Utils.AppDataFolder);

                File.WriteAllText(Utils.AppDataFolder + "\\Paksfolder.txt", install);

                await Utils.MainWindow.Setup();

            } else
            {
                await ShowInvalidPaksBox();
            }

        }

        public static string GetPaksPath() => paksPath;
        public static string GetInstalledFortniteVersion() => fortniteVersion;

        public static void SetInstalledFortniteVersion(string newversion) => fortniteVersion = newversion;
        public static string GetAPIFnVersion() => apiFnVersion;
        public static void SetAPIFortniteVersion(string newversion) => apiFnVersion = newversion;

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

using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.UObject;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebviewAppShared.Models;
using WebviewAppShared.Shared;
using WebviewAppShared.Swapper;
using static WebviewAppShared.Shared.MainLayout;

namespace WebviewAppShared.Utils
{
    public class Utils
    {

        public static string USER_VERSION = "2.0.12";
        public static Dictionary<string, List<Item>> cachedTabItems = new();

        public static string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\RuinationFN_Swapper\\";
        public static IJSObjectReference module;
        public static MainLayout MainWindow;
        public static string CutOffString(string s, int amount, bool addDots)
        {
            if (amount > s.Length) return s;
            if (addDots)
            {
                amount -= 3;
            }
            string b = s.Substring(0, amount);
            if (addDots)
                b += "...";
            return b;
        }

        public static async Task MessageBox(string message)
        {
            await module.InvokeAsync<string>("alertUser", message);
        }

        public static async Task ParseAllCosmetics()
        {
            Logger.Log("Downloading All Cosmetics");

            ParsingCosmeticsObj j;

            using(var httpclient = new HttpClient())
            {
                string allItemsDownloaded = File.ReadAllText("items");

                Utils.MainWindow.LoadingText = "Applying Json";
                Utils.MainWindow.UpdateUI();

                j = JsonConvert.DeserializeObject<ParsingCosmeticsObj>(allItemsDownloaded);

                int itemcount = j.data.Count();

                Utils.MainWindow.LoadingText = "Parsing Cosmetics (" + itemcount + ")";
                Utils.MainWindow.UpdateUI();
            }

            foreach (var item in j.data)
            {
                string type = "";
                try
                {

                    type = item.type.value.ToString().ToLower();

                    if (type != "outfit" && type != "emote" && type != "pickaxe" && type != "backpack") continue;

                    if (Config.GetConfig().CachedItems.Any(x => x.Id == item.id.ToString()))
                    {
                        continue;
                    }

                    var series = "";
                    var path = item.path.ToString() ?? "";
                    var defPath = item.definitionPath.ToString() ?? "";
                    var weaponAnim = "";
                    var charParts = new List<CacheMeshItem>();

                    switch (type)
                    {
                        case "pickaxe":
                            UObject wid = await SwapUtils.GetProvider().LoadObjectAsync(defPath);
                            wid.TryGetValue(out FSoftObjectPath fireabi, "PrimaryFireAbility");
                            weaponAnim = fireabi.AssetPathName.Text;
                            break;

                        case "backpack":
                            UObject bid = await SwapUtils.GetProvider().LoadObjectAsync(path);
                            bid.TryGetValue(out UObject[] bpcpparts, "CharacterParts");
                            foreach (var part in bpcpparts)
                            {
                                part.TryGetValue(out FSoftObjectPath SkeletalMesh, "SkeletalMesh");
                                charParts.Add(new()
                                {
                                    Path = part.GetPathName().Split(".").FirstOrDefault(),
                                    Mesh = SkeletalMesh.AssetPathName.Text
                                });
                            }
                            break;

                        case "outfit":
                            UObject[] cpparts = await SwapUtils.GetCharacterParts(SwapUtils.GetProvider(), path);
                            foreach (var part in cpparts)
                            {
                                part.TryGetValue(out FSoftObjectPath SkeletalMesh, "SkeletalMesh");
                                charParts.Add(new()
                                {
                                    Path = part.GetPathName().Split(".").FirstOrDefault(),
                                    Mesh = SkeletalMesh.AssetPathName.Text
                                });
                            }
                            break;
                    }


                    if (item.series != null) series = item.series.value.ToString();

                    var cachedItem = new CacheItem()
                    {
                        Name = item.name.ToString(),
                        Id = item.id.ToString(),
                        WeaponAnim = weaponAnim,
                        Series = series,
                        Path = path,
                        DefinitionPath = defPath,
                        Parts = charParts
                    };

                    Config.GetConfig().CachedItems.Add(cachedItem);

                }
                catch (Exception e) { 
                }

            }
            Config.Save();
            Logger.Log("Parsed Cosmetics");
        }

        public static async Task CheckForChangedConvertedItems()
        {
            Logger.Log("Checking for Converted Items");
            var prov = SwapUtils.GetProvider();

            var updatedList = new List<ConvertedItem>();

            if (Config.GetConfig() == null || Config.GetConfig().ConvertedItems == null) Config.LoadDefault();

            foreach(var item in Config.GetConfig().ConvertedItems)
            {
                try
                {
                    Logger.Log("Checking " + item.OptionID + " To " + item.ID);

                    bool itemChanged = false;

                    foreach (KeyValuePair<string, byte[]> asset in item.Assets)
                    {
                        if (asset.Key.StartsWith("TEXTURESWAP_")) continue;

                        byte[] savedBytes = await prov.SaveAssetAsync(asset.Key);

                        if (!savedBytes.SequenceEqual(asset.Value))
                        {
                            Logger.Log("ASSET HAS BEEN UNSWAPPED: " + asset.Key);
                            itemChanged = true;
                            break;
                        }

                    }

                    if(!itemChanged)
                    {
                        updatedList.Add(item);
                    }
                    
                } catch(Exception e)
                {
                    Logger.LogError(e.Message, e);
                }
            }

            Logger.Log("Setting new List");

            Config.GetConfig().ConvertedItems = updatedList;
            Config.Save();
        }

        public static async Task CheckForUpdate()
        {
            Logger.Log("Checking for Update");
            Logger.Log("User Version: " + USER_VERSION);
            Logger.Log("API Version: " + API.GetApi().Other.Version);
            if(Utils.USER_VERSION != API.GetApi().Other.Version)
            {
                await MessageBox("You are running an old version of Ruination. Please download the new one at:\n" + API.GetApi().Other.DownloadLink + " or \n" + API.GetApi().Other.Discord);
                StartUrl(API.GetApi().Other.DownloadLink);
                Environment.Exit(0);
                return;
            }
            Logger.Log("Latest update is installed");
        }

        public static void StartUrl(string url)
        {
            Logger.Log("Opening " + url);
            ProcessStartInfo Procc = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C start {url}",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            Process.Start(Procc);
        }

        public static async Task StartFortnite()
        {
            //Utils.MessageBox("Please do not close the swapper in order to cancel verification");
            Logger.Log("Starting Fortnite");
            StartUrl("com.epicgames.launcher://apps/Fortnite?action=launch&silent=true");
            return;

            //This should be done by the RuinationChecker Dependency
            Logger.Log("Waiting for Fortnite Process");

            Process fortniteProcess = null;

            while(fortniteProcess == null)
            {
                var processes = Process.GetProcessesByName("FortniteClient-Win64-Shipping");
                if (processes.Length > 0)
                    fortniteProcess = processes.FirstOrDefault();
                Thread.Sleep(3 * 1000);
            }

            Logger.Log("Found Fortnite Process with id: " + fortniteProcess.Id);
            Logger.Log("Waiting for Fortnite to exit");

            fortniteProcess.WaitForExit();

            Logger.Log("Fortnite has exited. Closing Epicgames");

            foreach (var epicProcess in Process.GetProcessesByName("EpicGamesLauncher"))
                epicProcess.Kill();
        }

        public static async Task ShowConvertedItems()
        {
            if(Config.GetConfig().ConvertedItems.Count == 0)
            {
                MessageBox("You have no items converted.");
                return;
            }

            string text = "";

            foreach (var item in Config.GetConfig().ConvertedItems)
            {
                text += item.Name + "\n";
            }

            text = text.Substring(0, text.Length - 2);

            MessageBox("You have these items converted:\n" + text);

        }

        public static async Task RevertAllItems(bool showmsgbox = true)
        {
            try
            {
               var prov = SwapUtils.GetProvider();

                Logger.Log("Reverting all - " + Config.GetConfig().ConvertedItems.Count + " Items");
                await LobbyBackground.RevertLobbyBG(false);

                string backupfolder = WebviewAppShared.Utils.Utils.AppDataFolder + "\\Backups\\";
                Directory.CreateDirectory(backupfolder);

                foreach (var file in Directory.GetFiles(backupfolder))
                {
                    string targetFile = Epicgames.GetPaksPath() + "\\" + Path.GetFileName(file);

                    if (Path.GetFileNameWithoutExtension(targetFile) == Path.GetFileNameWithoutExtension(API.GetApi().UEFNFiles.FileToUse))
                        continue;

                    Logger.Log("Restoring " + targetFile);
                    File.Copy(file, targetFile, true);
                }

                Config.GetConfig().ConvertedItems.Clear();
                Config.Save();

                if(showmsgbox) 
                    await MessageBox("Reverted all items");

            } catch(Exception e)
            {
                Logger.LogError(e.Message, e);
            }

        }

        public static async Task OpenDC()
        {
            StartUrl(API.GetApi().Other.Discord);
        }

        public static async Task ResetEverything()
        {
            await RevertAllItems(false);
            if(Directory.Exists(AppDataFolder))
                Directory.Delete(AppDataFolder, true);

            await MessageBox("Resetted everything. Exiting now");
            Environment.Exit(0);
        }

        public static async Task LogSwap(string id, bool convert, string optionid)
        {
            try
            {
                Logger.Log("Logging Swap: " + id);
                new Thread(async () =>
                {
                    string url = "https://ruination-server-api.vercel.app/v1/logswap";

                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("itemid", id);
                        wc.Headers.Add("userid", DiscordRPC.GetID().ToString());
                        wc.Headers.Add("convert", convert.ToString().ToLower());
                        wc.Headers.Add("optionid", optionid.ToLower());
                        await wc.DownloadStringTaskAsync(url);
                        Logger.Log("Logged swap");
                    }

                }).Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, ex);
            }
        }

        public static async Task LogUEFNSwap(string id, bool convert, string optionid)
        {
            try
            {
                Logger.Log("Logging Swap: " + id);
                new Thread(async () =>
                {
                    string url = "https://ruination-server-api.vercel.app/v1/logswap/uefn";

                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("itemid", id);
                        wc.Headers.Add("userid", DiscordRPC.GetID().ToString());
                        wc.Headers.Add("convert", convert.ToString().ToLower());
                        wc.Headers.Add("optionid", optionid.ToLower());
                        await wc.DownloadStringTaskAsync(url);
                        Logger.Log("Logged swap");
                    }

                }).Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, ex);
            }
        }

        public static async Task CheckRuinationUtils()
        {
            try
            {
                Logger.Log("Checking for Ruination Utils");

                var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

                bool startupFolderExists = Directory.Exists(startupFolder);

                if (!startupFolderExists)
                {
                    Logger.Log("Startup folder doesn't exist??: " + startupFolder);
                }

                var versionFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\RuinationCheckerVersion";

                if (!Directory.Exists(versionFolder))
                    Directory.CreateDirectory(versionFolder);

                if (Process.GetProcessesByName(API.GetApi().RuinationUtils.Processname).Length > 0)
                {
                    Logger.Log("Ruination Utils are running");
                    Logger.Log("Checking Installed Ruination Utils Version");

                    MainWindow.LoadingText = "Checking Installed Ruination Utils Version";
                    MainWindow.UpdateUI();

                    if (File.Exists(versionFolder + "\\RuinationVersion.txt"))
                    {
                        string installedVersion = File.ReadAllText(versionFolder + "\\RuinationVersion.txt");

                        if (installedVersion == API.GetApi().RuinationUtils.Version)
                        {
                            Logger.Log("Latest Version of RuinationUtils are installed");
                            return;
                        }

                        Logger.Log("Outdated Version of RuinationUtils are installed. Deleting..");
                    }
                }
                else
                {
                    Logger.Log("Ruination Utils are NOT running. Downloading Ruination Utils");
                }

                foreach (var proc in Process.GetProcessesByName(API.GetApi().RuinationUtils.Processname))
                {
                    try
                    {
                        proc.Kill();
                    }
                    catch { }
                }

                Logger.Log("Ruination Utils need to be installed.");

                Logger.Log("Downloading Ruination Utils");

                MainWindow.LoadingText = "Downloading Ruination Utils";
                MainWindow.UpdateUI();

                using (WebClient wc = new WebClient())
                {
                    wc.DownloadProgressChanged += (sender, e) =>
                    {
                        MainWindow.LoadingText = $"Downloading Ruination Utils ({e.ProgressPercentage}%)";
                        MainWindow.UpdateUI();
                    };

                    wc.DownloadFileCompleted += async delegate
                    {
                        Logger.Log("Unzipping file");
                        MainWindow.LoadingText = "Unzipping file";
                        MainWindow.UpdateUI();

                        Logger.Log("RuinationUtils: " + File.Exists("RuinationUtils.zip"));
                        Logger.Log("CurrentDir: " + Directory.GetCurrentDirectory());

                        var gf = new FileInfo(Directory.GetCurrentDirectory() + "\\RuinationUtils.zip");

                        ZipFile.ExtractToDirectory(gf.FullName, Directory.GetCurrentDirectory(), true);

                        string appPath = startupFolder + "\\" + API.GetApi().RuinationUtils.Filename;

                        if (startupFolderExists)
                        {
                            Logger.Log("Startupfolder: " + startupFolder);

                            if (!Directory.Exists(startupFolder)) return;

                            if (File.Exists(startupFolder + "\\" + API.GetApi().RuinationUtils.Filename))
                                File.Delete(startupFolder + "\\" + API.GetApi().RuinationUtils.Filename);

                            File.Move(API.GetApi().RuinationUtils.Filename, startupFolder + "\\" + API.GetApi().RuinationUtils.Filename, true);
                        } else
                        {
                            appPath = Directory.GetCurrentDirectory() + "\\" + API.GetApi().RuinationUtils.Filename;
                        }

                        File.Delete("RuinationUtils.zip");

                        File.WriteAllText(versionFolder + "\\RuinationVersion.txt", API.GetApi().RuinationUtils.Version);

                        ProcessStartInfo processStartInfo = new()
                        {
                            FileName = appPath,
                            UseShellExecute = true,
                            CreateNoWindow = true,
                        };

                        if (startupFolderExists)
                            processStartInfo.WorkingDirectory = startupFolder;
                        else
                            processStartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

                        Process.Start(processStartInfo);

                        Logger.Log("Waiting for Process");

                        MainWindow.LoadingText = "Waiting for Process";
                        MainWindow.UpdateUI();

                        while (Process.GetProcessesByName(API.GetApi().RuinationUtils.Processname).Length == 0)
                            await Task.Delay(500);

                        Logger.Log("Found Process");
                    };

                    await wc.DownloadFileTaskAsync(API.GetApi().RuinationUtils.Url, "RuinationUtils.zip");
                }
            } catch(Exception e)
            {
                Logger.LogError(e.Message, e);
            }
        }

        public static bool CanReadFile(string path)
        {
            try
            {
                using(FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return true;
                }
            } catch(Exception ex)
            {
                return false;
            }
        }

        public static async Task b()
        {
            IoPackage pack = (IoPackage)await SwapUtils.GetProvider().LoadPackageAsync("FortniteGame/Plugins/GameFeatures/BRCosmetics/Content/Athena/Items/Cosmetics/Characters/Character_WeepingWoodsFestive");
            var bytes = await SwapUtils.GetProvider().SaveAssetAsync("FortniteGame/Plugins/GameFeatures/BRCosmetics/Content/Athena/Items/Cosmetics/Characters/Character_WeepingWoodsFestive");
            UObject idk = await SwapUtils.GetProvider().LoadObjectAsync("FortniteGame/Plugins/GameFeatures/BRCosmetics/Content/Athena/Items/Cosmetics/Characters/Character_WeepingWoodsFestive");
            UObject toidk = await SwapUtils.GetProvider().LoadObjectAsync("FortniteGame/Content/Athena/Items/Cosmetics/Characters/CID_028_Athena_Commando_F");

            var nameprop = idk.Properties.First(x => x.Name == "DisplayName");
            var toprop = toidk.Properties.First(x => x.Name == "DisplayName");

            List<byte> dada = new List<byte>(bytes);

            dada.RemoveRange(nameprop.Position, nameprop.Size);
            dada.InsertRange(nameprop.Position, toprop._data);

            int ulongsize = sizeof(ulong);

            int SINGLE_EXPORTMAP_SIZE = 72;

            int cookedSerialSizeOffset =
                        pack.zenSummary.ExportMapOffset + ulongsize;

            Logger.Log("seriaksize: " + cookedSerialSizeOffset);

            dada.RemoveRange(cookedSerialSizeOffset, ulongsize);
            dada.InsertRange(cookedSerialSizeOffset, ConvertToBytes((ulong)537));
            Logger.Log("SERIALSIZE: " + pack.ExportMap[0].CookedSerialSize);
            await SwapUtils.SwapAsset(pack, dada.ToArray());
            pack = (IoPackage)await SwapUtils.GetProvider().LoadPackageAsync("FortniteGame/Plugins/GameFeatures/BRCosmetics/Content/Athena/Items/Cosmetics/Characters/Character_WeepingWoodsFestive");

            Logger.Log("SERIALSIZE: " + pack.ExportMap[0].CookedSerialSize);

            MessageBox("aa");
        }

        public static byte[] ConvertToBytes<T>(T value)
        {
            byte[] array = new byte[Marshal.SizeOf(value)];
            Unsafe.WriteUnaligned(ref array[0], value);
            return array;
        }

        public static int FindOffset(byte[] source, byte[] data)
        {
            for (int i = 0; i <= source.Length - data.Length; i++)
            {
                bool found = true;

                for (int j = 0; j < data.Length; j++)
                {
                    if (source[i + j] != data[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return i;
                }
            }

            return -1;
        }

        public static byte[] StrToByteArray(string str)
        {
            Dictionary<string, byte> hexindex = new Dictionary<string, byte>();
            for (int i = 0; i <= 255; i++)
                hexindex.Add(i.ToString("X2"), (byte)i);

            List<byte> hexres = new List<byte>();
            for (int i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2)]);

            return hexres.ToArray();
        }

        public static void CheckForBackupChanges(string pakDir)
        {
            try
            {
                Logger.Log("Checking Backups");
                string backupFolder = Utils.AppDataFolder + "\\Backups\\";

                bool invalid = false;

                foreach (var file in new DirectoryInfo(pakDir).GetFiles())
                {
                    if (Path.GetExtension(file.FullName).ToLower() == ".utoc" && !Path.GetFileNameWithoutExtension(file.FullName).Contains("optional"))
                    {
                        string backupPath = backupFolder + "\\" + Path.GetFileNameWithoutExtension(file.FullName) + ".utoc";

                        if (!File.Exists(backupPath))
                        {
                            invalid = true;
                            break;
                        }

                        if (new FileInfo(file.FullName).Length != new FileInfo(backupPath).Length)
                        {
                            invalid = true;
                            break;
                        }
                    }
                }

                if (invalid && Directory.Exists(backupFolder))
                {
                    Logger.Log("BACKUPS INVALID");
                    Directory.Delete(backupFolder, true);
                }
            } catch(Exception e)
            {
                Logger.LogError(e.Message, e);
            }
        }

        public static void DeleteOldBackups()
        {
            try
            {
                Logger.Log("Deleting Old Backups");
                string backupFolder = Utils.AppDataFolder + "\\Backups\\28.10\\";
                string newBackupFolder = Utils.AppDataFolder + "\\Backups\\";

                if (!Directory.Exists(backupFolder))
                {
                    return;
                }

                foreach (var file in Directory.GetFiles(backupFolder))
                {
                    Logger.Log("Moving Backup: " + file);
                    File.Move(file, newBackupFolder + "\\" + Path.GetFileName(file));
                }
            } catch(Exception ex)
            {
                Logger.LogError(ex.Message, ex);
            }
        }

    }
}

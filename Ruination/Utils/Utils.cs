using CUE4Parse.GameTypes.PUBG.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ruination_v2.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Ruination_v2.Utils
{
    public class Utils
    {

        public static string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\RuinationSwapperv2\\";
        public static Dictionary<ItemType, List<Item>> CachedItems = new Dictionary<ItemType, List<Item>>();

        public static string Version = "2.0.13";
        public static bool IsPremium = false;

        public static DiscordUserModel CurrentUser = new();

        public static async Task<List<Item>> GetTabItems(string tabType)
        {
            if (tabType == "misc")
                return await GetMiscItems();
            
            var list = new List<Item>();

            string url = "https://fortnite-api.com/v2/cosmetics/br/search/all?type=" + tabType;

            if (tabType == "car")
            {
                url = "https://fortnite-api.com/v2/cosmetics/cars";
            }

            var tabJson = JObject.Parse(await new System.Net.WebClient().DownloadStringTaskAsync(url));
            
            foreach (dynamic item in tabJson["data"])
            {
                var rarity = item.rarity.value.ToString().ToLower();
                string introductiontext = "";
                int introductionValue = 0;

                var type = ItemType.UNKNOWN;

                if (item.type.value.ToString().ToLower().Equals("outfit")) type = ItemType.SKIN;
                if (item.type.value.ToString().ToLower().Equals("emote")) type = ItemType.EMOTE;
                if (item.type.value.ToString().ToLower().Equals("pickaxe")) type = ItemType.PICKAXE;
                if (item.type.value.ToString().ToLower().Equals("backpack")) type = ItemType.BACKPACK;

                List<string> carTypes = new();
                carTypes.Add("body");
                carTypes.Add("wheel");
                carTypes.Add("drifttrail");
                carTypes.Add("booster");
                
                if (carTypes.Contains(item.type.value.ToString().ToLower()))
                    type = ItemType.CAR;
                
                if (item.id.ToString() == "DefaultPickaxe")
                {
                    introductiontext = "000000000";
                }
                else
                {
                    if (type == ItemType.CAR)
                    {
                        introductiontext = "0";
                        introductionValue = 0;
                    }
                    else
                    {
                        if (item.introduction == null || item.introduction.chapter == null || item.introduction.season == null) continue;
                        introductiontext = item.introduction.chapter + "|" + item.introduction.season;
                        introductionValue = int.Parse(item.introduction.backendValue.ToString());   
                    }
                }

                string series = "";

                if (item.series != null) series = item.series.value;

                string icon = string.Empty;

                if (type == ItemType.CAR)
                {
                    icon = item.images.small.ToString();
                }
                else
                {
                    icon = item.images.smallIcon ??
                           "https://fortnite-api.com/images/cosmetics/br/cid_028_athena_commando_f/icon.png";
                }

                string definitionPath = string.Empty;

                if (type != ItemType.CAR)
                    definitionPath = item.definitionPath.ToString() ?? "";

                string name = item.name.ToString();

                if (item.type.value.ToString().ToLower() == "booster")
                {
                    name += " (Booster)";
                } else if (item.type.value.ToString().ToLower() == "drifttrail")
                {
                    name += " (Drift Trail)";
                }

                list.Add(new()
                {
                    name = name,
                    id = item.id.ToString(),
                    path = item.path.ToString(),
                    definitionPath = definitionPath,
                    Type = type,
                    series = series,
                    rarcolor = Rarities.GetRarityColor(rarity),
                    description = item.description.ToString(),
                    added = introductiontext,
                    rarity = Rarities.GetRarityImage(rarity),
                    icon = icon,
                    backendReleaseValue = introductionValue,
                    subType = item.type.value.ToString().ToLower()
                });
            }

            if (tabType == "car")
            {
                foreach (var item in API.GetApi().AdditionalCarItems)
                {
                    list.Add(new()
                    {
                        name = item.Name,
                        id = item.ID,
                        path = item.Path,
                        definitionPath = string.Empty,
                        Type = ItemType.CAR,
                        series = string.Empty,
                        description = item.Description,
                        added = "!",
                        icon = item.Icon,
                        backendReleaseValue = 0,
                        subType = item.Type
                    });
                }
            }

            if (tabType == "car")
            {
                list = list.OrderBy(x => x.subType).ToList();                
            }
            else
            {
                list = list.OrderBy(x => x.added).ToList();
            }
            
            return list;
        }

        public static async Task<List<Item>> GetMiscItems()
        {
            var miscList = new List<Item>();

            miscList.Add(new()
            {
                name = "FOV",
                description = "Change your FOV.",
                added = "1",
                icon = "https://cdn.discordapp.com/attachments/1206254418329342022/1206632256278630410/image_2.png?ex=65dcb6dc&is=65ca41dc&hm=cb4d23d5b79028f7a0a42128e29f491711ad26af5a30d71797246de301f841a6&",
                id = "ruination_fov",
                Type = ItemType.MISC
            });
            
            miscList.Add(new()
            {
                name = "Background",
                description = "Change your Background.",
                added = "2",
                icon = "https://media.discordapp.net/attachments/1197950285834891325/1200550466275254392/image.png?ex=65d90bc0&is=65c696c0&hm=85c58cb32db40a0ead486efabf2792a0bb3a534cb778abf6ed75475bcf203904&=&format=webp&quality=lossless&width=1012&height=635",
                id = "ruination_background",
                Type = ItemType.MISC
            });
            
            return miscList;
        }

        public static Item GetActualChararacterFromLoadeditems(string id)
        {
            if (!Utils.CachedItems.ContainsKey(ItemType.SKIN)) return null;
            return Utils.CachedItems[ItemType.SKIN].FirstOrDefault(x => x.id.ToLower().Equals(id.ToLower()));
        }
        public static async Task ParseAllCosmetics()
        {
            Logger.Log("Parsing All Cosmetics");

            await ParsedCosmeticsAPI.Load();
            Config.GetConfig().CachedItems = ParsedCosmeticsAPI.GetApi().CachedItems;
            
            Logger.Log("Parsed Cosmetics");
        }

        public static async Task LogSwap(string id, bool convert, string optionid)
        {
            try
            {
                Logger.Log("Logging Swap: " + id);
                new Thread(async () =>
                {
                    try
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
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex.Message, ex);
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
                    try
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
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e.Message, e);
                    }

                }).Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, ex);
            }
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

        public static byte[] HexToByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            
            if (hexString.Length % 2 != 0)
                throw new ArgumentException("The hex string length must be an even number.");

            byte[] byteArray = new byte[hexString.Length / 2];

            for (int i = 0; i < byteArray.Length; i++)
            {
                byteArray[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return byteArray;
        }   
        
        public static async Task RevertAllItems(bool showmsgbox = true)
        {
            try
            {
                var prov = SwapUtils.GetProvider();

                Logger.Log("Reverting all - " + Config.GetConfig().ConvertedItems.Count + " Items");

                string backupfolder = AppDataFolder + "\\Backups\\";
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

                if (showmsgbox)
                    MessageBox.Show("Reverted All Items.", "Ruination", MessageBoxButton.OK,
                        MessageBoxImage.Information);

            } catch(Exception e)
            {
                Logger.LogError(e.Message, e);
            }

        }


        public static void CheckForBackupChanges(string pakDir)
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
            Logger.Log("Starting Fortnite");
            StartUrl("com.epicgames.launcher://apps/Fortnite?action=launch&silent=true");
        }
        
        public static async Task VerifyFortnite()
        {
            Logger.Log("Verifying Fortnite");
            StartUrl("com.epicgames.launcher://apps/Fortnite?action=verify&silent=false");
        }

        public static async Task ShowConvertedItems()
        {
            if (Config.GetConfig().ConvertedItems.Count == 0)
            {
                MessageBox.Show("You have no items converted.", "Ruination", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StringBuilder textBuilder = new StringBuilder();

            foreach (var item in Config.GetConfig().ConvertedItems)
            {
                textBuilder.AppendLine(item.Name);
            }

            string text = textBuilder.ToString().TrimEnd();

            MessageBox.Show("You have these items converted:\n" + text, "Ruination", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
         public static async Task CheckRuinationUtils(Label label)
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

                    label.Content = "Checking Installed Ruination Utils Version";

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

                label.Content = "Downloading Ruination Utils";

                using (WebClient wc = new WebClient())
                {
                    wc.DownloadProgressChanged += (sender, e) =>
                    {
                        label.Content = $"Downloading Ruination Utils ({e.ProgressPercentage}%)";
                    };

                    wc.DownloadFileCompleted += async delegate
                    {
                        Logger.Log("Unzipping file");
                        label.Content = "Unzipping file";

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

                        label.Content = "Waiting for Process";

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

    }
}

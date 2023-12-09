using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.UObject;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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

        public static string USER_VERSION = "2.0.6";
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
            string allItemsDownloaded = await new System.Net.WebClient().DownloadStringTaskAsync("https://fortnite-api.com/v2/cosmetics/br");

            JObject j = JObject.Parse(allItemsDownloaded);

            foreach (dynamic item in j["data"])
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
                    Logger.LogError(e.Message, e);
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
            Utils.MessageBox("Please do not close the swapper in order to cancel verification");
            Logger.Log("Starting Fortnite");
            StartUrl("com.epicgames.launcher://apps/Fortnite?action=launch&silent=true");

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
                if (Config.GetConfig().ConvertedItems.Count == 0)
                {
                    MessageBox("You have no items converted.");
                    return;
                }

                var prov = SwapUtils.GetProvider();

                Logger.Log("Reverting all - " + Config.GetConfig().ConvertedItems.Count + " Items");

                foreach (var item in Config.GetConfig().ConvertedItems)
                {
                    try
                    {
                        Logger.Log("Reverting " + item.Name + $"({item.Assets.Count})");
                        foreach (var asset in item.Assets)
                        {
                            Logger.Log("Reverting Asset " + asset.Key);
                            string assetkey = asset.Key.StartsWith("TEXTURESWAP_") ? asset.Key.Split("TEXTURESWAP_").LastOrDefault() : asset.Key;
                            await SwapUtils.RevertPackage((IoPackage)await prov.LoadPackageAsync(assetkey));
                        }
                    } catch(Exception ex)
                    {
                        Logger.LogError(ex.Message, ex);
                    }
                }

                Config.GetConfig().ConvertedItems.Clear();
                Config.Save();

                if(showmsgbox) await MessageBox("Reverted all items");

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

        public static async Task LogSwap(string id)
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

        public static async Task LogUEFNSwap(string id)
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

        public static async Task SwapDefaultData()
        {
            IoPackage defaultPack = (IoPackage)await SwapUtils.GetProvider().LoadPackageAsync("FortniteGame/Content/Balance/DefaultGameDataCosmetics");

            int index1 = defaultPack.NameMapAsStrings.ToList().IndexOf("/Game/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Fallback");
            int index2 = defaultPack.NameMapAsStrings.ToList().IndexOf("CP_Athena_Body_F_Fallback");

            defaultPack.NameMapAsStrings[index1] = "/Game/Athena/Heroes/Meshes/Bodies/CP_028_Athena_Body";
            defaultPack.NameMapAsStrings[index2] = "CP_028_Athena_Body";

            await SwapUtils.SwapAsset(defaultPack, new Serializer(defaultPack).Serialize());

            await Utils.MessageBox("aa");

        }

    }
}

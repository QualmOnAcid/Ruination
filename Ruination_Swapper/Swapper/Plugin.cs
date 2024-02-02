using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebviewAppShared.Models;
using WebviewAppShared.Utils;

namespace WebviewAppShared.Swapper
{
    public class Plugin
    {
        public static async Task<bool> ConvertPlugin(PluginModel Plugin)
        {
            try
            {
                Logger.Log("Converting Plugin " + Plugin.ID);

                var prov = SwapUtils.GetProvider();

                Logger.Log("Converting Swaps");
                int swappedCount = 0;

                Dictionary<string, byte[]> wroteAssets = new();

                var slot = prov.UnusedFiles[0];

                if (Plugin.Type.ToLower().Equals("uefn"))
                {
                    if(prov.UnusedFiles.Count == 0)
                    {
                        await Utils.Utils.MessageBox("No Unused file was found. Please verify Fortnite through Epic Games Launcher and try again!");
                        return false;
                    }

                    Logger.Log("Downloading UEFN Files");

                    Utils.Utils.MainWindow.LogText = "Downloading UEFN Files";
                    Utils.Utils.MainWindow.UpdateUI();

                    using (WebClient wc = new WebClient())
                    {
                        await wc.DownloadFileTaskAsync(Plugin.Files.pak, slot + ".pak");
                        await wc.DownloadFileTaskAsync(Plugin.Files.sig, slot + ".sig");
                        await wc.DownloadFileTaskAsync(Plugin.Files.utoc, slot + ".utoc");

                        //Move file for uefnprovider to load it
                        string backupfolder = WebviewAppShared.Utils.Utils.AppDataFolder + "\\Backups\\";
                        string backuppath = backupfolder + Path.GetFileName(slot + ".utoc");
                        File.Copy(slot + ".utoc", backuppath, true);

                        wc.DownloadProgressChanged += (sender, e) =>
                        {
                            Utils.Utils.MainWindow.LogText = $"Downloading UEFN Files ({e.ProgressPercentage}%)";
                            Utils.Utils.MainWindow.UpdateUI();
                        };

                        await wc.DownloadFileTaskAsync(Plugin.Files.ucas, slot + ".ucas");
                    }
                }

                var uefnprov = await SwapUtils.GetUEFNProvider(System.IO.Path.GetFileNameWithoutExtension(slot));

                if (Plugin.Swaps != null)
                {
                    foreach (var swap in Plugin.Swaps)
                    {
                        swappedCount++;
                        Logger.Log("Swapping Asset " + swappedCount);
                        Utils.Utils.MainWindow.LogText = "Swapping Asset " + swappedCount;
                        Utils.Utils.MainWindow.UpdateUI();

                        string tp = prov.DoesAssetExist(swap.ToAsset) ? swap.ToAsset : TryFixPath(swap.ToAsset, uefnprov).Split(".").FirstOrDefault();

                        var fromPackage = (IoPackage)await prov.LoadPackageAsync(swap.Asset);
                        var toPackage = prov.DoesAssetExist(swap.ToAsset) ? (IoPackage)await prov.LoadPackageAsync(swap.ToAsset) : (IoPackage)await uefnprov.LoadPackageAsync(TryFixPath(swap.ToAsset, uefnprov).Split(".").FirstOrDefault());

                        toPackage.ChangeProtectedStrings(fromPackage.GetProtectedStrings());
                        toPackage.ChangePublicExportHash(fromPackage);

                        if(swap.Swaps != null && swap.Swaps.Count > 0)
                        {

                            //Format strings with aa.bb

                            for(int i = 0; i < swap.Swaps.Count; i++)
                            {
                                if (!swap.Swaps[i].Type.ToLower().Equals("string")) continue;

                                string search = swap.Swaps[i].Search;
                                string replace = swap.Swaps[i].Replace;

                                if (!search.Contains(".") || !replace.Contains(".")) continue;

                                string s1 = search.Split(".").FirstOrDefault();
                                string s2 = search.Split(".").LastOrDefault();

                                string r1 = replace.Split(".").FirstOrDefault();
                                string r2 = replace.Split(".").LastOrDefault();

                                swap.Swaps[i].Search = s1;
                                swap.Swaps[i].Replace = r1;

                                swap.Swaps.Add(new()
                                {
                                    Type = "string",
                                    Search = s2,
                                    Replace = r2
                                });
                            }

                            foreach(var s in swap.Swaps)
                            {
                                try
                                {
                                    if (s.Type.ToLower().Equals("string"))
                                    {

                                        string rr = s.Replace;

                                        if(!prov.DoesAssetExist(rr))
                                        {
                                            if (uefnprov == null)
                                                uefnprov = await SwapUtils.GetUEFNProvider(slot);

                                            rr = TryFixPath(rr, uefnprov).SubstringBeforeLast(".");
                                        }

                                        if(toPackage.NameMapAsStrings.ToList().Contains(s.Search))
                                        {
                                            int index = toPackage.NameMapAsStrings.ToList().IndexOf(s.Search);

                                            toPackage.NameMapAsStrings[index] = s.Replace;

                                            Logger.Log($"Replaced String {s.Search} to {s.Replace}");

                                        } else
                                        {
                                            Logger.Log($"WARNING: String {s.Search} was not found in package");
                                        }

                                    }
                                } catch(Exception e)
                                {
                                    Logger.LogError(e.Message, e);
                                }
                            }

                        }

                        var data = new Serializer(toPackage).Serialize();

                        foreach(var s in swap.Swaps.Where(x => x.Type.ToLower().Equals("hex")))
                        {
                            Logger.Log("SWAPPING HEX: " + s.Search);
                            byte[] searchByte = Utils.Utils.StrToByteArray(s.Search);
                            byte[] replaceByte = Utils.Utils.StrToByteArray(s.Replace);

                            if (replaceByte.Length != searchByte.Length)
                                continue;

                            if (replaceByte.Length < searchByte.Length)
                                Array.Resize(ref replaceByte, searchByte.Length);


                            int offset = Utils.Utils.FindOffset(data, searchByte);

                            if(offset == -1)
                            {
                                Logger.Log("Byte Array not found.");
                                continue;
                            }

                            var dataAsList = new List<byte>(data);
                            dataAsList.RemoveRange(offset, searchByte.Length);
                            dataAsList.InsertRange(offset, replaceByte);
                            data = dataAsList.ToArray();
                        }

                        if (!await SwapUtils.SwapAsset(fromPackage, data))
                            return false;

                        wroteAssets.Add(swap.Asset, data);
                    }
                }

                Logger.Log("Adding Converted Item");

                var convertedItem = new ConvertedItem()
                {
                    ID = Plugin.ID,
                    OptionID = Plugin.ID,
                    Type = "plugin",
                    Assets = wroteAssets,
                    isPlugin = true,
                    Name = Plugin.Option.Name + " To " + Plugin.Name + " (Plugin)"
                };

                Config.GetConfig().ConvertedItems.Add(convertedItem);
                Config.Save();

                Logger.Log("Finished Plugin Swap");

                Utils.Utils.MainWindow.LogText = "Converted";
                Utils.Utils.MainWindow.UpdateUI();

                return true;
            } catch(Exception e)
            {
                Logger.LogError(e.Message, e);
                Utils.Utils.MessageBox(e.Message);
                return false;
            }
        }

        public static async Task<bool> RevertPlugin(PluginModel Plugin)
        {
            try
            {
                Logger.Log("Reverting Plugin " + Plugin.ID);

                var prov = SwapUtils.GetProvider();

                Logger.Log("Reverting Swaps");
                int swappedCount = 0;

                if(Plugin.Swaps != null)
                {
                    foreach (var swap in Plugin.Swaps)
                    {
                        swappedCount++;
                        Logger.Log("Swapping Asset " + swappedCount);
                        Utils.Utils.MainWindow.LogText = "Swapping Asset " + swappedCount;
                        Utils.Utils.MainWindow.UpdateUI();

                        var fromPackage = (IoPackage)await prov.LoadPackageAsync(swap.Asset);

                        if (!await SwapUtils.RevertPackage(fromPackage)) return false;
                    }
                }

                Config.GetConfig().ConvertedItems.RemoveAll(x => x.ID.ToLower().Equals(Plugin.ID.ToLower()) && x.Type == "plugin");
                Config.Save();

                Logger.Log("Finished Plugin Swap");

                Utils.Utils.MainWindow.LogText = "Reverted";
                Utils.Utils.MainWindow.UpdateUI();

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                Utils.Utils.MessageBox(e.Message);
                return false;
            }
        }

        public static async Task<bool> ConvertUEFNSkinPlugin(PluginModel Plugin, Item option)
        {
            var prov = SwapUtils.GetProvider();

            var textureSwaps = new List<ApiUEFNSkinTextureSwapObject>();

            if (prov.UnusedFiles.Count == 0)
            {
                await Utils.Utils.MessageBox("No Unused file was found. Please verify Fortnite through Epic Games Launcher and try again!");
                return false;
            }

            var slot = prov.UnusedFiles[0];

            Logger.Log("Downloading UEFN Files");

            Utils.Utils.MainWindow.LogText = "Downloading UEFN Files";
            Utils.Utils.MainWindow.UpdateUI();

            using (WebClient wc = new WebClient())
            {
                await wc.DownloadFileTaskAsync(Plugin.Files.pak, slot + ".pak");
                await wc.DownloadFileTaskAsync(Plugin.Files.sig, slot + ".sig");
                await wc.DownloadFileTaskAsync(Plugin.Files.utoc, slot + ".utoc");

                //Move file for uefnprovider to load it
                string backupfolder = WebviewAppShared.Utils.Utils.AppDataFolder + "\\Backups\\";
                string backuppath = backupfolder + Path.GetFileName(slot + ".utoc");
                File.Copy(slot + ".utoc", backuppath, true);

                wc.DownloadProgressChanged += (sender, e) =>
                {
                    Utils.Utils.MainWindow.LogText = $"Downloading UEFN Files ({e.ProgressPercentage}%)";
                    Utils.Utils.MainWindow.UpdateUI();
                };

                await wc.DownloadFileTaskAsync(Plugin.Files.ucas, slot + ".ucas");
            }

            Logger.Log("Loading UEFN Provider");
            Utils.Utils.MainWindow.LogText = "Loading UEFN Provider";
            Utils.Utils.MainWindow.UpdateUI();

            var uefnprovider = await SwapUtils.GetUEFNProvider(System.IO.Path.GetFileNameWithoutExtension   (slot));

            string meshPath = Plugin.Skin.Mesh;

            if(!prov.DoesAssetExist(meshPath))
            {
                meshPath = TryFixPath(meshPath, uefnprovider);
            }

            if (Plugin.Swaps != null && Plugin.Swaps.Count > 0)
            {
                foreach (var swap in Plugin.Swaps)
                {
                    textureSwaps.Add(new()
                    {
                        From = swap.Asset,
                        To = prov.DoesAssetExist(swap.ToAsset) ? swap.ToAsset : TryFixPath(swap.ToAsset, uefnprovider).Split(".").FirstOrDefault(),
                        Swaps = swap.Swaps
                    });
                }
            }

            uefnprovider.Dispose();

            ApiUEFNSkinObject Character = new()
            {
                Name = Plugin.Name,
                ID = Plugin.ID,
                Description = Plugin.Description,
                Type = "UEFN",
                Mesh = meshPath,
                Skeleton = Plugin.Skin.Skeleton,
                Animation = Plugin.Skin.Animation,
                Icon = Plugin.Icon,
                Rarity = Plugin.Rarity,
                hidpath = null,
                Info = "",
                PartModifierBlueprint = "",
                IdleEffectNiagara = "",
                Materials = Plugin.Materials ?? new(),
                TextureSwaps = textureSwaps
            };

            return await UEFN.ConvertUEFN(Character, option, prov, false, true);
        }

        private static string TryFixPath(string path, DefaultFileProvider prov)
        {
            Logger.Log("Trying to find path in uefn files for " + path);
            Logger.Log("Loading UEFN Provider");

            Logger.Log("Finding Asset");
            Logger.Log("Files: " + prov.Files.Count);

            foreach (var (assetpath, gamefile) in prov.Files)
            {
                string asset = assetpath.ToLower();
                string gf = "fortnitegame/plugins/gamefeatures";
                if (asset.StartsWith(gf))
                {
                    string assetwithoutstart = asset.Substring(gf.Length + 1);

                    string contentpath = assetwithoutstart.SubstringAfter("/");

                    string content = "content";

                    if (contentpath.StartsWith(content))
                    {
                        contentpath = contentpath.Substring(content.Length).Split(".").FirstOrDefault();
                        if (contentpath == path.Split(".")[0].ToLower())
                        {
                            string originalfilepath = gamefile.PathWithoutExtension;

                            string originalwithoutstart = originalfilepath.Substring(gf.Length + 1);

                            string uefnid = "/" + originalwithoutstart.SubstringBefore("/");

                            string cpath = originalwithoutstart.SubstringAfter("/");
                            cpath = cpath.Substring(content.Length);

                            string subpathstring = cpath.SubstringAfterLast("/");

                            path = uefnid + cpath + "." + subpathstring;

                            Logger.Log("FOUND UEFN PATH: " + path);

                            break;

                        }
                    }
                }
            }
            return path;
        }

        public static async Task<bool> RevertUEFNSkinPlugin(PluginModel Plugin, Item option)
        {

            var textureSwaps = new List<ApiUEFNSkinTextureSwapObject>();

            if (Plugin.Swaps != null && Plugin.Swaps.Count > 0)
            {
                foreach (var swap in Plugin.Swaps)
                {
                    textureSwaps.Add(new()
                    {
                        From = swap.Asset,
                        To = swap.ToAsset,
                        Swaps = swap.Swaps
                    });
                }
            }

            ApiUEFNSkinObject Character = new()
            {
                Name = Plugin.Name,
                ID = Plugin.ID,
                Description = Plugin.Description,
                Type = "UEFN",
                Mesh = Plugin.Skin.Mesh ?? "",
                Skeleton = Plugin.Skin.Skeleton ?? "",
                Animation = Plugin.Skin.Animation ?? "",
                Icon = Plugin.Icon,
                Rarity = Plugin.Rarity,
                hidpath = null,
                Info = "",
                PartModifierBlueprint = "",
                IdleEffectNiagara = "",
                Materials = Plugin.Materials ?? new(),
                TextureSwaps = textureSwaps
            };

            return await UEFN.RevertUEFN(Character, option, SwapUtils.GetProvider());
        }

    }
}

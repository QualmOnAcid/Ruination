using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.Utils;
using Ruination_v2.Models;
using Ruination_v2.Utils;
using Ruination_v2.Views;

namespace Ruination_v2.Swapper;

public class Plugin
{
    public static async Task<bool> ConvertPlugin(PluginModel Plugin, Label label)
    {
        try
        {
            Logger.Log("Converting Plugin " + Plugin.ID);

            var prov = SwapUtils.GetProvider();

            Logger.Log("Converting Swaps");
            int swappedCount = 0;

            Dictionary<string, byte[]> wroteAssets = new();

            var slot = prov.UnusedFiles[1];

            if (Plugin.Type.ToLower().Equals("uefn"))
            {
                if (prov.UnusedFiles.Count == 0)
                {
                    MessageBox.Show("No Unused file was found. Please verify Fortnite through Settings and try again!",
                        "Ruination", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                Logger.Log("Downloading UEFN Files");

                label.Content = "Downloading UEFN Files";

                using (WebClient wc = new WebClient())
                {
                    await wc.DownloadFileTaskAsync(Plugin.Files.pak, slot + ".pak");
                    await wc.DownloadFileTaskAsync(Plugin.Files.sig, slot + ".sig");
                    await wc.DownloadFileTaskAsync(Plugin.Files.utoc, slot + ".utoc");

                    //Move file for uefnprovider to load it
                    string backupfolder = Utils.Utils.AppDataFolder + "\\Backups\\";
                    string backuppath = backupfolder + Path.GetFileName(slot + ".utoc");
                    File.Copy(slot + ".utoc", backuppath, true);

                    wc.DownloadProgressChanged += (sender, e) =>
                    {
                        label.Content = $"Downloading UEFN Files ({e.ProgressPercentage}%)";
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
                    label.Content = "Swapping Asset " + swappedCount;

                    string tp = prov.DoesAssetExist(swap.ToAsset)
                        ? swap.ToAsset
                        : TryFixPath(swap.ToAsset, uefnprov).Split(".").FirstOrDefault();

                    var fromPackage = (IoPackage)await prov.LoadPackageAsync(swap.Asset);
                    var toPackage = prov.DoesAssetExist(swap.ToAsset)
                        ? (IoPackage)await prov.LoadPackageAsync(swap.ToAsset)
                        : (IoPackage)await uefnprov.LoadPackageAsync(TryFixPath(swap.ToAsset, uefnprov).Split(".")
                            .FirstOrDefault());

                    toPackage.ChangeProtectedStrings(fromPackage.GetProtectedStrings());
                    toPackage.ChangeLastPublicExportHash(fromPackage);

                    if (swap.Swaps != null && swap.Swaps.Count > 0)
                    {

                        //Format strings with aa.bb

                        for (int i = 0; i < swap.Swaps.Count; i++)
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

                        foreach (var s in swap.Swaps)
                        {
                            try
                            {
                                if (s.Type.ToLower().Equals("string"))
                                {

                                    string rr = s.Replace;

                                    if (!prov.DoesAssetExist(rr))
                                    {
                                        if (uefnprov == null)
                                            uefnprov = await SwapUtils.GetUEFNProvider(slot);

                                        rr = TryFixPath(rr, uefnprov).SubstringBeforeLast(".");
                                    }

                                    if (toPackage.NameMapAsStrings.ToList().Contains(s.Search))
                                    {
                                        int index = toPackage.NameMapAsStrings.ToList().IndexOf(s.Search);

                                        toPackage.NameMapAsStrings[index] = s.Replace;

                                        Logger.Log($"Replaced String {s.Search} to {s.Replace}");

                                    }
                                    else
                                    {
                                        Logger.Log($"WARNING: String {s.Search} was not found in package");
                                    }

                                }
                            }
                            catch (Exception e)
                            {
                                Logger.LogError(e.Message, e);
                            }
                        }

                    }

                    var data = new Serializer(toPackage).Serialize();

                    if (swap.Swaps != null)
                    {
                        foreach (var s in swap.Swaps.Where(x => x.Type.ToLower().Equals("hex")))
                        {
                            Logger.Log("SWAPPING HEX: " + s.Search);
                            byte[] searchByte = Utils.Utils.HexToByte(s.Search);
                            byte[] replaceByte = Utils.Utils.HexToByte(s.Replace);

                            if (replaceByte.Length != searchByte.Length)
                                continue;

                            if (replaceByte.Length < searchByte.Length)
                                Array.Resize(ref replaceByte, searchByte.Length);


                            int offset = Utils.Utils.FindOffset(data, searchByte);

                            if (offset == -1)
                            {
                                Logger.Log("Byte Array not found.");
                                continue;
                            }

                            var dataAsList = new List<byte>(data);
                            dataAsList.RemoveRange(offset, searchByte.Length);
                            dataAsList.InsertRange(offset, replaceByte);
                            data = dataAsList.ToArray();
                        }
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

            label.Content = "Converted";

            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message, e);
            MessageBox.Show("There was an error: " + e.Message, "Ruination", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    public static async Task<bool> RevertPlugin(PluginModel Plugin, Label label)
    {
        try
        {
            Logger.Log("Reverting Plugin " + Plugin.ID);

            var prov = SwapUtils.GetProvider();

            Logger.Log("Reverting Swaps");
            int swappedCount = 0;

            if (Plugin.Swaps != null)
            {
                foreach (var swap in Plugin.Swaps)
                {
                    swappedCount++;
                    Logger.Log("Swapping Asset " + swappedCount);
                    label.Content = "Swapping Asset " + swappedCount;

                    var fromPackage = (IoPackage)await prov.LoadPackageAsync(swap.Asset);

                    if (!await SwapUtils.RevertPackage(fromPackage)) return false;
                }
            }

            Config.GetConfig().ConvertedItems
                .RemoveAll(x => x.ID.ToLower().Equals(Plugin.ID.ToLower()) && x.Type == "plugin");
            Config.Save();

            Logger.Log("Finished Plugin Swap");

            label.Content = "Reverted";

            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message, e);
            MessageBox.Show("There was an error: " + e.Message, "Ruination", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    public static async Task<bool> ConvertUEFNSkinPlugin(PluginModel Plugin, Item option, UEFNSkinSwapForm label)
    {
        try
        {
            
            if (await SwapUtils.IsUEFNAlreadySwapped())
            {
                Logger.LogError("UEFN is already Swapped");
                if(System.Windows.MessageBox.Show("Looks like you already have a UEFN skin converted.\nDo you want to revert it?", "Ruination", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
                {
                    var convertedskin = Config.GetConfig().ConvertedItems.FirstOrDefault(x => x.Type == "uefn");
                    await SwapUtils.RevertConvertedItem(convertedskin);
                    Config.GetConfig().ConvertedItems.Remove(convertedskin);
                    Config.Save();
                } else
                {
                    label.updatetext("Waiting for Input");
                    return false;
                }
            }
            
            var prov = SwapUtils.GetProvider();
            var textureSwaps = new List<ApiUEFNSkinTextureSwapObject>();

            if (prov.UnusedFiles.Count == 0)
            {
                MessageBox.Show("No Unused file was found. Please verify Fortnite through Settings and try again!",
                    "Ruination", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            var slot = prov.UnusedFiles[1];

            Logger.Log("Downloading UEFN Files");

            label.updatetext("Downloading UEFN Files");
            
            using (WebClient wc = new WebClient())
            {
                await wc.DownloadFileTaskAsync(Plugin.Files.pak, slot + ".pak");
                await wc.DownloadFileTaskAsync(Plugin.Files.sig, slot + ".sig");
               
                await wc.DownloadFileTaskAsync(Plugin.Files.utoc, slot + ".utoc");

                //Move file for uefnprovider to load it
                string backupfolder = Utils.Utils.AppDataFolder + "\\Backups\\";
                string backuppath = backupfolder + Path.GetFileName(slot + ".utoc");
                File.Copy(slot + ".utoc", backuppath, true);

                wc.DownloadProgressChanged += (sender, e) =>
                {
                    label.updatetext($"Downloading UEFN Files ({e.ProgressPercentage}%)");
                };

                await wc.DownloadFileTaskAsync(Plugin.Files.ucas, slot + ".ucas");
            }

            Logger.Log("Loading UEFN Provider");
            label.updatetext("Loading UEFN Provider");

            var uefnprovider = await SwapUtils.GetUEFNProvider(System.IO.Path.GetFileNameWithoutExtension(slot));

            string meshPath = Plugin.Skin.Mesh;

            if (!prov.DoesAssetExist(meshPath))
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
                        To = prov.DoesAssetExist(swap.ToAsset)
                            ? swap.ToAsset
                            : TryFixPath(swap.ToAsset, uefnprovider).Split(".").FirstOrDefault(),
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
                hidpath = string.IsNullOrEmpty(Plugin.Skin.HIDPath) ? null : Plugin.Skin.HIDPath,
                Info = "",
                PartModifierBlueprint = Plugin.Skin.PartmodifierBlueprint,
                IdleEffectNiagara = Plugin.Skin.IdleEffectNiagara,
                Materials = Plugin.Materials ?? new(),
                TextureSwaps = textureSwaps,
                IdleFXSocket = Plugin.Skin.IdleFXSocket,
                UseIdleEffectPackage =  Plugin.Skin.UseIdleEffectPackage
            };
            
            Logger.Log("USING HID PATH: " + (Character.hidpath == null ? "None" : Character.hidpath));

            return await UEFN.ConvertUEFN(Character, option, prov, label, false, true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message, ex);
            MessageBox.Show("There was an error: " + ex.Message, "Ruination", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
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

        public static async Task<bool> RevertUEFNSkinPlugin(PluginModel Plugin, Item option, UEFNSkinSwapForm lab)
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
                hidpath = string.IsNullOrEmpty(Plugin.Skin.HIDPath) ? null : Plugin.Skin.HIDPath,
                Info = "",
                PartModifierBlueprint = "",
                IdleEffectNiagara = "",
                Materials = Plugin.Materials ?? new(),
                TextureSwaps = textureSwaps
            };

            return await UEFN.RevertUEFN(Character, option, lab);
        }
}
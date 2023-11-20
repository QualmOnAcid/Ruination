using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebviewAppShared.Models;
using WebviewAppShared.Utils;
using Newtonsoft.Json;

namespace WebviewAppShared.Swapper
{
    public class Standard
    {
        public static async Task<bool> Convert(Item currentitem, Item currentoption)
        {
            try
            {
                Logger.Log("Converting Standard -> " + currentoption.name + " To " + currentitem.name);
                Utils.Utils.MainWindow.LogText = "Loading Provider";
                var prov = SwapUtils.GetProvider();
                Utils.Utils.MainWindow.LogText = "Preparing Assets";

                Logger.Log("Preparing Assets");

                var assetStrings = await SwapUtils.GetAssetsForSwap(currentitem, currentoption);

                Logger.Log("Found Assets: " + JsonConvert.SerializeObject(assetStrings));

                if (assetStrings.Count == 0)
                {
                    Utils.Utils.MessageBox("Swapper could not find any Assets to swap");
                    Utils.Utils.MainWindow.LogText = "Waiting for Input";
                    return false;
                }

                int swappedIndex = 1;

                Dictionary<string, byte[]> wroteAssets = new();

                foreach (KeyValuePair<string, string> entry in assetStrings)
                {
                    Logger.Log("Swapping Asset " + swappedIndex);
                    Utils.Utils.MainWindow.LogText = "Swapping Asset " + swappedIndex;
                    Logger.Log("Loading " + entry.Key);
                    var fromPack = (IoPackage)(await prov.LoadPackageAsync(entry.Key));
                    Logger.Log("Loading " + entry.Value);
                    var toPack = (IoPackage)(await prov.LoadPackageAsync(entry.Value));

                    Logger.Log("Changing Protected Strings");

                    if (!toPack.ChangeProtectedStrings(fromPack.GetProtectedStrings()))
                    {
                        return false;
                    }

                    Logger.Log("Changing Export Hash");

                    toPack.ChangePublicExportHash(fromPack);

                    var data = new Serializer(toPack).Serialize();

                    Logger.Log("Serialized with Size of " + data.Length);

                    if (!await SwapUtils.SwapAsset(fromPack, data))
                        return false;

                    wroteAssets.Add(entry.Key, data);

                    swappedIndex++;
                }

                Logger.Log("Adding Converted Item");

                var convertedItem = new ConvertedItem()
                {
                    ID = currentitem.id,
                    OptionID = currentoption.id,
                    Type = currentoption.IsTransformCharacter ? "transform" : "standard",
                    Assets = wroteAssets,
                    Name = currentoption.name + " To " + currentitem.name
                };

                Config.GetConfig().ConvertedItems.RemoveAll(x => x.OptionID.ToLower().Equals(currentoption.id.ToLower()));
                Config.GetConfig().ConvertedItems.Add(convertedItem);
                Config.Save();

                Utils.Utils.MainWindow.LogText = "Converted";
                Logger.Log("Swap finished");

                return true;
            }
            catch (Exception e)
            {
                Utils.Utils.MessageBox("There was an error: " + e.Message);
                Utils.Utils.MainWindow.LogText = "Waiting for Input";
                return false;
            }
        }
        public static async Task<bool> Revert(Item currentitem, Item currentoption)
        {
            try
            {
                Logger.Log("Reverting Standard -> " + currentoption.name + " To " + currentitem.name);
                Utils.Utils.MainWindow.LogText = "Loading Provider";
                var prov = SwapUtils.GetProvider();
                Utils.Utils.MainWindow.LogText = "Preparing Assets";

                Logger.Log("Preparing Assets");

                var assetStrings = await SwapUtils.GetAssetsForSwap(currentitem, currentoption);

                Logger.Log("Found Assets: " + JsonConvert.SerializeObject(assetStrings));

                if (assetStrings.Count == 0)
                {
                    Utils.Utils.MessageBox("Swapper could not find any Assets to swap");
                    Utils.Utils.MainWindow.LogText = "Waiting for Input";
                    return false;
                }

                int swappedIndex = 1;

                foreach (KeyValuePair<string, string> entry in assetStrings)
                {
                    Logger.Log("Swapping Asset " + swappedIndex);
                    Utils.Utils.MainWindow.LogText = "Swapping Asset " + swappedIndex;
                    Logger.Log("Loading " + entry.Key);
                    var fromPack = (IoPackage)(await prov.LoadPackageAsync(entry.Key));

                    if (!await SwapUtils.RevertPackage(fromPack))
                    {
                        return false;
                    }

                    swappedIndex++;
                }

                Logger.Log("Removing Converted Item");

                Config.GetConfig().ConvertedItems.RemoveAll(x => x.OptionID.ToLower().Equals(currentoption.id.ToLower()));
                Config.Save();

                Utils.Utils.MainWindow.LogText = "Reverted";

                Logger.Log("Swap finished");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                Utils.Utils.MessageBox("There was an error: " + e.Message);
                Utils.Utils.MainWindow.LogText = "Waiting for Input";
                return false;
            }
        }

    }
}

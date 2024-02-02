using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebviewAppShared.Models;
using WebviewAppShared.Utils;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Connections.Features;
using CUE4Parse.UE4.Objects.UObject;

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

                bool doEmoteSeriesSwap = false;

                if(currentitem.Type == ItemType.EMOTE && currentoption.Type == ItemType.EMOTE)
                {
                    if (currentitem.series != currentoption.series)
                        doEmoteSeriesSwap = true;
                }

                if(false)
                {
                    Logger.Log("Doing Emote Series Swap");

                    IoPackage fromEmote = (IoPackage)await prov.LoadPackageAsync(currentoption.path);
                    UObject toEmoteObject = await prov.LoadObjectAsync(currentitem.path);

                    IoPackage boogieDownPackage = (IoPackage)await prov.LoadPackageAsync("/BRCosmetics/Athena/Items/Cosmetics/Dances/EID_BoogieDown");

                    string boogiedownMaleAnim = "/Game/Animation/Game/MainPlayer/Emotes/Boogie_Down/Emote_Boogie_Down_CMM.Emote_Boogie_Down_CMM";
                    string boogiedownFemaleAnim = "/Game/Animation/Game/MainPlayer/Emotes/Boogie_Down/Emote_Boogie_Down_CMF.Emote_Boogie_Down_CMF";

                    string boogiedownSmallIcon = "/BRCosmetics/UI/Foundation/Textures/Icons/Emotes/T-Icon-Emotes-E-BoogieDown.T-Icon-Emotes-E-BoogieDown";
                    string boogiedownIcon = "/BRCosmetics/UI/Foundation/Textures/Icons/Emotes/T-Icon-Emotes-E-BoogieDown-L.T-Icon-Emotes-E-BoogieDown-L";

                    var maleAnim = "";
                    var femaleAnim = "";
                    var smallImage = "";
                    var largeImage = "";

                    Logger.Log("Finding Animations");

                    if (toEmoteObject.Properties.Count(x => x.Name == "Animation") > 0)
                    {
                        toEmoteObject.TryGetValue(out FSoftObjectPath maleAnimObj, "Animation");
                        maleAnim = maleAnimObj.AssetPathName.ToString();
                    }

                    if (toEmoteObject.Properties.Count(x => x.Name == "AnimationFemaleOverride") > 0)
                    {
                        toEmoteObject.TryGetValue(out FSoftObjectPath femaleAnimObj, "AnimationFemaleOverride");
                        femaleAnim = femaleAnimObj.AssetPathName.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(maleAnim) && !string.IsNullOrWhiteSpace(femaleAnim))
                    {
                        maleAnim = femaleAnim;
                    }

                    if (string.IsNullOrWhiteSpace(femaleAnim) && !string.IsNullOrWhiteSpace(maleAnim))
                    {
                        femaleAnim = maleAnim;
                    }

                    if(string.IsNullOrWhiteSpace(maleAnim) || string.IsNullOrWhiteSpace(femaleAnim))
                    {
                        await Utils.Utils.MessageBox("No Animations were found for ID: " + currentitem.id);
                        return false;
                    }

                    Logger.Log("Animations:");
                    Logger.Log(maleAnim);
                    Logger.Log(femaleAnim);

                    Logger.Log("Getting Images");

                    toEmoteObject.TryGetValue(out FSoftObjectPath smallPreviewImage, "SmallPreviewImage");
                    toEmoteObject.TryGetValue(out FSoftObjectPath largePreviewImage, "LargePreviewImage");

                    smallImage = smallPreviewImage.AssetPathName.ToString();
                    largeImage = largePreviewImage.AssetPathName.ToString();

                    Dictionary<string, string> swaps = new();

                    swaps.Add(boogiedownMaleAnim.Split(".").FirstOrDefault(), maleAnim.Split(".").FirstOrDefault());
                    swaps.Add(boogiedownMaleAnim.Split(".").LastOrDefault(), maleAnim.Split(".").LastOrDefault());

                    swaps.Add(boogiedownFemaleAnim.Split(".").FirstOrDefault(), femaleAnim.Split(".").FirstOrDefault());
                    swaps.Add(boogiedownFemaleAnim.Split(".").LastOrDefault(), femaleAnim.Split(".").LastOrDefault());

                    swaps.Add(boogiedownSmallIcon.Split(".").FirstOrDefault(), smallImage.Split(".").FirstOrDefault());
                    swaps.Add(boogiedownSmallIcon.Split(".").LastOrDefault(), smallImage.Split(".").LastOrDefault());

                    swaps.Add(boogiedownIcon.Split(".").FirstOrDefault(), largeImage.Split(".").FirstOrDefault());
                    swaps.Add(boogiedownIcon.Split(".").LastOrDefault(), largeImage.Split(".").LastOrDefault());

                    Logger.Log("Changing Data");

                    boogieDownPackage.ChangeProtectedStrings(fromEmote.GetProtectedStrings());
                    boogieDownPackage.ChangePublicExportHash(fromEmote);

                    Logger.Log("Chaning Swaps");

                    foreach(var swap in swaps)
                    {
                        Logger.Log("Replacing " + swap.Key + " with " + swap.Value);

                        if(boogieDownPackage.NameMapAsStrings.ToList().Contains(swap.Key))
                        {

                            int index = boogieDownPackage.NameMapAsStrings.ToList().IndexOf(swap.Key);

                            boogieDownPackage.NameMapAsStrings[index] = swap.Value;

                        } else
                        {
                            Logger.Log("WARNING: String was not found: " + swap.Key);
                        }

                    }

                    Logger.Log("Replaced Strings");

                    var data = new Serializer(boogieDownPackage).Serialize();

                    if (!await SwapUtils.SwapAsset(fromEmote, data))
                        return false;

                    wroteAssets.Add(currentoption.path, data);

                } else
                {
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

                Utils.Utils.LogSwap(currentitem.id, true, currentoption.id);

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

                Utils.Utils.LogSwap(currentitem.id, false, currentoption.id);

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

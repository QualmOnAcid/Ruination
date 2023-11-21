using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
using WebviewAppShared.Utils;
using WebviewAppShared.Models;
using BlazorWpfApp.CUE4Parse;
using Microsoft.Extensions.FileProviders;

namespace WebviewAppShared.Swapper
{
    public class UEFN
    {

        public static async Task<bool> ConvertUEFN(ApiUEFNSkinObject character, Item option, DefaultFileProvider fileProvider)
        {

            return await Task.Run(async () =>
            {
                try
                {
                    Logger.Log("Converting UEFN -> " + option.name + " To " + character.Name);
                    Utils.Utils.MainWindow.LogText ="Loading Provider";
                    Utils.Utils.MainWindow.UpdateUI();
                    var prov = fileProvider;

                    Logger.Log("Unusedfile Count: " + prov.UnusedFiles.Count);
                    if (prov.UnusedFiles.Count == 0) return false;

                    Utils.Utils.MainWindow.LogText = "Checking if UEFN already swapped";
                    Utils.Utils.MainWindow.UpdateUI();

                    if(await IsUEFNAlreadySwapped())
                    {
                        Logger.LogError("UEFN is already Swapped");
                        await Utils.Utils.MessageBox("Looks like you already have a UEFN skin converted. Please revert it first");
                        Utils.Utils.MainWindow.LogText = "Waiting for Input";
                        Utils.Utils.MainWindow.UpdateUI();
                        return false;
                    }

                    Logger.Log("Preparing Assets");

                    Utils.Utils.MainWindow.LogText = "Preparing Assets";
                    Utils.Utils.MainWindow.UpdateUI();

                    List<KeyValuePair<string, string>> otherReplaces = new();

                    otherReplaces.Add(new("/Game/Characters/Player/Female/Medium/Bodies/F_Med_Soldier_01/Meshes/F_Med_Soldier_01_Skeleton_AnimBP.F_Med_Soldier_01_Skeleton_AnimBP_C", character.Animation));
                    otherReplaces.Add(new("/Game/Characters/Player/Female/Medium/Base/SK_M_Female_Base_Skeleton.SK_M_Female_Base_Skeleton", character.Skeleton));
                    otherReplaces.Add(new("/Game/Characters/Player/Female/Medium/Bodies/F_Med_Soldier_01/Meshes/F_Med_Soldier_01.F_Med_Soldier_01", character.Mesh));

                    string idleEffectNiagaraReplace = !string.IsNullOrEmpty(character.IdleEffectNiagara) ? character.IdleEffectNiagara : "/Game/B.C";
                    string partModifierBlueprintReplace = !string.IsNullOrEmpty(character.PartModifierBlueprint) ? character.PartModifierBlueprint : "/Game/B.C";

                    otherReplaces.Add(new("/BRCosmetics/Effects/Fort_Effects/Effects/Characters/Athena_Parts/RenegadeRaider_Fire/NS_RenegadeRaider_Fire.NS_RenegadeRaider_Fire", idleEffectNiagaraReplace));
                    otherReplaces.Add(new("/BRCosmetics/Athena/Cosmetics/Blueprints/Part_Modifiers/B_Athena_PartModifier_RenegadeRaider_Fire.B_Athena_PartModifier_RenegadeRaider_Fire_C", partModifierBlueprintReplace));

                    string replacement = "FortniteGame/Plugins/GameFeatures/BRCosmetics/Content/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_RenegadeRaiderFire";

                    Logger.Log("Loading Replacment package");

                    Utils.Utils.MainWindow.LogText ="Exporting Asset";
                    Utils.Utils.MainWindow.UpdateUI();

                    IoPackage replacementPackage = (IoPackage)await prov.LoadPackageAsync(replacement);

                    Utils.Utils.MainWindow.LogText ="Creaing new Namemap";
                    Utils.Utils.MainWindow.UpdateUI();

                    Logger.Log("Creating new Namemap");

                    List<KeyValuePair<int, int>> nameMapWithIndexes = new();

                    string frists = replacementPackage.NameMapAsStrings[^2];
                    string seconds = replacementPackage.NameMapAsStrings[^1];

                    var nameMapAsLis1 = replacementPackage.NameMapAsStrings.ToList();

                    replacementPackage.NameMapAsStrings = nameMapAsLis1.ToArray();

                    foreach (var mat in character.Materials)
                    {
                        int nameMapSize = replacementPackage.NameMapAsStrings.Length;
                        string first = mat.Split(".").FirstOrDefault();
                        string second = mat.Split(".").LastOrDefault();

                        var nameMapAsList = replacementPackage.NameMapAsStrings.ToList();
                        nameMapAsList.Add(first);
                        nameMapAsList.Add(second);

                        replacementPackage.NameMapAsStrings = nameMapAsList.ToArray();

                        nameMapWithIndexes.Add(new(nameMapSize, nameMapSize + 1));
                    }

                    var nameMapAsLis2 = replacementPackage.NameMapAsStrings.ToList();
                    nameMapAsLis2.Add(frists);
                    nameMapAsLis2.Add(seconds);

                    replacementPackage.NameMapAsStrings = nameMapAsLis2.ToArray();

                    List<KeyValuePair<string, string>> finalReplaces = new List<KeyValuePair<string, string>>();

                    foreach (var item in otherReplaces)
                    {
                        if (item.Key.Contains("."))
                        {

                            string searchBefore = item.Key.Split(".").FirstOrDefault();
                            string searchAfter = item.Key.Split(".").LastOrDefault();

                            string replaceBefore = "";
                            string replaceAfter = "";

                            if (item.Value.Contains("."))
                            {
                                replaceBefore = item.Value.Split(".").FirstOrDefault();
                                replaceAfter = item.Value.Split(".").LastOrDefault();
                            }
                            else
                            {
                                replaceBefore = item.Value;
                                replaceAfter = item.Value;
                            }

                            finalReplaces.Add(new(searchBefore, replaceBefore));
                            finalReplaces.Add(new(searchAfter, replaceAfter));

                        }
                        else
                        {
                            finalReplaces.Add(new(item.Key, item.Value));
                        }
                    }

                    Utils.Utils.MainWindow.LogText ="Finding Pakchunk";
                    Utils.Utils.MainWindow.UpdateUI();

                    var slot = prov.UnusedFiles[0];

                    Logger.Log("Using slot: " + slot);

                    Utils.Utils.MainWindow.LogText ="Finding Body Asset";
                    Utils.Utils.MainWindow.UpdateUI();

                    Logger.Log("Finding Body Asset");

                    string fromBodyAsset = await SwapUtils.GetBodyAssetForCharacter(prov, option);

                    if (fromBodyAsset == "")
                        return false;

                    Logger.Log("Using Body Asset: " + fromBodyAsset);

                    Logger.Log("Creating Body Part");

                    Utils.Utils.MainWindow.LogText ="Creating Body Part";
                    Utils.Utils.MainWindow.UpdateUI();

                    var bytes = CreateBodyPartWithMaterials(prov, nameMapWithIndexes, replacement);

                    replacementPackage.PropertiesWithExportMap = bytes.Skip(replacementPackage.PropertiesExportMapOffset)
                        .Take(bytes.Length).ToArray();
                    bytes.Skip(replacementPackage.PropertiesExportMapOffset).Take(bytes.Length).ToArray();

                    fromBodyAsset = fromBodyAsset.Split(".").FirstOrDefault();
                    IoPackage ioPackage = (IoPackage)await prov.LoadPackageAsync(fromBodyAsset);

                    var pathNameIndex = ioPackage.NameMapAsStrings.ToList().IndexOf(ioPackage.GetProtectedStrings()[1]);

                    char[] invalidNameMapString = ioPackage.NameMapAsStrings[pathNameIndex].ToCharArray();

                    for (int i = 0; i < invalidNameMapString.Length; i++)
                    {
                        invalidNameMapString[i] = '1';
                    }

                    ioPackage.NameMapAsStrings[pathNameIndex] = invalidNameMapString.ToString();

                    Logger.Log("Loading Fallback Asset");

                    Utils.Utils.MainWindow.LogText ="Exporting Fallback Asset";
                    Utils.Utils.MainWindow.UpdateUI();
                    string fallbackCharacter = "FortniteGame/Content/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Fallback";

                    IoPackage fallbackPackage = (IoPackage)await prov.LoadPackageAsync(fallbackCharacter);

                    Logger.Log("Loading CID Asset");
                    Utils.Utils.MainWindow.LogText ="Exporting CID Asset";
                    Utils.Utils.MainWindow.UpdateUI();
                    IoPackage cidPackage = (IoPackage)await prov.LoadPackageAsync(option.path);
                    var cidObject = await prov.LoadObjectAsync(option.path);

                    Dictionary<string, string> cidReplaces = new Dictionary<string, string>();

                    Utils.Utils.MainWindow.LogText ="Creating CID Replaces";
                    Utils.Utils.MainWindow.UpdateUI();

                    Logger.Log("Creating CID Replaces");

                    cidObject.TryGetValue(out UObject[] characterParts, "BaseCharacterParts");

                    var cidAssets = SwapUtils.GetCIDCharacterPartsSwaps(characterParts);

                    foreach (var item in cidAssets)
                    {
                        cidReplaces.Add(item.Value.Key.Split(".").FirstOrDefault(), item.Value.Value.Split(".").FirstOrDefault());
                        cidReplaces.Add(item.Value.Key.Split(".").LastOrDefault(), item.Value.Value.Split(".").LastOrDefault());
                    }

                    foreach (var VARIABLE in cidReplaces)
                    {

                        if (cidPackage.NameMapAsStrings.Contains(VARIABLE.Key))
                        {
                            int offset = cidPackage.NameMapAsStrings.ToList().IndexOf(VARIABLE.Key);
                            cidPackage.NameMapAsStrings[offset] = VARIABLE.Value;
                        }

                    }

                    Logger.Log("Downloading UEFN Files");

                    Utils.Utils.MainWindow.LogText ="Downloading UEFN Files";
                    Utils.Utils.MainWindow.UpdateUI();

                    using (WebClient wc = new WebClient())
                    {
                        await wc.DownloadFileTaskAsync(API.GetApi().UEFNFiles.pak, slot + ".pak");
                        await wc.DownloadFileTaskAsync(API.GetApi().UEFNFiles.sig, slot + ".sig");
                        await wc.DownloadFileTaskAsync(API.GetApi().UEFNFiles.ucas, slot + ".ucas");
                        await wc.DownloadFileTaskAsync(API.GetApi().UEFNFiles.utoc, slot + ".utoc");
                    }

                    Logger.Log("Creating new Serializesize");

                    Utils.Utils.MainWindow.LogText ="Creating new Serializesize";
                    Utils.Utils.MainWindow.UpdateUI();
                    
                    ulong customSerializeSize = replacementPackage.ExportMap[1].CookedSerialSize;

                    if (nameMapWithIndexes.Count > 1)
                    {
                        for (int i = 0; i < nameMapWithIndexes.Count - 1; i++)
                        {
                            //Just adding materials to the Asset - One Material Property = 26 Bytes
                            customSerializeSize += 26;
                        }
                    }

                    Logger.Log("Created new Serializesize: " + customSerializeSize);

                    Dictionary<string, byte[]> wroteAssets = new();

                    Utils.Utils.MainWindow.LogText ="Swapping Fallback Asset";
                    Utils.Utils.MainWindow.UpdateUI();

                    Logger.Log("Swapping Fallback Asset");

                    var fallbackChangedPackage = await ChangeUEFNPackageData(fallbackPackage, replacementPackage, finalReplaces, customSerializeSize);
                    var fallbackPackageBytes = new Serializer(fallbackChangedPackage).Serialize(customSerializeSize);
                    if (!await SwapUtils.SwapAsset(fallbackPackage, fallbackPackageBytes))
                    {
                        return false;
                    }

                    Logger.Log("Swapping Invalid Asset");

                    Utils.Utils.MainWindow.LogText ="Swapping Invalid Asset";
                    Utils.Utils.MainWindow.UpdateUI();

                    var bodyPackageBytes = new Serializer(ioPackage).Serialize();

                    Logger.Log("Swapping Body Asset");

                    if (!await SwapUtils.SwapAsset(ioPackage, bodyPackageBytes))
                        return false;

                    Utils.Utils.MainWindow.LogText ="Swapping CID Asset";
                    Utils.Utils.MainWindow.UpdateUI();

                    Logger.Log("Swapping CID Asset");

                    var cidPackageBytes = new Serializer(cidPackage).Serialize();

                    if (!await SwapUtils.SwapAsset(cidPackage, cidPackageBytes))
                        return false;

                    Logger.Log("Loading HID Asset");

                    var fromHidPackage = (IoPackage)await prov.LoadPackageAsync(option.definitionPath);
                    var toHidPackage = (IoPackage)await prov.LoadPackageAsync(character.hidpath);

                    if (!toHidPackage.ChangeProtectedStrings(fromHidPackage.GetProtectedStrings()))
                    {
                        return false;
                    }

                    toHidPackage.ChangePublicExportHash(fromHidPackage);

                    var data = new Serializer(toHidPackage).Serialize();

                    Logger.Log("Swapping HID Asset");

                    if (!await SwapUtils.SwapAsset(fromHidPackage, data))
                        return false;

                    wroteAssets.Add(fallbackCharacter, fallbackPackageBytes);
                    wroteAssets.Add(fromBodyAsset, bodyPackageBytes);
                    wroteAssets.Add(option.path, cidPackageBytes);
                    wroteAssets.Add(option.definitionPath, data);

                    var convertedItem = new ConvertedItem()
                    {
                        ID = character.ID,
                        OptionID = option.id,
                        Type = "uefn",
                        Assets = wroteAssets,
                        Name = option.name + " To " + character.Name,
                    };

                    Config.GetConfig().ConvertedItems.RemoveAll(x => x.OptionID.ToLower().Equals(option.id.ToLower()));
                    Config.GetConfig().ConvertedItems.Add(convertedItem);
                    Config.Save();

                    Utils.Utils.LogSwap(character.ID.Split("?????").FirstOrDefault());

                    Utils.Utils.MainWindow.LogText ="Converted";
                    Utils.Utils.MainWindow.UpdateUI();

                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    Utils.Utils.MessageBox("There was an error: " + e.Message);
                    Utils.Utils.MainWindow.LogText ="Waiting for Input";
                    return false;
                }
            });
        }

        public static async Task<bool> RevertUEFN(ApiUEFNSkinObject character, Item option, DefaultFileProvider prov)
        {
             try
             {
                Logger.Log("Reverting UEFN -> " + option.name);
                Logger.Log("Loading Fallback Package");
                IoPackage fallbackPackage = (IoPackage)await prov.LoadPackageAsync("FortniteGame/Content/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Fallback");

                Utils.Utils.MainWindow.LogText ="Exporting CID Asset";
                Utils.Utils.MainWindow.UpdateUI();

                Logger.Log("Loading CID Package");

                IoPackage cidPackage = (IoPackage)await prov.LoadPackageAsync(option.path);

                Logger.Log("Reverting CID Package");

                if (!await SwapUtils.RevertPackage(cidPackage))
                {
                    return false;
                }

                Logger.Log("Finding Body Asset");

                Utils.Utils.MainWindow.LogText ="Finding Body Asset";
                Utils.Utils.MainWindow.UpdateUI();

                string fromBodyAsset = await SwapUtils.GetBodyAssetForCharacter(prov, option);

                if (fromBodyAsset == "")
                    return false;

                fromBodyAsset = fromBodyAsset.Split(".").FirstOrDefault();

                Logger.Log("Found Body Asset: " + fromBodyAsset);

                Utils.Utils.MainWindow.LogText = "Reverting Body Asset";
                Utils.Utils.MainWindow.UpdateUI();

                Logger.Log("Loading Body Package");

                IoPackage bodyPackage = (IoPackage)await prov.LoadPackageAsync(fromBodyAsset);

                Logger.Log("Reverting Body Package");

                if (!await SwapUtils.RevertPackage(bodyPackage))
                {
                    return false;
                }

                Utils.Utils.MainWindow.LogText = "Reverting Fallback Asset";
                Utils.Utils.MainWindow.UpdateUI();

                Logger.Log("Reverting Fallback Package");

                if (!await SwapUtils.RevertPackage(fallbackPackage))
                {
                    return false;
                }

                Logger.Log("Loading HID Package");

                IoPackage hidPackage = (IoPackage)await prov.LoadPackageAsync(option.definitionPath);

                Utils.Utils.MainWindow.LogText = "Reverting HID Asset";
                Utils.Utils.MainWindow.UpdateUI();

                Logger.Log("Reverting HID Package");

                if (!await SwapUtils.RevertPackage(hidPackage))
                {
                    return false;
                }

                Config.GetConfig().ConvertedItems.RemoveAll(x => x.OptionID.ToLower().Equals(option.id.ToLower()));
                Config.Save();

                Utils.Utils.LogSwap(character.ID.Split("?????").FirstOrDefault());

                Utils.Utils.MainWindow.LogText ="Reverted";
                Utils.Utils.MainWindow.UpdateUI();

                return true;
            } catch(Exception e)
            {
                Logger.LogError(e.Message);
                WebviewAppShared.Utils.Utils.MessageBox("There was an error reverting: " + e.Message);
                Utils.Utils.MainWindow.LogText = "Waiting for Input";
                return false;
            }
        }

        private static async Task<IoPackage> ChangeUEFNPackageData(IoPackage ioPackage, IoPackage ioPackage1, List<KeyValuePair<string, string>> replaces, ulong customSerializeSize)
        {
            if (!ioPackage1.ChangeProtectedStrings(ioPackage.GetProtectedStrings()))
            {
                return null;
            }

            ioPackage1.ChangePublicExportHash(ioPackage);

            foreach (var item in replaces)
            {
                try
                {
                    if (ioPackage1.NameMapAsStrings.Contains(item.Key))
                    {
                        int index = ioPackage1.NameMapAsStrings.ToList().FindIndex(x => x.Equals(item.Key));
                        ioPackage1.NameMapAsStrings[index] = item.Value;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                }
            }

            return ioPackage1;

        }

        public static byte[] CreateBodyPartWithMaterials(DefaultFileProvider fileProvider, List<KeyValuePair<int, int>> materialsWithNamemapIndex, string baseCharacterPart)
        {
            DefaultFileProvider provider = fileProvider;
            var obj = provider.LoadObject(baseCharacterPart);
            List<byte> assetBytes =
                provider.SaveAsset(
                    baseCharacterPart).ToList();

            var materialProperty = obj.Properties.FirstOrDefault(x => x.Name.Text.Equals("MaterialOverrides"));
            var materialOverrideFlagsProperty =
                obj.Properties.FirstOrDefault(x => x.Name.Text.Equals("MaterialOverrideFlags"));

            if (materialProperty == null)
            {
                throw new Exception("Could not find MaterialOverrides Property");
            }

            if (materialOverrideFlagsProperty == null)
            {
                throw new Exception("Could not find MaterialOverrideFlags Property");
            }

            assetBytes.RemoveAt(materialProperty.Position);
            assetBytes.Insert(materialProperty.Position, (byte)materialsWithNamemapIndex.Count);

            int materialArrayStartOffset = materialProperty.Position + 5;

            assetBytes.RemoveRange(materialOverrideFlagsProperty.Position, materialOverrideFlagsProperty.Size);

            byte[] materialOverrideFlagsBytes = new byte[]
            {
                (byte) API.GetApi().AssetUtils.MaterialOverrideFlags
            };

            Array.Resize(ref materialOverrideFlagsBytes, materialOverrideFlagsProperty.Size);

            int remainingSize = assetBytes.Count - materialOverrideFlagsProperty.Position;

            assetBytes.InsertRange(materialOverrideFlagsProperty.Position, materialOverrideFlagsBytes);

            assetBytes.RemoveRange(materialArrayStartOffset, materialProperty.Size - 5);

            int materialIndex = 0;
            int assetBytesPosition = materialArrayStartOffset;

            foreach (var materialToInsert in materialsWithNamemapIndex)
            {
                int firstNamemapIndex = materialToInsert.Key;
                int secondNamemapIndex = materialToInsert.Value;

                List<byte> materialBytes = new()
                {
                    0,
                    5,
                    (byte) materialIndex,
                    0,
                    0,
                    0,
                    (byte) firstNamemapIndex,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    (byte) secondNamemapIndex,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0
                };

                if (materialIndex == 0)
                {
                    materialBytes.RemoveRange(2, 4);
                    //Change header to skip 0
                    materialBytes[0] = 5;
                    materialBytes[1] = 1;
                }

                assetBytes.InsertRange(assetBytesPosition, materialBytes);

                assetBytesPosition += materialBytes.Count;

                materialIndex++;
            }

            return assetBytes.ToArray();
        }

        private static async Task<bool> IsUEFNAlreadySwapped()
        {
            try
            {

                var provider = SwapUtils.GetProvider();

                IoPackage fallbackPackage = (IoPackage)await provider.LoadPackageAsync("FortniteGame/Content/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Fallback");

                return fallbackPackage.LoadAlreadySwapped();
            } catch(Exception e)
            {
                //Returning true just to give user information
                return true;
            }
        }

    }

}

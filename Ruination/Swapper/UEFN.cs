using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using Ruination_v2.Models;
using Ruination_v2.Utils;
using Ruination_v2.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace Ruination_v2.Swapper
{
    public class UEFN
    {

        public static async Task<bool> ConvertUEFN(ApiUEFNSkinObject character, Item option, DefaultFileProvider fileProvider, UEFNSkinSwapForm lab, bool downloadUefnFiles = true, bool isPlugin = false)
        {

            return await Task.Run(async () =>
            {
                try
                {
                    Logger.Log("Converting UEFN -> " + option.name + " To " + character.Name);
                    lab.updatetext("Loading Provider");
                    
                    var prov = fileProvider;

                    Logger.Log("Unusedfile Count: " + prov.UnusedFiles.Count);
                    if (prov.UnusedFiles.Count == 0)
                    {
                        Logger.LogError("No Unusedfile was found.");
                        System.Windows.MessageBox.Show("No unusedfile was found. Please verify Fortnite to fix this error.");
                        lab.updatetext("Waiting for Input");
                        
                        return false;
                    }

                    lab.updatetext("Checking if UEFN already swapped");

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
                            lab.updatetext("Waiting for Input");

                            return false;
                        }
                    }

                    Logger.Log("Preparing Assets");

                    lab.updatetext("Preparing Assets");

                    List<KeyValuePair<string, string>> otherReplaces = new();

                    otherReplaces.Add(new("/Game/Characters/Player/Female/Medium/Bodies/F_Med_Soldier_01/Meshes/F_Med_Soldier_01_Skeleton_AnimBP.F_Med_Soldier_01_Skeleton_AnimBP_C", character.Animation));
                    otherReplaces.Add(new("/BRCosmetics/Characters/Player/Female/Medium/Bodies/F_MED_Eternity_Elite/Meshes/F_MED_Eternity_Elite_AnimBP.F_MED_Eternity_Elite_AnimBP_C", character.Animation));
                    otherReplaces.Add(new("/Game/Characters/Player/Female/Medium/Base/SK_M_Female_Base_Skeleton.SK_M_Female_Base_Skeleton", character.Skeleton));
                    otherReplaces.Add(new("/Game/Characters/Player/Female/Medium/Bodies/F_Med_Soldier_01/Meshes/F_Med_Soldier_01.F_Med_Soldier_01", character.Mesh));
                    otherReplaces.Add(new("/BRCosmetics/Characters/Player/Female/Medium/Bodies/F_MED_Eternity_Elite/Meshes/F_MED_Eternity_Elite.F_MED_Eternity_Elite", character.Mesh));

                    string idleEffectNiagaraReplace = !string.IsNullOrEmpty(character.IdleEffectNiagara) ? character.IdleEffectNiagara : "/Game/B.C";
                    string partModifierBlueprintReplace = !string.IsNullOrEmpty(character.PartModifierBlueprint) ? character.PartModifierBlueprint : "/Game/B.C";
                    string IdleFXSocket = !string.IsNullOrEmpty(character.IdleFXSocket) ? character.IdleFXSocket : "root";

                    otherReplaces.Add(new("/BRCosmetics/Effects/Fort_Effects/Effects/Characters/Athena_Parts/RenegadeRaider_Fire/NS_RenegadeRaider_Fire.NS_RenegadeRaider_Fire", idleEffectNiagaraReplace));
                    otherReplaces.Add(new("/BRCosmetics/Athena/Cosmetics/Blueprints/Part_Modifiers/B_Athena_PartModifier_RenegadeRaider_Fire.B_Athena_PartModifier_RenegadeRaider_Fire_C", partModifierBlueprintReplace));
                    otherReplaces.Add(new("root", IdleFXSocket));

                    otherReplaces.Add(new("/BRCosmetics/Characters/Player/Female/Medium/Bodies/F_MED_Eternity_Elite/FX/NS_Eternity_Body.NS_Eternity_Body", idleEffectNiagaraReplace));
                    otherReplaces.Add(new("/Game/Athena/Cosmetics/Blueprints/B_Athena_PartModifier_Generic.B_Athena_PartModifier_Generic_C", partModifierBlueprintReplace));
                    otherReplaces.Add(new("/BRCosmetics/Effects/Fort_Effects/Effects/Characters/Athena_Parts/Gingy/P_Gingy_BurntBody.P_Gingy_BurntBody", idleEffectNiagaraReplace));

                    string replacement = "FortniteGame/Plugins/GameFeatures/BRCosmetics/Content/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_RenegadeRaiderFire";
                    if (character.Materials.Count == 0)
                        replacement = "/BRCosmetics/Athena/Heroes/Meshes/Bodies/CP_Body_Commando_F_Eternity_Elite";

                    if(character.UseIdleEffectPackage)
                    {
                        replacement = "/BRCosmetics/Athena/Heroes/Meshes/Bodies/CP_commando_Gingerbread_F";
                    }

                    Logger.Log("Loading Replacment package");

                    lab.updatetext("Exporting Asset");

                    IoPackage replacementPackage = (IoPackage)await prov.LoadPackageAsync(replacement);

                    lab.updatetext("Creaing new Namemap");

                    Logger.Log("Creating new Namemap");

                    List<KeyValuePair<int, int>> nameMapWithIndexes = new();

                    string frists = replacementPackage.NameMapAsStrings[^2];
                    string seconds = replacementPackage.NameMapAsStrings[^1];

                    var nameMapAsLis1 = replacementPackage.NameMapAsStrings.ToList();

                    replacementPackage.NameMapAsStrings = nameMapAsLis1.ToArray();

                    foreach (var mat in character.Materials)
                    {
                        int nameMapSize = replacementPackage.NameMapAsStrings.Length;
                        string first = mat.SubstringBeforeLast(".");
                        string second = mat.SubstringBeforeLast(".").SubstringAfterLast("/");

                        var nameMapAsList = replacementPackage.NameMapAsStrings.ToList();
                        nameMapAsList.Add(first);
                        nameMapAsList.Add(second);

                        replacementPackage.NameMapAsStrings = nameMapAsList.ToArray();

                        nameMapWithIndexes.Add(new(nameMapSize, nameMapSize + 1));
                    }

                    if(character.Materials.Count == 0)
                    {
                        nameMapWithIndexes.Add(new(-1, -1));
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

                    lab.updatetext("Finding Pakchunk");

                    var slot = prov.UnusedFiles[0];

                    Logger.Log("Using slot: " + slot);

                    bool isDefaultSwap = option.isDefault;

                    Logger.Log("Creating Body Part");

                    lab.updatetext("Creating Body Part");
                    
                    if(character.Materials.Count > 0)
                    {
                        var bytes = CreateBodyPartWithMaterials(prov, nameMapWithIndexes, replacement);

                        replacementPackage.PropertiesWithExportMap = bytes.Skip(replacementPackage.PropertiesExportMapOffset)
                            .Take(bytes.Length).ToArray();
                    }

                    if (!isDefaultSwap)
                    {
                        if (await SwapUtils.GetBodyAssetForCharacter(prov, option) == "")
                        {
                            Logger.LogError("Could not find Body Asset");
                            return false;
                        }
                    }

                    Logger.Log("Loading Fallback Asset");

                    lab.updatetext("Exporting Fallback Asset");
                    
                    string fallbackCharacter = "FortniteGame/Content/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Fallback";

                    IoPackage fallbackPackage = (IoPackage)await prov.LoadPackageAsync(fallbackCharacter);

                    if (downloadUefnFiles)
                    {
                        Logger.Log("Downloading UEFN Files");

                        lab.updatetext("Downloading UEFN Files");

                        string pak = character.OverrideFiles != null
                            ? character.OverrideFiles.pak
                            : API.GetApi().UEFNFiles.pak;
                        string sig = character.OverrideFiles != null
                            ? character.OverrideFiles.sig
                            : API.GetApi().UEFNFiles.sig;
                        string ucas = character.OverrideFiles != null
                            ? character.OverrideFiles.ucas
                            : API.GetApi().UEFNFiles.ucas;
                        string utoc = character.OverrideFiles != null
                            ? character.OverrideFiles.utoc
                            : API.GetApi().UEFNFiles.utoc;

                        if (character.ChunkedFile != null)
                        {
                            var chunkedFile = API.GetApi().ChunkedFiles.FirstOrDefault(x => x.Name == character.ChunkedFile);

                            if (chunkedFile != null)
                            {
                                pak = chunkedFile.pak;
                                sig = chunkedFile.sig;
                                ucas = chunkedFile.ucas;
                                utoc = chunkedFile.utoc;
                            }
                        }

                        using (WebClient wc = new WebClient())
                        {
                            await wc.DownloadFileTaskAsync(pak, slot + ".pak");
                            await wc.DownloadFileTaskAsync(sig, slot + ".sig");
                            await wc.DownloadFileTaskAsync(utoc, slot + ".utoc");

                            //Move file for uefnprovider to load it
                            string backupfolder = Ruination_v2.Utils.Utils.AppDataFolder + "\\Backups\\";
                            string backuppath = backupfolder + Path.GetFileName(slot + ".utoc");
                            File.Copy(slot + ".utoc",  backuppath, true);

                            wc.DownloadProgressChanged += (sender, e) =>
                            {
                                lab.updatetext($"Downloading UEFN Files ({e.ProgressPercentage}%)");
                            };

                            await wc.DownloadFileTaskAsync(ucas, slot + ".ucas");
                        }
                    }

                    Logger.Log("Creating new Serializesize");

                    lab.updatetext("Creating new Serializesize");

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

                    Logger.Log("Loading Texture Swaps");
                    lab.updatetext("Loading Texture Swaps");
                    

                    Dictionary<string, byte[]> wroteAssets = new();

                    lab.updatetext("Swapping Fallback Asset");
                    

                    Logger.Log("Swapping Fallback Asset");

                    var fallbackChangedPackage = await ChangeUEFNPackageData(fallbackPackage, replacementPackage, finalReplaces, customSerializeSize);
                    var fallbackPackageBytes = new Serializer(fallbackChangedPackage).Serialize(customSerializeSize);
                    if (!await SwapUtils.SwapAsset(fallbackPackage, fallbackPackageBytes))
                    {
                        return false;
                    }

                    if (!isDefaultSwap)
                    {
                        lab.updatetext("Finding Body Asset");
                        

                        Logger.Log("Finding Body Asset");

                        string fromBodyAsset = await SwapUtils.GetBodyAssetForCharacter(prov, option);

                        if (fromBodyAsset == "")
                            return false;

                        Logger.Log("Using Body Asset: " + fromBodyAsset);

                        fromBodyAsset = fromBodyAsset.Split(".").FirstOrDefault();
                        IoPackage ioPackage = (IoPackage)await prov.LoadPackageAsync(fromBodyAsset);

                        var pathNameIndex = ioPackage.NameMapAsStrings.ToList().IndexOf(ioPackage.GetProtectedStrings()[1]);

                        char[] invalidNameMapString = ioPackage.NameMapAsStrings[pathNameIndex].ToCharArray();

                        for (int i = 0; i < invalidNameMapString.Length; i++)
                        {
                            invalidNameMapString[i] = '1';
                        }

                        ioPackage.NameMapAsStrings[pathNameIndex] = new string(invalidNameMapString);

                        Logger.Log("Loading CID Asset");
                        lab.updatetext("Exporting CID Asset");
                        
                        IoPackage cidPackage = (IoPackage)await prov.LoadPackageAsync(option.path);
                        var cidObject = await prov.LoadObjectAsync(option.path);

                        Dictionary<string, string> cidReplaces = new Dictionary<string, string>();

                        lab.updatetext("Creating CID Replaces");
                        

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

                        Logger.Log("Swapping Invalid Asset");

                        lab.updatetext("Swapping Invalid Asset");

                        var bodyPackageBytes = new Serializer(ioPackage).Serialize();

                        Logger.Log("Swapping Body Asset");

                        if (!await SwapUtils.SwapAsset(ioPackage, bodyPackageBytes))
                            return false;

                        lab.updatetext("Swapping CID Asset");
                        

                        Logger.Log("Swapping CID Asset");

                        var cidPackageBytes = new Serializer(cidPackage).Serialize();
                        
                        if (option.name.Length >= character.Name.Length)
                        {
                            byte[] nameSearch = Encoding.ASCII.GetBytes(option.name);
                            byte[] nameReplace = Encoding.ASCII.GetBytes(character.Name);
                        
                            Array.Resize(ref nameReplace, nameSearch.Length);

                            int offset = Utils.Utils.FindOffset(cidPackageBytes, nameSearch);

                            if (offset != -1)
                            {
                                var dataAsList = cidPackageBytes.ToList();
                            
                                dataAsList.RemoveRange(offset, nameSearch.Length);
                                dataAsList.InsertRange(offset, nameReplace);

                                cidPackageBytes = dataAsList.ToArray();
                            }
                        }
                        
                        if (option.description.Length >= character.Description.Length)
                        {
                            byte[] nameSearch = Encoding.ASCII.GetBytes(option.description);
                            byte[] nameReplace = Encoding.ASCII.GetBytes(character.Description);
                        
                            Array.Resize(ref nameReplace, nameSearch.Length);

                            int offset = Utils.Utils.FindOffset(cidPackageBytes, nameSearch);

                            if (offset != -1)
                            {
                                var dataAsList = cidPackageBytes.ToList();
                            
                                dataAsList.RemoveRange(offset, nameSearch.Length);
                                dataAsList.InsertRange(offset, nameReplace);

                                cidPackageBytes = dataAsList.ToArray();
                            }
                        }

                        if (!await SwapUtils.SwapAsset(cidPackage, cidPackageBytes))
                            return false;

                        if (character.hidpath != null)
                        {
                            Logger.Log("Loading HID Asset");

                            var fromHidPackage = (IoPackage)await prov.LoadPackageAsync(option.definitionPath);
                            var toHidPackage = (IoPackage)await prov.LoadPackageAsync(character.hidpath);

                            if (!toHidPackage.ChangeProtectedStrings(fromHidPackage.GetProtectedStrings()))
                            {
                                return false;
                            }

                            toHidPackage.ChangeLastPublicExportHash(fromHidPackage);

                            var data = new Serializer(toHidPackage).Serialize();

                            Logger.Log("Swapping HID Asset");

                            if (!await SwapUtils.SwapAsset(fromHidPackage, data))
                                return false;

                            wroteAssets.Add(option.definitionPath, data);
                        }

                        wroteAssets.Add(fromBodyAsset, bodyPackageBytes);
                        wroteAssets.Add(option.path, cidPackageBytes);
                    }
                    else
                    {

                        Logger.Log("Swapping Default CPS");

                        int defaultCPSwappedIndex = 0;
                        foreach (var defaultCP in GetDefaultCPS())
                        {
                            defaultCPSwappedIndex++;
                            Logger.Log("Swapping DefaultCP -> " + defaultCP);
                            lab.updatetext("Swapping Default Asset " + defaultCPSwappedIndex);
                            
                            var defaultPackage = (IoPackage)await prov.LoadPackageAsync(defaultCP);

                            string packageName = defaultPackage.GetProtectedStrings()[1];

                            int namemapindex = defaultPackage.NameMapAsStrings.ToList().IndexOf(packageName);

                            defaultPackage.NameMapAsStrings[namemapindex] = packageName.Replace("_", "?");

                            var defaultPackageData = new Serializer(defaultPackage).Serialize();

                            if (!await SwapUtils.SwapAsset(defaultPackage, defaultPackageData)) return false;

                            wroteAssets.Add(defaultCP, defaultPackageData);
                        }

                    }

                    wroteAssets.Add(fallbackCharacter, fallbackPackageBytes);

                    if (character.TextureSwaps != null && character.TextureSwaps.Count > 0)
                    {
                        //New FileProvider for inserted UEFN assets

                        Logger.Log("Loading UEFN Provider");
                        lab.updatetext("Loading UEFN Provider");
                        
                        var uefnprovider = await SwapUtils.GetUEFNProvider(System.IO.Path.GetFileNameWithoutExtension(slot));

                        Logger.Log("Loaded UEFN Provider with " + uefnprovider.Files.Count + " files");
                        lab.updatetext("Loaded UEFN Provider");
                        
                        int textureSwappedIndex = 0;

                        foreach (var textureSwap in character.TextureSwaps)
                        {
                            Logger.Log("Swapping Texture: " + textureSwap.From + " -> " + textureSwap.To);
                            textureSwappedIndex++;

                            lab.updatetext("Swapping Texture " + textureSwappedIndex);
                            
                            var fromPack = (IoPackage)await prov.LoadPackageAsync(textureSwap.From);

                            bool existsInUefn = uefnprovider.DoesAssetExist(textureSwap.To);
                            IoPackage toPack = null;

                            if (existsInUefn)
                            {
                                toPack = (IoPackage)await uefnprovider.LoadPackageAsync(textureSwap.To);
                            }
                            else
                            {
                                toPack = (IoPackage)await prov.LoadPackageAsync(textureSwap.To);
                            }

                            toPack.ChangeProtectedStrings(fromPack.GetProtectedStrings());
                            toPack.ChangeLastPublicExportHash(fromPack);

                            if (textureSwap.Swaps != null && textureSwap.Swaps.Count > 0)
                            {
                                //Format strings with aa.bb

                                for (int i = 0; i < textureSwap.Swaps.Count; i++)
                                {
                                    if (!textureSwap.Swaps[i].Type.ToLower().Equals("string")) continue;

                                    string search = textureSwap.Swaps[i].Search;
                                    string replace = textureSwap.Swaps[i].Replace;

                                    if (!search.Contains(".") || !replace.Contains(".")) continue;

                                    string s1 = search.Split(".").FirstOrDefault();
                                    string s2 = search.Split(".").LastOrDefault();

                                    string r1 = replace.Split(".").FirstOrDefault();
                                    string r2 = replace.Split(".").LastOrDefault();

                                    textureSwap.Swaps[i].Search = s1;
                                    textureSwap.Swaps[i].Replace = r1;

                                    textureSwap.Swaps.Add(new()
                                    {
                                        Type = "string",
                                        Search = s2,
                                        Replace = r2
                                    });
                                }

                                foreach (var s in textureSwap.Swaps)
                                {
                                    try
                                    {
                                        if (s.Type.ToLower().Equals("string"))
                                        {

                                            if (toPack.NameMapAsStrings.ToList().Contains(s.Search))
                                            {
                                                int index = toPack.NameMapAsStrings.ToList().IndexOf(s.Search);

                                                toPack.NameMapAsStrings[index] = s.Replace;

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

                            var toPackData = new Serializer(toPack).Serialize();

                            if (textureSwap.Swaps != null)
                            {
                                foreach (var s in textureSwap.Swaps.Where(x => x.Type.ToLower().Equals("hex")))
                                {
                                    Logger.Log("SWAPPING HEX: " + s.Search);
                                    byte[] searchByte = Utils.Utils.HexToByte(s.Search);
                                    byte[] replaceByte = Utils.Utils.HexToByte(s.Replace);

                                    if (replaceByte.Length != searchByte.Length)
                                        continue;

                                    if (replaceByte.Length < searchByte.Length)
                                        Array.Resize(ref replaceByte, searchByte.Length);


                                    int offset = Utils.Utils.FindOffset(toPackData, searchByte);

                                    if (offset == -1)
                                    {
                                        Logger.Log("Byte Array not found.");
                                        continue;
                                    }

                                    var dataAsList = new List<byte>(toPackData);
                                    dataAsList.RemoveRange(offset, searchByte.Length);
                                    dataAsList.InsertRange(offset, replaceByte);
                                    toPackData = dataAsList.ToArray();
                                }   
                            }

                            if (!await SwapUtils.SwapAsset(fromPack, toPackData)) return false;

                            wroteAssets.Add(textureSwap.From, toPackData);
                        }

                        Logger.Log("Disposing UEFN Provider");

                        lab.updatetext("Disposing UEFN Provider");

                        uefnprovider.Dispose();
                        GC.Collect();
                    }

                    var convertedItem = new ConvertedItem()
                    {
                        ID = character.ID,
                        OptionID = option.id,
                        Type = "uefn",
                        Assets = wroteAssets,
                        isPlugin = isPlugin,
                        Name = option.name + " To " + character.Name,
                    };

                    Config.GetConfig().ConvertedItems.RemoveAll(x => x.OptionID.ToLower().Equals(option.id.ToLower()));
                    Config.GetConfig().ConvertedItems.Add(convertedItem);
                    Config.Save();

                    Utils.Utils.LogUEFNSwap(character.ID, true, option.id);

                    lab.updatetext("Converted");
                    

                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    if (e.Message.Contains("Index was outside the bounds"))
                    {
                        System.Windows.MessageBox.Show("There was an error: " + e.Message + "\n\nPlease go to settings and click Verify Fortnite to solve this issue.", "Ruination", MessageBoxButton.OK, MessageBoxImage.Error);   
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("There was an error: " + e.Message + "\n\nMake sure Fortnite is closed.", "Ruination", MessageBoxButton.OK, MessageBoxImage.Error);   
                    }
                    lab.updatetext("Waiting for Input");
                    return false;
                }
            });
        }

        private static async Task<IoPackage> ChangeUEFNPackageData(IoPackage ioPackage, IoPackage ioPackage1, List<KeyValuePair<string, string>> replaces, ulong customSerializeSize)
        {
            if (!ioPackage1.ChangeProtectedStrings(ioPackage.GetProtectedStrings()))
            {
                return null;
            }

            ioPackage1.ChangeLastPublicExportHash(ioPackage);

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

            int materialOverrideFlags = 1;

            if (materialsWithNamemapIndex.Count > 0)
                materialOverrideFlags = (int)Math.Pow(2, materialsWithNamemapIndex.Count) - 1;

            byte[] materialOverrideFlagsBytes = new byte[]
            {
                (byte) materialOverrideFlags
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

        private static List<string> GetDefaultCPS()
        {
            return new()
            {
                "/BRCosmetics/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Prime",
                "/Game/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Prime_A",
                "/BRCosmetics/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Prime_B",
                "/BRCosmetics/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Prime_C",
                "/BRCosmetics/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Prime_E",
                "/BRCosmetics/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Prime_G",
                "/BRCosmetics/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_M_Prime",
                "/BRCosmetics/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_M_Prime_G"
            };
        }

        public static async Task<bool> RevertUEFN(ApiUEFNSkinObject character, Item option, UEFNSkinSwapForm lab)
        {
            try
            {
                Logger.Log("Reverting UEFN -> " + option.name);

                var prov = SwapUtils.GetProvider();

                Logger.Log("Loading Fallback Package");
                IoPackage fallbackPackage = (IoPackage)await prov.LoadPackageAsync("FortniteGame/Content/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Fallback");

                if (!option.isDefault)
                {
                    lab.updatetext("Exporting CID Asset");

                    Logger.Log("Loading CID Package");

                    IoPackage cidPackage = (IoPackage)await prov.LoadPackageAsync(option.path);

                    Logger.Log("Reverting CID Package");

                    if (!await SwapUtils.RevertPackage(cidPackage))
                    {
                        return false;
                    }

                    Logger.Log("Finding Body Asset");

                    lab.updatetext("Finding Body Asset");
                    

                    string fromBodyAsset = await SwapUtils.GetBodyAssetForCharacter(prov, option);

                    if (fromBodyAsset == "")
                        return false;

                    fromBodyAsset = fromBodyAsset.Split(".").FirstOrDefault();

                    Logger.Log("Found Body Asset: " + fromBodyAsset);

                    lab.updatetext("Reverting Body Asset");

                    Logger.Log("Loading Body Package");

                    IoPackage bodyPackage = (IoPackage)await prov.LoadPackageAsync(fromBodyAsset);

                    Logger.Log("Reverting Body Package");

                    if (!await SwapUtils.RevertPackage(bodyPackage))
                    {
                        return false;
                    }

                    if (character.hidpath != null)
                    {
                        Logger.Log("Loading HID Package");

                        IoPackage hidPackage = (IoPackage)await prov.LoadPackageAsync(option.definitionPath);

                        lab.updatetext("Reverting HID Asset");
                        

                        Logger.Log("Reverting HID Package");

                        if (!await SwapUtils.RevertPackage(hidPackage))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    foreach (var defaultcp in GetDefaultCPS())
                    {
                        Logger.Log("Reverting -> " + defaultcp);
                        IoPackage defaultcpPackage = (IoPackage)await prov.LoadPackageAsync(defaultcp);
                        if (!await SwapUtils.RevertPackage(defaultcpPackage)) return false;
                    }
                }

                lab.updatetext("Reverting Fallback Asset");

                Logger.Log("Reverting Fallback Package");

                if (!await SwapUtils.RevertPackage(fallbackPackage))
                {
                    return false;
                }

                if (character.TextureSwaps != null && character.TextureSwaps.Count > 0)
                {

                    foreach (var item in character.TextureSwaps)
                    {
                        Logger.Log("Reverting Texture: " + item.From);
                        var fromTexture = (IoPackage)await prov.LoadPackageAsync(item.From);
                        if (!await SwapUtils.RevertPackage(fromTexture)) return false;
                    }

                }

                Config.GetConfig().ConvertedItems.RemoveAll(x => x.Type == "uefn");
                Config.Save();

                Utils.Utils.LogUEFNSwap(character.ID, false, option.id);

                lab.updatetext("Reverted");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
                             System.Windows.MessageBox.Show("There was an error: " + e.Message + "\n\nMake sure Fortnite is closed.", "Ruination", MessageBoxButton.OK, MessageBoxImage.Error);  
                lab.updatetext("Waiting for Input");
                return false;
            }
        }

    }

}


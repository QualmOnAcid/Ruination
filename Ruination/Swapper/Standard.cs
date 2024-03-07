using CUE4Parse;
using CUE4Parse.GameTypes.PUBG.Assets.Exports;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using Ruination_v2.Models;
using Ruination_v2.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.Utils;

namespace Ruination_v2.Swapper
{
    public class Standard
    {

        public static async Task<bool> Convert(Item currentitem, Item currentoption, Label lab)
        {
            try
            {
                Logger.Log("Converting Standard -> " + currentoption.name + " To " + currentitem.name);
                lab.Content = "Loading Provider";
                var prov = SwapUtils.GetProvider();
                lab.Content = "Preparing Assets";

                Logger.Log("Preparing Assets");

                var assetStrings = await SwapUtils.GetAssetsForSwap(currentitem, currentoption);

                Logger.Log("Found Assets: " + JsonConvert.SerializeObject(assetStrings));

                if (assetStrings.Count == 0)
                {
                    MessageBox.Show("Swapper could not find any Assets to swap", "Error");
                    lab.Content = "Waiting for Input";
                    return false;
                }

                Dictionary<string, byte[]> wroteAssets = new();

                bool doEmoteSwap = currentitem.Type == ItemType.EMOTE && currentoption.Type == ItemType.EMOTE;

                bool doPickaxeCustomSwap = false;

                if(currentitem.Type == ItemType.PICKAXE && currentoption.Type == ItemType.PICKAXE)
                {
                    if(currentitem.series != currentoption.series)
                    {
                        doPickaxeCustomSwap = true;
                    }

                    if (currentitem.backendReleaseValue >= 28 || currentoption.backendReleaseValue >= 28)
                    {
                        doPickaxeCustomSwap = true;
                    }
                }

                if(doPickaxeCustomSwap)
                {
                    lab.Content = "Getting Pickaxe Data";

                    string pickaxePath = currentoption.definitionPath;

                    if (API.GetApi().PickaxePathOverrides.Any(x => x.ID == currentitem.id))
                    {
                        var pickaxeOverride = API.GetApi().PickaxePathOverrides.First(x => x.ID == currentitem.id);
                        
                        pickaxePath = pickaxeOverride.Override;
                        Logger.Log("Using Override Pickaxe");
                    }
                    
                    IoPackage fromPackage = (IoPackage)await prov.LoadPackageAsync(currentoption.definitionPath);
                    IoPackage fromPackage1 = (IoPackage)await prov.LoadPackageAsync(pickaxePath);

                    fromPackage1.ChangeProtectedStrings(fromPackage.GetProtectedStrings());
                    fromPackage1.ChangeLastPublicExportHash(fromPackage);
                    
                    var data = await GetPickaxedata(fromPackage1, currentitem, currentoption, pickaxePath);

                    lab.Content = "Swapping Asset";

                    if (!await SwapUtils.SwapAsset(fromPackage, data))
                        return false;

                    lab.Content = "Swapped Asset";

                    wroteAssets.Add(currentoption.definitionPath, data);

                } else if (doEmoteSwap)
                {
                    string option = currentoption.path;
                    IoPackage pack = (IoPackage) await prov.LoadPackageAsync(option);
                    UObject optionObject = await prov.LoadObjectAsync(option);
                    UObject toEmoteObject = await prov.LoadObjectAsync(currentitem.path);
                    IoPackage fromPackage = (IoPackage)await prov.LoadPackageAsync(currentoption.path);
                    List<string> PropertiesToSwap = new List<string>()
                    {
                        "LargePreviewImage",
                        "SmallPreviewImage",
                        "Animation",
                        "AnimationFemaleOverride",
                     //   "Rarity"
                    };
                    
                    ulong serialSize = pack.ExportMap[0].CookedSerialSize;

                    int assetDifference = 0;

                    FPropertyTag toMaleAnimation = null;
                    FPropertyTag toFemaleAnimation = null;

                    Dictionary<string, string> packageReplaces = new();

                    foreach(var prop in PropertiesToSwap)
                    {
                        var fromProp = optionObject.Properties.FirstOrDefault(x => x.Name == prop);
                        var toProp = toEmoteObject.Properties.FirstOrDefault(x => x.Name == prop);

                        if(fromProp == null || toProp == null)
                        {
                            //Some emotes only have one property like Floss
                            if(prop == "Animation" && toEmoteObject.Properties.Any(x => x.Name == "AnimationFemaleOverride"))
                            {
                                toProp = toEmoteObject.Properties.FirstOrDefault(x => x.Name == "AnimationFemaleOverride");
                            } else if (prop == "AnimationFemaleOverride" && toEmoteObject.Properties.Any(x => x.Name == "Animation"))
                            {
                                toProp = toEmoteObject.Properties.FirstOrDefault(x => x.Name == "Animation");
                            } else
                            {
                                Logger.Log("WARNING: Skipping Property beacuse not found: " + prop);
                                continue;
                            }
                        }

                        if (prop == "Animation")
                            toMaleAnimation = toProp;
                        else if (prop == "AnimationFemaleOverride")
                            toFemaleAnimation = toProp;

                        Logger.Log("Swapping Property " + prop);

                        if (fromProp == null)
                            continue;

                        if(fromProp.PropertyType.ToString().Equals("SoftObjectProperty"))
                        {
                            string fromString = fromProp.Tag.GenericValue.ToString();
                            string toString = toProp.Tag.GenericValue.ToString();

                            string s1 = fromString.Split(".").FirstOrDefault();
                            string s2 = fromString.Split(".").LastOrDefault();

                            string r1 = toString.Split(".").FirstOrDefault();
                            string r2 = toString.Split(".").LastOrDefault();

                            if(!packageReplaces.ContainsKey(s1))
                                packageReplaces.Add(s1, r1);
                            
                            if(!packageReplaces.ContainsKey(s2))
                                packageReplaces.Add(s2, r2);

                        } else
                        {
                            int difference = toProp.Size - fromProp.Size;

                            serialSize += (ulong)difference;

                            var propertiesWithExportMap = new List<byte>(pack.PropertiesWithExportMap);

                            int propOffset = fromProp.Position - pack.PropertiesExportMapOffset;

                            propertiesWithExportMap.RemoveRange(propOffset, fromProp.Size);
                            propertiesWithExportMap.InsertRange(propOffset, toProp.Data);

                            pack.PropertiesWithExportMap = propertiesWithExportMap.ToArray();
                        }
                    }

                    if (optionObject.TryGetValue(out FStructFallback[] AnimationOverrides, "AnimationOverrides"))
                    {
                        Logger.Log("Option has Animation Overrides");
                        foreach (var animationOverride in AnimationOverrides)
                        {
                            var gender = animationOverride.GetOrDefault("Gender", EFortCustomGender.Male);
                            if (animationOverride.TryGetValue(out FSoftObjectPath EmoteMontage, "EmoteMontage"))
                            {
                                string animation = EmoteMontage.AssetPathName.Text;

                                string toAnim = gender == EFortCustomGender.Male
                                    ? toMaleAnimation.Tag.GenericValue.ToString()
                                    : toFemaleAnimation.Tag.GenericValue.ToString();
                                
                                if(!packageReplaces.ContainsKey(animation.Split(".").FirstOrDefault()))
                                    packageReplaces.Add(animation.Split(".").FirstOrDefault(), toAnim.Split(".").FirstOrDefault());
                                
                                if(!packageReplaces.ContainsKey(animation.Split(".").LastOrDefault()))
                                    packageReplaces.Add(animation.Split(".").LastOrDefault(), toAnim.Split(".").LastOrDefault());
                            }
                        }
                    }

                    packageReplaces.Add("Cosmetics.Filter.Season." + currentoption.backendReleaseValue, "Cosmetics.Filter.Season." + currentitem.backendReleaseValue);
                    
                    foreach(var swap in packageReplaces)
                    {
                        if(pack.NameMapAsStrings.ToList().Contains(swap.Key))
                        {

                            int index = pack.NameMapAsStrings.ToList().IndexOf(swap.Key);
                            pack.NameMapAsStrings[index] = swap.Value;

                            Logger.Log($"Replaced {swap.Key} with {swap.Value}");
                        } else
                        {
                            Logger.Log("WARNING: String was not found: " + swap.Key);
                        }
                    }
                    
                    int cookedSerialSizeOffset =
                        pack.zenSummary.ExportMapOffset + (72* (0)) + sizeof(ulong) - pack.PropertiesExportMapOffset;

                    var _propertiesWithExportMap = new List<byte>(pack.PropertiesWithExportMap);

                    _propertiesWithExportMap.RemoveRange(cookedSerialSizeOffset, sizeof(ulong));
                    _propertiesWithExportMap.InsertRange(cookedSerialSizeOffset, BitConverter.GetBytes(serialSize));

                    pack.PropertiesWithExportMap = _propertiesWithExportMap.ToArray();

                    pack.ChangeProtectedStrings(fromPackage.GetProtectedStrings());
                    pack.ChangeLastPublicExportHash(fromPackage);

                    var data = new Serializer(pack).Serialize();

                    if (currentoption.name.Length >= currentitem.name.Length)
                    {
                        byte[] nameSearch = Encoding.ASCII.GetBytes(currentoption.name);
                        byte[] nameReplace = Encoding.ASCII.GetBytes(currentitem.name);
                        
                        Array.Resize(ref nameReplace, nameSearch.Length);

                        int offset = Utils.Utils.FindOffset(data, nameSearch);

                        if (offset != -1)
                        {
                            var dataAsList = data.ToList();
                            
                            dataAsList.RemoveRange(offset, nameSearch.Length);
                            dataAsList.InsertRange(offset, nameReplace);

                            data = dataAsList.ToArray();
                        }
                    }
                    
                    if (currentoption.description.Length >= currentitem.description.Length)
                    {
                        byte[] descriptionSearch = Encoding.ASCII.GetBytes(currentoption.description);
                        byte[] descriptionReplace = Encoding.ASCII.GetBytes(currentitem.description);
                        
                        Array.Resize(ref descriptionReplace, descriptionSearch.Length);

                        int offset = Utils.Utils.FindOffset(data, descriptionSearch);

                        if (offset != -1)
                        {
                            var dataAsList = data.ToList();
                            
                            dataAsList.RemoveRange(offset, descriptionSearch.Length);
                            dataAsList.InsertRange(offset, descriptionReplace);

                            data = dataAsList.ToArray();
                        }
                    }

                    await SwapUtils.SwapAsset(fromPackage, data);

                    wroteAssets.Add(currentoption.path, data);

                } else
                {
                    int swappedIndex = 1;

                    foreach (KeyValuePair<string, string> entry in assetStrings)
                    {
                        Logger.Log("Swapping Asset " + swappedIndex);
                        lab.Content = "Swapping Asset " + swappedIndex;
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

                        toPack.ChangeLastPublicExportHash(fromPack);

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

                lab.Content = "Converted";
                Logger.Log("Swap finished");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
               MessageBox.Show("There was an error: " + e.Message, "");
                lab.Content = "Waiting for Input";
                return false;
            }
        }

        private static async Task<byte[]> GetPickaxedata(IoPackage package, Item item, Item option, string pickaxePath)
        {
            try
            {

                var prov = SwapUtils.GetProvider();

                UObject fromPickaxe = await prov.LoadObjectAsync(pickaxePath);
                UObject toPickaxe = await prov.LoadObjectAsync(item.definitionPath);

                List<string> thingsToChange = new()
                {
                    "WeaponMeshOverride",
                    "SmallPreviewImage",
                    "LargePreviewImage",
                    "SwingEffectNiagara",
                    "AnimTrailsNiagara",
                    "IdleEffect",
                    "IdleEffectNiagara",
                    "SwingEffect",
                    "SwingEffectOffhandNiagara",
                    "AnimTrails",
                    "AnimTrailsOffhand",
                    "AnimClass",
                    "WeaponActorClass"
                };

                List<string> cuesToChange = new()
                {
                    "ReloadSoundsMap",
                    "PrimaryFireSoundMap",
                    "OffhandImpactNiagaraPhysicalSurfaceEffects",
                    "ImpactPhysicalSurfaceSoundsMap",
                    "ImpactNiagaraPhysicalSurfaceEffectsMap"
                };

                List<string> nameMapValuesToChange = new()
                {
                    "AnimTrailsFirstSocketName",
                    "AnimTrailsSecondSocketName"
                };

                foreach (string thing in thingsToChange)
                {
                    Logger.Log("Swapping Property " + thing);

                    if(!fromPickaxe.TryGetValue(out FSoftObjectPath fromProp, thing))
                    {
                        Logger.Log("From Pickaxe does not have Property: " + thing);
                        continue;
                    }

                    if(!toPickaxe.TryGetValue(out FSoftObjectPath toProp, thing))
                    {
                        Logger.Log("To Pickaxe does not have Property: " + thing);
                        continue;
                    }

                    string s1 = fromProp.AssetPathName.Text.Split(".").FirstOrDefault();
                    string s2 = fromProp.AssetPathName.Text.Split(".").LastOrDefault();

                    string r1 = toProp.AssetPathName.Text.Split(".").FirstOrDefault();
                    string r2 = toProp.AssetPathName.Text.Split(".").LastOrDefault();

                    if (package.NameMapAsStrings.ToList().Contains(s1))
                    {
                        int index = package.NameMapAsStrings.ToList().IndexOf(s1);
                        package.NameMapAsStrings[index] = r1;
                    }

                    if (package.NameMapAsStrings.ToList().Contains(s2))
                    {
                        int index = package.NameMapAsStrings.ToList().IndexOf(s2);
                        package.NameMapAsStrings[index] = r2;
                    }
                }

                foreach(var cue in cuesToChange)
                {
                    try
                    {
                        Logger.Log("Swapping Cue: " + cue);

                        FPropertyTagType? currentCue = null;
                        if (fromPickaxe.TryGetValue(out UScriptMap currentFromMap, cue))
                            currentFromMap.Properties.TryGetValue(currentFromMap.Properties.Keys.First(), out currentCue);

                        FPropertyTagType? currentToCue = null;
                        if (toPickaxe.TryGetValue(out UScriptMap currentToMap, cue))
                            currentToMap.Properties.TryGetValue(currentToMap.Properties.Keys.First(), out currentToCue);

                        string search = ((FSoftObjectPath)currentCue.GenericValue).AssetPathName.Text;
                        string replace = ((FSoftObjectPath)currentToCue.GenericValue).AssetPathName.Text;

                        if (string.IsNullOrEmpty(search) || string.IsNullOrEmpty(replace))
                            continue;
                        
                        string s1 = search.Split(".").FirstOrDefault();
                        string s2 = search.Split(".").LastOrDefault();

                        string r1 = replace.Split(".").FirstOrDefault();
                        string r2 = replace.Split(".").LastOrDefault();

                        if (package.NameMapAsStrings.ToList().Contains(s1))
                        {
                            int index = package.NameMapAsStrings.ToList().IndexOf(s1);
                            package.NameMapAsStrings[index] = r1;
                        }

                        if (package.NameMapAsStrings.ToList().Contains(s2))
                        {
                            int index = package.NameMapAsStrings.ToList().IndexOf(s2);
                            package.NameMapAsStrings[index] = r2;
                        }
                    } catch(Exception e)
                    {
                        Logger.Log("Could not swap Cue: " + cue + ". Error:");
                        Logger.LogError(e.Message, e);
                    }
                }

                foreach (var nameMapValue in nameMapValuesToChange)
                {
                    Logger.Log("Swapping Namemap Value " + nameMapValue);

                    if (!fromPickaxe.TryGetValue(out FName fromName, nameMapValue))
                    {
                        Logger.Log("From Pickaxe does not have " + nameMapValue);
                        continue;
                    }
                    
                    if (!toPickaxe.TryGetValue(out FName toName, nameMapValue))
                    {
                        Logger.Log("To Pickaxe does not have " + nameMapValue);
                        continue;
                    }

                    if (package.NameMapAsStrings.Contains(fromName.Text))
                    {
                        int index = package.NameMapAsStrings.ToList().IndexOf(fromName.Text);
                        package.NameMapAsStrings[index] = toName.Text;
                    }
                }

                var data = new Serializer(package).Serialize();

                return data;
            } catch (Exception e)
            {
                return null;
            }
        }

        public static async Task<bool> Revert(Item item, Item option, Label lab)
        {
            try
            {
                Logger.Log("Reverting Standard -> " + option.name + " To " + item.name);
                lab.Content = "Loading Provider";
                var prov = SwapUtils.GetProvider();
                lab.Content = "Preparing Assets";

                Logger.Log("Preparing Assets");

                var assetStrings = await SwapUtils.GetAssetsForSwap(item, option);

                Logger.Log("Found Assets: " + JsonConvert.SerializeObject(assetStrings));

                if (assetStrings.Count == 0)
                {
                    MessageBox.Show("Swapper could not find any Assets to swap");
                    lab.Content = "Waiting for Input";
                    return false;
                }

                    int swappedIndex = 1;

                    foreach (KeyValuePair<string, string> entry in assetStrings)
                    {
                        Logger.Log("Swapping Asset " + swappedIndex);
                        lab.Content = "Swapping Asset " + swappedIndex;
                        Logger.Log("Loading " + entry.Key);
                        var fromPack = (IoPackage)(await prov.LoadPackageAsync(entry.Key));

                        if (!await SwapUtils.RevertPackage(fromPack))
                        {
                            return false;
                        }

                        swappedIndex++;
                    }
                

                Logger.Log("Removing Converted Item");

                Config.GetConfig().ConvertedItems.RemoveAll(x => x.OptionID.ToLower().Equals(option.id.ToLower()));
                Config.Save();

                Utils.Utils.LogSwap(item.id, false, option.id);

                lab.Content = "Reverted";

                Logger.Log("Swap finished");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                MessageBox.Show("There was an error: " + e.Message);
                lab.Content = "Waiting for Input";
                return false;
            }
        }

        public static async Task<bool> SwapCID(string option, string item)
        {
            try
            {
                var provider = SwapUtils.GetProvider();

                Logger.Log("Exporting Option Packacges");

                IoPackage optionPackage = (IoPackage)await provider.LoadPackageAsync(option);

                Logger.Log("Exporting Item Packacges");
                
                UObject itemObject = await provider.LoadObjectAsync(item);
                UObject optionObject = await provider.LoadObjectAsync(option);

                List<KeyValuePair<int, int>> nameMapWithIndexes = new();

                int lengthDifference = 0;
                
                if (itemObject.TryGetValue(out FSoftObjectPath[] ItemBaseCharacterParts, "BaseCharacterParts"))
                {
                    foreach (var cp in ItemBaseCharacterParts)
                    {
                        int nameMapSize = optionPackage.NameMapAsStrings.Length;
                        string first = cp.AssetPathName.Text.SubstringBeforeLast(".");
                        string second = cp.AssetPathName.Text.SubstringBeforeLast(".").SubstringAfterLast("/");

                        var nameMapAsList = optionPackage.NameMapAsStrings.ToList();
                        nameMapAsList.Add(first);
                        nameMapAsList.Add(second);

                        lengthDifference += first.Length;
                        lengthDifference += second.Length;

                        optionPackage.NameMapAsStrings = nameMapAsList.ToArray();

                        nameMapWithIndexes.Add(new(nameMapSize, nameMapSize + 1));
                    }
                }
                
                var fromprop = optionObject.Properties.FirstOrDefault(x => x.Name == "BaseCharacterParts");

                byte[] cpsArray = CreateBaseCharacterPartsArray(nameMapWithIndexes);
                
                ulong serialSize = optionPackage.ExportMap[0].CookedSerialSize;
                
                serialSize += (ulong)cpsArray.Length - (ulong)fromprop.Data.Length;
                
                int cookedSerialSizeOffset =
                    optionPackage.zenSummary.ExportMapOffset + (72* (0)) + sizeof(ulong) - optionPackage.PropertiesExportMapOffset;

                var _propertiesWithExportMap = new List<byte>(optionPackage.PropertiesWithExportMap);

                _propertiesWithExportMap.RemoveRange(cookedSerialSizeOffset, sizeof(ulong));
                _propertiesWithExportMap.InsertRange(cookedSerialSizeOffset, BitConverter.GetBytes(serialSize));

                optionPackage.PropertiesWithExportMap = _propertiesWithExportMap.ToArray();

                var data = new Serializer(optionPackage).Serialize().ToList();

                int offset = Utils.Utils.FindOffset(data.ToArray(), fromprop.Data);
                
                data.RemoveRange(offset, fromprop.Data.Length);
                data.InsertRange(offset, cpsArray);
                
                await SwapUtils.SwapAsset(optionPackage, data.ToArray());

                MessageBox.Show("swapped");
                
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private static byte[] CreateBaseCharacterPartsArray(List<KeyValuePair<int, int>> nameMapWithIndexes)
        {
            List<byte> data = new();
            
            data.AddRange(BitConverter.GetBytes(nameMapWithIndexes.Count));

            foreach (var namemapIndexes in nameMapWithIndexes)
            {
                List<byte> bytes = new()
                {
                    (byte) namemapIndexes.Key,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    (byte) namemapIndexes.Value,
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
                
                data.AddRange(bytes);
            }
            
            return data.ToArray();
        }
        
    }
    
}

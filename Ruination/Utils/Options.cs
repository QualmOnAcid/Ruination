using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using Ruination_v2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ruination_v2.Utils
{
    public class Options
    {
        public static bool IsOption(Item option, Item toitem)
        {
            try
            {
                if (API.GetApi().BlacklistedOptionIDS.Contains(option.id))
                    return false;

                if (option.id == toitem.id)
                    return false;
                
                if (option.Type == ItemType.EMOTE && toitem.Type == ItemType.EMOTE)
                {
                    return true;
                }

                if (option.Type == ItemType.CAR && toitem.Type == ItemType.CAR)
                {
                    if(toitem.subType == "body" && option.subType == "body")
                        return true;
                    if(toitem.subType == "wheel" && option.subType == "wheel")
                        return true;
                    if(toitem.subType == "drifttrail" && option.subType == "drifttrail")
                        return true;
                    if (toitem.subType == "booster" && option.subType == "booster")
                        return true;
                }

                if (option.Type == ItemType.PICKAXE && toitem.Type == ItemType.PICKAXE)
                {
                    var fromCached = Config.GetConfig().CachedItems.FirstOrDefault(x => x.Id.ToLower().Equals(option.id.ToLower()));
                    var toCached = Config.GetConfig().CachedItems.FirstOrDefault(x => x.Id.ToLower().Equals(toitem.id.ToLower()));

                    if (fromCached.WeaponAnim != toCached.WeaponAnim) return false;

                    return true;
                }

                if (option.Type == ItemType.BACKPACK && toitem.Type == ItemType.BACKPACK)
                {
                    var fromCached = Config.GetConfig().CachedItems.FirstOrDefault(x => x.Id.ToLower().Equals(option.id.ToLower()));
                    var toCached = Config.GetConfig().CachedItems.FirstOrDefault(x => x.Id.ToLower().Equals(toitem.id.ToLower()));

                    if (fromCached.Parts.Count != toCached.Parts.Count) return false;

                    foreach (var fromPart in fromCached.Parts)
                    {
                        if (!toCached.Parts.Any(x => x.Mesh == fromPart.Mesh)) return false;
                    }

                    return true;

                }

                if (option.Type == ItemType.SKIN && toitem.Type == ItemType.SKIN)
                {
                    var fromCached = Config.GetConfig().CachedItems.FirstOrDefault(x => x.Id.ToLower().Equals(option.id.ToLower()));
                    var toCached = Config.GetConfig().CachedItems.FirstOrDefault(x => x.Id.ToLower().Equals(toitem.id.ToLower()));

                    if (fromCached.Parts.Count != toCached.Parts.Count) return false;

                    foreach (var fromPart in fromCached.Parts)
                    {
                        if (!toCached.Parts.Any(x => x.Mesh == fromPart.Mesh)) return false;
                    }

                    return true;
                }

                return false;

            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static bool IsTransformCharacterOption(DefaultFileProvider provider, ApiTransformCharacterObject option, bool HasHat, bool HasFaceacc, int CharacterPartCount)
        {
            return Task.Run(async () =>
            {
                if (CharacterPartCount > option.CharacterParts.Count) return false;

                bool hasOptionHat = false;
                bool hasOptionFaceacc = false;

                List<string> fromCharacterPartsTypes = new();

                for (int i = 0; i < option.CharacterParts.Count; i++)
                {
                    var uobject = await provider.LoadObjectAsync(option.CharacterParts[i].Split(".").FirstOrDefault());
                    fromCharacterPartsTypes.Add(SwapUtils.GetCharacterPartType(uobject).ToString());
                }

                hasOptionHat = fromCharacterPartsTypes.Contains("Hat");
                hasOptionFaceacc = fromCharacterPartsTypes.Contains("Face");

                if (HasHat && !hasOptionHat) return false;
                if (HasFaceacc && !hasOptionFaceacc) return false;

                return true;

            }).GetAwaiter().GetResult();
        }

        public static List<ApiTransformCharacterObject> GetAllTransformOptions(DefaultFileProvider provider, Item item)
        {
            return Task.Run(async () =>
            {
                var list = new List<ApiTransformCharacterObject>();

                var itemCidExport = await provider.LoadObjectAsync(item.path);

                if (itemCidExport.TryGetValue(out UObject[] characterParts, "BaseCharacterParts"))
                {
                    Dictionary<string, string> assetsList = new();

                    foreach (var characterPart in characterParts)
                    {
                        CharacterPartType characterPartType = SwapUtils.GetCharacterPartType(characterPart);

                        if (characterPart == null)
                        {
                            continue;
                        }

                        assetsList.Add(characterPartType.ToString(), characterPart.GetPathName().Split(".")[0]);
                    }


                    bool HasHat = assetsList.ContainsKey("Hat");
                    bool HasFaceacc = assetsList.ContainsKey("Face");
                    int CharacterPartCount = assetsList.Count;

                    foreach (var transformChar in API.GetApi().TransformCharacters)
                    {
                        if (IsTransformCharacterOption(provider, transformChar, HasHat, HasFaceacc, CharacterPartCount))
                            list.Add(transformChar);
                    }

                }

                return list;

            }).GetAwaiter().GetResult();

        }

    }
}

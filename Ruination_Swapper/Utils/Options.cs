using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebviewAppShared.Models;
using static WebviewAppShared.Utils.SwapUtils;

namespace WebviewAppShared.Utils
{
    public class Options
    {
        public static bool IsOption(Item option, Item toitem)
        {
            try
            {
                if (option.Type == ItemType.EMOTE && toitem.Type == ItemType.EMOTE)
                {
                    if (option.series != toitem.series) return false;
                    return true;
                }

                if (option.Type == ItemType.PICKAXE && toitem.Type == ItemType.PICKAXE)
                {
                    if (option.series != toitem.series) return false;
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
                   fromCharacterPartsTypes.Add(GetCharacterPartType(uobject).ToString());
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
                        CharacterPartType characterPartType = GetCharacterPartType(characterPart);

                        if (characterPart == null)
                        {
                            continue;
                        }

                        assetsList.Add(characterPartType.ToString(), characterPart.GetPathName().Split(".")[0]);
                    }


                    bool HasHat = assetsList.ContainsKey("Hat");
                    bool HasFaceacc = assetsList.ContainsKey("Face");
                    int CharacterPartCount = assetsList.Count;

                    foreach(var transformChar in API.GetApi().TransformCharacters)
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

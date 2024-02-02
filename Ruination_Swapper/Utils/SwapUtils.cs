using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets;
using System.IO;
using CUE4Parse.UE4.Objects.UObject;
using WebviewAppShared.Models;
using BlazorWpfApp.CUE4Parse;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using WebviewAppShared.Swapper;
using CUE4Parse.UE4.Readers;
using System.Globalization;

namespace WebviewAppShared.Utils
{
    public class SwapUtils
    {

        private static DefaultFileProvider fileProvider;

        public static DefaultFileProvider GetProvider() => fileProvider;

        public static async Task<DefaultFileProvider> LoadProvider()
        {
            try
            {
                CUtils.isExport = false;

                JObject aesObject = JObject.Parse(await new System.Net.WebClient().DownloadStringTaskAsync("https://fortnitecentral.genxgames.gg/api/v1/aes"));

                Epicgames.SetAPIFortniteVersion(aesObject["version"].ToString());

                Logger.Log("Loading Provider");
                DefaultFileProvider provider = new DefaultFileProvider(Epicgames.GetPaksPath(),
                    System.IO.SearchOption.TopDirectoryOnly, true, new(CUE4Parse.UE4.Versions.EGame.GAME_UE5_4));
                Logger.Log("Initializing Provider");
                provider.Initialize();

                Logger.Log("Loading Mappings");
                provider.MappingsContainer = new FileUsmapTypeMappingsProvider(Mappings.GetMappingsPath());
                Logger.Log("Loading Keys");
                var keys = new List<KeyValuePair<FGuid, FAesKey>>();

                string mainAes = aesObject["mainKey"].ToString();

                keys.Add(new(new FGuid(), new FAesKey(mainAes)));

                foreach (dynamic dyKey in aesObject["dynamicKeys"])
                {
                    keys.Add(new(new FGuid(dyKey.guid.ToString()), new FAesKey(dyKey.key.ToString())));
                }

                if (string.IsNullOrEmpty(Epicgames.GetInstalledFortniteVersion()) || string.IsNullOrWhiteSpace(Epicgames.GetInstalledFortniteVersion()))
                    Epicgames.SetInstalledFortniteVersion(aesObject["version"].ToString());

                await provider.SubmitKeysAsync(keys);
                Logger.Log("Loaded " + keys.Count + " Keys");
                CUtils.isExport = true;

                fileProvider = provider;
                return GetProvider();
            } catch(Exception ex) {
                Logger.LogError(ex.Message, ex);
                Environment.Exit(0);
                return null;
            }
        }

        public static async Task<DefaultFileProvider> GetUEFNProvider(string uefnfile)
        {
            try
            {
                CUtils.isExport = false;
                Logger.Log("Loading Provider");
                DefaultFileProvider provider = new DefaultFileProvider(Epicgames.GetPaksPath(),
                    System.IO.SearchOption.TopDirectoryOnly, true, new(CUE4Parse.UE4.Versions.EGame.GAME_UE5_4));
                Logger.Log("Initializing Provider");
                provider.Initialize(uefnfile);
                Logger.Log("Loading Mappings");
                provider.MappingsContainer = new FileUsmapTypeMappingsProvider(Mappings.GetMappingsPath());
                Logger.Log("Loading Keys");
                JObject aesObject = JObject.Parse(await new System.Net.WebClient().DownloadStringTaskAsync("https://fortnitecentral.genxgames.gg/api/v1/aes"));
                var keys = new List<KeyValuePair<FGuid, FAesKey>>();

                string mainAes = aesObject["mainKey"].ToString();

                keys.Add(new(new FGuid(), new FAesKey(mainAes)));

                foreach (dynamic dyKey in aesObject["dynamicKeys"])
                {
                    keys.Add(new(new FGuid(dyKey.guid.ToString()), new FAesKey(dyKey.key.ToString())));
                }

                await provider.SubmitKeysAsync(keys);
                Logger.Log("Loaded " + keys.Count + " Keys");
                CUtils.isExport = true;
                return provider;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, ex);
                return null;
            }
        }

        public static byte[] CreateOffsetAndLength(ulong offset, ulong length)
        {
            byte[] offsetAndLength = new byte[10];

            offsetAndLength[0] = (byte)(offset >> 32 & 0xFF);
            offsetAndLength[1] = (byte)(offset >> 24 & 0xFF);
            offsetAndLength[2] = (byte)(offset >> 16 & 0xFF);
            offsetAndLength[3] = (byte)(offset >> 8 & 0xFF);
            offsetAndLength[4] = (byte)(offset & 0xFF);

            offsetAndLength[5] = (byte)(length >> 32 & 0xFF);
            offsetAndLength[6] = (byte)(length >> 24 & 0xFF);
            offsetAndLength[7] = (byte)(length >> 16 & 0xFF);
            offsetAndLength[8] = (byte)(length >> 8 & 0xFF);
            offsetAndLength[9] = (byte)(length & 0xFF);

            return offsetAndLength;
        }

        public static List<byte> CompressionBlock(uint Offset, uint CompressedSize, uint UncompressedSize, uint partition, uint compressionMethod = 0)
        {
            var CompressionData = new List<byte>(12);

            CompressionData.Add((byte)(Offset & 0xFF));
            CompressionData.Add((byte)(Offset >> 8 & 0xFF));
            CompressionData.Add((byte)(Offset >> 16 & 0xFF));
            CompressionData.Add((byte)(Offset >> 24 & 0xFF));

            CompressionData.Add((byte)partition);

            CompressionData.Add((byte)(CompressedSize & 0xFF));
            CompressionData.Add((byte)(CompressedSize >> 8 & 0xFF));
            CompressionData.Add((byte)(CompressedSize >> 16 & 0xFF));

            CompressionData.Add((byte)(UncompressedSize & 0xFF));
            CompressionData.Add((byte)(UncompressedSize >> 8 & 0xFF));
            CompressionData.Add((byte)(UncompressedSize >> 16 & 0xFF));

            CompressionData.Add((byte)compressionMethod);

            return CompressionData;
        }

        public static async Task<string> GetBodyAssetForCharacter(DefaultFileProvider provider, Item fromOutfit)
        {
            var fromCharacterParts = await GetCharacterParts(provider, fromOutfit.path);

            if (fromCharacterParts == null)
                return "";

            foreach (var item in fromCharacterParts)
            {
                var type = GetCharacterPartType(item);
                if (type.Equals(CharacterPartType.Body))
                {
                    return item.GetPathName();
                }
            }

            return "";

        }

        public static Dictionary<string, KeyValuePair<string, string>> GetCIDCharacterPartsSwaps(UObject[] list1)
        {

            Dictionary<string, string> assetsList = new();

            foreach (var item in list1)
            {
                CharacterPartType characterPartType = GetCharacterPartType(item);
                assetsList.Add(characterPartType.ToString(), item.GetPathName().Split(".")[0]);
            }

            Dictionary<string, string> placeHolderAssets = new Dictionary<string, string>();
            placeHolderAssets.Add("Body", "/Game/Athena/Heroes/Meshes/Bodies/CP_Athena_Body_F_Fallback.CP_Athena_Body_F_Fallback");
            placeHolderAssets.Add("Head", "/Game/Characters/CharacterParts/Common/CP_Head_Med_Empty.CP_Head_Med_Empty");
            placeHolderAssets.Add("Hat", "/Game/Characters/CharacterParts/Hats/Empty_None.Empty_None");
            placeHolderAssets.Add("Face", "/Game/Characters/CharacterParts/FaceAccessories/CP_M_FaceAcc_Empty.CP_M_FaceAcc_Empty");
            placeHolderAssets.Add("MiscOrTail", "/Game/Characters/CharacterParts/Charms/NoCharm.NoCharm");

            Dictionary<string, KeyValuePair<string, string>> output = new();

            foreach (var entry in assetsList)
            {
                if (placeHolderAssets.ContainsKey(entry.Key) && !output.ContainsKey(entry.Key))
                {
                    output.Add(entry.Key, new(entry.Value + "." + entry.Value.Split("/").LastOrDefault(), placeHolderAssets[entry.Key]));
                }
            }

            return output;

        }

        public static async Task<Dictionary<string, string>> GetAssetsForSkin(DefaultFileProvider provider, Item fromOutfit, Item toOutfit)
        {
            var fromCharacterParts = await GetCharacterParts(provider, fromOutfit.path);
            var toCharacterParts = await GetCharacterParts(provider, toOutfit.path);

            if (fromCharacterParts == null || toCharacterParts == null)
                return new();

            var orderedAssets = await OrderAssets(fromCharacterParts, toCharacterParts);

            return orderedAssets;
        }

        public static async Task<Dictionary<string, string>> OrderAssets(UObject[] list1, UObject[] list2)
        {

            Dictionary<string, string> assetsList = new();
            Dictionary<string, string> assetsToList = new();

            foreach (var item in list1)
            {
                CharacterPartType characterPartType = GetCharacterPartType(item);
                assetsList.Add(characterPartType.ToString(), item.GetPathName().Split(".")[0]);
            }

            foreach (var item in list2)
            {
                CharacterPartType characterPartType = GetCharacterPartType(item);
                assetsToList.Add(characterPartType.ToString(), item.GetPathName().Split(".")[0]);
            }

            Dictionary<string, string> placeHolderAssets = new Dictionary<string, string>();
            placeHolderAssets.Add("Body", "/Game/Athena/Heroes/Meshes/Bodies/CP_Mannequin_Body");
            placeHolderAssets.Add("Head", "/Game/Characters/CharacterParts/Common/CP_Head_Med_Empty");
            placeHolderAssets.Add("Hat", "/Game/Characters/CharacterParts/Hats/Empty_None");
            placeHolderAssets.Add("Face", "/Game/Characters/CharacterParts/FaceAccessories/CP_M_FaceAcc_Empty");
            placeHolderAssets.Add("MiscOrTail", "/Game/Characters/CharacterParts/Charms/NoCharm");

            Dictionary<string, string> output = new Dictionary<string, string>();

            foreach (var entry in assetsList)
            {
                if (assetsToList.ContainsKey(entry.Key))
                {
                    output.Add(entry.Value, assetsToList[entry.Key]);
                }
                else if (placeHolderAssets.ContainsKey(entry.Key))
                {
                    output.Add(entry.Value, placeHolderAssets[entry.Key]);
                }
            }

            return output;

        }

        public static CharacterPartType GetCharacterPartType(UObject uobject)
        {
            var itemAsJson = JsonConvert.SerializeObject(uobject, Formatting.Indented);

            string partType = "Head";

            try
            {
                var jsonpartType = JObject.Parse(itemAsJson)["Properties"]["CharacterPartType"].ToString();
                partType = jsonpartType.Split("::")[1];
            }
            catch (Exception e) { }

            switch (partType)
            {
                case "Body": return CharacterPartType.Body;
                case "Head": return CharacterPartType.Head;
                case "Hat": return CharacterPartType.Hat;
                case "Face": return CharacterPartType.Face;
                case "MiscOrTail": return CharacterPartType.MiscOrTail;
                default: return CharacterPartType.Unknown;
            }
        }

        public static async Task<UObject[]> GetCharacterParts(DefaultFileProvider provider, string id)
        {
            return await Task.Run(async () =>
            {
                UObject idAsset = await provider.LoadObjectAsync(id);
                if (idAsset.TryGetValue(out FSoftObjectPath[] characterParts, "BaseCharacterParts"))
                {

                    var cps = new List<UObject>();

                    List<string> classNames = new List<string>()
                 {
                     "CustomCharacterBodyPartData",
                     "CustomCharacterHeadData",
                     "CustomCharacterHatData",
                     "CustomCharacterFaceData",
                     "CustomCharacterCharmData"
                 };
                    string allclassnames = "";
                    foreach (var cp in characterParts)
                    {
                        //CUE4Parse does not load single Objects when invalid Name which is used for UEFN Swaps
                        var cpExports = provider.LoadAllObjects(cp.AssetPathName.Text.Split(".").FirstOrDefault()).ToArray();

                        UObject actualCP = null;

                        foreach (var export in cpExports)
                        {
                            allclassnames += export.Name + "\n";
                            if (!classNames.Any(x => export.Name.Contains(x)))
                            {
                                actualCP = export;
                                break;
                            }
                        }

                        if (actualCP != null)
                            cps.Add(actualCP);
                    }

                    return cps.ToArray();
                }


                return null;
            });
        }

        public static async Task<List<string>> GetBackpackCPParts(DefaultFileProvider provider, string bid)
        {
            var pack = await provider.LoadObjectAsync(bid);

            if (pack.TryGetValue(out UObject[] CharacterParts, "CharacterParts"))
            {
                var list = new List<string>();
                foreach (var item in CharacterParts)
                {
                    list.Add(item.GetPathName().Split('.').FirstOrDefault());
                }
                return list;
            }

            return new();
        }

        public static async Task<bool> SwapAsset(IoPackage fromPack, byte[] data)
        {
            try
            {
                long UcasFileLength = new FileInfo(fromPack.ExportData.UcasFile).Length;

                Logger.Log("Writing Ucas " + fromPack.ExportData.UcasFile);

                byte[] buffer = data;
                using (FileStream fileStream =
                       new FileStream(fromPack.ExportData.UcasFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileStream.Seek(UcasFileLength, SeekOrigin.Begin);
                    fileStream.Write(buffer, 0, buffer.Length);
                    fileStream.Close();
                }

                //Utoc

                //Create OffsetLengthEntry
                var oldEntry = fromPack.ExportData.OffsetAndLengthEntry;
                var offset = oldEntry.Offset;

                string utocfile = Epicgames.GetPaksPath() + "\\" + Path.GetFileName(fromPack.ExportData.UtocFile);

                Logger.Log("Writing Utoc " + utocfile);

                using (FileStream fileStream =
                       new FileStream(utocfile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {

                    fileStream.Seek(fromPack.ExportData.UtocCBlockOffset, SeekOrigin.Begin);
                    var compBlock = CompressionBlock((uint)UcasFileLength, (uint)buffer.Length, (uint)buffer.Length,
                        (uint)fromPack.ExportData.PartitionCount - 1).ToArray();

                    fileStream.Write(compBlock, 0, 12);

                    fileStream.Seek(fromPack.ExportData.UtocLOOffset, SeekOrigin.Begin);
                    var offsetAndLengthEntry = CreateOffsetAndLength(offset, (ulong)buffer.Length).ToArray();
                    fileStream.Write(offsetAndLengthEntry, 0, 10);

                    fileStream.Close();
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static async Task<bool> RevertPackage(IoPackage pack)
        {
            try
            {
                Logger.Log("Getting Compression Block");
                var compBlock = GetBytesAtPosition(pack.ExportData.UtocFile,
                    pack.ExportData.UtocCBlockOffset, 12);

                Logger.Log("Getting OffsetAndLength");
                var offsetAndLength = GetBytesAtPosition(pack.ExportData.UtocFile,
                    pack.ExportData.UtocLOOffset, 10);

                string utocfile = Epicgames.GetPaksPath() + "\\" + Path.GetFileName(pack.ExportData.UtocFile);

                Logger.Log("Writing Compression Block");
                WriteBytesAtPosition(utocfile, pack.ExportData.UtocCBlockOffset, compBlock);

                Logger.Log("Writing Offset And Length");
                WriteBytesAtPosition(utocfile, pack.ExportData.UtocLOOffset, offsetAndLength);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                return false;
            }
        }

        public static async Task<bool> RevertConvertedItem(ConvertedItem item)
        {
            try
            {
                Logger.Log("Reverting " + item.Name);

                foreach (var asset in item.Assets)
                {
                    Logger.Log("Exporting " + asset.Key);
                    IoPackage pack = (IoPackage)await GetProvider().LoadPackageAsync(asset.Key);

                    if (!await RevertPackage(pack))
                        return false;
                }

                Logger.Log("Reverted Item");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                return false;
            }

        }

        public static byte[] GetBytesAtPosition(string file, long offset, int length)
        {
            var bytes = new byte[length];

            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(bytes, 0, (int)length);
            }

            return bytes;
        }

        private static void WriteBytesAtPosition(string file, long offset, byte[] buffer)
        {
            using (FileStream fileStream =
                       new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                fileStream.Write(buffer, 0, buffer.Length);
                fileStream.Close();
            }
        }

        public static void SaveRevertData(IoPackage ioPackage)
        {
            string FolderPath = Utils.AppDataFolder + "\\RevertData\\" + Epicgames.GetInstalledFortniteVersion() + "\\" + ioPackage
                .Name.Replace("/", "");

            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);

            if (!File.Exists(FolderPath + "\\Compressionblock.uasset"))
            {
                var compressionBlock = CompressionBlock((uint)ioPackage.ExportData.CompressionBlock.Offset,
                    ioPackage.ExportData.CompressionBlock.CompressedSize,
                    ioPackage.ExportData.CompressionBlock.UncompressedSize,
                    (uint)ioPackage.ExportData.CompressionBlock.Partition,
                    ioPackage.ExportData.CompressionBlock.CompressionMethodIndex);

                File.WriteAllBytes(FolderPath + "\\Compressionblock.uasset", compressionBlock.ToArray());
            }

            if (!File.Exists(FolderPath + "\\OffsetAndLength.uasset"))
            {
                  var offsetAndLength = CreateOffsetAndLength(ioPackage.ExportData.OffsetAndLengthEntry.Offset,
                    ioPackage.ExportData.OffsetAndLengthEntry.Length);

                File.WriteAllBytes(FolderPath + "\\OffsetAndLength.uasset", offsetAndLength);
            }
        }

        public static async Task<Dictionary<string, string>> GetAssetsForSwap(Item item, Item option)
        {
            return await Task.Run(async () =>
            {
                var prov = GetProvider();

                Dictionary<string, string> assetStrings = new();

                if (item.Type == ItemType.EMOTE)
                {
                    assetStrings.Add(option.path, item.path);
                }
                else if (item.Type == ItemType.PICKAXE)
                {
                    assetStrings.Add(option.definitionPath, item.definitionPath);
                }
                else if (item.Type == ItemType.BACKPACK)
                {
                    var bp = await SwapUtils.GetBackpackCPParts(prov, option.path);
                    var tobp = await SwapUtils.GetBackpackCPParts(prov, item.path);
                    for (int i = 0; i < bp.Count; i++)
                    {
                        assetStrings.Add(bp[i], tobp[i]);
                    }
                }
                else if (item.Type == ItemType.SKIN)
                {

                    if (option.IsTransformCharacter)
                    {

                        var transformCharacter = API.GetApi().TransformCharacters.FirstOrDefault(x => x.ID.ToLower().Equals(option.id.ToLower()));

                        if (transformCharacter == null) return new();

                        var fromCharacterParts = new UObject[transformCharacter.CharacterParts.Count];

                        for (int i = 0; i < transformCharacter.CharacterParts.Count; i++)
                        {
                            fromCharacterParts[i] = await prov.LoadObjectAsync(transformCharacter.CharacterParts[i].Split(".").FirstOrDefault());
                        }

                        var toCharacterParts = await SwapUtils.GetCharacterParts(prov, item.path);

                        var orderedAssets = await SwapUtils.OrderAssets(fromCharacterParts, toCharacterParts);

                        foreach (var entry in orderedAssets)
                        {
                            assetStrings.Add(entry.Key, entry.Value);
                        }

                    }
                    else
                    {
                        var assetDic = await SwapUtils.GetAssetsForSkin(prov, option, item);

                        foreach (var entry in assetDic)
                        {
                            assetStrings.Add(entry.Key, entry.Value);
                        }

                        assetStrings.Add(option.definitionPath, item.definitionPath);
                    }

                }

                return assetStrings;
            });
        }

        public static IoPackage ChangeReplaces(List<KeyValuePair<string, string>> replaces, IoPackage package)
        {

            foreach(var item in replaces)
            {
                if(package.NameMapAsStrings.Contains(item.Key))
                {
                    int index = package.NameMapAsStrings.ToList().FindIndex(x => x == item.Key);
                    package.NameMapAsStrings[index] = item.Value;
                }
            }

            return package;

        }

    }

}

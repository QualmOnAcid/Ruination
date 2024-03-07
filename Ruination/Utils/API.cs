using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Ruination_v2.Models;

namespace Ruination_v2.Utils
{
    public class API
    {

        private static ApiObject _api;
        private static ApiPluginsObject _pluginsApi;

        public static async Task Load()
        {
            Logger.Log("Downloading API");
            try
            {
                using(HttpClient _client = new HttpClient())
                {
                    var resp = await _client.GetAsync("https://raw.githubusercontent.com/QualmOnAcid/Ruination/main/api.json").ConfigureAwait(false);
                    string apitext = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _api = JsonConvert.DeserializeObject<ApiObject>(Encoding.UTF8.GetString(Convert.FromBase64String(apitext)));
                    
                    var pluginsResponse = await _client.GetAsync("https://raw.githubusercontent.com/QualmOnAcid/Ruination/main/Plugins.json").ConfigureAwait(false);
                    _pluginsApi =
                        JsonConvert.DeserializeObject<ApiPluginsObject>(await pluginsResponse.Content
                            .ReadAsStringAsync().ConfigureAwait(false));
                }

                if (File.Exists("C:\\Users\\Youri\\Documents\\RuinationAPI\\api.json"))
                {
                    _api = JsonConvert.DeserializeObject<ApiObject>(
                        File.ReadAllText("C:\\Users\\Youri\\Documents\\RuinationAPI\\api.json"));   
                }
                
                Logger.Log("Downloaded API");
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                Environment.Exit(0);
            }
        }

        public static ApiObject GetApi() => _api;
        public static ApiPluginsObject GetPluginsApi() => _pluginsApi;

    }

    public class ApiObject
    {
        public ApiOtherObject Other;
        public ApiAssetUtilsObject AssetUtils;
        public ApiUEFNFilesObject UEFNFiles;
        public List<ApiTransformCharacterObject> TransformCharacters = new();
        public List<ApiUEFNSkinObject> Characters = new();
        public List<string> PremiumUsers = new();
        public List<SingleChunkedFileObject> ChunkedFiles = new();
        public List<string> BackpackIDS = new();
        public List<PickaxePathOverrideObject> PickaxePathOverrides = new();
        public ApiRuinationUtilsObject RuinationUtils;
        public List<string> BlacklistedOptionIDS = new();
        public List<AdditionalCarItem> AdditionalCarItems = new();
    }

    public class ApiPluginsObject
    {
        public List<ApiPluginsVersionObject> Versions = new();
    }

    public class ApiPluginsVersionObject
    {
        public string Version { get; set; }
        public List<ApiPluginsVersionPluginObject> Plugins = new();
    }

    public class ApiPluginsVersionPluginObject
    {
        public string Name;
        public string Icon;
        public string PluginURL;
    }
    
    public class ApiRuinationUtilsObject
    {
        public string Url;
        public string Filename;
        public string Processname;
        public string Version;
    }

    public class ApiOtherObject
    {
        public string Version;
        public string KeyLink;
        public string DownloadLink;
        public string MappingsUrl;
        public string Discord;
        public bool Disabled = true;
        public string DisabledMessage = "Swapper is under maintenance.";
        public bool DoPickaxeWarning = false;
        public string OodleDLL = string.Empty;
    }

    public class ApiAssetUtilsObject
    {
        public int MaterialOverrideFlags;
    }

    public class ApiUEFNFilesObject
    {
        public string pak;
        public string sig;
        public string ucas;
        public string utoc;
        public string FileToUse;
        public string PluginFileToUse;
    }

    public class ApiTransformCharacterObject
    {
        public string Name;
        public string ID;
        public string ImageID;
        public string Icon;
        public List<string> CharacterParts = new();
    }

    public class ApiUEFNSkinObject
    {
        public string Name;
        public string ID;
        public string Description;
        public string Type;
        public string Mesh;
        public string Skeleton;
        public string Animation;
        public string IdleFXSocket;
        public string Icon;
        public string Rarity;
        public List<string> Materials = new();
        public string hidpath;
        public string Info;
        public string PartModifierBlueprint = "";
        public string IdleEffectNiagara = "";
        public ApiUEFNFilesObject OverrideFiles = null;
        public List<ApiUEFNSkinTextureSwapObject> TextureSwaps = new();
        public bool UseIdleEffectPackage = false;
        public string ChunkedFile = null;
    }

    public class ApiUEFNSkinTextureSwapObject
    {
        public string From;
        public string To;
        public List<PluginAssetSwapSwapModel> Swaps = new();
    }

    public class SingleChunkedFileObject
    {
        public string Name;
        public string pak;
        public string sig;
        public string ucas;
        public string utoc;
    }

    public class PickaxePathOverrideObject
    {
        public string ID;
        public string Override;
        public string Series = "None";
    }

    public class AdditionalCarItem
    {
        public string Name;
        public string Description;
        public string Icon;
        public string ID;
        public string Path;
        public string Type;
    }

}

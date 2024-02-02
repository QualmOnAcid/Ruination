using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebviewAppShared.Models;

namespace WebviewAppShared.Utils
{
    public class API
    {

        private static ApiObject _api;

        public static void Load()
        {
            Logger.Log("Downloading API");
            try
            {
                _api = JsonConvert.DeserializeObject<ApiObject>(Encoding.UTF8.GetString(Convert.FromBase64String(new System.Net.WebClient().DownloadString("https://raw.githubusercontent.com/QualmOnAcid/Ruination/main/api.json"))));
                Logger.Log("Downloaded API");
            } catch(Exception e)
            {
                Logger.LogError(e.Message, e);
                Environment.Exit(0);
            }
        }

        public static ApiObject GetApi() => _api;

    }

    public class ApiObject
    {
        public ApiOtherObject Other;
        public ApiRuinationUtilsObject RuinationUtils;
        public ApiUEFNFilesObject UEFNFiles;
        public List<ApiTransformCharacterObject> TransformCharacters = new();
        public List<ApiUEFNSkinObject> Characters = new();
        public List<string> PremiumUsers = new();
        public List<ApiBundleObject> Bundles = new();
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
    }

    public class ApiRuinationUtilsObject
    {
        public string Url;
        public string Filename;
        public string Processname;
        public string Version;
    }

    public class ApiUEFNFilesObject
    {
        public string pak;
        public string sig;
        public string ucas;
        public string utoc;
        public string FileToUse;
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
        public List<ApiUEFNSkinTextureSwapObject> TextureSwaps = new();
        public bool UseIdleEffectPackage = false;
    }

    public class ApiUEFNSkinTextureSwapObject
    {
        public string From;
        public string To;
        public List<PluginAssetSwapSwapModel> Swaps = new();
    }

    public class ApiBundleObject
    {
        public string Name;
        public string ID;
        public string Icon;
        public List<ApiBundleItemObject> Items = new();
    }

    public class ApiBundleItemObject
    {
        public string Name;
        public string ID;
        public string Type;
    }

}

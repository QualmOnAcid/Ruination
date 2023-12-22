using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebviewAppShared.Models
{
    public class PluginModel
    {
        public string Type = "";
        public string ID = "";

        public string Name = "";
        public string Description = "";
        public string Rarity = "";
        public string Icon = "";

        public PluginOptionModel Option = new();

        public PluginUefnSkinModel Skin = new();

        public PluginUEFNFilesModel Files = new();
        public List<PluginAssetSwapModel> Swaps;
        public List<string> Materials;
    }

    public class PluginAssetSwapModel
    {
        public string Asset;
        public string ToAsset;
    }

    public class PluginUEFNFilesModel
    {
        public string pak;
        public string sig;
        public string ucas;
        public string utoc;
    }

    public class PluginOptionModel
    {
        public string Name = "";
        public string Description = "";
        public string Rarity = "";
        public string Icon = "";
    }

    public class PluginUefnSkinModel
    {
        public string Mesh = "";
        public string Skeleton = "";
        public string Animation = "";
        public string PartmodifierBlueprint = "";
        public string IdleEffectNiagara = "";
    }

}

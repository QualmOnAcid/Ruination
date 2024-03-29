﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ruination_v2.Models;

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

    public string FilePath = null;

    public async Task Delete(bool switchTab = true)
    {
        if (this.FilePath == null) return;
        if (!System.IO.File.Exists(this.FilePath)) return;

        System.IO.File.Delete(this.FilePath);
    }
}

public class PluginAssetSwapModel
{
    public string Asset;
    public string ToAsset;
    public List<PluginAssetSwapSwapModel> Swaps = null;
}

public class PluginAssetSwapSwapModel
{
    public string Search;
    public string Replace;
    public string Type;
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
    public string HIDPath = "";
    public string IdleFXSocket = "";
    public bool UseIdleEffectPackage = false;
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebviewAppShared.Utils
{
    public class Rarities
    {

        public static async Task<string> GetRarityImage(string rarity)
        {
            rarity = rarity.ToLower();

            Dictionary<string, string> raritiesImages = new Dictionary<string, string>();
            raritiesImages.Add("common", "https://cdn.discordapp.com/attachments/1047519391979425812/1142977990230294628/common.png");
            raritiesImages.Add("epic", "https://cdn.discordapp.com/attachments/1047519391979425812/1142977988812619777/epic.png");
            raritiesImages.Add("legendary", "https://cdn.discordapp.com/attachments/1047519391979425812/1142977989177528320/legendary.png");
            raritiesImages.Add("rare", "https://cdn.discordapp.com/attachments/1047519391979425812/1142977989412405329/rare.png");
            raritiesImages.Add("uncommon", "https://cdn.discordapp.com/attachments/1047519391979425812/1142977989739548692/uncommon.png");
            raritiesImages.Add("lava", "https://cdn.discordapp.com/attachments/1047519391979425812/1142984814857834576/lava.png");
            raritiesImages.Add("marvel", "https://cdn.discordapp.com/attachments/1047519391979425812/1143208920324788284/marvel.png");
            raritiesImages.Add("dark", "https://cdn.discordapp.com/attachments/1047519391979425812/1143209931709874286/dark.png");
            raritiesImages.Add("dc", "https://cdn.discordapp.com/attachments/1047519391979425812/1143210661338423316/dcseries.png");
            raritiesImages.Add("icon", "https://cdn.discordapp.com/attachments/1047519391979425812/1143212958177034340/latest.png");
            raritiesImages.Add("frozen", "https://cdn.discordapp.com/attachments/1047519391979425812/1143213256366895145/latest.png");
            raritiesImages.Add("starwars", "https://cdn.discordapp.com/attachments/1047519391979425812/1143214373930807356/latest.png");
            raritiesImages.Add("shadow", "https://cdn.discordapp.com/attachments/1047519391979425812/1143214686905573536/latest.png");
            raritiesImages.Add("slurp", "https://cdn.discordapp.com/attachments/1047519391979425812/1143214788713922662/latest.png");
            raritiesImages.Add("gaminglegends", "https://cdn.discordapp.com/attachments/1047519391979425812/1143215207162859540/latest.png");
            
            if (!raritiesImages.ContainsKey(rarity))
                rarity = "common";

            return raritiesImages[rarity];
        }

        public static string GetRarityColor(string rarity)
        {
            rarity = rarity.ToLower();

            if (rarity == "uncommon") return "#88e339";
            if (rarity == "rare") return "#37d0ff";
            if (rarity == "epic") return "#ea5eff";
            if (rarity == "legendary") return "#e98d4b";

            if (rarity == "dark") return "#FF4FC5";
            if (rarity == "frozen") return "#D8EDFF";
            if (rarity == "icon") return "#5CF2F3";
            if (rarity == "lava") return "#FACE36";
            if (rarity == "marvel") return "#ED1C24";
            if (rarity == "shadow") return "#FFFFFF";
            if (rarity == "gaminglegends") return "#8078FF";
            if (rarity == "dc") return "#007AF1";
            if (rarity == "slurp") return "#1DF9F7";
            if (rarity == "starwars") return "#FFD800";

            return "#a6a2a2";
        }

        public static List<string> GetRarities() => new List<string>()
        {
            "Common",
            "Uncommon",
            "Rare",
            "Epic",
            "Legendary",
            "Dark",
            "Frozen",
            "Icon",
            "Lava",
            "Marvel",
            "Shadow",
            "GamingLegends",
            "DC",
            "Slurp",
            "Starwars"
        };

    }
}

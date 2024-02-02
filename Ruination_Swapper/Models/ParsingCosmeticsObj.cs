using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebviewAppShared.Models
{
    public class ParsingCosmeticsObj
    {
        public List<ParsingCosmeticsItemObj> data = new();

    }

    public class ParsingCosmeticsItemObj
    {
        public string id;
        public string name;
        public string description;
        public ParsingCosmeticsItemSeriesObj series;
        public ParsingCosmeticsItemTypeObj type;
        public ParsingCosmeticsItemRarityObj rarity;
        public string path;
        public string definitionPath;
        public string added;
    }

    public class ParsingCosmeticsItemTypeObj
    {
        public string value;
    }

    public class ParsingCosmeticsItemRarityObj
    {
        public string value;
    }
    public class ParsingCosmeticsItemSeriesObj
    {
        public string value;
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ruination_v2.Models
{
    public class Item
    {
        public string name;
        public string id;
        public string description;
        public string rarity;
        public string icon;
        public string added;
        public string path;
        public string definitionPath;
        public ItemType Type;
        public string rarcolor;
        public string series;
        public bool IsTransformCharacter = false;
        public bool isDefault = false;
        public int backendReleaseValue;
        public string subType = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebviewAppShared.Models
{
    public class ConvertedItem
    {
        public string ID;
        public string OptionID;
        public string Type;
        public string Name;
        public Dictionary<string, byte[]> Assets = new();
    }
}

﻿using CUE4Parse.UE4.IO.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUE4Parse
{
    public class CUtils
    {
        public static IoPackageExport Export = new();
        public static bool isExport = false;
        public static int fragmentindex = 0;
        public static long EmoteHeaderPos = 0;
        public static bool savePropertyBytes = false;
        public class IoPackageExport
        {
            public long Offset;
            public string UcasFile;
            public string UtocFile;
            public long UtocCBlockOffset;
            public long UtocLOOffset;
            public long UtocLOSize;
            public bool Compressed;
            public long compressedsize;
            public FIoOffsetAndLength OffsetAndLengthEntry;
            public FIoStoreTocCompressedBlockEntry CompressionBlock;
            public int PartitionCount;
            public int partitionIndex;
        }
    }
}

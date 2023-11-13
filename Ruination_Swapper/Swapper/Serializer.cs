using CUE4Parse.UE4.Assets;
using CUE4Parse.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WebviewAppShared.Utils;

namespace WebviewAppShared.Swapper
{
    public class Serializer
    {

        private IoPackage IoPackage;

        public Serializer(IoPackage IoPackage)
        {
            this.IoPackage = IoPackage;
        }

        public byte[] Serialize(ulong customSerialCookedSize = 0)
        {
            Logger.Log("Serializing Package - " + IoPackage.Name);
            List<byte> serialization = new List<byte>();

            var oldNameMap = new List<string>();

            foreach (var entry in IoPackage.NameMap)
                oldNameMap.Add(entry.Name);

            int AssetLengthDifference = GetNamemapSize(IoPackage.NameMapAsStrings.ToList()) - GetNamemapSize(oldNameMap);

            //Zen Summary
            serialization.AddRange(ConvertToBytes(IoPackage.zenSummary.bHasVersioningInfo));
            serialization.AddRange(ConvertToBytes((uint)(IoPackage.zenSummary.HeaderSize + (uint)AssetLengthDifference)));
            serialization.AddRange(ConvertToBytes(IoPackage.zenSummary.Name));
            serialization.AddRange(ConvertToBytes((uint)IoPackage.zenSummary.PackageFlags));
            serialization.AddRange(ConvertToBytes((uint)(IoPackage.zenSummary.CookedHeaderSize + (uint)AssetLengthDifference)));
            serialization.AddRange(ConvertToBytes((uint)(IoPackage.zenSummary.ImportedPublicExportHashesOffset + (uint)AssetLengthDifference)));
            serialization.AddRange(ConvertToBytes((uint)(IoPackage.zenSummary.ImportMapOffset + (uint)AssetLengthDifference)));
            serialization.AddRange(ConvertToBytes((uint)(IoPackage.zenSummary.ExportMapOffset + (uint)AssetLengthDifference)));
            serialization.AddRange(ConvertToBytes((uint)(IoPackage.zenSummary.ExportBundleEntriesOffset + (uint)AssetLengthDifference)));
            serialization.AddRange(ConvertToBytes((uint)(IoPackage.zenSummary.DependencyBundleHeadersOffset + (uint)AssetLengthDifference)));
            serialization.AddRange(ConvertToBytes((uint)(IoPackage.zenSummary.DependencyBundleEntriesOffset + (uint)AssetLengthDifference)));
            serialization.AddRange(ConvertToBytes((uint)(IoPackage.zenSummary.ImportedPackageNamesOffset + (uint)AssetLengthDifference)));

            //NameMap

            // Entry Size -> All String Count -> Hash Version -> String Hashes -> String Lengths -> Strings

            serialization.AddRange(ConvertToBytes((uint)IoPackage.NameMapAsStrings.Length));
            serialization.AddRange(ConvertToBytes((uint)IoPackage.NameMapAsStrings.Sum(x => x.Length)));
            serialization.AddRange(ConvertToBytes(IoPackage.HashVersion));

            for (int i = 0; i < IoPackage.NameMapAsStrings.Length; i++)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(IoPackage.NameMapAsStrings[i].ToLower());
                ulong cityHash64 = CityHash.CityHash64(buffer);
                serialization.AddRange(ConvertToBytes(cityHash64));
            }

            serialization.AddRange(new byte[] { 0 });

            for (int i = 0; i < IoPackage.NameMapAsStrings.Length; i++)
            {
                serialization.AddRange(ConvertToBytes((uint)IoPackage.NameMapAsStrings[i].Length).Take(2));
            }

            serialization.RemoveAt(serialization.Count - 1);

            for (int i = 0; i < IoPackage.NameMapAsStrings.Length; i++)
            {
                serialization.AddRange(Encoding.UTF8.GetBytes(IoPackage.NameMapAsStrings[i]));
            }

            serialization.AddRange(IoPackage.BulkDataArray);

            //Properties and Export Map

            serialization.AddRange(IoPackage.PropertiesWithExportMap);

            //Overwrite Export Map

            int ulongsize = sizeof(ulong);

            int SINGLE_EXPORTMAP_SIZE = 72;

            //Mainly for UEFN Swaps
            if (customSerialCookedSize != 0)
            {

                int cookedSerialSizeOffset =
                    IoPackage.zenSummary.ExportMapOffset + AssetLengthDifference + SINGLE_EXPORTMAP_SIZE + ulongsize; //ulongsize skips first property
                    
                serialization.RemoveRange(cookedSerialSizeOffset, ulongsize);
                serialization.InsertRange(cookedSerialSizeOffset, ConvertToBytes(customSerialCookedSize));
            }
            else
            {
                int exportsMapEnding =
                    IoPackage.zenSummary.ExportMapOffset + AssetLengthDifference + (SINGLE_EXPORTMAP_SIZE * IoPackage.ExportMap.Length);

                exportsMapEnding -= 16;

                serialization.RemoveRange(exportsMapEnding, ulongsize);
                serialization.InsertRange(exportsMapEnding, ConvertToBytes(IoPackage.ExportMap.Last().PublicExportHash));
            }

            return serialization.ToArray();
        }

        public byte[] ConvertToBytes<T>(T value)
        {
            byte[] array = new byte[Marshal.SizeOf(value)];
            Unsafe.WriteUnaligned(ref array[0], value);
            return array;
        }

        //https://github.com/EpicGames/UnrealEngine/blob/release/Engine/Source/Runtime/CoreUObject/Public/Serialization/AsyncLoading2.h

        public ulong GetExportHash()
        {
            ulong exportHashHigh = 0;
            ulong exportHashLow = 0;

            ulong result = (exportHashHigh << 32) | exportHashLow;

            return result;
        }

        public int GetNamemapSize(List<string> nameMap)
        {
            int count = 0;

            foreach (var entry in nameMap)
            {
                count += 8; //Hash
                count += 2; //String length
                count += entry.Length; //String size
            }

            return count;
        }

    }
}

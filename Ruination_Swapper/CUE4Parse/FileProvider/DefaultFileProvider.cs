﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using WebviewAppShared.Utils;

namespace CUE4Parse.FileProvider
{
    public class DefaultFileProvider : AbstractVfsFileProvider
    {
        private readonly DirectoryInfo _workingDirectory;
        private readonly DirectoryInfo[] _extraDirectories;
        private readonly SearchOption _searchOption;

        public DefaultFileProvider(string directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null)
            : this(new DirectoryInfo(directory), searchOption, isCaseInsensitive, versions) { }
        public DefaultFileProvider(DirectoryInfo directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null)
            : this(directory, Array.Empty<DirectoryInfo>(), searchOption, isCaseInsensitive, versions) { }
        public DefaultFileProvider(DirectoryInfo directory, DirectoryInfo[] extraDirectories, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null)
            : base(isCaseInsensitive, versions)
        {
            _workingDirectory = directory;
            _extraDirectories = extraDirectories;
            _searchOption = searchOption;
        }

        public List<string> UnusedFiles = new();

        public override void Initialize()
        {
            if (!_workingDirectory.Exists)
                throw new DirectoryNotFoundException("Given working directory must exist");

            var availableFiles = new List<Dictionary<string, GameFile>> { IterateFiles(_workingDirectory, _searchOption) };
            if (_extraDirectories is { Length: > 0 })
            {
                availableFiles.AddRange(_extraDirectories.Select(directory => IterateFiles(directory, _searchOption)));
            }

            foreach (var osFiles in availableFiles)
            {
                _files.AddFiles(osFiles);
            }
        }

        public void Initialize(string singleFile)
        {
            if (!_workingDirectory.Exists)
                throw new DirectoryNotFoundException("Given working directory must exist");

            var availableFiles = new List<Dictionary<string, GameFile>> { IterateFiles(_workingDirectory, _searchOption, singleFile) };
            if (_extraDirectories is { Length: > 0 })
            {
                availableFiles.AddRange(_extraDirectories.Select(directory => IterateFiles(directory, _searchOption, singleFile)));
            }

            foreach (var osFiles in availableFiles)
            {
                _files.AddFiles(osFiles);
            }
        }

        private Dictionary<string, GameFile> IterateFiles(DirectoryInfo directory, SearchOption option, string filetoload = "")
        {
            var osFiles = new Dictionary<string, GameFile>();
            if (!directory.Exists) return osFiles;

            // Look for .uproject file to get the correct mount point
            var uproject = directory.GetFiles("*.uproject", SearchOption.TopDirectoryOnly).FirstOrDefault();
            string mountPoint;
            if (uproject != null)
            {
                mountPoint = uproject.Name.SubstringBeforeLast('.') + '/';
            }
            else
            {
                // Or use the directory name
                mountPoint = directory.Name + '/';
            }

            // In .uproject mode, we must recursively look for files
            option = uproject != null ? SearchOption.AllDirectories : option;

            foreach (var file in directory.EnumerateFiles("*.*", option))
            {
                if (!string.IsNullOrEmpty(filetoload) && !Path.GetFileNameWithoutExtension(file.Name).ToLower().Equals("global"))
                {

                    if (!Path.GetFileNameWithoutExtension(file.Name).ToLower().Equals(filetoload.ToLower()))
                    {
                        continue;
                    }

                    Logger.Log("Loading file: " + file.Name);

                }
                else
                {
                    if (file.Name.ToLower().Contains("optional"))
                    {
                        continue;
                    }
                }

                var upperExt = file.Extension.SubstringAfter('.').ToUpper();

                // Only load containers if .uproject file is not found
                if (uproject == null && upperExt is "PAK" or "UTOC")
                {
                    RegisterVfs(file.FullName, new Stream[] { file.OpenRead() }, it => new FStreamArchive(it, File.OpenRead(it), Versions));
                    continue;
                }

                // Register local file only if it has a known extension, we don't need every file
                if (!GameFile.Ue4KnownExtensions.Contains(upperExt, StringComparer.OrdinalIgnoreCase))
                    continue;

                var osFile = new OsGameFile(_workingDirectory, file, mountPoint, Versions);
                if (IsCaseInsensitive) osFiles[osFile.Path.ToLowerInvariant()] = osFile;
                else osFiles[osFile.Path] = osFile;
            }

            List<string> optionalFileExtensions = new List<string>()
            {
                "pak",
                "sig",
                "ucas",
                "utoc"
            };

            UnusedFiles.Add(_workingDirectory.FullName + "\\" + API.GetApi().UEFNFiles.FileToUse);

            for (int i = 0; i < 11; i++)
            {
                string targetFile = _workingDirectory.FullName + "\\pakchunk" + i + "optional-WindowsClient";
                if (optionalFileExtensions.All(x => File.Exists(targetFile + "." + x)))
                {
                    UnusedFiles.Add(targetFile);
                }
            }

            return osFiles;
        }

        public bool DoesAssetExist(string path)
        {
            try
            {
                this.SaveAsset(path);
                return true;
            } catch(Exception e)
            {
                return false;
            }
        }

    }
}

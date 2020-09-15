using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UnityVRPatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
                var gameExePath = args[0];
                var gamePath = Path.GetDirectoryName(gameExePath);
                var gameName = Path.GetFileNameWithoutExtension(gameExePath);
                var dataPath = Path.Combine(gamePath, $"{gameName}_Data/");
                var gameManagersPath = Path.Combine(dataPath, $"globalgamemanagers");
                var gameManagersBackupPath = CreateGameManagersBackup(gameManagersPath);
                var patcherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                var classDataPath = Path.Combine(patcherPath, "classdata.tpk");

                CopyPlugins(patcherPath, dataPath);
                PatchVR(gameManagersBackupPath, gameManagersPath, classDataPath);

                Console.WriteLine("Patched successfully, probably.");
            }
            finally
            {
                Console.WriteLine("Press any key to close this console.");
                Console.ReadKey();
            }
        }

        static void CopyPlugins(string patcherPath, string dataPath)
        {
            Console.WriteLine("Copying plugins...");

            var gamePluginsPath = Path.Combine(dataPath, "Plugins");
            if (!Directory.Exists(gamePluginsPath))
            {
                Directory.CreateDirectory(gamePluginsPath);
            }
            var patcherPluginsPath = Path.Combine(patcherPath, "Plugins");
            var pluginFilePaths = Directory.GetFiles(patcherPluginsPath);
            Console.WriteLine($"Found plugins:\n {string.Join(",\n", pluginFilePaths)}");
            foreach (var filePath in pluginFilePaths)
            {
                File.Copy(filePath, Path.Combine(gamePluginsPath, Path.GetFileName(filePath)), true);
            }
        }

        static string CreateGameManagersBackup(string gameManagersPath)
        {
            Console.WriteLine($"Backing up '{gameManagersPath}'...");
            var backupPath = gameManagersPath + ".bak";
            if (File.Exists(backupPath))
            {
                Console.WriteLine($"Backup already exists.");
                return backupPath;
            }
            File.Copy(gameManagersPath, backupPath);
            Console.WriteLine($"Created backup in '{backupPath}'");
            return backupPath;
        }

        static void PatchVR(string gameManagersBackupPath, string gameManagersPath, string classDataPath)
        {
            Console.WriteLine("Patching globalgamemanagers...");
            Console.WriteLine($"Using classData file from path '{classDataPath}'");

            AssetsManager am = new AssetsManager();
            am.LoadClassPackage(classDataPath);
            AssetsFileInstance ggm = am.LoadAssetsFile(gameManagersBackupPath, false);
            AssetsFile ggmFile = ggm.file;
            AssetsFileTable ggmTable = ggm.table;
            am.LoadClassDatabaseFromPackage(ggmFile.typeTree.unityVersion);

            List<AssetsReplacer> replacers = new List<AssetsReplacer>();

            AssetFileInfoEx buildSettings = ggmTable.GetAssetInfo(11);
            AssetTypeValueField buildSettingsBase = am.GetATI(ggmFile, buildSettings).GetBaseField();
            AssetTypeValueField enabledVRDevices = buildSettingsBase.Get("enabledVRDevices").Get("Array");
            AssetTypeTemplateField stringTemplate = enabledVRDevices.templateField.children[1];
            AssetTypeValueField[] vrDevicesList = new AssetTypeValueField[] { StringField("OpenVR", stringTemplate) };
            enabledVRDevices.SetChildrenList(vrDevicesList);

            replacers.Add(new AssetsReplacerFromMemory(0, buildSettings.index, (int)buildSettings.curFileType, 0xffff, buildSettingsBase.WriteToByteArray()));

            using (AssetsFileWriter writer = new AssetsFileWriter(File.OpenWrite(gameManagersPath)))
            {
                ggmFile.Write(writer, 0, replacers, 0);
            }
        }

        static AssetTypeValueField StringField(string str, AssetTypeTemplateField template)
        {
            return new AssetTypeValueField()
            {
                children = null,
                childrenCount = 0,
                templateField = template,
                value = new AssetTypeValue(EnumValueTypes.ValueType_String, str)
            };
        }
    }
}

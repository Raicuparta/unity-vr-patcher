using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                var gamePath = $"{gameExePath}/..";
                var gameName = Path.GetFileNameWithoutExtension(gameExePath);
                var dataPath = Path.Combine(gamePath, $"{gameName}_Data/");
                var pluginsPath = Path.Combine(dataPath, "Plugins");
                var gameManagersPath = Path.Combine(dataPath, $"globalgamemanagers");
                var patcherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                var classDataPath = Path.Combine(patcherPath, "classdata.tpk");
                PatchVR(gameManagersPath, classDataPath);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        static void PatchVR(string gameManagersPath, string classDataPath)
        {
            AssetsManager am = new AssetsManager();
            am.LoadClassPackage(classDataPath);
            AssetsFileInstance ggm = am.LoadAssetsFile(gameManagersPath, false);
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

            using (AssetsFileWriter writer = new AssetsFileWriter(File.OpenWrite(gameManagersPath + ".patched")))
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

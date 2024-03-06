using System.Diagnostics;
using AssetsTools.NET.Extra;

namespace AssetBundleRewriter;
using AssetsTools.NET;
public class Cli {
    static void Main(string[] args) {
        var manager = new AssetsManager();

        Stopwatch s = new Stopwatch();
        s.Start();
        var bundleFileInstance = manager.LoadBundleFile(args[0], true);
        var bundle = bundleFileInstance.file;

        void VisitAssetTypeValueField(AssetsFile file, AssetTypeValueField f, ref bool modified) {
            if (f.TemplateField.Type.Contains("Ptr<")) {
                // Console.WriteLine(f.TemplateField.Type);
                var fileID = f["m_FileID"].Value.AsInt;
                if (fileID != 0) {
                    var name = file.Metadata.Externals[fileID - 1].PathName;
                    if (name.StartsWith("archive:/CAB-")) {
                        Console.WriteLine(f["m_PathID"].Value.AsLong);
                        f["m_PathID"].Value.AsInt = 255;
                        modified = true;
                    }
                    else {
                        Console.WriteLine(f["m_PathID"].Value.AsLong);
                    }
                }
                else {
                    Console.WriteLine(f["m_PathID"].Value.AsLong);
                }
            }
            foreach(var c in f.Children) {
                VisitAssetTypeValueField(file, c, ref modified);
                
                // Console.WriteLine(c.TemplateField.Type);
                // // Console.WriteLine(c.TemplateField.ValueType);
                // // Console.WriteLine(c.TemplateField.Name);
                // foreach(var c2 in c.Children) {
                //     Console.WriteLine($"  {c2.TemplateField.Type} {c2.FieldName}={c2.Value}");
                // }
            }
        }

        for(var i = 0; i < bundle.BlockAndDirInfo.DirectoryInfos.Count; i++) {
            var assetsFileInstance = manager.LoadAssetsFileFromBundle(bundleFileInstance, i, false);
            var assetsFile = assetsFileInstance.file;
            
            foreach(var external in assetsFile.Metadata.Externals) {
                Console.WriteLine($"{external.PathName} {external.OriginalPathName} {external.VirtualAssetPathName} {external.Guid}");
            }
            
            foreach(var assetFileInfo in assetsFile.AssetInfos) {
                var baseField = manager.GetBaseField(assetsFileInstance, assetFileInfo, AssetReadFlags.None);
                var modified = false;
                VisitAssetTypeValueField(assetsFile, baseField, ref modified);
                if (modified) {
                    assetFileInfo.SetNewData(baseField);
                }
            }
            
            bundle.BlockAndDirInfo.DirectoryInfos[i].SetNewData(assetsFile);
        }
        
        
        using (AssetsFileWriter writer = new AssetsFileWriter(args[0] + "_mod"))
        {
            bundle.Write(writer);
        }
        
        s.Stop();
        Console.WriteLine(s);
    }
}
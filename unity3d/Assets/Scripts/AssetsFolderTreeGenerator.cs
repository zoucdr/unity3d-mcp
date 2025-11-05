using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

public class AssetsFolderTreeGenerator
{
    [MenuItem("Tools/Generate Assets YAML Tree")]
    public static void GenerateYamlTree()
    {
        string assetsPath = Application.dataPath;
        StringBuilder yaml = new StringBuilder();

        yaml.AppendLine("Assets:");
        GenerateFolderTree(assetsPath, yaml, 1);

        string outputPath = Path.Combine(Application.dataPath, "../AssetsTree.yaml");
        File.WriteAllText(outputPath, yaml.ToString());

        Debug.Log($"YAML树结构已生成到: {outputPath}");
        Debug.Log("内容预览:");
        Debug.Log(yaml.ToString());

        // 刷新项目窗口
        AssetDatabase.Refresh();
    }

    private static void GenerateFolderTree(string path, StringBuilder yaml, int depth)
    {
        try
        {
            string[] directories = Directory.GetDirectories(path);

            foreach (string dir in directories)
            {
                string folderName = Path.GetFileName(dir);

                // 跳过隐藏文件夹和.meta文件夹
                if (folderName.StartsWith(".") || folderName.EndsWith(".meta"))
                    continue;

                string indent = new string(' ', depth * 2);

                // 检查是否有子文件夹
                string[] subDirs = Directory.GetDirectories(dir);
                bool hasSubFolders = false;

                foreach (string subDir in subDirs)
                {
                    string subFolderName = Path.GetFileName(subDir);
                    if (!subFolderName.StartsWith(".") && !subFolderName.EndsWith(".meta"))
                    {
                        hasSubFolders = true;
                        break;
                    }
                }

                if (hasSubFolders)
                {
                    yaml.AppendLine($"{indent}- {folderName}:");
                    GenerateFolderTree(dir, yaml, depth + 1);
                }
                else
                {
                    yaml.AppendLine($"{indent}- {folderName}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"处理目录时出错: {e.Message}");
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UniMcp.Models;

namespace UniMcp.Tools
{
    /// <summary>
    /// 专门的模型管理工具，提供模型的导入、修改、复制、删除等操作
    /// 对应方法名: manage_model
    /// </summary>
    [ToolName("edit_model", "资源管理")]
    public class EditModel : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // 操作类型
                new MethodStr("action", "操作类型", false)
                    .SetEnumValues("import", "modify", "duplicate", "delete", "get_info", "search", "set_import_settings", "extract_materials", "optimize", "remap_materials")
                    .AddExamples("import", "get_info"),
                
                // 模型路径
                new MethodStr("path", "模型资源路径", false)
                    .AddExamples("Assets/Models/Character.fbx", "Assets/Models/Building.obj"),
                
                // 源文件路径
                new MethodStr("source_file", "源文件路径")
                    .AddExamples("D:/Models/character.fbx", "C:/Assets/model.obj"),
                
                // 目标路径
                new MethodStr("destination", "目标路径")
                    .AddExamples("Assets/Models/Copy/", "Assets/NewModels/"),
                
                // 搜索模式
                new MethodStr("query", "搜索模式")
                    .AddExamples("*.fbx", "*.obj"),
                
                // 递归搜索
                new MethodBool("recursive", "递归搜索"),
                
                // 导入设置
                new MethodObj("import_settings", "导入设置"),
                
                // 缩放因子
                new MethodFloat("scale_factor", "缩放因子")
                    .SetRange(0.01f, 100f)
                    .AddExample("1.0"),
                
                // 使用文件缩放
                new MethodBool("use_file_scale", "使用文件缩放"),
                
                // 导入混合形状
                new MethodBool("import_blend_shapes", "导入混合形状"),
                
                // 导入相机
                new MethodBool("import_cameras", "导入相机"),
                
                // 保持层级
                new MethodBool("preserve_hierarchy", "保持层级"),
                
                // 优化网格
                new MethodBool("optimize_mesh", "优化网格"),
                
                // 次要UV硬角度
                new MethodFloat("secondary_uv_hard_angle", "次要UV硬角度")
                    .SetRange(0f, 180f)
                    .AddExample("88.0"),
                
                // 次要UV打包边距
                new MethodFloat("secondary_uv_pack_margin", "次要UV打包边距")
                    .SetRange(1f, 64f)
                    .AddExample("4.0"),
                
                // 次要UV角度扭曲
                new MethodFloat("secondary_uv_angle_distortion", "次要UV角度扭曲")
                    .SetRange(1f, 75f)
                    .AddExample("8.0"),
                
                // 次要UV面积扭曲
                new MethodFloat("secondary_uv_area_distortion", "次要UV面积扭曲")
                    .SetRange(1f, 75f)
                    .AddExample("15.0"),
                
                // 次要UV边缘扭曲
                new MethodFloat("secondary_uv_edge_distortion", "次要UV边缘扭曲")
                    .SetRange(1f, 75f)
                    .AddExample("10.0"),
                
                // 启用读写
                new MethodBool("read_write_enabled", "启用读写"),
                
                // 导入材质
                new MethodBool("import_materials", "导入材质"),
                
                // 材质搜索模式
                new MethodStr("material_search", "材质搜索模式")
                    .SetEnumValues("Local", "RecursiveUp", "Everywhere")
                    .AddExample("Local"),
                
                // 提取材质
                new MethodBool("extract_materials", "提取材质"),
                
                // 网格压缩
                new MethodStr("mesh_compression", "网格压缩")
                    .SetEnumValues("Off", "Low", "Medium", "High")
                    .AddExample("Off"),
                
                // 添加碰撞器
                new MethodBool("add_collider", "添加碰撞器"),
                
                // 焊接顶点
                new MethodBool("weld_vertices", "焊接顶点"),
                
                // 传统混合形状法线
                new MethodBool("legacy_blend_shape_normals", "传统混合形状法线"),
                
                // 切线模式
                new MethodStr("tangents", "切线模式")
                    .SetEnumValues("Default", "None", "Calculate", "Import")
                    .AddExample("Default"),
                
                // 平滑度来源
                new MethodStr("smoothness_source", "平滑度来源")
                    .SetEnumValues("None", "DiffuseAlpha", "SpecularAlpha")
                    .AddExample("None"),
                
                // 平滑度
                new MethodFloat("smoothness", "平滑度")
                    .SetRange(0f, 1f)
                    .AddExample("0.5"),
                
                // 法线导入模式
                new MethodStr("normal_import_mode", "法线导入模式")
                    .SetEnumValues("Default", "None", "Calculate", "Import")
                    .AddExample("Default"),
                
                // 法线贴图模式
                new MethodStr("normal_map_mode", "法线贴图模式")
                    .SetEnumValues("Default", "OpenGL", "DirectX")
                    .AddExample("Default"),
                
                // 高度贴图模式
                new MethodStr("height_map_mode", "高度贴图模式")
                    .SetEnumValues("Default", "OpenGL", "DirectX")
                    .AddExample("Default"),
                
                // 材质重定向映射
                new MethodObj("material_remaps", "材质重定向映射")
            };
        }

        /// <summary>
        /// 创建状态树
        /// </summary>
        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("import", ImportModel)
                    .Leaf("modify", ModifyModel)
                    .Leaf("duplicate", DuplicateModel)
                    .Leaf("delete", DeleteModel)
                    .Leaf("get_info", GetModelInfo)
                    .Leaf("search", SearchModels)
                    .Leaf("set_import_settings", SetModelImportSettings)
                    .Leaf("extract_materials", ExtractModelMaterials)
                    .Leaf("optimize", OptimizeModel)
                    .Leaf("remap_materials", RemapMaterials)
                .Build();
        }

        // --- 状态树操作方法 ---

        private object ImportModel(JsonClass args)
        {
            string sourceFile = args["source_file"]?.Value;
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(sourceFile))
                return Response.Error("'source_file' is required for import.");
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for import.");

            string fullPath = SanitizeAssetPath(path);
            string directory = Path.GetDirectoryName(fullPath);

            // 确保目录存在
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), directory)))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), directory));
                AssetDatabase.Refresh();
            }

            if (AssetExists(fullPath))
                return Response.Error($"Model already exists at path: {fullPath}");

            try
            {
                // 检查源文件是否存在
                if (!File.Exists(sourceFile))
                    return Response.Error($"Source file not found: {sourceFile}");

                // 复制文件到目标路径
                string targetFilePath = Path.Combine(Directory.GetCurrentDirectory(), fullPath);
                File.Copy(sourceFile, targetFilePath);

                // 导入设置
                JsonClass importSettings = args["import_settings"] as JsonClass;
                if (importSettings != null && importSettings.Count > 0)
                {
                    AssetDatabase.ImportAsset(fullPath);
                    ModelImporter importer = AssetImporter.GetAtPath(fullPath) as ModelImporter;
                    if (importer != null)
                    {
                        ApplyModelImportSettings(importer, importSettings);
                        importer.SaveAndReimport();
                    }
                }
                else
                {
                    AssetDatabase.ImportAsset(fullPath);
                }

                McpLogger.Log($"[ManageModel] Imported model from '{sourceFile}' to '{fullPath}'");
                return Response.Success($"Model imported successfully to '{fullPath}'.", GetModelData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to import model to '{fullPath}': {e.Message}");
            }
        }

        private object ModifyModel(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonClass importSettings = args["import_settings"] as JsonClass;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for modify.");
            if (importSettings == null || importSettings.Count == 0)
                return Response.Error("'import_settings' are required for modify.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Model not found at path: {fullPath}");

            try
            {
                ModelImporter importer = AssetImporter.GetAtPath(fullPath) as ModelImporter;
                if (importer == null)
                    return Response.Error($"Failed to get ModelImporter for '{fullPath}'");

                bool modified = ApplyModelImportSettings(importer, importSettings);

                if (modified)
                {
                    importer.SaveAndReimport();
                    McpLogger.Log($"[ManageModel] Modified model import settings at '{fullPath}'");
                    return Response.Success($"Model '{fullPath}' modified successfully.", GetModelData(fullPath));
                }
                else
                {
                    return Response.Success($"No applicable settings found to modify for model '{fullPath}'.", GetModelData(fullPath));
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify model '{fullPath}': {e.Message}");
            }
        }

        private object DuplicateModel(JsonClass args)
        {
            string path = args["path"]?.Value;
            string destinationPath = args["destination"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for duplicate.");

            string sourcePath = SanitizeAssetPath(path);
            if (!AssetExists(sourcePath))
                return Response.Error($"Source model not found at path: {sourcePath}");

            string destPath;
            if (string.IsNullOrEmpty(destinationPath))
            {
                destPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);
            }
            else
            {
                destPath = SanitizeAssetPath(destinationPath);
                if (AssetExists(destPath))
                    return Response.Error($"Model already exists at destination path: {destPath}");
                EnsureDirectoryExists(Path.GetDirectoryName(destPath));
            }

            try
            {
                bool success = AssetDatabase.CopyAsset(sourcePath, destPath);
                if (success)
                {
                    McpLogger.Log($"[ManageModel] Duplicated model from '{sourcePath}' to '{destPath}'");
                    return Response.Success($"Model '{sourcePath}' duplicated to '{destPath}'.", GetModelData(destPath));
                }
                else
                {
                    return Response.Error($"Failed to duplicate model from '{sourcePath}' to '{destPath}'.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error duplicating model '{sourcePath}': {e.Message}");
            }
        }

        private object DeleteModel(JsonClass args)
        {
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for delete.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Model not found at path: {fullPath}");

            try
            {
                bool success = AssetDatabase.DeleteAsset(fullPath);
                if (success)
                {
                    McpLogger.Log($"[ManageModel] Deleted model at '{fullPath}'");
                    return Response.Success($"Model '{fullPath}' deleted successfully.");
                }
                else
                {
                    return Response.Error($"Failed to delete model '{fullPath}'. Check logs or if the file is locked.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error deleting model '{fullPath}': {e.Message}");
            }
        }

        private object GetModelInfo(JsonClass args)
        {
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for get_info.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Model not found at path: {fullPath}");

            try
            {
                return Response.Success("Model info retrieved.", GetModelData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting info for model '{fullPath}': {e.Message}");
            }
        }

        private object SearchModels(JsonClass args)
        {
            string searchPattern = args["query"]?.Value;
            string pathScope = args["path"]?.Value;
            bool recursive = args["recursive"].AsBoolDefault(true);

            List<string> searchFilters = new List<string>();
            if (!string.IsNullOrEmpty(searchPattern))
                searchFilters.Add(searchPattern);
            searchFilters.Add("t:GameObject");

            string[] folderScope = null;
            if (!string.IsNullOrEmpty(pathScope))
            {
                folderScope = new string[] { SanitizeAssetPath(pathScope) };
                if (!AssetDatabase.IsValidFolder(folderScope[0]))
                {
                    LogWarning($"Search path '{folderScope[0]}' is not a valid folder. Searching entire project.");
                    folderScope = null;
                }
            }

            try
            {
                string[] guids = AssetDatabase.FindAssets(string.Join(" ", searchFilters), folderScope);
                List<object> results = new List<object>();

                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath))
                        continue;

                    // 检查是否是模型文件
                    if (IsModelFile(assetPath))
                    {
                        results.Add(GetModelData(assetPath));
                    }
                }

                McpLogger.Log($"[ManageModel] Found {results.Count} model(s)");
                return Response.Success($"Found {results.Count} model(s).", results);
            }
            catch (Exception e)
            {
                return Response.Error($"Error searching models: {e.Message}");
            }
        }

        private object SetModelImportSettings(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonClass importSettings = args["import_settings"] as JsonClass;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for set_import_settings.");
            if (importSettings == null || importSettings.Count == 0)
                return Response.Error("'import_settings' are required for set_import_settings.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Model not found at path: {fullPath}");

            try
            {
                ModelImporter importer = AssetImporter.GetAtPath(fullPath) as ModelImporter;
                if (importer == null)
                    return Response.Error($"Failed to get ModelImporter for '{fullPath}'");

                bool modified = ApplyModelImportSettings(importer, importSettings);

                if (modified)
                {
                    importer.SaveAndReimport();
                    McpLogger.Log($"[ManageModel] Set import settings on model '{fullPath}'");
                    return Response.Success($"Import settings set on model '{fullPath}'.", GetModelData(fullPath));
                }
                else
                {
                    return Response.Success($"No valid import settings found to set on model '{fullPath}'.", GetModelData(fullPath));
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error setting import settings on model '{fullPath}': {e.Message}");
            }
        }

        private object ExtractModelMaterials(JsonClass args)
        {
            string path = args["path"]?.Value;
            string extractPath = args["extract_path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for extract_materials.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Model not found at path: {fullPath}");

            try
            {
                ModelImporter importer = AssetImporter.GetAtPath(fullPath) as ModelImporter;
                if (importer == null)
                    return Response.Error($"Failed to get ModelImporter for '{fullPath}'");

                // 设置提取材质
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

                if (!string.IsNullOrEmpty(extractPath))
                {
                    string extractFullPath = SanitizeAssetPath(extractPath);
                    EnsureDirectoryExists(extractFullPath);
                    importer.materialLocation = ModelImporterMaterialLocation.External;
                    importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
                    importer.materialSearch = ModelImporterMaterialSearch.Everywhere;
                }

                importer.SaveAndReimport();

                McpLogger.Log($"[ManageModel] Extracted materials from model '{fullPath}'");
                return Response.Success($"Materials extracted from model '{fullPath}'.", GetModelData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error extracting materials from model '{fullPath}': {e.Message}");
            }
        }

        private object OptimizeModel(JsonClass args)
        {
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for optimize.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Model not found at path: {fullPath}");

            try
            {
                ModelImporter importer = AssetImporter.GetAtPath(fullPath) as ModelImporter;
                if (importer == null)
                    return Response.Error($"Failed to get ModelImporter for '{fullPath}'");

                // 应用优化设置
                // 替换过时的optimizeMesh API
                importer.optimizeMeshPolygons = true;
                importer.optimizeMeshVertices = true;
                importer.optimizeGameObjects = true;
                importer.weldVertices = true;
                importer.indexFormat = ModelImporterIndexFormat.Auto;
                importer.meshCompression = ModelImporterMeshCompression.Medium;

                importer.SaveAndReimport();

                McpLogger.Log($"[ManageModel] Optimized model '{fullPath}'");
                return Response.Success($"Model '{fullPath}' optimized successfully.", GetModelData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error optimizing model '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// 重定向模型的材质球
        /// </summary>
        private object RemapMaterials(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonClass materialRemaps = args["material_remaps"] as JsonClass;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for remap_materials.");
            if (materialRemaps == null || materialRemaps.Count == 0)
                return Response.Error("'material_remaps' is required for remap_materials.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Model not found at path: {fullPath}");

            try
            {
                ModelImporter importer = AssetImporter.GetAtPath(fullPath) as ModelImporter;
                if (importer == null)
                    return Response.Error($"Failed to get ModelImporter for '{fullPath}'");

                int remapCount = 0;
                foreach (var materialRemap in materialRemaps.Properties())
                {
                    string sourceName = materialRemap.Key;
                    string targetPath = materialRemap.Value?.Value;

                    if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(targetPath))
                        continue;

                    // 加载目标材质球
                    Material targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(SanitizeAssetPath(targetPath));
                    if (targetMaterial == null)
                    {
                        LogWarning($"[RemapMaterials] Target material not found at path: {targetPath}");
                        continue;
                    }

                    // 创建材质球标识符并添加重定向
                    var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName);
                    importer.AddRemap(id, targetMaterial);
                    remapCount++;

                    McpLogger.Log($"[RemapMaterials] Remapped material '{sourceName}' to '{targetPath}'");
                }

                if (remapCount > 0)
                {
                    importer.SaveAndReimport();
                    return Response.Success($"Successfully remapped {remapCount} materials for model '{fullPath}'.", GetModelData(fullPath));
                }
                else
                {
                    return Response.Error("No materials were remapped. Check your material names and target paths.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error remapping materials for model '{fullPath}': {e.Message}");
            }
        }

        // --- 内部辅助方法 ---

        /// <summary>
        /// 确保资产路径以"Assets/"开头
        /// </summary>
        private string SanitizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return "Assets/" + path.TrimStart('/');
            }
            return path;
        }

        /// <summary>
        /// 检查资产是否存在
        /// </summary>
        private bool AssetExists(string sanitizedPath)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(sanitizedPath)))
            {
                return true;
            }
            if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), sanitizedPath)))
            {
                return AssetDatabase.IsValidFolder(sanitizedPath);
            }
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), sanitizedPath)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private void EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return;
            string fullDirPath = Path.Combine(Directory.GetCurrentDirectory(), directoryPath);
            if (!Directory.Exists(fullDirPath))
            {
                Directory.CreateDirectory(fullDirPath);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 检查是否是模型文件
        /// </summary>
        private bool IsModelFile(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".fbx" || extension == ".obj" || extension == ".dae" ||
                   extension == ".3ds" || extension == ".dxf" || extension == ".skp" ||
                   extension == ".max" || extension == ".c4d" || extension == ".blend";
        }

        /// <summary>
        /// 应用模型导入设置
        /// </summary>
        private bool ApplyModelImportSettings(ModelImporter importer, JsonClass settings)
        {
            if (importer == null || settings == null)
                return false;
            bool modified = false;

            foreach (var setting in settings.Properties())
            {
                string settingName = setting.Key;
                JsonNode settingValue = setting.Value;

                try
                {
                    switch (settingName.ToLowerInvariant())
                    {
                        case "scale_factor":
                            if (settingValue.type == JsonNodeType.Float || settingValue.type == JsonNodeType.Integer)
                            {
                                float scaleFactor = settingValue.AsFloat;
                                if (Math.Abs(importer.globalScale - scaleFactor) > 0.001f)
                                {
                                    importer.globalScale = scaleFactor;
                                    modified = true;
                                }
                            }
                            break;
                        case "use_file_scale":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool useFileScale = settingValue.AsBool;
                                if (importer.useFileScale != useFileScale)
                                {
                                    importer.useFileScale = useFileScale;
                                    modified = true;
                                }
                            }
                            break;
                        case "use_file_units":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool useFileUnits = settingValue.AsBool;
                                if (importer.useFileUnits != useFileUnits)
                                {
                                    importer.useFileUnits = useFileUnits;
                                    modified = true;
                                }
                            }
                            break;
                        case "import_blend_shapes":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool importBlendShapes = settingValue.AsBool;
                                if (importer.importBlendShapes != importBlendShapes)
                                {
                                    importer.importBlendShapes = importBlendShapes;
                                    modified = true;
                                }
                            }
                            break;
                        case "import_visibility":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool importVisibility = settingValue.AsBool;
                                if (importer.importVisibility != importVisibility)
                                {
                                    importer.importVisibility = importVisibility;
                                    modified = true;
                                }
                            }
                            break;
                        case "import_cameras":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool importCameras = settingValue.AsBool;
                                if (importer.importCameras != importCameras)
                                {
                                    importer.importCameras = importCameras;
                                    modified = true;
                                }
                            }
                            break;
                        case "import_lights":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool importLights = settingValue.AsBool;
                                if (importer.importLights != importLights)
                                {
                                    importer.importLights = importLights;
                                    modified = true;
                                }
                            }
                            break;
                        case "preserve_hierarchy":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool preserveHierarchy = settingValue.AsBool;
                                if (importer.preserveHierarchy != preserveHierarchy)
                                {
                                    importer.preserveHierarchy = preserveHierarchy;
                                    modified = true;
                                }
                            }
                            break;
                        case "animation_type":
                            if (settingValue.type == JsonNodeType.String)
                            {
                                string animationType = settingValue.Value;
                                ModelImporterAnimationType animType = ModelImporterAnimationType.None;

                                switch (animationType.ToLowerInvariant())
                                {
                                    case "legacy":
                                        animType = ModelImporterAnimationType.Legacy;
                                        break;
                                    case "generic":
                                        animType = ModelImporterAnimationType.Generic;
                                        break;
                                    case "humanoid":
                                        // 注意：ModelImporterAnimationType.Humanoid 在某些Unity版本中可能不可用
                                        // animType = ModelImporterAnimationType.Humanoid;
                                        LogWarning($"[ApplyModelImportSettings] ModelImporterAnimationType.Humanoid not supported in current Unity version");
                                        break;
                                    case "none":
                                    default:
                                        animType = ModelImporterAnimationType.None;
                                        break;
                                }

                                if (importer.animationType != animType)
                                {
                                    importer.animationType = animType;
                                    modified = true;
                                }
                            }
                            break;
                        case "optimize_mesh":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool optimizeMesh = settingValue.AsBool;
                                // 替换过时的optimizeMesh API
                                if (importer.optimizeMeshPolygons != optimizeMesh || importer.optimizeMeshVertices != optimizeMesh)
                                {
                                    importer.optimizeMeshPolygons = optimizeMesh;
                                    importer.optimizeMeshVertices = optimizeMesh;
                                    modified = true;
                                }
                            }
                            break;
                        case "generate_secondary_uv":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool generateSecondaryUV = settingValue.AsBool;
                                if (importer.generateSecondaryUV != generateSecondaryUV)
                                {
                                    importer.generateSecondaryUV = generateSecondaryUV;
                                    modified = true;
                                }
                            }
                            break;
                        case "read_write_enabled":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool readWriteEnabled = settingValue.AsBool;
                                if (importer.isReadable != readWriteEnabled)
                                {
                                    importer.isReadable = readWriteEnabled;
                                    modified = true;
                                }
                            }
                            break;
                        case "optimize_game_objects":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool optimizeGameObjects = settingValue.AsBool;
                                if (importer.optimizeGameObjects != optimizeGameObjects)
                                {
                                    importer.optimizeGameObjects = optimizeGameObjects;
                                    modified = true;
                                }
                            }
                            break;
                        case "import_materials":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool importMaterials = settingValue.AsBool;
                                ModelImporterMaterialImportMode materialMode = importMaterials ?
                                    ModelImporterMaterialImportMode.ImportStandard :
                                    ModelImporterMaterialImportMode.None;

                                if (importer.materialImportMode != materialMode)
                                {
                                    importer.materialImportMode = materialMode;
                                    modified = true;
                                }
                            }
                            break;
                        case "mesh_compression":
                            if (settingValue.type == JsonNodeType.String)
                            {
                                string compression = settingValue.Value;
                                ModelImporterMeshCompression meshCompression = ModelImporterMeshCompression.Off;

                                switch (compression.ToLowerInvariant())
                                {
                                    case "low":
                                        meshCompression = ModelImporterMeshCompression.Low;
                                        break;
                                    case "medium":
                                        meshCompression = ModelImporterMeshCompression.Medium;
                                        break;
                                    case "high":
                                        meshCompression = ModelImporterMeshCompression.High;
                                        break;
                                    case "off":
                                    default:
                                        meshCompression = ModelImporterMeshCompression.Off;
                                        break;
                                }

                                if (importer.meshCompression != meshCompression)
                                {
                                    importer.meshCompression = meshCompression;
                                    modified = true;
                                }
                            }
                            break;
                        case "add_collider":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool addCollider = settingValue.AsBool;
                                if (importer.addCollider != addCollider)
                                {
                                    importer.addCollider = addCollider;
                                    modified = true;
                                }
                            }
                            break;
                        case "weld_vertices":
                            if (settingValue.type == JsonNodeType.Boolean)
                            {
                                bool weldVertices = settingValue.AsBool;
                                if (importer.weldVertices != weldVertices)
                                {
                                    importer.weldVertices = weldVertices;
                                    modified = true;
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[ApplyModelImportSettings] Error setting '{settingName}': {ex.Message}");
                }
            }

            return modified;
        }

        /// <summary>
        /// 获取模型数据
        /// </summary>
        private object GetModelData(string path)
        {
            if (string.IsNullOrEmpty(path) || !AssetExists(path))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(path);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (model == null)
                return null;

            // 获取模型导入器信息
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            object importSettings = null;
            object remapInfo = null;

            if (importer != null)
            {
                importSettings = new
                {
                    scale_factor = importer.globalScale,
                    use_file_scale = importer.useFileScale,
                    use_file_units = importer.useFileUnits,
                    import_blend_shapes = importer.importBlendShapes,
                    import_visibility = importer.importVisibility,
                    import_cameras = importer.importCameras,
                    import_lights = importer.importLights,
                    preserve_hierarchy = importer.preserveHierarchy,
                    animation_type = importer.animationType.ToString(),
                    optimize_mesh = importer.optimizeMeshPolygons && importer.optimizeMeshVertices,
                    generate_secondary_uv = importer.generateSecondaryUV,
                    read_write_enabled = importer.isReadable,
                    optimize_game_objects = importer.optimizeGameObjects,
                    import_materials = importer.materialImportMode != ModelImporterMaterialImportMode.None,
                    mesh_compression = importer.meshCompression.ToString(),
                    add_collider = importer.addCollider,
                    weld_vertices = importer.weldVertices
                };

                // 获取当前的重定向信息
                var remaps = new Dictionary<string, string>();
                try
                {
                    var remapObjects = importer.GetExternalObjectMap();
                    if (remapObjects != null && remapObjects.Count > 0)
                    {
                        foreach (var remap in remapObjects)
                        {
                            if (remap.Key.type == typeof(Material))
                            {
                                string sourceName = remap.Key.name;
                                string targetPath = AssetDatabase.GetAssetPath(remap.Value);
                                if (!string.IsNullOrEmpty(targetPath))
                                {
                                    remaps[sourceName] = targetPath;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[GetModelData] Error getting remap info: {ex.Message}");
                }

                if (remaps.Count > 0)
                {
                    remapInfo = remaps;
                }
            }

            return new
            {
                path = path,
                guid = guid,
                name = Path.GetFileNameWithoutExtension(path),
                fileName = Path.GetFileName(path),
                file_extension = Path.GetExtension(path),
                is_model_file = IsModelFile(path),
                import_settings = importSettings,
                material_remaps = remapInfo,
                lastWriteTimeUtc = File.GetLastWriteTimeUtc(Path.Combine(Directory.GetCurrentDirectory(), path)).ToString("o")
            };
        }
    }
}
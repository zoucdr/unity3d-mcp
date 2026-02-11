using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UniMcp;
using UniMcp;
using UniMcp.Models;

namespace UniMcp.Tools
{
    /// <summary>
    /// 专门的材质管理工具，提供材质的创建、修改、复制、删除等操作
    /// 对应方法名: manage_material
    /// </summary>
    [ToolName("edit_material", "Resource Management")]
    public class EditMaterial : StateMethodBase
    {
        public override string Description => L.T("Manage material assets including create and modify properties", "管理材质资源，包括创建和修改属性");

        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // 操作类型 - 枚举字符串
                new MethodStr("action", L.T("Action type", "操作类型"), false)
                    .SetEnumValues("create", "set_properties", "duplicate", "delete", "get_info", "search", "copy_properties", "change_shader", "enable_keyword", "disable_keyword")
                    .SetDefault("get_info"),
                
                // 材质路径 - 必需参数
                new MethodStr("path", L.T("Material asset path, Unity standard format", "材质资源路径，Unity标准格式"), false)
                    .AddExamples("Assets/Materials/MyMaterial.mat", "Assets/Materials/UI/ButtonMaterial.mat")
                    .SetDefault("Assets/Materials/NewMaterial.mat"),
                
                // 着色器名称 - 可选参数
                new MethodStr("shader_name", L.T("Shader name or path", "着色器名称或路径"))
                    .AddExamples("Standard", "Universal Render Pipeline/Lit", "Unlit/Color", "Assets/Shaders/CustomShader.shader")
                    .SetDefault("Standard"),
                
                // 材质属性 - 对象类型
                new MethodObj("properties", L.T("Material properties dictionary, including color, texture, float, etc.", "材质属性字典，包含颜色、纹理、浮点数等属性"))
                    .AddProperty("_Color", "array")
                    .AddProperty("_MainTex", "string")
                    .AddProperty("_Metallic", "number")
                    .AddProperty("_Glossiness", "number")
                    .AddExample("{\"_Color\": [1.0, 0.0, 0.0, 1.0], \"_Metallic\": 0.5, \"_Glossiness\": 0.8}")
                    .AddExample("{\"_MainTex\": \"Assets/Textures/Diffuse.png\", \"_BumpMap\": \"Assets/Textures/Normal.png\"}"),
                
                // 源路径 - 用于复制操作
                new MethodStr("source_path", L.T("Source material path (used when copying properties)", "源材质路径（复制属性时使用）"))
                    .AddExamples("Assets/Materials/SourceMaterial.mat", "Assets/Materials/Templates/BaseMaterial.mat"),
                
                // 目标路径 - 用于复制/移动操作
                new MethodStr("destination", L.T("Target path (used when copying/moving)", "目标路径（复制/移动时使用）"))
                    .AddExamples("Assets/Materials/CopiedMaterial.mat", "Assets/Materials/Backup/MaterialCopy.mat"),
                
                // 搜索模式
                new MethodStr("query", L.T("Search pattern, supports wildcards", "搜索模式，支持通配符"))
                    .AddExamples("*.mat", "*UI*", "Button*", "Material_*")
                    .SetDefault("*.mat"),
                
                // 递归搜索
                new MethodBool("recursive", L.T("Whether to recursively search subfolders", "是否递归搜索子文件夹"))
                    .SetDefault(true),
                
                // 强制执行
                new MethodBool("force", L.T("Whether to force operation (overwrite existing files, etc.)", "是否强制执行操作（覆盖现有文件等）"))
                    .SetDefault(false),
                
                // 渲染队列
                new MethodInt("render_queue", L.T("Render queue value, controls render order", "渲染队列值，控制渲染顺序"))
                    .SetRange(1000, 5000)
                    .AddExample(2000) // Geometry
                    .AddExample(2450) // AlphaTest
                    .AddExample(3000) // Transparent
                    .SetDefault(2000),
                
                // GPU实例化
                new MethodBool("enable_instancing", L.T("Whether to enable GPU instancing", "是否启用GPU实例化"))
                    .SetDefault(false),
                
                // 双面全局光照
                new MethodBool("double_sided_global_illumination", L.T("Whether to enable double-sided global illumination", "是否启用双面全局光照"))
                    .SetDefault(false),
                
                // 关键字 - 用于启用/禁用着色器关键字
                new MethodStr("keyword", L.T("Shader keyword name", "着色器关键字名称"))
                    .AddExamples("_NORMALMAP", "_EMISSION", "_METALLICGLOSSMAP", "_ALPHATEST_ON")
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
                    .Leaf("create", CreateMaterial)
                    .Leaf("set_properties", SetProperties)
                    .Leaf("duplicate", DuplicateMaterial)
                    .Leaf("get_info", GetMaterialInfo)
                    .Leaf("search", SearchMaterials)
                    .Leaf("copy_properties", CopyMaterialProperties)
                    .Leaf("change_shader", ChangeMaterialShader)
                    .Leaf("enable_keyword", EnableMaterialKeyword)
                    .Leaf("disable_keyword", DisableMaterialKeyword)
                .Build();
        }

        // --- 状态树操作方法 ---

        private object CreateMaterial(JsonClass args)
        {
            string path = args["path"]?.Value;
            string shaderName = args["shader_name"]?.Value;
            JsonClass properties = args["properties"] as JsonClass;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for create.");

            string fullPath = SanitizeAssetPath(path);
            string directory = Path.GetDirectoryName(fullPath);

            // 确保目录存在
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), directory)))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), directory));
                AssetDatabase.Refresh();
            }

            if (AssetExists(fullPath))
                return Response.Error($"Material already exists at path: {fullPath}");

            try
            {
                // 确定着色器
                Shader shader = null;
                if (!string.IsNullOrEmpty(shaderName))
                {
                    shader = Shader.Find(shaderName);
                    if (shader == null)
                    {
                        // 尝试从路径加载着色器
                        shader = AssetDatabase.LoadAssetAtPath<Shader>(SanitizeAssetPath(shaderName));
                    }
                }

                if (shader == null)
                {
                    shader = Shader.Find("Standard"); // 默认着色器
                }

                Material material = new Material(shader);

                // 应用属性
                if (properties != null && properties.Count > 0)
                {
                    ApplyMaterialProperties(material, properties);
                }

                AssetDatabase.CreateAsset(material, fullPath);
                AssetDatabase.SaveAssets();

                McpLogger.Log($"[ManageMaterial] Created material at '{fullPath}' with shader '{shader.name}'");
                return Response.Success($"Material '{fullPath}' created successfully.", GetMaterialData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create material at '{fullPath}': {e.Message}");
            }
        }

        private object SetProperties(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonClass properties = args["properties"] as JsonClass;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for modify.");
            if (properties == null || properties.Count == 0)
                return Response.Error("'properties' are required for set_properties.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Material not found at path: {fullPath}");

            try
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
                if (material == null)
                    return Response.Error($"Failed to load material at path: {fullPath}");

                Undo.RecordObject(material, $"Set Properties on Material '{Path.GetFileName(fullPath)}'");

                bool modified = ApplyMaterialProperties(material, properties);

                if (modified)
                {
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssets();
                    McpLogger.Log($"[ManageMaterial] Set properties on material at '{fullPath}'");
                    return Response.Success($"Material '{fullPath}' properties set successfully.", GetMaterialData(fullPath));
                }
                else
                {
                    return Response.Success($"No applicable properties found to set for material '{fullPath}'.", GetMaterialData(fullPath));
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set properties on material '{fullPath}': {e.Message}");
            }
        }

        private object DuplicateMaterial(JsonClass args)
        {
            string path = args["path"]?.Value;
            string destinationPath = args["destination"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for duplicate.");

            string sourcePath = SanitizeAssetPath(path);
            if (!AssetExists(sourcePath))
                return Response.Error($"Source material not found at path: {sourcePath}");

            string destPath;
            if (string.IsNullOrEmpty(destinationPath))
            {
                destPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);
            }
            else
            {
                destPath = SanitizeAssetPath(destinationPath);
                if (AssetExists(destPath))
                    return Response.Error($"Material already exists at destination path: {destPath}");
                EnsureDirectoryExists(Path.GetDirectoryName(destPath));
            }

            try
            {
                bool success = AssetDatabase.CopyAsset(sourcePath, destPath);
                if (success)
                {
                    McpLogger.Log($"[ManageMaterial] Duplicated material from '{sourcePath}' to '{destPath}'");
                    return Response.Success($"Material '{sourcePath}' duplicated to '{destPath}'.", GetMaterialData(destPath));
                }
                else
                {
                    return Response.Error($"Failed to duplicate material from '{sourcePath}' to '{destPath}'.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error duplicating material '{sourcePath}': {e.Message}");
            }
        }



        private object GetMaterialInfo(JsonClass args)
        {
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for get_info.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Material not found at path: {fullPath}");

            try
            {
                return Response.Success("Material info retrieved.", GetMaterialData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting info for material '{fullPath}': {e.Message}");
            }
        }

        private object SearchMaterials(JsonClass args)
        {
            string searchPattern = args["query"]?.Value;
            string pathScope = args["path"]?.Value;
            bool recursive = args["recursive"].AsBoolDefault(true);

            List<string> searchFilters = new List<string>();
            if (!string.IsNullOrEmpty(searchPattern))
                searchFilters.Add(searchPattern);
            searchFilters.Add("t:Material");

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

                    results.Add(GetMaterialData(assetPath));
                }

                McpLogger.Log($"[ManageMaterial] Found {results.Count} material(s)");
                return Response.Success($"Found {results.Count} material(s).", results);
            }
            catch (Exception e)
            {
                return Response.Error($"Error searching materials: {e.Message}");
            }
        }

        private object CopyMaterialProperties(JsonClass args)
        {
            string sourcePath = args["source_path"]?.Value;
            string destinationPath = args["destination"]?.Value;

            if (string.IsNullOrEmpty(sourcePath))
                return Response.Error("'source_path' is required for copy_properties.");
            if (string.IsNullOrEmpty(destinationPath))
                return Response.Error("'destination' is required for copy_properties.");

            string sourceFullPath = SanitizeAssetPath(sourcePath);
            string destFullPath = SanitizeAssetPath(destinationPath);

            if (!AssetExists(sourceFullPath))
                return Response.Error($"Source material not found at path: {sourceFullPath}");
            if (!AssetExists(destFullPath))
                return Response.Error($"Destination material not found at path: {destFullPath}");

            try
            {
                Material sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(sourceFullPath);
                Material destMaterial = AssetDatabase.LoadAssetAtPath<Material>(destFullPath);

                if (sourceMaterial == null)
                    return Response.Error($"Failed to load source material at path: {sourceFullPath}");
                if (destMaterial == null)
                    return Response.Error($"Failed to load destination material at path: {destFullPath}");

                Undo.RecordObject(destMaterial, $"Copy Properties to Material '{Path.GetFileName(destFullPath)}'");

                // 复制着色器
                destMaterial.shader = sourceMaterial.shader;

                // 复制所有属性
                Shader shader = sourceMaterial.shader;
                int propertyCount = ShaderUtil.GetPropertyCount(shader);

                for (int i = 0; i < propertyCount; i++)
                {
                    string propertyName = ShaderUtil.GetPropertyName(shader, i);
                    ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(shader, i);

                    switch (propertyType)
                    {
                        case ShaderUtil.ShaderPropertyType.Color:
                            destMaterial.SetColor(propertyName, sourceMaterial.GetColor(propertyName));
                            break;
                        case ShaderUtil.ShaderPropertyType.Vector:
                            destMaterial.SetVector(propertyName, sourceMaterial.GetVector(propertyName));
                            break;
                        case ShaderUtil.ShaderPropertyType.Float:
                        case ShaderUtil.ShaderPropertyType.Range:
                            destMaterial.SetFloat(propertyName, sourceMaterial.GetFloat(propertyName));
                            break;
                            // 注意：ShaderPropertyType.Texture 在某些Unity版本中可能不可用
                            // case ShaderUtil.ShaderPropertyType.Texture:
                            //     destMaterial.SetTexture(propertyName, sourceMaterial.GetTexture(propertyName));
                            //     break;
                    }
                }

                // 复制关键字
                string[] keywords = sourceMaterial.shaderKeywords;
                destMaterial.shaderKeywords = keywords;

                // 复制渲染队列
                destMaterial.renderQueue = sourceMaterial.renderQueue;

                EditorUtility.SetDirty(destMaterial);
                AssetDatabase.SaveAssets();

                McpLogger.Log($"[ManageMaterial] Copied properties from '{sourceFullPath}' to '{destFullPath}'");
                return Response.Success($"Material properties copied from '{sourceFullPath}' to '{destFullPath}'.", GetMaterialData(destFullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error copying material properties: {e.Message}");
            }
        }

        private object ChangeMaterialShader(JsonClass args)
        {
            string path = args["path"]?.Value;
            string shaderName = args["shader_name"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for change_shader.");
            if (string.IsNullOrEmpty(shaderName))
                return Response.Error("'shader' is required for change_shader.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Material not found at path: {fullPath}");

            try
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
                if (material == null)
                    return Response.Error($"Failed to load material at path: {fullPath}");

                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    shader = AssetDatabase.LoadAssetAtPath<Shader>(SanitizeAssetPath(shaderName));
                }

                if (shader == null)
                    return Response.Error($"Shader '{shaderName}' not found.");

                Undo.RecordObject(material, $"Change Shader on Material '{Path.GetFileName(fullPath)}'");
                material.shader = shader;
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();

                McpLogger.Log($"[ManageMaterial] Changed shader to '{shader.name}' on material '{fullPath}'");
                return Response.Success($"Shader changed to '{shader.name}' on material '{fullPath}'.", GetMaterialData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error changing shader on material '{fullPath}': {e.Message}");
            }
        }

        private object EnableMaterialKeyword(JsonClass args)
        {
            string path = args["path"]?.Value;
            string keyword = args["keyword"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for enable_keyword.");
            if (string.IsNullOrEmpty(keyword))
                return Response.Error("'keyword' is required for enable_keyword.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Material not found at path: {fullPath}");

            try
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
                if (material == null)
                    return Response.Error($"Failed to load material at path: {fullPath}");

                Undo.RecordObject(material, $"Enable Keyword on Material '{Path.GetFileName(fullPath)}'");
                material.EnableKeyword(keyword);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();

                McpLogger.Log($"[ManageMaterial] Enabled keyword '{keyword}' on material '{fullPath}'");
                return Response.Success($"Keyword '{keyword}' enabled on material '{fullPath}'.", GetMaterialData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error enabling keyword on material '{fullPath}': {e.Message}");
            }
        }

        private object DisableMaterialKeyword(JsonClass args)
        {
            string path = args["path"]?.Value;
            string keyword = args["keyword"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for disable_keyword.");
            if (string.IsNullOrEmpty(keyword))
                return Response.Error("'keyword' is required for disable_keyword.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Material not found at path: {fullPath}");

            try
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
                if (material == null)
                    return Response.Error($"Failed to load material at path: {fullPath}");

                Undo.RecordObject(material, $"Disable Keyword on Material '{Path.GetFileName(fullPath)}'");
                material.DisableKeyword(keyword);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();

                McpLogger.Log($"[ManageMaterial] Disabled keyword '{keyword}' on material '{fullPath}'");
                return Response.Success($"Keyword '{keyword}' disabled on material '{fullPath}'.", GetMaterialData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Error disabling keyword on material '{fullPath}': {e.Message}");
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
            // var guid = AssetDatabase.AssetPathToGUID(sanitizedPath);
            // if (!string.IsNullOrEmpty(guid))
            // {
            //     return true;
            // }
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
        /// 应用材质属性
        /// </summary>
        private bool ApplyMaterialProperties(Material material, JsonClass properties)
        {
            if (material == null || properties == null)
                return false;
            bool modified = false;

            foreach (var prop in properties.Properties())
            {
                string propName = prop.Key;
                JsonNode propValue = prop.Value;

                try
                {
                    if (material.HasProperty(propName))
                    {
                        if (propValue is JsonClass colorObj &&
                            colorObj["r"] != null && colorObj["g"] != null && colorObj["b"] != null)
                        {
                            Color newColor = new Color(
                                colorObj["r"].AsFloat,
                                colorObj["g"].AsFloat,
                                colorObj["b"].AsFloat,
                                colorObj["a"].AsFloatDefault(1.0f)
                            );
                            if (material.GetColor(propName) != newColor)
                            {
                                material.SetColor(propName, newColor);
                                modified = true;
                            }
                        }
                        else if (propValue is JsonArray vecArray && vecArray.Count >= 4)
                        {
                            Vector4 newVector = new Vector4(
                                vecArray[0].AsFloat,
                                vecArray[1].AsFloat,
                                vecArray[2].AsFloat,
                                vecArray[3].AsFloat
                            );
                            if (material.GetVector(propName) != newVector)
                            {
                                material.SetVector(propName, newVector);
                                modified = true;
                            }
                        }
                        else if (propValue.type == JsonNodeType.Float || propValue.type == JsonNodeType.Integer)
                        {
                            float newVal = propValue.AsFloat;
                            if (Math.Abs(material.GetFloat(propName) - newVal) > 0.001f)
                            {
                                material.SetFloat(propName, newVal);
                                modified = true;
                            }
                        }
                        else if (propValue.type == JsonNodeType.String)
                        {
                            string texPath = propValue.Value;
                            if (!string.IsNullOrEmpty(texPath))
                            {
                                Texture newTex = AssetDatabase.LoadAssetAtPath<Texture>(SanitizeAssetPath(texPath));
                                if (newTex != null && material.GetTexture(propName) != newTex)
                                {
                                    material.SetTexture(propName, newTex);
                                    modified = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[ApplyMaterialProperties] Error setting property '{propName}': {ex.Message}");
                }
            }

            return modified;
        }

        /// <summary>
        /// 获取材质数据 - 使用简化的YAML格式
        /// </summary>
        private object GetMaterialData(string path)
        {
            if (string.IsNullOrEmpty(path) || !AssetExists(path))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(path);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
                return null;

            // 获取关键属性，避免返回所有详细属性
            string mainTexture = null;
            string color = null;
            string metallic = null;
            string smoothness = null;

            Shader shader = material.shader;
            if (shader != null)
            {
                // 尝试获取常见的Standard shader属性
                if (material.HasProperty("_MainTex"))
                {
                    Texture tex = material.GetTexture("_MainTex");
                    mainTexture = tex != null ? Path.GetFileName(AssetDatabase.GetAssetPath(tex)) : "none";
                }

                if (material.HasProperty("_Color"))
                {
                    Color c = material.GetColor("_Color");
                    color = $"[{c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2}]";
                }

                if (material.HasProperty("_Metallic"))
                    metallic = material.GetFloat("_Metallic").ToString("F2");

                if (material.HasProperty("_Glossiness"))
                    smoothness = material.GetFloat("_Glossiness").ToString("F2");
            }

            // 使用YAML格式大幅减少token使用量
            var yaml = $@"name: {Path.GetFileNameWithoutExtension(path)}
path: {path}
guid: {guid}
shader: {material.shader?.name ?? "None"}
renderQueue: {material.renderQueue}
keywords: {material.shaderKeywords?.Length ?? 0}
mainTexture: {mainTexture ?? "none"}
color: {color ?? "[1.0, 1.0, 1.0, 1.0]"}
metallic: {metallic ?? "0.0"}
smoothness: {smoothness ?? "0.5"}
lastModified: {File.GetLastWriteTimeUtc(Path.Combine(Directory.GetCurrentDirectory(), path)):yyyy-MM-dd}";

            return new { yaml = yaml };
        }
    }
}
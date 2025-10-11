using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.U2D;
using UnityMcp.Models;

namespace UnityMcp.Tools
{
    /// <summary>
    /// 处理Unity图集(Sprite Atlas)的创建和编辑操作
    /// 对应方法名: edit_sprite_atlas
    /// </summary>
    [ToolName("edit_sprite_atlas", "资源管理")]
    public class EditSpriteAtlas : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "操作类型：create, add_sprites, remove_sprites, set_settings, get_settings, pack", false),
                new MethodKey("atlas_path", "图集资源路径", false),
                new MethodKey("sprite_paths", "要添加或移除的精灵路径数组", true),
                new MethodKey("folder_paths", "要添加或移除的文件夹路径数组", true),
                new MethodKey("include_subfolders", "是否包含子文件夹", true),
                new MethodKey("filter_pattern", "筛选模式，例如 *.png", true),
                new MethodKey("type", "图集类型：Master, Variant", true),
                new MethodKey("master_atlas_path", "主图集路径（仅当type为Variant时有效）", true),
                new MethodKey("allow_rotation", "是否允许旋转", true),
                new MethodKey("tight_packing", "是否使用紧凑排列", true),
                new MethodKey("padding", "图像间距", true),
                new MethodKey("readable", "是否可读", true),
                new MethodKey("generate_mip_maps", "是否生成Mip贴图", true),
                new MethodKey("filter_mode", "过滤模式：Point, Bilinear, Trilinear", true),
                new MethodKey("compression", "压缩格式：None, LowQuality, NormalQuality, HighQuality", true)
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("create", HandleCreateAction)
                    .Leaf("add_sprites", HandleAddSpritesAction)
                    .Leaf("remove_sprites", HandleRemoveSpritesAction)
                    .Leaf("set_settings", HandleSetSettingsAction)
                    .Leaf("get_settings", HandleGetSettingsAction)
                    .Leaf("pack", HandlePackAction)
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// 处理创建图集的操作
        /// </summary>
        private object HandleCreateAction(JsonClass args)
        {
            string atlasPath = args["atlas_path"]?.Value;

            if (string.IsNullOrEmpty(atlasPath))
            {
                return Response.Error("'atlas_path' parameter is required.");
            }

            LogInfo($"[EditSpriteAtlas] Creating sprite atlas at '{atlasPath}'");
            return CreateSpriteAtlas(args, atlasPath);
        }

        /// <summary>
        /// 处理添加精灵到图集的操作
        /// </summary>
        private object HandleAddSpritesAction(JsonClass args)
        {
            string atlasPath = args["atlas_path"]?.Value;

            if (string.IsNullOrEmpty(atlasPath))
            {
                return Response.Error("'atlas_path' parameter is required.");
            }

            LogInfo($"[EditSpriteAtlas] Adding sprites to atlas '{atlasPath}'");
            return AddSpritesToAtlas(args, atlasPath);
        }

        /// <summary>
        /// 处理从图集移除精灵的操作
        /// </summary>
        private object HandleRemoveSpritesAction(JsonClass args)
        {
            string atlasPath = args["atlas_path"]?.Value;

            if (string.IsNullOrEmpty(atlasPath))
            {
                return Response.Error("'atlas_path' parameter is required.");
            }

            LogInfo($"[EditSpriteAtlas] Removing sprites from atlas '{atlasPath}'");
            return RemoveSpritesFromAtlas(args, atlasPath);
        }

        /// <summary>
        /// 处理设置图集参数的操作
        /// </summary>
        private object HandleSetSettingsAction(JsonClass args)
        {
            string atlasPath = args["atlas_path"]?.Value;

            if (string.IsNullOrEmpty(atlasPath))
            {
                return Response.Error("'atlas_path' parameter is required.");
            }

            LogInfo($"[EditSpriteAtlas] Setting atlas settings for '{atlasPath}'");
            return SetAtlasSettings(args, atlasPath);
        }

        /// <summary>
        /// 处理获取图集参数的操作
        /// </summary>
        private object HandleGetSettingsAction(JsonClass args)
        {
            string atlasPath = args["atlas_path"]?.Value;

            if (string.IsNullOrEmpty(atlasPath))
            {
                return Response.Error("'atlas_path' parameter is required.");
            }

            LogInfo($"[EditSpriteAtlas] Getting atlas settings for '{atlasPath}'");
            return GetAtlasSettings(atlasPath);
        }

        /// <summary>
        /// 处理打包图集的操作
        /// </summary>
        private object HandlePackAction(JsonClass args)
        {
            string atlasPath = args["atlas_path"]?.Value;

            if (string.IsNullOrEmpty(atlasPath))
            {
                return Response.Error("'atlas_path' parameter is required.");
            }

            LogInfo($"[EditSpriteAtlas] Packing atlas '{atlasPath}'");
            return PackAtlas(atlasPath);
        }

        // --- Core Methods ---

        /// <summary>
        /// 创建精灵图集
        /// </summary>
        private object CreateSpriteAtlas(JsonClass args, string atlasPath)
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(atlasPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 检查是否已存在
                if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath) != null)
                {
                    return Response.Error($"Sprite Atlas already exists at '{atlasPath}'.");
                }

                // 创建新的图集
                SpriteAtlas atlas = new SpriteAtlas();

                // 设置图集类型
                string atlasType = args["type"]?.Value?.ToLower();
                if (atlasType == "variant")
                {
                    string masterAtlasPath = args["master_atlas_path"]?.Value;
                    if (string.IsNullOrEmpty(masterAtlasPath))
                    {
                        return Response.Error("'master_atlas_path' parameter is required for variant atlas.");
                    }

                    SpriteAtlas masterAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(masterAtlasPath);
                    if (masterAtlas == null)
                    {
                        return Response.Error($"Master atlas not found at '{masterAtlasPath}'.");
                    }

                    // Note: SetVariant is not available in Unity 2022.3, variant creation is not supported
                    LogWarning("[EditSpriteAtlas] Variant atlas creation is not supported in Unity 2022.3");
                }

                // 应用设置
                ApplyAtlasSettings(atlas, args);

                // 保存图集
                AssetDatabase.CreateAsset(atlas, atlasPath);

                // 添加精灵（如果提供了）
                if (args["sprite_paths"] != null || args["folder_paths"] != null)
                {
                    AddSpritesToAtlas(args, atlasPath);
                }

                // 保存并刷新
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                LogInfo($"[EditSpriteAtlas] Successfully created sprite atlas at '{atlasPath}'");
                return Response.Success($"Sprite Atlas created at '{atlasPath}'.");
            }
            catch (Exception e)
            {
                LogError($"[EditSpriteAtlas] Error creating sprite atlas: {e.Message}");
                return Response.Error($"Error creating sprite atlas: {e.Message}");
            }
        }

        /// <summary>
        /// 添加精灵到图集
        /// </summary>
        private object AddSpritesToAtlas(JsonClass args, string atlasPath)
        {
            try
            {
                // 加载图集
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null)
                {
                    return Response.Error($"Sprite Atlas not found at '{atlasPath}'.");
                }

                List<UnityEngine.Object> objectsToAdd = new List<UnityEngine.Object>();
                Debug.Log($"[EditSpriteAtlas] Adding sprite to atlas: {args["sprite_paths"] is JsonArray} {args["sprite_paths"].type}");

                // 处理单个精灵路径
                if (args["sprite_paths"] != null && args["sprite_paths"].type == JsonNodeType.Array)
                {
                    var spritePaths = args["sprite_paths"].AsArray;
                    foreach (var pathNode in spritePaths.Childs)
                    {
                        string spritePath = pathNode.Value;
                        if (!string.IsNullOrEmpty(spritePath))
                        {
                            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                            if (sprite != null)
                            {
                                objectsToAdd.Add(sprite);
                            }
                            else
                            {
                                // 也可能是包含多个精灵的纹理
                                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
                                if (texture != null)
                                {
                                    objectsToAdd.Add(texture);
                                }
                                else
                                {
                                    LogWarning($"[EditSpriteAtlas] Could not find sprite or texture at '{spritePath}'");
                                }
                            }
                        }
                    }
                }

                // 处理文件夹路径
                if (args["folder_paths"] != null && args["folder_paths"].type == JsonNodeType.Array)
                {
                    bool includeSubfolders = args["include_subfolders"].AsBoolDefault(true);
                    string filterPattern = args["filter_pattern"]?.Value;

                    var folderPaths = args["folder_paths"].AsArray;
                    foreach (var pathNode in folderPaths.Childs)
                    {
                        string folderPath = pathNode.Value;
                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
                            if (folder != null)
                            {
                                objectsToAdd.Add(folder);
                            }
                            else
                            {
                                LogWarning($"[EditSpriteAtlas] Could not find folder at '{folderPath}'");
                            }
                        }
                    }
                }

                // 添加到图集
                if (objectsToAdd.Count > 0)
                {
                    atlas.Add(objectsToAdd.ToArray());
                    EditorUtility.SetDirty(atlas);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                LogInfo($"[EditSpriteAtlas] Added {objectsToAdd.Count} objects to sprite atlas '{atlasPath}'");
                return Response.Success($"Added {objectsToAdd.Count} objects to sprite atlas '{atlasPath}'.");
            }
            catch (Exception e)
            {
                LogError($"[EditSpriteAtlas] Error adding sprites to atlas: {e.Message}");
                return Response.Error($"Error adding sprites to atlas: {e.Message}");
            }
        }

        /// <summary>
        /// 从图集移除精灵
        /// </summary>
        private object RemoveSpritesFromAtlas(JsonClass args, string atlasPath)
        {
            try
            {
                // 加载图集
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null)
                {
                    return Response.Error($"Sprite Atlas not found at '{atlasPath}'.");
                }

                List<UnityEngine.Object> objectsToRemove = new List<UnityEngine.Object>();

                // 处理单个精灵路径
                if (args["sprite_paths"] != null && args["sprite_paths"].type == JsonNodeType.Array)
                {
                    var spritePaths = args["sprite_paths"].AsArray;
                    foreach (var pathNode in spritePaths.Childs)
                    {
                        string spritePath = pathNode.Value;
                        if (!string.IsNullOrEmpty(spritePath))
                        {
                            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                            if (sprite != null)
                            {
                                objectsToRemove.Add(sprite);
                            }
                            else
                            {
                                // 也可能是包含多个精灵的纹理
                                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
                                if (texture != null)
                                {
                                    objectsToRemove.Add(texture);
                                }
                            }
                        }
                    }
                }

                // 处理文件夹路径
                if (args["folder_paths"] != null && args["folder_paths"].type == JsonNodeType.Array)
                {
                    var folderPaths = args["folder_paths"].AsArray;
                    foreach (var pathNode in folderPaths.Childs)
                    {
                        string folderPath = pathNode.Value;
                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
                            if (folder != null)
                            {
                                objectsToRemove.Add(folder);
                            }
                        }
                    }
                }

                // 从图集移除
                if (objectsToRemove.Count > 0)
                {
                    atlas.Remove(objectsToRemove.ToArray());
                    EditorUtility.SetDirty(atlas);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                LogInfo($"[EditSpriteAtlas] Removed {objectsToRemove.Count} objects from sprite atlas '{atlasPath}'");
                return Response.Success($"Removed {objectsToRemove.Count} objects from sprite atlas '{atlasPath}'.");
            }
            catch (Exception e)
            {
                LogError($"[EditSpriteAtlas] Error removing sprites from atlas: {e.Message}");
                return Response.Error($"Error removing sprites from atlas: {e.Message}");
            }
        }

        /// <summary>
        /// 设置图集参数
        /// </summary>
        private object SetAtlasSettings(JsonClass args, string atlasPath)
        {
            try
            {
                // 加载图集
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null)
                {
                    return Response.Error($"Sprite Atlas not found at '{atlasPath}'.");
                }

                // 应用设置
                ApplyAtlasSettings(atlas, args);

                // 保存
                EditorUtility.SetDirty(atlas);
                AssetDatabase.SaveAssets();

                LogInfo($"[EditSpriteAtlas] Successfully applied settings to sprite atlas '{atlasPath}'");
                return Response.Success($"Settings applied to sprite atlas '{atlasPath}'.");
            }
            catch (Exception e)
            {
                LogError($"[EditSpriteAtlas] Error setting atlas settings: {e.Message}");
                return Response.Error($"Error setting atlas settings: {e.Message}");
            }
        }

        /// <summary>
        /// 获取图集参数
        /// </summary>
        private object GetAtlasSettings(string atlasPath)
        {
            try
            {
                // 加载图集
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null)
                {
                    return Response.Error($"Sprite Atlas not found at '{atlasPath}'.");
                }

                // 获取设置
                var textureSettings = atlas.GetTextureSettings();
                var packingSettings = atlas.GetPackingSettings();

                // 构建返回数据
                var settings = new Dictionary<string, object>
                {
                    { "isVariant", false }, // Variant detection not available in Unity 2022.3
                    { "allowRotation", packingSettings.enableRotation },
                    { "tightPacking", packingSettings.enableTightPacking },
                    { "padding", packingSettings.padding },
                    { "filterMode", textureSettings.filterMode.ToString() },
                    { "generateMipMaps", textureSettings.generateMipMaps },
                    { "readable", textureSettings.readable },
                    { "sRGB", textureSettings.sRGB }
                };

                // 获取包含的精灵
                var packedSprites = new List<string>();
                var sprites = atlas.GetPackables();
                if (sprites != null)
                {
                    foreach (var obj in sprites)
                    {
                        if (obj != null)
                        {
                            packedSprites.Add(AssetDatabase.GetAssetPath(obj));
                        }
                    }
                }
                settings.Add("packedSprites", packedSprites);

                LogInfo($"[EditSpriteAtlas] Successfully retrieved settings for sprite atlas '{atlasPath}'");
                return Response.Success($"Retrieved settings for sprite atlas '{atlasPath}'.", settings);
            }
            catch (Exception e)
            {
                LogError($"[EditSpriteAtlas] Error getting atlas settings: {e.Message}");
                return Response.Error($"Error getting atlas settings: {e.Message}");
            }
        }

        /// <summary>
        /// 打包图集
        /// </summary>
        private object PackAtlas(string atlasPath)
        {
            try
            {
                // 加载图集
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null)
                {
                    return Response.Error($"Sprite Atlas not found at '{atlasPath}'.");
                }

                // 打包图集
                EditorUtility.SetDirty(atlas);
                UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);

                LogInfo($"[EditSpriteAtlas] Successfully packed sprite atlas '{atlasPath}'");
                return Response.Success($"Packed sprite atlas '{atlasPath}'.");
            }
            catch (Exception e)
            {
                LogError($"[EditSpriteAtlas] Error packing atlas: {e.Message}");
                return Response.Error($"Error packing atlas: {e.Message}");
            }
        }

        // --- Helper Methods ---

        /// <summary>
        /// 应用图集设置
        /// </summary>
        private void ApplyAtlasSettings(SpriteAtlas atlas, JsonClass args)
        {
            try
            {
                // 获取当前设置
                var textureSettings = atlas.GetTextureSettings();
                var packingSettings = atlas.GetPackingSettings();

                // 更新打包设置
                bool allowRotation = args["allow_rotation"].AsBoolDefault(packingSettings.enableRotation);
                packingSettings.enableRotation = allowRotation;

                bool tightPacking = args["tight_packing"].AsBoolDefault(packingSettings.enableTightPacking);
                packingSettings.enableTightPacking = tightPacking;

                int padding = args["padding"].AsIntDefault(packingSettings.padding);
                packingSettings.padding = padding;

                // 应用打包设置
                atlas.SetPackingSettings(packingSettings);

                // 更新纹理设置
                bool generateMipMaps = args["generate_mip_maps"].AsBoolDefault(textureSettings.generateMipMaps);
                textureSettings.generateMipMaps = generateMipMaps;

                bool readable = args["readable"].AsBoolDefault(textureSettings.readable);
                textureSettings.readable = readable;

                // 设置过滤模式
                string filterModeStr = args["filter_mode"]?.Value;
                if (!string.IsNullOrEmpty(filterModeStr))
                {
                    if (Enum.TryParse<FilterMode>(filterModeStr, true, out FilterMode filterMode))
                    {
                        textureSettings.filterMode = filterMode;
                    }
                }

                // Note: compressionQuality property is not available in Unity 2022.3
                // The compression is set via platform-specific texture settings

                // 应用纹理设置
                atlas.SetTextureSettings(textureSettings);
            }
            catch (Exception e)
            {
                LogError($"[EditSpriteAtlas] Error applying atlas settings: {e.Message}");
                throw;
            }
        }
    }
}

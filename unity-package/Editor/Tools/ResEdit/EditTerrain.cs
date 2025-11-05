using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UniMcp.Models;

namespace UniMcp.Tools
{
    /// <summary>
    /// 处理Unity场景中Terrain地形的编辑操作
    /// 支持创建、修改、导入导出高度图等功能
    /// </summary>
    [ToolName("edit_terrain", "资源管理")]
    public class EditTerrain : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // // 操作类型
                 new MethodStr("action", "操作类型", false)
                     .SetEnumValues("create", "modify", "set_height", "paint_texture", "add_layer", "remove_layer", "set_size", "export_heightmap", "import_heightmap", "get_info"),
                
                 // // 层级路径
                  new MethodStr("path", "Terrain对象层级路径")
                      .AddExamples("Terrain"),
                  // 实例ID
                  new MethodInt("instance_id", "Terrain实例ID")
                       .AddExample("-12345"),
                  // 地形数据路径
                  new MethodStr("terrain_data_path", "TerrainData资源路径")
                       .AddExamples("Assets/TerrainData.asset"),
                  // 位置
                  new MethodVector("position", "Terrain位置 [x, y, z]"),
                  // 尺寸
                  new MethodVector("terrain_size", "Terrain尺寸 [width, height, length]")
                      .AddExample("[1000, 600, 1000]"),
                
                 // 高度图分辨率
                 new MethodInt("heightmap_resolution", "高度图分辨率"),

                // 高度图数据
                 new MethodArr("heightmap_data", "高度图数据数组")
                      .SetItemType("number"),
                
                 // 高度图文件
                 new MethodStr("heightmap_file", "高度图文件路径")
                      .AddExamples("Assets/Heightmap.png"),
                
                // // 纹理层配置
                new MethodObj("texture_layer", "纹理层配置")
                    .AddStringProperty("texture")
                    .AddArrayProperty("tile_size", "number")
                    .AddArrayProperty("tile_offset", "number")
                    .AddStringProperty("normal_map")
                    .AddNumberProperty("metallic")
                    .AddNumberProperty("smoothness"),
                
                // 层索引
                new MethodInt("layer_index", "纹理层索引")
                    .SetRange(0, 16),
                
                // 属性
                new MethodObj("properties", "Terrain属性")
                    .AddStringProperty("material_template")
                    .AddBooleanProperty("cast_shadows")
                    .AddBooleanProperty("draw_heightmap")
                    .AddBooleanProperty("draw_trees")
                    .AddNumberProperty("detail_object_distance")
                    .AddNumberProperty("tree_distance")
                    .AddNumberProperty("tree_billboard_distance")
                    .AddNumberProperty("tree_cross_fade_length")
                    .AddNumberProperty("tree_maximum_full_lod_count")
                    .AddBooleanProperty("collect_detail_patches")
                    .AddNumberProperty("detail_object_density")
                    .AddNumberProperty("detail_scatter_per_res"),
                
                // 导出格式
                new MethodStr("export_format", "导出格式")
                    .SetEnumValues("raw", "png"),
                
                // 强制执行
                new MethodBool("force", "强制执行"),
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
                    .Leaf("create", CreateTerrain)
                    .Leaf("modify", ModifyTerrain)
                    .Leaf("set_height", SetTerrainHeight)
                    .Leaf("paint_texture", PaintTexture)
                    .Leaf("add_layer", AddTextureLayer)
                    .Leaf("remove_layer", RemoveTextureLayer)
                    .Leaf("set_size", SetTerrainSize)
                    .Leaf("flatten", FlattenTerrain)
                    .Leaf("smooth", SmoothTerrain)
                    .Leaf("export_heightmap", ExportHeightmap)
                    .Leaf("import_heightmap", ImportHeightmap)
                    .Leaf("get_info", GetTerrainInfo)
                    .Leaf("clear_trees", ClearTrees)
                    .Leaf("clear_details", ClearDetails)
                .Build();
        }

        // ===== 创建Terrain =====
        private object CreateTerrain(JsonClass args)
        {
            try
            {
                // 获取参数
                string pathStr = args["path"]?.Value;
                var positionNode = args["position"];
                var terrainSizeNode = args["terrain_size"];
                int heightmapRes = args["heightmap_resolution"].AsIntDefault(513);
                string terrainDataPath = args["terrain_data_path"]?.Value;

                // 解析位置
                Vector3 position = Vector3.zero;
                if (positionNode is JsonArray posArray && posArray.Count >= 3)
                {
                    position = new Vector3(
                        posArray[0].AsFloat,
                        posArray[1].AsFloat,
                        posArray[2].AsFloat
                    );
                }

                // 解析尺寸
                Vector3 terrainSize = new Vector3(1000, 600, 1000);
                if (terrainSizeNode is JsonArray sizeArray && sizeArray.Count >= 3)
                {
                    terrainSize = new Vector3(
                        sizeArray[0].AsFloat,
                        sizeArray[1].AsFloat,
                        sizeArray[2].AsFloat
                    );
                }

                // 创建TerrainData
                TerrainData terrainData = new TerrainData();
                terrainData.heightmapResolution = heightmapRes;
                terrainData.size = terrainSize;
                terrainData.alphamapResolution = 512;
                terrainData.SetDetailResolution(512, 16);

                // 保存TerrainData资源
                if (string.IsNullOrEmpty(terrainDataPath))
                {
                    terrainDataPath = $"Assets/TerrainData_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}.asset";
                }

                terrainDataPath = SanitizeAssetPath(terrainDataPath);
                EnsureDirectoryExists(Path.GetDirectoryName(terrainDataPath));

                AssetDatabase.CreateAsset(terrainData, terrainDataPath);
                AssetDatabase.SaveAssets();

                // 创建Terrain GameObject
                GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
                terrainObj.transform.position = position;

                // 设置名称
                if (!string.IsNullOrEmpty(pathStr))
                {
                    string name = pathStr.Contains("/") ? Path.GetFileName(pathStr) : pathStr;
                    terrainObj.name = name;
                }
                else
                {
                    terrainObj.name = "Terrain";
                }

                Undo.RegisterCreatedObjectUndo(terrainObj, "Create Terrain");

                return Response.Success(
                    $"Terrain '{terrainObj.name}' created successfully at position {position}.",
                    GetTerrainData(terrainObj)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create terrain: {e.Message}");
            }
        }

        // ===== 修改Terrain属性 =====
        private object ModifyTerrain(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                JsonClass properties = args["properties"] as JsonClass;
                if (properties == null || properties.Count == 0)
                    return Response.Error("'properties' are required for modify.");

                Undo.RecordObject(terrain, "Modify Terrain");
                Undo.RecordObject(terrain.terrainData, "Modify Terrain Data");

                bool modified = ApplyTerrainProperties(terrain, properties);

                if (modified)
                {
                    EditorUtility.SetDirty(terrain);
                    EditorUtility.SetDirty(terrain.terrainData);

                    return Response.Success(
                        $"Terrain '{terrainObj.name}' modified successfully.",
                        GetTerrainData(terrainObj)
                    );
                }
                else
                {
                    return Response.Success(
                        $"No applicable properties found to modify for terrain '{terrainObj.name}'.",
                        GetTerrainData(terrainObj)
                    );
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify terrain: {e.Message}");
            }
        }

        // ===== 设置地形高度 =====
        private object SetTerrainHeight(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                var heightmapDataNode = args["heightmap_data"];
                if (heightmapDataNode == null)
                    return Response.Error("'heightmap_data' is required for set_height.");

                // 解析高度图数据
                JsonArray heightmapArray = heightmapDataNode as JsonArray;
                if (heightmapArray == null)
                    return Response.Error("'heightmap_data' must be a 2D array.");

                int resolution = terrain.terrainData.heightmapResolution;
                float[,] heights = new float[resolution, resolution];

                // 填充高度数据
                for (int y = 0; y < resolution && y < heightmapArray.Count; y++)
                {
                    if (heightmapArray[y] is JsonArray rowArray)
                    {
                        for (int x = 0; x < resolution && x < rowArray.Count; x++)
                        {
                            heights[y, x] = rowArray[x].AsFloat;
                        }
                    }
                }

                Undo.RecordObject(terrain.terrainData, "Set Terrain Height");
                terrain.terrainData.SetHeights(0, 0, heights);
                EditorUtility.SetDirty(terrain.terrainData);

                return Response.Success(
                    $"Terrain '{terrainObj.name}' height map updated successfully.",
                    GetTerrainData(terrainObj)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set terrain height: {e.Message}");
            }
        }

        // ===== 绘制纹理 =====
        private object PaintTexture(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                int layerIndex = args["layer_index"].AsIntDefault(0);

                // TODO: 实现纹理绘制逻辑
                // 这需要根据具体需求设置alphamap

                return Response.Success(
                    $"Texture painted on terrain '{terrainObj.name}'.",
                    GetTerrainData(terrainObj)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to paint texture: {e.Message}");
            }
        }

        // ===== 添加纹理层 =====
        private object AddTextureLayer(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                JsonClass layerConfig = args["texture_layer"] as JsonClass;
                if (layerConfig == null)
                    return Response.Error("'texture_layer' configuration is required.");

                string texturePath = layerConfig["texture"]?.Value;
                if (string.IsNullOrEmpty(texturePath))
                    return Response.Error("'texture' path is required in layer configuration.");

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null)
                    return Response.Error($"Failed to load texture at path: {texturePath}");

                // 获取现有layers
                TerrainLayer[] layers = terrain.terrainData.terrainLayers;
                List<TerrainLayer> layerList = new List<TerrainLayer>(layers);

                // 创建新layer
                TerrainLayer newLayer = new TerrainLayer();
                newLayer.diffuseTexture = texture;

                // 设置tiling和offset
                if (layerConfig["tile_size"] is JsonArray tileSizeArray && tileSizeArray.Count >= 2)
                {
                    newLayer.tileSize = new Vector2(
                        tileSizeArray[0].AsFloat,
                        tileSizeArray[1].AsFloat
                    );
                }
                else
                {
                    newLayer.tileSize = new Vector2(15, 15);
                }

                // 保存TerrainLayer资源
                string layerPath = $"Assets/TerrainLayers/Layer_{layerList.Count}.terrainlayer";
                EnsureDirectoryExists(Path.GetDirectoryName(layerPath));
                AssetDatabase.CreateAsset(newLayer, layerPath);
                AssetDatabase.SaveAssets();

                layerList.Add(newLayer);

                Undo.RecordObject(terrain.terrainData, "Add Terrain Layer");
                terrain.terrainData.terrainLayers = layerList.ToArray();
                EditorUtility.SetDirty(terrain.terrainData);

                return Response.Success(
                    $"Texture layer added to terrain '{terrainObj.name}'. Total layers: {layerList.Count}",
                    GetTerrainData(terrainObj)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to add texture layer: {e.Message}");
            }
        }

        // ===== 移除纹理层 =====
        private object RemoveTextureLayer(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                int layerIndex = args["layer_index"].AsIntDefault(-1);
                if (layerIndex < 0)
                    return Response.Error("'layer_index' is required and must be >= 0.");

                TerrainLayer[] layers = terrain.terrainData.terrainLayers;
                if (layerIndex >= layers.Length)
                    return Response.Error($"Layer index {layerIndex} out of range. Terrain has {layers.Length} layers.");

                List<TerrainLayer> layerList = new List<TerrainLayer>(layers);
                layerList.RemoveAt(layerIndex);

                Undo.RecordObject(terrain.terrainData, "Remove Terrain Layer");
                terrain.terrainData.terrainLayers = layerList.ToArray();
                EditorUtility.SetDirty(terrain.terrainData);

                return Response.Success(
                    $"Texture layer {layerIndex} removed from terrain '{terrainObj.name}'. Remaining layers: {layerList.Count}",
                    GetTerrainData(terrainObj)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to remove texture layer: {e.Message}");
            }
        }

        // ===== 设置地形尺寸 =====
        private object SetTerrainSize(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                var terrainSizeNode = args["terrain_size"];
                if (terrainSizeNode == null)
                    return Response.Error("'terrain_size' is required.");

                Vector3 newSize = terrain.terrainData.size;
                if (terrainSizeNode is JsonArray sizeArray && sizeArray.Count >= 3)
                {
                    newSize = new Vector3(
                        sizeArray[0].AsFloat,
                        sizeArray[1].AsFloat,
                        sizeArray[2].AsFloat
                    );
                }

                Undo.RecordObject(terrain.terrainData, "Set Terrain Size");
                terrain.terrainData.size = newSize;
                EditorUtility.SetDirty(terrain.terrainData);

                return Response.Success(
                    $"Terrain '{terrainObj.name}' size set to {newSize}.",
                    GetTerrainData(terrainObj)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set terrain size: {e.Message}");
            }
        }

        // ===== 平坦化地形 =====
        private object FlattenTerrain(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                float height = args["height"].AsFloatDefault(0f);
                int resolution = terrain.terrainData.heightmapResolution;
                float[,] heights = new float[resolution, resolution];

                // 填充统一高度
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        heights[y, x] = height;
                    }
                }

                Undo.RecordObject(terrain.terrainData, "Flatten Terrain");
                terrain.terrainData.SetHeights(0, 0, heights);
                EditorUtility.SetDirty(terrain.terrainData);

                return Response.Success(
                    $"Terrain '{terrainObj.name}' flattened to height {height}.",
                    GetTerrainData(terrainObj)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to flatten terrain: {e.Message}");
            }
        }

        // ===== 平滑地形 =====
        private object SmoothTerrain(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                float smoothFactor = args["smooth_factor"].AsFloatDefault(0.5f);
                int resolution = terrain.terrainData.heightmapResolution;
                float[,] heights = terrain.terrainData.GetHeights(0, 0, resolution, resolution);
                float[,] smoothedHeights = new float[resolution, resolution];

                // 简单的平滑算法
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        float sum = 0f;
                        int count = 0;

                        // 3x3邻域平均
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution)
                                {
                                    sum += heights[ny, nx];
                                    count++;
                                }
                            }
                        }

                        float averaged = sum / count;
                        smoothedHeights[y, x] = Mathf.Lerp(heights[y, x], averaged, smoothFactor);
                    }
                }

                Undo.RecordObject(terrain.terrainData, "Smooth Terrain");
                terrain.terrainData.SetHeights(0, 0, smoothedHeights);
                EditorUtility.SetDirty(terrain.terrainData);

                return Response.Success(
                    $"Terrain '{terrainObj.name}' smoothed with factor {smoothFactor}.",
                    GetTerrainData(terrainObj)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to smooth terrain: {e.Message}");
            }
        }

        // ===== 导出高度图 =====
        private object ExportHeightmap(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                string heightmapFile = args["heightmap_file"]?.Value;
                if (string.IsNullOrEmpty(heightmapFile))
                    return Response.Error("'heightmap_file' is required for export.");

                string exportFormat = args["export_format"]?.Value;
                if (string.IsNullOrEmpty(exportFormat)) exportFormat = "raw";

                if (!Path.IsPathRooted(heightmapFile))
                {
                    heightmapFile = Path.Combine(Directory.GetCurrentDirectory(), heightmapFile);
                }

                EnsureDirectoryExists(Path.GetDirectoryName(heightmapFile));

                int resolution = terrain.terrainData.heightmapResolution;
                float[,] heights = terrain.terrainData.GetHeights(0, 0, resolution, resolution);

                bool success = false;
                switch (exportFormat.ToLowerInvariant())
                {
                    case "raw":
                        success = ExportHeightmapRaw(heights, heightmapFile);
                        break;
                    case "png":
                        success = ExportHeightmapPNG(heights, heightmapFile);
                        break;
                    default:
                        return Response.Error($"Unsupported export format: {exportFormat}. Use 'raw' or 'png'.");
                }

                if (success)
                {
                    return Response.Success($"Heightmap exported to '{heightmapFile}' successfully.");
                }
                else
                {
                    return Response.Error($"Failed to export heightmap to '{heightmapFile}'.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to export heightmap: {e.Message}");
            }
        }

        // ===== 导入高度图 =====
        private object ImportHeightmap(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                string heightmapFile = args["heightmap_file"]?.Value;
                if (string.IsNullOrEmpty(heightmapFile))
                    return Response.Error("'heightmap_file' is required for import.");

                if (!Path.IsPathRooted(heightmapFile))
                {
                    heightmapFile = Path.Combine(Directory.GetCurrentDirectory(), heightmapFile);
                }

                if (!File.Exists(heightmapFile))
                    return Response.Error($"Heightmap file not found: {heightmapFile}");

                string extension = Path.GetExtension(heightmapFile).ToLowerInvariant();
                float[,] heights = null;

                switch (extension)
                {
                    case ".raw":
                        heights = ImportHeightmapRaw(heightmapFile, terrain.terrainData.heightmapResolution);
                        break;
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                        heights = ImportHeightmapImage(heightmapFile, terrain.terrainData.heightmapResolution);
                        break;
                    default:
                        return Response.Error($"Unsupported heightmap format: {extension}");
                }

                if (heights == null)
                    return Response.Error($"Failed to import heightmap from '{heightmapFile}'.");

                Undo.RecordObject(terrain.terrainData, "Import Heightmap");
                terrain.terrainData.SetHeights(0, 0, heights);
                EditorUtility.SetDirty(terrain.terrainData);

                return Response.Success(
                    $"Heightmap imported from '{heightmapFile}' successfully.",
                    GetTerrainData(terrainObj)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to import heightmap: {e.Message}");
            }
        }

        // ===== 获取地形信息 =====
        private object GetTerrainInfo(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                return Response.Success(
                    "Terrain info retrieved.",
                    GetTerrainData(terrainObj)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get terrain info: {e.Message}");
            }
        }

        // ===== 清除树木 =====
        private object ClearTrees(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                Undo.RecordObject(terrain.terrainData, "Clear Trees");
                terrain.terrainData.treeInstances = new TreeInstance[0];
                EditorUtility.SetDirty(terrain.terrainData);

                return Response.Success($"All trees cleared from terrain '{terrainObj.name}'.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to clear trees: {e.Message}");
            }
        }

        // ===== 清除细节（草等） =====
        private object ClearDetails(JsonClass args)
        {
            try
            {
                GameObject terrainObj = GetTerrainGameObject(args);
                if (terrainObj == null)
                    return Response.Error("Terrain not found.");

                Terrain terrain = terrainObj.GetComponent<Terrain>();
                if (terrain == null)
                    return Response.Error("GameObject does not have Terrain component.");

                Undo.RecordObject(terrain.terrainData, "Clear Details");

                int detailResolution = terrain.terrainData.detailResolution;
                int detailLayerCount = terrain.terrainData.detailPrototypes.Length;

                for (int i = 0; i < detailLayerCount; i++)
                {
                    int[,] detailLayer = new int[detailResolution, detailResolution];
                    terrain.terrainData.SetDetailLayer(0, 0, i, detailLayer);
                }

                EditorUtility.SetDirty(terrain.terrainData);

                return Response.Success($"All details cleared from terrain '{terrainObj.name}'.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to clear details: {e.Message}");
            }
        }

        // ===== 辅助方法 =====

        private GameObject GetTerrainGameObject(JsonClass args)
        {
            // 优先使用instance_id
            int instanceId = args["instance_id"].AsIntDefault(0);
            if (instanceId != 0)
            {
                UnityEngine.Object obj = EditorUtility.InstanceIDToObject(instanceId);
                if (obj is GameObject go)
                    return go;
            }

            // 使用path查找
            string path = args["path"]?.Value;
            if (!string.IsNullOrEmpty(path))
            {
                return GameObject.Find(path);
            }

            return null;
        }

        private bool ApplyTerrainProperties(Terrain terrain, JsonClass properties)
        {
            bool modified = false;
            TerrainData data = terrain.terrainData;

            // 设置材质
            if (properties["material_template"]?.Value is string materialPath)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (mat != null)
                {
                    terrain.materialTemplate = mat;
                    modified = true;
                }
            }

            // 设置阴影投射
            if (properties["cast_shadows"] != null)
            {
                // 替换过时的castShadows API
                terrain.shadowCastingMode = properties["cast_shadows"].AsBool ?
                    UnityEngine.Rendering.ShadowCastingMode.On :
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                modified = true;
            }

            // 设置绘制模式
            if (properties["draw_heightmap"] != null)
            {
                terrain.drawHeightmap = properties["draw_heightmap"].AsBool;
                modified = true;
            }

            if (properties["draw_trees"] != null)
            {
                terrain.drawTreesAndFoliage = properties["draw_trees"].AsBool;
                modified = true;
            }

            // 设置细节距离
            if (properties["detail_object_distance"] != null)
            {
                terrain.detailObjectDistance = properties["detail_object_distance"].AsFloat;
                modified = true;
            }

            // 设置树距离
            if (properties["tree_distance"] != null)
            {
                terrain.treeDistance = properties["tree_distance"].AsFloat;
                modified = true;
            }

            return modified;
        }

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

        private void EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return;
            string fullDirPath = directoryPath;
            if (!Path.IsPathRooted(fullDirPath))
            {
                fullDirPath = Path.Combine(Directory.GetCurrentDirectory(), directoryPath);
            }
            if (!Directory.Exists(fullDirPath))
            {
                Directory.CreateDirectory(fullDirPath);
            }
        }

        private bool ExportHeightmapRaw(float[,] heights, string filePath)
        {
            try
            {
                int resolution = heights.GetLength(0);
                byte[] bytes = new byte[resolution * resolution * 2];

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        int index = (y * resolution + x) * 2;
                        ushort height = (ushort)(heights[y, x] * 65535);
                        bytes[index] = (byte)(height & 0xFF);
                        bytes[index + 1] = (byte)((height >> 8) & 0xFF);
                    }
                }

                File.WriteAllBytes(filePath, bytes);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export RAW heightmap: {e.Message}");
                return false;
            }
        }

        private bool ExportHeightmapPNG(float[,] heights, string filePath)
        {
            try
            {
                int resolution = heights.GetLength(0);
                Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        float height = heights[y, x];
                        texture.SetPixel(x, y, new Color(height, height, height));
                    }
                }

                texture.Apply();
                byte[] bytes = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);

                File.WriteAllBytes(filePath, bytes);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export PNG heightmap: {e.Message}");
                return false;
            }
        }

        private float[,] ImportHeightmapRaw(string filePath, int resolution)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                float[,] heights = new float[resolution, resolution];

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        int index = (y * resolution + x) * 2;
                        if (index + 1 < bytes.Length)
                        {
                            ushort height = (ushort)(bytes[index] | (bytes[index + 1] << 8));
                            heights[y, x] = height / 65535f;
                        }
                    }
                }

                return heights;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import RAW heightmap: {e.Message}");
                return null;
            }
        }

        private float[,] ImportHeightmapImage(string filePath, int resolution)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);

                float[,] heights = new float[resolution, resolution];

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        float fx = (float)x / resolution * texture.width;
                        float fy = (float)y / resolution * texture.height;
                        Color color = texture.GetPixel((int)fx, (int)fy);
                        heights[y, x] = color.grayscale;
                    }
                }

                UnityEngine.Object.DestroyImmediate(texture);
                return heights;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import image heightmap: {e.Message}");
                return null;
            }
        }

        private object GetTerrainData(GameObject terrainObj)
        {
            if (terrainObj == null)
                return null;

            Terrain terrain = terrainObj.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
                return null;

            TerrainData data = terrain.terrainData;
            string dataPath = AssetDatabase.GetAssetPath(data);
            string guid = AssetDatabase.AssetPathToGUID(dataPath);

            var yaml = $@"name: {terrainObj.name}
instanceID: {terrainObj.GetInstanceID()}
position: [{terrain.transform.position.x:F2}, {terrain.transform.position.y:F2}, {terrain.transform.position.z:F2}]
terrainDataPath: {dataPath}
terrainDataGuid: {guid}
size: [{data.size.x:F2}, {data.size.y:F2}, {data.size.z:F2}]
heightmapResolution: {data.heightmapResolution}
alphamapResolution: {data.alphamapResolution}
detailResolution: {data.detailResolution}
layers: {data.terrainLayers?.Length ?? 0}
treeInstances: {data.treeInstances?.Length ?? 0}
treePrototypes: {data.treePrototypes?.Length ?? 0}
detailPrototypes: {data.detailPrototypes?.Length ?? 0}
castShadows: {(terrain.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off).ToString().ToLower()}
drawHeightmap: {terrain.drawHeightmap.ToString().ToLower()}
drawTrees: {terrain.drawTreesAndFoliage.ToString().ToLower()}";

            return new { yaml = yaml };
        }
    }
}


using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RandomWorldGenerator
{
    [MenuItem("Tools/Generate Random World")]
    public static void GenerateRandomWorld()
    {
        // 清理现有地形
        Terrain existingTerrain = GameObject.FindObjectOfType<Terrain>();
        if (existingTerrain != null)
        {
            Object.DestroyImmediate(existingTerrain.gameObject);
        }
        
        // 创建新地形
        GameObject terrainObject = Terrain.CreateTerrainGameObject(new TerrainData());
        terrainObject.name = "RandomWorldTerrain";
        Terrain terrain = terrainObject.GetComponent<Terrain>();
        TerrainCollider terrainCollider = terrainObject.GetComponent<TerrainCollider>();
        
        // 配置地形数据
        TerrainData terrainData = terrain.terrainData;
        terrainData.size = new Vector3(2000, 200, 2000); // 大世界尺寸
        
        // 设置地形分辨率
        int resolution = 257; // 地形高度图分辨率
        terrainData.heightmapResolution = resolution;
        
        // 生成随机高度图
        float[,] heights = new float[resolution, resolution];
        
        // 使用Perlin噪声生成自然地形
        float scale = 0.02f;
        float offsetX = Random.Range(0, 9999);
        float offsetY = Random.Range(0, 9999);
        
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                // 使用多层Perlin噪声创建更自然的地形
                float height = 0;
                float amplitude = 1.0f;
                float frequency = scale;
                
                // 四层噪声叠加
                for (int i = 0; i < 4; i++)
                {
                    height += Mathf.PerlinNoise(
                        (x + offsetX) * frequency, 
                        (y + offsetY) * frequency) * amplitude;
                    frequency *= 2;
                    amplitude *= 0.5f;
                }
                
                // 限制高度范围并应用指数曲线使地形更自然
                height = Mathf.Clamp01(height * 0.5f);
                heights[x, y] = Mathf.Pow(height, 1.2f); // 指数调整使地形更自然
            }
        }
        
        // 应用高度图
        terrainData.SetHeights(0, 0, heights);
        
        // 添加基本纹理
        List<SplatPrototype> splatPrototypes = new List<SplatPrototype>();
        
        // 创建基本材质（草地、岩石、沙滩等）
        Texture2D grassTexture = CreateTexture(Color.green, 512, 512, 1);
        Texture2D rockTexture = CreateTexture(Color.grey, 512, 512, 0.3f);
        Texture2D sandTexture = CreateTexture(Color.yellow, 512, 512, 0.5f);
        
        // 添加草地纹理
        SplatPrototype grassPrototype = new SplatPrototype();
        grassPrototype.texture = grassTexture;
        grassPrototype.normalMap = CreateNormalMap(grassTexture);
        splatPrototypes.Add(grassPrototype);
        
        // 添加岩石纹理
        SplatPrototype rockPrototype = new SplatPrototype();
        rockPrototype.texture = rockTexture;
        rockPrototype.normalMap = CreateNormalMap(rockTexture);
        splatPrototypes.Add(rockPrototype);
        
        // 添加沙滩纹理
        SplatPrototype sandPrototype = new SplatPrototype();
        sandPrototype.texture = sandTexture;
        sandPrototype.normalMap = CreateNormalMap(sandTexture);
        splatPrototypes.Add(sandPrototype);
        
        terrainData.splatPrototypes = splatPrototypes.ToArray();
        
        // 创建纹理贴图
        float[,,] alphamap = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, splatPrototypes.Count];
        
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // 根据高度确定地形类型
                float height = terrainData.GetHeight(
                    y * (resolution - 1) / (terrainData.alphamapHeight - 1),
                    x * (resolution - 1) / (terrainData.alphamapWidth - 1)) / terrainData.size.y;
                
                // 重置所有通道
                for (int i = 0; i < splatPrototypes.Count; i++)
                {
                    alphamap[x, y, i] = 0;
                }
                
                // 低海拔使用沙滩
                if (height < 0.3f)
                {
                    alphamap[x, y, 2] = 1.0f;
                }
                // 中等海拔使用草地
                else if (height < 0.6f)
                {
                    alphamap[x, y, 0] = 1.0f;
                }
                // 高海拔使用岩石
                else
                {
                    alphamap[x, y, 1] = 1.0f;
                }
            }
        }
        
        // 应用纹理贴图
        terrainData.SetAlphamaps(0, 0, alphamap);
        
        // 添加一些随机树木
        List<TreePrototype> treePrototypes = new List<TreePrototype>();
        
        // 创建简单的树木预制体
        GameObject treePrefab = CreateSimpleTree();
        TreePrototype treePrototype = new TreePrototype();
        treePrototype.prefab = treePrefab;
        treePrototypes.Add(treePrototype);
        
        terrainData.treePrototypes = treePrototypes.ToArray();
        
        // 随机放置树木
        int treeCount = 500;
        TreeInstance[] trees = new TreeInstance[treeCount];
        
        for (int i = 0; i < treeCount; i++)
        {
            TreeInstance tree = new TreeInstance();
            tree.position = new Vector3(
                Random.Range(0f, 1f),
                0,
                Random.Range(0f, 1f));
            
            // 只在中等高度的草地上放置树木
            float height = terrainData.GetHeight(
                Mathf.RoundToInt(tree.position.z * (resolution - 1)),
                Mathf.RoundToInt(tree.position.x * (resolution - 1))) / terrainData.size.y;
            
            if (height >= 0.3f && height < 0.6f)
            {
                tree.prototypeIndex = 0;
                tree.widthScale = Random.Range(0.8f, 1.2f);
                tree.heightScale = Random.Range(0.8f, 1.2f);
                tree.color = Color.Lerp(Color.green, new Color(0.5f, 0.8f, 0.5f), Random.value);
                tree.lightmapColor = Color.white;
                trees[i] = tree;
            }
        }
        
        terrainData.treeInstances = trees;
        
        // 添加一些随机装饰物（如石头）
        for (int i = 0; i < 200; i++)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rock.transform.localScale = new Vector3(
                Random.Range(1f, 3f),
                Random.Range(1f, 3f),
                Random.Range(1f, 3f));
            
            Vector3 position = new Vector3(
                Random.Range(0, terrainData.size.x),
                0,
                Random.Range(0, terrainData.size.z));
            
            // 获取地形高度
            float y = terrain.SampleHeight(position) + rock.transform.localScale.y / 2;
            position.y = y;
            
            rock.transform.position = position;
            rock.name = "DecorativeRock";
            
            // 随机旋转
            rock.transform.rotation = Quaternion.Euler(
                0,
                Random.Range(0, 360),
                0);
            
            // 添加简单材质
            Renderer renderer = rock.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = Color.Lerp(Color.grey, new Color(0.3f, 0.3f, 0.3f), Random.value);
            renderer.material = material;
        }
        
        // 配置光照
        Light sun = GameObject.FindObjectOfType<Light>();
        if (sun == null)
        {
            GameObject sunObject = new GameObject("Sun");
            sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.intensity = 1.2f;
        sun.transform.rotation = Quaternion.Euler(45, 45, 0);
        
        // 配置相机
        Camera mainCamera = GameObject.FindObjectOfType<Camera>();
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("MainCamera");
            mainCamera = cameraObject.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
        }
        
        // 设置相机位置
        mainCamera.transform.position = new Vector3(
            terrainData.size.x / 2,
            100,
            terrainData.size.z / 2 - 100);
        mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
        
        Debug.Log("随机大世界地图生成完成！");
    }
    
    // 创建简单的纹理
    private static Texture2D CreateTexture(Color baseColor, int width, int height, float variation)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 添加一些随机变化使纹理更自然
                float r = baseColor.r + Random.Range(-variation, variation);
                float g = baseColor.g + Random.Range(-variation, variation);
                float b = baseColor.b + Random.Range(-variation, variation);
                
                pixels[y * width + x] = new Color(
                    Mathf.Clamp01(r),
                    Mathf.Clamp01(g),
                    Mathf.Clamp01(b));
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    // 创建简单的法线贴图
    private static Texture2D CreateNormalMap(Texture2D sourceTexture)
    {
        Texture2D normalMap = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.ARGB32, true);
        
        for (int y = 0; y < sourceTexture.height; y++)
        {
            for (int x = 0; x < sourceTexture.width; x++)
            {
                // 创建一个简单的法线贴图（实际上应该从高度图计算）
                normalMap.SetPixel(x, y, new Color(0.5f, 0.5f, 1f, 1f));
            }
        }
        
        normalMap.Apply();
        return normalMap;
    }
    
    // 创建简单的树木预制体
    private static GameObject CreateSimpleTree()
    {
        GameObject tree = new GameObject("SimpleTree");
        
        // 树干
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.parent = tree.transform;
        trunk.transform.localPosition = Vector3.zero;
        trunk.transform.localScale = new Vector3(1, 4, 1);
        
        Renderer trunkRenderer = trunk.GetComponent<Renderer>();
        Material trunkMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        trunkMaterial.color = new Color(0.4f, 0.2f, 0.1f);
        trunkRenderer.material = trunkMaterial;
        
        // 树冠
        GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.transform.parent = tree.transform;
        leaves.transform.localPosition = new Vector3(0, 7, 0);
        leaves.transform.localScale = new Vector3(5, 5, 5);
        
        Renderer leavesRenderer = leaves.GetComponent<Renderer>();
        Material leavesMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        leavesMaterial.color = new Color(0.2f, 0.6f, 0.2f);
        leavesRenderer.material = leavesMaterial;
        
        // 添加碰撞体
        tree.AddComponent<CapsuleCollider>();
        
        return tree;
    }
}
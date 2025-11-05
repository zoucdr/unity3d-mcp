using UnityEngine;
using UnityEditor;

public class ManualWorldGenerator : MonoBehaviour
{
    [MenuItem("Tools/Manual Generate World")]
    public static void GenerateRandomWorld()
    {
        // 创建新地形
        GameObject terrainObject = Terrain.CreateTerrainGameObject(new TerrainData());
        terrainObject.name = "RandomWorldTerrain";
        Terrain terrain = terrainObject.GetComponent<Terrain>();
        
        // 配置地形数据
        TerrainData terrainData = terrain.terrainData;
        terrainData.size = new Vector3(2000, 200, 2000);
        terrainData.heightmapResolution = 257;
        
        // 生成随机高度图
        float[,] heights = new float[257, 257];
        for (int x = 0; x < 257; x++)
        {
            for (int y = 0; y < 257; y++)
            {
                heights[x, y] = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.5f;
            }
        }
        terrainData.SetHeights(0, 0, heights);
        
        Debug.Log("随机大世界地图生成完成！");
    }
}
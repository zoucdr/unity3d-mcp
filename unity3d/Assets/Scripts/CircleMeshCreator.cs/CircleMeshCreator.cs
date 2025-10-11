using UnityEngine;
using UnityEditor;
[ExecuteInEditMode]
public class CircleMeshCreator : MonoBehaviour
{
    [MenuItem("Tools/Create Circle Mesh")]
    public static void CreateCircleMesh()
    {
        // 创建圆形网格
        Mesh mesh = new Mesh();
        mesh.name = "CircleMesh60";
        
        int segments = 60;
        float radius = 1f;
        
        // 创建顶点数组（中心点 + 圆周上的60个点）
        Vector3[] vertices = new Vector3[segments + 1];
        Vector2[] uv = new Vector2[segments + 1];
        
        // 中心点
        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);
        
        // 圆周上的点
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices[i + 1] = new Vector3(x, 0, z);
            uv[i + 1] = new Vector2((x + radius) / (radius * 2f), (z + radius) / (radius * 2f));
        }
        
        // 创建三角形索引
        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // 保存到Assets/Meshes目录
        string path = "Assets/Meshes/CircleMesh60.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Meshes"))
        {
            AssetDatabase.CreateFolder("Assets", "Meshes");
        }
        
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Circle mesh created successfully with {vertices.Length} vertices at: {path}");
    }
}
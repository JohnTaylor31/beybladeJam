using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class RidgeArena : MonoBehaviour
{
    [Header("Arena Shape")]
    [Min(2)] public int resolution = 100;
    [Min(0.01f)] public float radius = 20f;
    public float curveStrength = 0.05f;

    [Header("Ridge Settings")]
    public float ridgeRadius = 4f;
    public float ridgeHeight = 1f;
    public float ridgeWidth = 1.5f;

    private Mesh mesh;

    void OnEnable() => Generate();
    void OnValidate()
    {
        resolution = Mathf.Max(2, resolution);
        radius = Mathf.Max(0.01f, radius);
        Generate();
    }

    void Generate()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshCollider mc = GetComponent<MeshCollider>();
        if (!mf || !mc) return;

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "RidgeArenaMesh";
        }
        else mesh.Clear();

        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector2[] uvs = new Vector2[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

        int v = 0;
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float nx = (x / (float)(resolution - 1) - 0.5f) * radius;
                float ny = (y / (float)(resolution - 1) - 0.5f) * radius;

                float baseZ = curveStrength * (nx * nx + ny * ny);

                float dist = Mathf.Sqrt(nx * nx + ny * ny);

                float ridgeOffset = 0f;
                if (dist < ridgeRadius + ridgeWidth && dist > ridgeRadius - ridgeWidth)
                {
                    float t = 1f - Mathf.Abs(dist - ridgeRadius) / ridgeWidth;
                    ridgeOffset = Mathf.Lerp(0f, ridgeHeight, t);
                }

                float finalZ = baseZ - ridgeOffset;

                vertices[v] = new Vector3(nx, finalZ, ny);
                uvs[v] = new Vector2(x / (float)(resolution - 1), y / (float)(resolution - 1));
                v++;
            }
        }

        int tIndex = 0;
        for (int x = 0; x < resolution - 1; x++)
        {
            for (int y = 0; y < resolution - 1; y++)
            {
                int i = x * resolution + y;

                triangles[tIndex++] = i;
                triangles[tIndex++] = i + 1;
                triangles[tIndex++] = i + resolution;

                triangles[tIndex++] = i + 1;
                triangles[tIndex++] = i + resolution + 1;
                triangles[tIndex++] = i + resolution;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }
}

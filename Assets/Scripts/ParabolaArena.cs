using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ParabolaArena : MonoBehaviour
{
    [Header("Arena Shape")]
    [Min(2)] public int resolution = 80;
    [Min(0.01f)] public float radius = 5f;
    public float curveStrength = 0.15f;

    private Mesh mesh;

    void OnEnable()
    {
        Generate();
    }

    void OnValidate()
    {
        // Clamp values BEFORE generating, otherwise resolution <= 1 or
        // radius <= 0 produces an invalid triangle/vertex mismatch and
        // Unity silently fails to build the mesh (arena won't appear).
        resolution = Mathf.Max(2, resolution);
        radius = Mathf.Max(0.01f, radius);
        Generate();
    }

    void Generate()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        MeshCollider mc = GetComponent<MeshCollider>();
        if (!mf || !mr || !mc)
            return;

        // Reuse the mesh instead of allocating a new one every OnValidate
        // call. With ExecuteInEditMode, OnValidate fires very often, and
        // creating a fresh Mesh object each time leaks hidden mesh assets
        // in the editor (memory bloat, eventually sluggish/broken editor).
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "ParabolaArenaMesh";
        }
        else
        {
            mesh.Clear();
        }

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
                float z = curveStrength * (nx * nx + ny * ny);

                // Bowl dips downward (centre lower than edges)
                vertices[v] = new Vector3(nx, z, ny);
                uvs[v] = new Vector2(x / (float)(resolution - 1), y / (float)(resolution - 1));
                v++;
            }
        }

        int t = 0;
        for (int x = 0; x < resolution - 1; x++)
        {
            for (int y = 0; y < resolution - 1; y++)
            {
                int i = x * resolution + y;

                // Correct winding order (normals face inward)
                triangles[t++] = i;
                triangles[t++] = i + 1;
                triangles[t++] = i + resolution;

                triangles[t++] = i + 1;
                triangles[t++] = i + resolution + 1;
                triangles[t++] = i + resolution;
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
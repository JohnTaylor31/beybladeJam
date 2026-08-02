using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class PocketArena : MonoBehaviour
{
    [Header("Arena Shape")]
    [Min(2)] public int resolution = 100;
    [Min(0.01f)] public float radius = 20f;
    public float curveStrength = 0.05f;

    [Header("Pocket Settings")]
    public float pocketDepth = 1.2f;     // shallow, realistic
    public float pocketAngularWidth = 0.12f; // 12% of circle
    public float pocketRadialStart = 0.75f;  // pockets only near rim
    public float pocketRadialBlend = 0.2f;   // smooth blend inward
    public int pocketCount = 3;

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
            mesh.name = "PocketArenaMesh";
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
                float radialNorm = dist / radius;

                float angle = Mathf.Atan2(ny, nx);
                float normalizedAngle = (angle + Mathf.PI) / (2f * Mathf.PI);

                float pocketOffset = 0f;

                // Only apply pockets near the rim
                if (radialNorm > pocketRadialStart)
                {
                    float radialBlend = Mathf.InverseLerp(
                        pocketRadialStart,
                        pocketRadialStart + pocketRadialBlend,
                        radialNorm
                    );

                    for (int p = 0; p < pocketCount; p++)
                    {
                        float pocketCenter = p / (float)pocketCount;
                        float diff = Mathf.Abs(normalizedAngle - pocketCenter);
                        diff = Mathf.Min(diff, 1f - diff);

                        if (diff < pocketAngularWidth * 0.5f)
                        {
                            float t = 1f - (diff / (pocketAngularWidth * 0.5f));
                            pocketOffset = Mathf.Lerp(0f, pocketDepth, t * radialBlend);
                            break;
                        }
                    }
                }

                float finalZ = baseZ + pocketOffset;

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

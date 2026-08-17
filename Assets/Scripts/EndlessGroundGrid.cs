using System.Collections.Generic;
using UnityEngine;

namespace FlightSimulation
{
    public sealed class EndlessGroundGrid : MonoBehaviour
    {
        [SerializeField, InspectorName("跟随目标")] private Transform target;
        [SerializeField, InspectorName("地面材质")] private Material groundMaterial;
        [SerializeField, InspectorName("网格材质")] private Material gridMaterial;
        [SerializeField, InspectorName("方向标记材质")] private Material directionMaterial;
        [SerializeField, InspectorName("地块尺寸"), Min(100f)] private float tileSize = 500f;
        [SerializeField, InspectorName("地块半径"), Range(1, 4)] private int tileRadius = 2;
        [SerializeField, InspectorName("地面高度")] private float groundHeight;
        [SerializeField, InspectorName("网格间距"), Min(20f)] private float gridSpacing = 100f;
        [SerializeField, InspectorName("网格线宽"), Min(0.5f)] private float gridLineWidth = 2.5f;

        private Transform[] tiles;
        private Mesh surfaceMesh;
        private Vector2Int centerCell = new Vector2Int(int.MinValue, int.MinValue);

        public int TileCount => tiles != null ? tiles.Length : 0;
        public Vector2Int CenterCell => centerCell;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void Awake()
        {
            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
            }

            surfaceMesh = BuildSurfaceMesh();
            CreateTilePool();
            RefreshTilePositions(true);
        }

        private void LateUpdate()
        {
            RefreshTilePositions(false);
        }

        private void OnDestroy()
        {
            if (surfaceMesh == null) return;

            if (Application.isPlaying) Destroy(surfaceMesh);
            else DestroyImmediate(surfaceMesh);
        }

        private void CreateTilePool()
        {
            int diameter = tileRadius * 2 + 1;
            tiles = new Transform[diameter * diameter];

            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = new GameObject($"Ground Tile {i:00}");
                tile.transform.SetParent(transform, false);

                var filter = tile.AddComponent<MeshFilter>();
                filter.sharedMesh = surfaceMesh;

                var renderer = tile.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { groundMaterial, gridMaterial, directionMaterial };
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = true;

                var collider = tile.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, -2f, 0f);
                collider.size = new Vector3(tileSize, 4f, tileSize);

                tiles[i] = tile.transform;
            }
        }

        private void RefreshTilePositions(bool force)
        {
            if (target == null || tiles == null) return;

            Vector2Int nextCenter = new Vector2Int(
                Mathf.FloorToInt((target.position.x + tileSize * 0.5f) / tileSize),
                Mathf.FloorToInt((target.position.z + tileSize * 0.5f) / tileSize));

            if (!force && nextCenter == centerCell) return;

            centerCell = nextCenter;
            int index = 0;
            for (int z = -tileRadius; z <= tileRadius; z++)
            {
                for (int x = -tileRadius; x <= tileRadius; x++)
                {
                    Vector2Int cell = centerCell + new Vector2Int(x, z);
                    tiles[index++].position = new Vector3(cell.x * tileSize, groundHeight, cell.y * tileSize);
                }
            }
        }

        private Mesh BuildSurfaceMesh()
        {
            var vertices = new List<Vector3>(256);
            var groundTriangles = new List<int>(6);
            var gridTriangles = new List<int>(256);
            var directionTriangles = new List<int>(256);
            float half = tileSize * 0.5f;

            AddHorizontalQuad(vertices, groundTriangles, -half, half, -half, half, 0f);

            float markerY = 0.08f;
            for (float offset = -half; offset <= half + 0.01f; offset += gridSpacing)
            {
                AddHorizontalQuad(vertices, gridTriangles, offset - gridLineWidth * 0.5f, offset + gridLineWidth * 0.5f, -half, half, markerY);
                AddHorizontalQuad(vertices, gridTriangles, -half, half, offset - gridLineWidth * 0.5f, offset + gridLineWidth * 0.5f, markerY);
            }

            float arrowLength = Mathf.Min(tileSize * 0.34f, 170f);
            float arrowWidth = Mathf.Max(8f, tileSize * 0.025f);
            AddHorizontalQuad(vertices, directionTriangles, -arrowWidth * 0.5f, arrowWidth * 0.5f, -arrowLength * 0.5f, arrowLength * 0.25f, markerY + 0.04f);
            AddHorizontalTriangle(
                vertices,
                directionTriangles,
                new Vector3(-arrowWidth * 2f, markerY + 0.04f, arrowLength * 0.2f),
                new Vector3(0f, markerY + 0.04f, arrowLength * 0.5f),
                new Vector3(arrowWidth * 2f, markerY + 0.04f, arrowLength * 0.2f));

            float pylonOffset = half * 0.34f;
            AddBox(vertices, directionTriangles, new Vector3(-pylonOffset, 22.5f, pylonOffset), new Vector3(8f, 45f, 8f));
            AddBox(vertices, directionTriangles, new Vector3(pylonOffset, 22.5f, pylonOffset), new Vector3(8f, 45f, 8f));
            AddBox(vertices, directionTriangles, new Vector3(-pylonOffset, 10f, -pylonOffset), new Vector3(8f, 20f, 8f));
            AddBox(vertices, directionTriangles, new Vector3(pylonOffset, 10f, -pylonOffset), new Vector3(8f, 20f, 8f));

            var mesh = new Mesh { name = "Endless Ground Tile" };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(groundTriangles, 0);
            mesh.SetTriangles(gridTriangles, 1);
            mesh.SetTriangles(directionTriangles, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddHorizontalQuad(List<Vector3> vertices, List<int> triangles, float minX, float maxX, float minZ, float maxZ, float y)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(minX, y, minZ));
            vertices.Add(new Vector3(minX, y, maxZ));
            vertices.Add(new Vector3(maxX, y, maxZ));
            vertices.Add(new Vector3(maxX, y, minZ));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static void AddHorizontalTriangle(List<Vector3> vertices, List<int> triangles, Vector3 left, Vector3 tip, Vector3 right)
        {
            int start = vertices.Count;
            vertices.Add(left);
            vertices.Add(tip);
            vertices.Add(right);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        private static void AddBox(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size)
        {
            Vector3 half = size * 0.5f;
            float x0 = center.x - half.x;
            float x1 = center.x + half.x;
            float y0 = center.y - half.y;
            float y1 = center.y + half.y;
            float z0 = center.z - half.z;
            float z1 = center.z + half.z;

            AddQuad(vertices, triangles, new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0));
            AddQuad(vertices, triangles, new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1));
            AddQuad(vertices, triangles, new Vector3(x1, y0, z0), new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0));
            AddQuad(vertices, triangles, new Vector3(x1, y0, z1), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1));
            AddQuad(vertices, triangles, new Vector3(x0, y0, z0), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0));
        }

        private static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }
    }
}

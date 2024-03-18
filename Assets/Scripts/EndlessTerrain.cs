using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
	const float scale = 2f;

    const float viewerMoveThresholdForChunkUpdate = 25f * 25f;
    public LODInfo[] detailLevels;
    public static float maxViewDistance;
    public Transform viewer;
    static MapGenerator mapGenerator;
    public Material mapMaterial;
    public static Vector2 viewPosition;
    Vector2 oldViewerPosition;
    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start() {
        mapGenerator = FindAnyObjectByType<MapGenerator>();

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);

        UpdateVisibleChunks();
    }

    void Update() {
        viewPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;

        if ((oldViewerPosition - viewPosition).sqrMagnitude > viewerMoveThresholdForChunkUpdate) {
            oldViewerPosition = viewPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks() {

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
                Vector2 viewChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewChunkCoord)) {
                    terrainChunkDictionary[viewChunkCoord].UpdateTerrainChunk();
                }
                else {
                    terrainChunkDictionary.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk {
        GameObject meshObject;
        Bounds bounds;
        Vector2 position;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;
        LODInfo[] detailLevels;
        LODMesh[] LODMeshes;
        LODMesh colliderLODMesh;
        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
            this.detailLevels = detailLevels;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 1, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            LODMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                LODMeshes[i] = new LODMesh(detailLevels[i].LOD, UpdateTerrainChunk);
                colliderLODMesh = LODMeshes[i];
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData) {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk() {

            if (mapDataReceived) {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewPosition));
                bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

                if (visible) {
                    int LODIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++) {
                        if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold) {LODIndex = i + 1;}
                        else {break;}
                    }

                    if (LODIndex != previousLODIndex) {
                        LODMesh lodMesh = LODMeshes[LODIndex];

                        if (lodMesh.hasMesh) {
                            previousLODIndex = LODIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh){
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    if (colliderLODMesh.hasMesh) {
                        meshCollider.sharedMesh = colliderLODMesh.mesh;
                    }
                    else if (!colliderLODMesh.hasRequestedMesh) {
                        colliderLODMesh.RequestMesh(mapData);
                    }

					terrainChunksVisibleLastUpdate.Add(this);
                }
            
                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }

    class LODMesh {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int LOD;
        System.Action updateCallBack;

        public LODMesh(int LOD, System.Action updateCallBack) {
            this.LOD = LOD;
            this.updateCallBack = updateCallBack;
        }

        void OnMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallBack();
        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, LOD, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo {
        public int LOD;
        public float visibleDistanceThreshold;
        public bool useForCollider;
    }
}
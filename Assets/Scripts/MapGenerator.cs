using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode{NoiseMap, ColorMap, Mesh, FallOffMap};
    public DrawMode drawMode;
    
    public bool useFlatShading;
    [Range(0, 6)]
    public int editorPreviewLOD;
    public float noiseScale;
    public int octaves;
    [Range(0 ,1)]
    public float persistance;
    public float lacunarity;
    public Vector2 offset;
	public Noise.NormalizedMode normalizedMode;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public int seed;

    public bool autoUpdate;
    public bool useFallOff;
    static MapGenerator instance;

    public TerrainType[] regions;
    float[,] FallOffMap;
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Awake() {
        FallOffMap = FallOffGenerator.GenerateFallOffMap(mapChunkSize);
    }

    public static int mapChunkSize {
        get {
            if (instance == null) {
                instance = FindAnyObjectByType<MapGenerator>();
            }

            if (instance.useFlatShading) {
                return 95;
            }
            else {
                return 239;
            }
        }
    }
    public void DrawMapInEditor() {
        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindAnyObjectByType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap) {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD, useFlatShading), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.FallOffMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FallOffGenerator.GenerateFallOffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			MapDataThread(center, callback);
		};

		new Thread(threadStart).Start();
	}

	void MapDataThread(Vector2 center, Action<MapData> callback) {
		MapData mapData = GenerateMapData(center);
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData>(callback, mapData));
		}
	}

	public void RequestMeshData(MapData mapData, int LOD, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread(mapData, LOD, callback);
		};

		new Thread(threadStart).Start ();
	}

	void MeshDataThread(MapData mapData, int LOD, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, LOD, useFlatShading);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	void Update() {
		if (mapDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}
	}


    MapData GenerateMapData(Vector2 center) {
        float [,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale, octaves, persistance, lacunarity, offset + center, normalizedMode);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {

                if (useFallOff) {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - FallOffMap[x, y]);
                }

                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++) {
                    
					if (currentHeight >= regions[i].height) {
                        colorMap[y * mapChunkSize + x] = regions[i].colour;
                    }
					else {
						break;
					}
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }
        else if (octaves < 0) {
            octaves = 0;
        }

        FallOffMap = FallOffGenerator.GenerateFallOffMap(mapChunkSize);
    }
}

public struct MapThreadInfo<T> {
    public readonly Action<T> callback;
    public readonly T parameter;
    public MapThreadInfo(Action<T> callback, T parameter) {
        this.callback = callback;
        this.parameter = parameter;
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color colour;
}

public struct MapData {
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;
    public MapData(float[,] heightMap, Color[] colorMap) {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
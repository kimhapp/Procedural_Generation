using UnityEngine;
using System;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, Mesh};
	public DrawMode drawMode;

	public Vector2Int size;

	public float noiseScale;
	public int octaves;
	[Range(0,1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public bool autoUpdate;

	float[,] fallOffMap;

    [Range(0, 1)]
    public float fallOffStart;
    [Range(0, 1)]
    public float fallOffEnd;

	void Awake() {
		fallOffMap = FalloffGenerator.GenerateFalloffMap(size, fallOffStart, fallOffEnd);
	}

	public void DrawMapInEditor() {
		MapData mapData = GenerateMapData(Vector2.zero);

		MapDisplay display = FindAnyObjectByType<MapDisplay>();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap(mapData.size));
		} else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh(mapData.size, meshHeightMultiplier, meshHeightCurve), TextureGenerator.TextureFromColourMap(mapData.colourMap, size.x, size.y));
		}
	}
	MapData GenerateMapData(Vector2 centre) {
		float[,] noiseMap = Noise.GenerateNoiseMap(size.x, size.y, seed, noiseScale, octaves, persistance, lacunarity, centre + offset);

		Color[] colourMap = new Color[size.x * size.y]; 
		for (int y = 0; y < size.y; y++) {
			for (int x = 0; x < size.x; x++) {
				noiseMap[x, y] = Mathf.Clamp01(fallOffMap[x, y] - noiseMap[x, y]); 
			}
		}

		return new MapData(noiseMap, colourMap);
	}

	void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}

        fallOffMap = FalloffGenerator.GenerateFalloffMap(size, fallOffStart, fallOffEnd);
	}

    public struct MapData
    {
        public readonly float[,] size;
        public readonly Color[] colourMap;

        public MapData(float[,] size, Color[] colourMap)
        {
            this.size = size;
            this.colourMap = colourMap;
        }
    }
}

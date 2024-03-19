using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData {
    
    public Noise.NormalizedMode normalizedMode;
    public float noiseScale;
    public int octaves;
    [Range(0 ,1)]
    public float persistance;
    public float lacunarity;
    public Vector2 offset;
    public int seed;

    protected override void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }
        else if (octaves < 0) {
            octaves = 0;
        }

        base.OnValidate();
    }
}

using UnityEngine;

public static class HeightMapGenerator {
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre) {
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                values[i, j] *= settings.heightCurve.Evaluate(values[i, j]) * settings.heightMultiplier;
            }
        }
    }
}

public struct HeightMap {
	public readonly float[,] heightMap;
	public readonly float minValue;
	public readonly float maxValue;

	public HeightMap (float[,] heightMap, float minValue, float maxValue)
	{
		this.heightMap = heightMap;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}
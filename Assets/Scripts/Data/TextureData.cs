using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData {

    public Color[] baseColors;
    [Range(0, 1)]
    public float[] baseStartHeights;
    public void ApplyToMaterial(Material material) {
        material.SetInt("baseColorsCount", baseColors.Length);
        material.SetColorArray("baseColors", baseColors);
        material.SetFloatArray("baseStartHeights", baseStartHeights);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight) {

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }
}

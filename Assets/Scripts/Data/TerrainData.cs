using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData {

    public float uniformScale = 2.5f;
    public bool useFallOff;
    public bool useFlatShading;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
}

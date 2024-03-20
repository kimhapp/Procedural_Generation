using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData {

    public float uniformScale = 2.5f;
    public bool useFallOff;
    public bool useFlatShading;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public float minHeight {
        get {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
        }
    }

    public float maxHeight {
        get {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
        }
    }
}

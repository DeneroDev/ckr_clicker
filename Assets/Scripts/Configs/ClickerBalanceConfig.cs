using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(menuName = "CKR/Clicker Balance Config", fileName = "ClickerBalanceConfig")]
    public sealed class ClickerBalanceConfig : ScriptableObject
    {
        [field: SerializeField, Min(1)] public int InitialEnergy { get; private set; } = 1000;
        [field: SerializeField, Min(1)] public int MaxEnergy { get; private set; } = 1000;
        [field: SerializeField, Min(1)] public int TapEnergyCost { get; private set; } = 1;
        [field: SerializeField, Min(1)] public int TapCurrencyReward { get; private set; } = 1;

        [field: SerializeField, Min(0.1f)] public float AutoCollectIntervalSec { get; private set; } = 3f;
        [field: SerializeField, Min(1)] public int AutoCollectEnergyCost { get; private set; } = 1;
        [field: SerializeField, Min(1)] public int AutoCollectCurrencyReward { get; private set; } = 1;

        [field: SerializeField, Min(0.1f)] public float EnergyRegenIntervalSec { get; private set; } = 10f;
        [field: SerializeField, Min(1)] public int EnergyRegenAmount { get; private set; } = 10;

        [field: Header("VFX")]
        [field: SerializeField] public string TapParticleAddressableKey { get; private set; }
        [field: SerializeField, Min(0)] public int TapParticlePrewarmCount { get; private set; } = 8;
        [field: SerializeField] public string FloatingCurrencyTextAddressableKey { get; private set; }
        [field: SerializeField, Min(0)] public int FloatingCurrencyTextPrewarmCount { get; private set; } = 8;

        [field: Header("Floating Currency Motion")]
        [field: SerializeField, Min(0f)] public float FloatingCurrencyHorizontalOffsetMin { get; private set; } = 40f;
        [field: SerializeField, Min(0f)] public float FloatingCurrencyHorizontalOffsetMax { get; private set; } = 140f;
        [field: SerializeField, Min(0f)] public float FloatingCurrencyDownwardOffsetMin { get; private set; } = 90f;
        [field: SerializeField, Min(0f)] public float FloatingCurrencyDownwardOffsetMax { get; private set; } = 170f;
        [field: SerializeField, Min(0f)] public float FloatingCurrencyArcHeightMin { get; private set; } = 45f;
        [field: SerializeField, Min(0f)] public float FloatingCurrencyArcHeightMax { get; private set; } = 110f;
        [field: SerializeField, Range(0f, 1f)] public float FloatingCurrencyHorizontalControlInfluence { get; private set; } = 0.2f;
        [field: SerializeField, Min(0.01f)] public float FloatingCurrencyMinDurationSec { get; private set; } = 0.01f;
    }
}

using System.Collections.Generic;

namespace FirstPersonCameraContinued.Helpers
{
    internal struct WildlifePreset
    {
        public int BoneIndex;
        public float ForwardOffset;
        public float UpOffset;

        public WildlifePreset(int boneIndex, float forwardOffset, float upOffset)
        {
            BoneIndex = boneIndex;
            ForwardOffset = forwardOffset;
            UpOffset = upOffset;
        }
    }

    internal static class WildlifePresets
    {
        // Keyed by prefab name substring (case-insensitive match)
        private static readonly Dictionary<string, WildlifePreset> Presets = new Dictionary<string, WildlifePreset>
        {
            { "Bear", new WildlifePreset(24, 0.25f, 0.05f) },
            { "Goose", new WildlifePreset(3, 0.1f, 0.1f) },
        };

        public static bool TryGetPreset(string prefabName, out WildlifePreset preset, out string matchedName)
        {
            preset = default;
            matchedName = null;

            if (string.IsNullOrEmpty(prefabName))
                return false;

            foreach (KeyValuePair<string, WildlifePreset> entry in Presets)
            {
                if (prefabName.IndexOf(entry.Key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    preset = entry.Value;
                    matchedName = entry.Key;
                    return true;
                }
            }

            return false;
        }
    }
}

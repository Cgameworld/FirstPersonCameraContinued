using System.Collections.Generic;

namespace FirstPersonCameraContinued.Transformer
{
    internal struct WildlifePreset
    {
        public int BoneIndex;
        public float ForwardOffset;
        public float Height;

        public WildlifePreset(int boneIndex, float forwardOffset, float heightOffset)
        {
            BoneIndex = boneIndex;
            ForwardOffset = forwardOffset;
            Height = heightOffset;
        }
    }

    internal static class WildlifePresets
    {
        // Keyed by prefab name substring (case-insensitive match)
        private static readonly Dictionary<string, WildlifePreset> Presets = new Dictionary<string, WildlifePreset>
        {
            { "Bear", new WildlifePreset(24, 0.225f, 0.05f) },
            { "Goose", new WildlifePreset(3, 0.1f, 0.05f) },
            { "Moose", new WildlifePreset(5, 0f, -1f) },
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

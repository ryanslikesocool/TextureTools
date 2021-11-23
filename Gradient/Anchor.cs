// Developed with love by Ryan Boyer http://ryanjboyer.com <3

#if UNITY_EDITOR
using System;
using UnityEngine;
using Unity.Mathematics;

namespace TextureTools.Gradient {
    [Serializable]
    internal struct Anchor {
        [Range(0, 1)] public float time;
        public float pixel;
        public Color color;

        public static float4 Lerp(Anchor lhs, Anchor rhs, int pixel, Func<float4, float4, float, float4> lerp) {
            float t = math.remap(lhs.time, rhs.time, 0, 1, pixel);
            return lerp((Vector4)lhs.color, (Vector4)rhs.color, t);
        }
    }
}
#endif
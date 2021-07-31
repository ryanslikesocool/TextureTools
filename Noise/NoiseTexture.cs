// Made with <3 by Ryan Boyer http://ryanjboyer.com

#if UNITY_EDITOR
using UnityEngine;
using Unity.Mathematics;

namespace TextureTools.Noise
{
    [CreateAssetMenu(menuName = "ifelse/TextureTools/Noise Texture")]
    internal class NoiseTexture : ScriptableObject
    {
        public DynamicRange dynamicRange = DynamicRange.LDR;
        public Dimensionality dimensionality = Dimensionality._2D;
        public int2 size2D = 512;
        public int3 size3D = 512;
        public Channels channels = Channels.RGBA;
        public float4 offset = 0;
        public float4 scale = 5;
        public RandomType noise = RandomType.Perlin;
    }
}
#endif
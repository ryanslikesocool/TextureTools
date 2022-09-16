// Developed with love by Ryan Boyer http://ryanjboyer.com <3

#if UNITY_EDITOR
using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using MathNoise = Unity.Mathematics.noise;
using Random = Unity.Mathematics.Random;

namespace TextureTools.Noise {
    [CustomEditor(typeof(NoiseTexture))]
    internal class NoiseCreator : Editor {
        public NoiseTexture textureAsset = null;

        public DynamicRange DynamicRange => textureAsset.dynamicRange;
        public Dimensionality Dimensionality => textureAsset.dimensionality;
        public int2 Size2D => textureAsset.size2D;
        public int3 Size3D => textureAsset.size3D;
        public Channels Channels => textureAsset.channels;
        public float4 Offset => textureAsset.offset;
        public float4 Scale => textureAsset.scale;
        public RandomType Noise => textureAsset.noise;

        private void OnEnable() {
            textureAsset = target as NoiseTexture;
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            DrawDefaultInspector();

            if (GUILayout.Button("Create", GUILayout.Height(48))) {
                Create();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void Create() {
            Random random = new Random((uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue));

            TextureFormat textureFormat = Channels switch {
                Channels.R => DynamicRange == DynamicRange.LDR ? TextureFormat.R8 : TextureFormat.R16,
                Channels.RG => DynamicRange == DynamicRange.LDR ? TextureFormat.RG16 : TextureFormat.RG32,
                Channels.RGB => DynamicRange == DynamicRange.LDR ? TextureFormat.RGB24 : TextureFormat.RGB48,
                Channels.RGBA => DynamicRange == DynamicRange.LDR ? TextureFormat.RGBA32 : TextureFormat.RGBA64,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (Dimensionality == Dimensionality._2D) {
                Create2D(textureFormat, random);
            } else {
                Create3D(textureFormat, random);
            }
        }

        private void Create2D(TextureFormat format, Random random) {
            if (!Extensions.GetTexturePath(DynamicRange, out string path)) { return; }

            Func<float2, float> noiseFunc2d = Noise switch {
                RandomType.Perlin => (float2 p) => (MathNoise.cnoise(p) + 1) * 0.5f,
                RandomType.Simplex => (float2 p) => (MathNoise.snoise(p) + 1) * 0.5f,
                _ => (float2 p) => random.NextFloat(1)
            };

            Func<float4, float> noiseFunc4d = Noise switch {
                RandomType.Perlin => (float4 p) => (MathNoise.cnoise(p) + 1) * 0.5f,
                RandomType.Simplex => (float4 p) => (MathNoise.snoise(p) + 1) * 0.5f,
                _ => (float4 p) => random.NextFloat(1)
            };

            Color[] pixels = new Color[Size2D.x * Size2D.y];
            for (int x = 0; x < Size2D.x; x++) {
                for (int y = 0; y < Size2D.y; y++) {
                    int index = x + y * Size2D.x;

                    if (!textureAsset.wrapping) {
                        float4 scaler = Scale / math.max(Size2D.x, Size2D.y);

                        pixels[index] = new Color(
                           noiseFunc2d((new float2(x, y) + Offset.x) * scaler.x),
                           math.select(0, noiseFunc2d((new float2(y, x) + Offset.y) * scaler.y), (int)Channels >= (int)Channels.RG),
                           math.select(0, noiseFunc2d((new float2(x, y) + Offset.z) * scaler.z), (int)Channels >= (int)Channels.RGB),
                           math.select(1, noiseFunc2d((new float2(y, x) + Offset.w) * scaler.w), (int)Channels >= (int)Channels.RGBA)
                       );
                    } else {
                        pixels[index] = new Color(
                            WrappingNoise(new float2(x, y) + Offset.x, Size2D, Scale.x, noiseFunc4d),
                            math.select(0, WrappingNoise(new float2(y, x) + Offset.y, Size2D, Scale.y, noiseFunc4d), (int)Channels >= (int)Channels.RG),
                            math.select(0, WrappingNoise(new float2(x, y) + Offset.z, Size2D, Scale.z, noiseFunc4d), (int)Channels >= (int)Channels.RGB),
                            math.select(1, WrappingNoise(new float2(y, x) + Offset.w, Size2D, Scale.w, noiseFunc4d), (int)Channels >= (int)Channels.RGBA)
                        );
                    }
                }
            }

            Extensions.SaveTexture(pixels, Size2D, DynamicRange, path);
        }

        private void Create3D(TextureFormat format, Random random) {
            if (!Extensions.GetTexturePath("asset", out string path)) { return; }

            Func<float3, float> noiseFunc = Noise switch {
                RandomType.Perlin => (float3 p) => (MathNoise.cnoise(p) + 1) * 0.5f,
                RandomType.Simplex => (float3 p) => (MathNoise.snoise(p) + 1) * 0.5f,
                _ => (float3 p) => random.NextFloat(1)
            };

            Color[] pixels = new Color[Size3D.x * Size3D.y * Size3D.y];
            for (int x = 0; x < Size3D.x; x++) {
                for (int y = 0; y < Size3D.y; y++) {
                    for (int z = 0; z < Size3D.z; z++) {
                        int index = x + y * Size3D.x + z * Size3D.x * Size3D.y;
                        float4 scaler = Scale / (math.max(Size3D.x, math.max(Size3D.y, Size3D.z)));
                        pixels[index] = new Color(
                            noiseFunc((new float3(x, y, z) + Offset.x) * scaler.x),
                            math.select(0, (noiseFunc(new float3(z, y, x) + Offset.y) * scaler.y), (int)Channels >= (int)Channels.RG),
                            math.select(0, (noiseFunc(new float3(x, y, z) + Offset.z) * scaler.z), (int)Channels >= (int)Channels.RGB),
                            math.select(1, (noiseFunc(new float3(z, y, x) + Offset.w) * scaler.w), (int)Channels >= (int)Channels.RGBA)
                        );
                    }
                }
            }

            Texture3D texture = new Texture3D(Size3D.x, Size3D.y, Size3D.z, format, 0);
            texture.SetPixels(pixels);
            texture.Apply();

            AssetDatabase.CreateAsset(texture, path);
            AssetDatabase.ImportAsset(path);
        }

        // https://www.gamedev.net/blog/33/entry-2138456-seamless-noise/
        private float WrappingNoise(float2 position, float2 size, float2 scale, Func<float4, float> function) {
            float s = position.x / size.x;
            float t = position.y / size.y;

            float divX = scale.x / (2f * math.PI);
            float divY = scale.y / (2f * math.PI);

            float nx = (-scale.x * 0.5f) + math.cos(s * 2f * math.PI) * divX;
            float ny = (-scale.y * 0.5f) + math.cos(t * 2f * math.PI) * divY;
            float nz = (-scale.x * 0.5f) + math.sin(s * 2f * math.PI) * divX;
            float nw = (-scale.y * 0.5f) + math.sin(t * 2f * math.PI) * divY;

            return function(new float4(nx, ny, nz, nw));
        }
    }
}
#endif
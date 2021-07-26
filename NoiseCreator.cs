// Made with <3 by Ryan Boyer http://ryanjboyer.com

#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Noise = Unity.Mathematics.noise;

namespace TextureTools
{
    public class NoiseCreator : EditorWindow
    {
        private DynamicRange dynamicRange = DynamicRange.LDR;
        private ColorSpace colorSpace = ColorSpace.Gamma;

        private Dimensionality dimensionality = Dimensionality._2D;
        private int2 size2D = 512;
        private int3 size3D = 512;

        private Channels channels = Channels.RGBA;
        private float4 offset = 0;
        private float4 scale = 5;

        private RandomType noise = RandomType.Perlin;

        [MenuItem("Tools/ifelse/TextureTools/Noise Creator")]
        private static void Init()
        {
            NoiseCreator window = (NoiseCreator)EditorWindow.GetWindow(typeof(NoiseCreator));
            window.Show();
            window.titleContent = new GUIContent("Noise Creator");
        }

        private void OnGUI()
        {
            dimensionality = (Dimensionality)EditorGUILayout.EnumPopup("Dimensionality", dimensionality);
            channels = (Channels)EditorGUILayout.EnumPopup("Channels", channels);
            dynamicRange = (DynamicRange)EditorGUILayout.EnumPopup("Dynamic Range", dynamicRange);
            colorSpace = (ColorSpace)EditorGUILayout.EnumPopup("Color Space", colorSpace);
            if (dimensionality == Dimensionality._2D)
            {
                Vector2Int size = new Vector2Int(size2D.x, size2D.y);
                size = EditorGUILayout.Vector2IntField("Size", size);
                size2D = new int2(size.x, size.y);
            }
            else
            {
                Vector3Int size = new Vector3Int(size3D.x, size3D.y, size3D.z);
                size = EditorGUILayout.Vector3IntField("Size", size);
                size3D = new int3(size.x, size.y, size.z);
            }

            EditorGUILayout.Space();

            noise = (RandomType)EditorGUILayout.EnumPopup("Random", noise);
            scale = EditorGUILayout.Vector4Field("Scale", scale);
            offset = EditorGUILayout.Vector4Field("Offset", offset);

            if (GUILayout.Button("Create"))
            {
                Create();
            }
        }

        private void Create()
        {
            Random random = new Random((uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue));

            TextureFormat textureFormat;
            switch (channels)
            {
                case Channels.R:
                    textureFormat = dynamicRange == DynamicRange.LDR ? TextureFormat.R8 : TextureFormat.R16;
                    break;
                case Channels.RG:
                    textureFormat = dynamicRange == DynamicRange.LDR ? TextureFormat.RG16 : TextureFormat.RG32;
                    break;
                case Channels.RGB:
                    textureFormat = dynamicRange == DynamicRange.LDR ? TextureFormat.RGB24 : TextureFormat.RGB48;
                    break;
                default:
                    textureFormat = dynamicRange == DynamicRange.LDR ? TextureFormat.RGBA32 : TextureFormat.RGBA64;
                    break;
            }

            if (dimensionality == Dimensionality._2D)
            {
                Create2D(textureFormat, random);
            }
            else
            {
                Create3D(textureFormat, random);
            }
        }

        private void Create2D(TextureFormat format, Random random)
        {
            if (!Extensions.GetTexturePath(dynamicRange, out string path)) { return; }

            Func<float2, float> noiseFunc;
            switch (noise)
            {
                case RandomType.Perlin:
                    noiseFunc = (float2 p) => (Noise.cnoise(p) + 1) * 0.5f;
                    break;
                case RandomType.Simplex:
                    noiseFunc = (float2 p) => (Noise.snoise(p) + 1) * 0.5f;
                    break;
                default:
                    noiseFunc = (float2 p) => random.NextFloat(1);
                    break;
            }

            Color[] pixels = new Color[size2D.x * size2D.y];
            for (int x = 0; x < size2D.x; x++)
            {
                for (int y = 0; y < size2D.y; y++)
                {
                    int index = x + y * size2D.x;
                    float4 scaler = scale / size2D.xyxy;
                    pixels[index] = new Color(
                        noiseFunc((new float2(x, y) + offset.x) * scaler.x),
                        math.select(0, noiseFunc((new float2(y, x) + offset.y) * scaler.y), (int)channels >= (int)Channels.RG),
                        math.select(0, noiseFunc((new float2(x, y) + offset.z) * -scaler.z), (int)channels >= (int)Channels.RGB),
                        math.select(1, noiseFunc((new float2(y, x) + offset.w) * -scaler.w), (int)channels >= (int)Channels.RGBA)
                    );
                }
            }

            Extensions.SaveTexture(pixels, size2D, dynamicRange, path);
        }

        private void Create3D(TextureFormat format, Random random)
        {
            if (!Extensions.GetTexturePath("asset", out string path)) { return; }

            Func<float3, float> noiseFunc;
            switch (noise)
            {
                case RandomType.Perlin:
                    noiseFunc = (float3 p) => (Noise.cnoise(p) + 1) * 0.5f;
                    break;
                case RandomType.Simplex:
                    noiseFunc = (float3 p) => (Noise.snoise(p) + 1) * 0.5f;
                    break;
                default:
                    noiseFunc = (float3 p) => random.NextFloat(1);
                    break;
            }

            Color[] pixels = new Color[size3D.x * size3D.y * size3D.y];
            for (int x = 0; x < size3D.x; x++)
            {
                for (int y = 0; y < size3D.y; y++)
                {
                    for (int z = 0; z < size3D.z; z++)
                    {
                        int index = x + y * size3D.x + z * size3D.x * size3D.y;
                        pixels[index] = new Color(
                            noiseFunc(new float3(x, y, z) + offset.x),
                            math.select(0, noiseFunc(new float3(z, y, x) + offset.y), (int)channels >= (int)Channels.RG),
                            math.select(0, noiseFunc(new float3(x, y, z) + offset.z), (int)channels >= (int)Channels.RGB),
                            math.select(1, noiseFunc(new float3(z, y, x) + offset.w), (int)channels >= (int)Channels.RGBA)
                        );
                    }
                }
            }

            Texture3D texture = new Texture3D(size3D.x, size3D.y, size3D.z, format, 0);
            texture.SetPixels(pixels);
            texture.Apply();

            AssetDatabase.CreateAsset(texture, path);
            AssetDatabase.ImportAsset(path);
        }

        private enum RandomType
        {
            Random,
            Perlin,
            Simplex
        }
    }
}
#endif
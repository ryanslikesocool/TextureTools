// Made with <3 by Ryan Boyer http://ryanjboyer.com

#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using MathNoise = Unity.Mathematics.noise;

namespace TextureTools.Noise
{
    internal class NoiseCreator : EditorWindow
    {
        public NoiseTexture textureAsset = null;

        public DynamicRange dynamicRange = DynamicRange.LDR;
        public ColorSpace colorSpace = ColorSpace.Gamma;
        public Dimensionality dimensionality = Dimensionality._2D;
        public int2 size2D = 512;
        public int3 size3D = 512;
        public Channels channels = Channels.RGBA;
        public float4 offset = 0;
        public float4 scale = 5;
        public RandomType noise = RandomType.Perlin;

        private SerializedObject serializedObject = null;

        [MenuItem("Tools/ifelse/TextureTools/Noise Creator")]
        private static void Init()
        {
            NoiseCreator window = (NoiseCreator)EditorWindow.GetWindow(typeof(NoiseCreator));
            window.Show();
            window.titleContent = new GUIContent("Noise Creator");
        }

        private void OnGUI()
        {
            if (textureAsset == null || serializedObject.targetObject == null)
            {
                TemporaryNoiseEditor();
            }
            else
            {
                AssetNoiseEditor();
                serializedObject.Update();
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("dynamicRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colorSpace"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dimensionality"));
            if ((textureAsset?.dimensionality ?? this.dimensionality) == Dimensionality._2D)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("size2D"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("size3D"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("channels"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("noise"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));

            serializedObject.ApplyModifiedProperties();

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
                    noiseFunc = (float2 p) => (MathNoise.cnoise(p) + 1) * 0.5f;
                    break;
                case RandomType.Simplex:
                    noiseFunc = (float2 p) => (MathNoise.snoise(p) + 1) * 0.5f;
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
                    noiseFunc = (float3 p) => (MathNoise.cnoise(p) + 1) * 0.5f;
                    break;
                case RandomType.Simplex:
                    noiseFunc = (float3 p) => (MathNoise.snoise(p) + 1) * 0.5f;
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

        private void TemporaryNoiseEditor()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                serializedObject = new SerializedObject(this);
            }

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textureAsset"));
            if (GUILayout.Button("Create Asset"))
            {
                Save();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void AssetNoiseEditor()
        {
            if (serializedObject == null || serializedObject.targetObject == this)
            {
                serializedObject = new SerializedObject(textureAsset);
            }
        }

        private void Save()
        {
            if (!Extensions.GetTexturePath("asset", out string path)) { return; }

            textureAsset = (NoiseTexture)ScriptableObject.CreateInstance(typeof(NoiseTexture));

            textureAsset.dynamicRange = dynamicRange;
            textureAsset.channels = channels;
            textureAsset.dimensionality = dimensionality;
            textureAsset.size2D = size2D;
            textureAsset.size3D = size3D;
            textureAsset.offset = offset;
            textureAsset.scale = scale;
            textureAsset.noise = noise;

            AssetDatabase.CreateAsset(textureAsset, path);
            AssetDatabase.ImportAsset(path);
        }
    }
}
#endif
// Made with love by Ryan Boyer http://ryanjboyer.com <3

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
        public Dimensionality dimensionality = Dimensionality._2D;
        public int2 size2D = 512;
        public int3 size3D = 512;
        public Channels channels = Channels.RGBA;
        public float4 offset = 0;
        public float4 scale = 5;
        public RandomType noise = RandomType.Perlin;

        public DynamicRange DynamicRange => textureAsset?.dynamicRange ?? dynamicRange;
        public Dimensionality Dimensionality => textureAsset?.dimensionality ?? dimensionality;
        public int2 Size2D => textureAsset?.size2D ?? size2D;
        public int3 Size3D => textureAsset?.size3D ?? size3D;
        public Channels Channels => textureAsset?.channels ?? channels;
        public float4 Offset => textureAsset?.offset ?? offset;
        public float4 Scale => textureAsset?.scale ?? scale;
        public RandomType Noise => textureAsset?.noise ?? noise;

        private SerializedObject editorObject = null;
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
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("dynamicRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dimensionality"));
            if (Dimensionality == Dimensionality._2D)
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
            switch (Channels)
            {
                case Channels.R:
                    textureFormat = DynamicRange == DynamicRange.LDR ? TextureFormat.R8 : TextureFormat.R16;
                    break;
                case Channels.RG:
                    textureFormat = DynamicRange == DynamicRange.LDR ? TextureFormat.RG16 : TextureFormat.RG32;
                    break;
                case Channels.RGB:
                    textureFormat = DynamicRange == DynamicRange.LDR ? TextureFormat.RGB24 : TextureFormat.RGB48;
                    break;
                default:
                    textureFormat = DynamicRange == DynamicRange.LDR ? TextureFormat.RGBA32 : TextureFormat.RGBA64;
                    break;
            }

            if (Dimensionality == Dimensionality._2D)
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
            if (!Extensions.GetTexturePath(DynamicRange, out string path)) { return; }

            Func<float2, float> noiseFunc;
            switch (Noise)
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

            Color[] pixels = new Color[Size2D.x * Size2D.y];
            for (int x = 0; x < Size2D.x; x++)
            {
                for (int y = 0; y < Size2D.y; y++)
                {
                    int index = x + y * Size2D.x;
                    float4 scaler = Scale / math.max(Size2D.x, Size2D.y);
                    pixels[index] = new Color(
                        noiseFunc((new float2(x, y) + offset.x) * scaler.x),
                        math.select(0, noiseFunc((new float2(y, x) + offset.y) * scaler.y), (int)Channels >= (int)Channels.RG),
                        math.select(0, noiseFunc((new float2(x, y) + offset.z) * -scaler.z), (int)Channels >= (int)Channels.RGB),
                        math.select(1, noiseFunc((new float2(y, x) + offset.w) * -scaler.w), (int)Channels >= (int)Channels.RGBA)
                    );
                }
            }

            Extensions.SaveTexture(pixels, Size2D, DynamicRange, path);
        }

        private void Create3D(TextureFormat format, Random random)
        {
            if (!Extensions.GetTexturePath("asset", out string path)) { return; }

            Func<float3, float> noiseFunc;
            switch (Noise)
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

            Color[] pixels = new Color[Size3D.x * Size3D.y * Size3D.y];
            for (int x = 0; x < Size3D.x; x++)
            {
                for (int y = 0; y < Size3D.y; y++)
                {
                    for (int z = 0; z < Size3D.z; z++)
                    {
                        int index = x + y * Size3D.x + z * Size3D.x * Size3D.y;
                        float4 scaler = Scale / (math.max(Size3D.x, math.max(Size3D.y, Size3D.z)));
                        pixels[index] = new Color(
                            noiseFunc((new float3(x, y, z) + offset.x) * scaler.x),
                            math.select(0, (noiseFunc(new float3(z, y, x) + offset.y) * scaler.y), (int)Channels >= (int)Channels.RG),
                            math.select(0, (noiseFunc(new float3(x, y, z) + offset.z) * scaler.z), (int)Channels >= (int)Channels.RGB),
                            math.select(1, (noiseFunc(new float3(z, y, x) + offset.w) * scaler.w), (int)Channels >= (int)Channels.RGBA)
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
            if (editorObject == null)
            {
                editorObject = new SerializedObject(this);
            }

            serializedObject.Update();
            editorObject.Update();

            EditorGUILayout.PropertyField(editorObject.FindProperty("textureAsset"));
            EditorGUILayout.Space();
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
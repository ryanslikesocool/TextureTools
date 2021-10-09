// Made with love by Ryan Boyer http://ryanjboyer.com <3

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

namespace TextureTools.Gradient
{
    internal class GradientCreator : EditorWindow
    {
        public GradientTexture textureAsset = null;

        public Direction direction = Direction.Horizontal;
        public DynamicRange dynamicRange = DynamicRange.LDR;
        public ColorDefinition colorDefinition = ColorDefinition.RGB;
        public int2 textureSize = new int2(1024, 4);
        public AnchorMode anchorMode = AnchorMode.Percent;
        public Anchor[] anchors = new Anchor[0];

        public Direction Direction => textureAsset?.direction ?? direction;
        public DynamicRange DynamicRange => textureAsset?.dynamicRange ?? dynamicRange;
        public ColorDefinition ColorDefinition => textureAsset?.colorDefinition ?? colorDefinition;
        public int2 TextureSize => textureAsset?.textureSize ?? textureSize;
        public AnchorMode AnchorMode => textureAsset?.anchorMode ?? anchorMode;
        public Anchor[] Anchors => textureAsset?.anchors ?? anchors;

        private SerializedObject editorObject = null;
        private SerializedObject serializedObject = null;

        [MenuItem("Tools/ifelse/TextureTools/Gradient Creator")]
        private static void Init()
        {
            GradientCreator window = (GradientCreator)EditorWindow.GetWindow(typeof(GradientCreator));
            window.Show();
            window.titleContent = new GUIContent("Gradient Creator");
        }

        private void OnGUI()
        {
            if (textureAsset == null || serializedObject.targetObject == null)
            {
                TemporaryGradientEditor();
            }
            else
            {
                AssetGradientEditor();
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("direction"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dynamicRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colorDefinition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textureSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("anchorMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("anchors"));

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Create Gradient"))
            {
                Create();
            }
        }

        private void TemporaryGradientEditor()
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

        private void AssetGradientEditor()
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

        private void Create()
        {
            if (!Extensions.GetTexturePath(DynamicRange, out string path)) { return; }

            Func<float4, float4, float, float4> lerpFunc;
            switch (ColorDefinition)
            {
                default:
                    lerpFunc = (float4 lhs, float4 rhs, float t) => math.lerp(lhs, rhs, t);
                    break;
                case ColorDefinition.HSV:
                    lerpFunc = (float4 lhs, float4 rhs, float t) => Extensions.LerpHSV(lhs, rhs, t);
                    break;
                case ColorDefinition.HCL:
                    lerpFunc = (float4 lhs, float4 rhs, float t) => Extensions.LerpHCL(lhs, rhs, t);
                    break;
            }

            Anchor[] anchors = Anchors.OrderBy(a => AnchorMode == AnchorMode.Percent ? a.time : a.pixel).Select(a =>
            {
                if (Direction == Direction.Vertical)
                {
                    if (AnchorMode == AnchorMode.Percent)
                    {
                        a.time = 1 - a.time;
                    }
                    else
                    {
                        a.pixel = TextureSize.y - a.pixel;
                    }
                }
                if (AnchorMode == AnchorMode.Percent)
                {
                    a.time *= math.select(TextureSize.y, TextureSize.x, Direction == Direction.Horizontal);
                    a.time = math.round(a.time);
                }
                else
                {
                    a.time = a.pixel;
                }
                return a;
            }).ToArray();

            Color[] pixels = new Color[TextureSize.x * TextureSize.y];

            if (Direction == Direction.Horizontal)
            {
                for (int x = 0; x < TextureSize.x; x++)
                {
                    Color color = Color.magenta;
                    if (x < anchors[0].time)
                    {
                        color = anchors[0].color;
                    }
                    else if (x > anchors[anchors.Length - 1].time)
                    {
                        color = anchors[anchors.Length - 1].color;
                    }

                    for (int a = 0; a < anchors.Length - 1; a++)
                    {
                        if (x.WithinRange((int)anchors[a].time, (int)anchors[a + 1].time))
                        {
                            color = (Vector4)Anchor.Lerp(anchors[a], anchors[a + 1], x, lerpFunc);
                            break;
                        }
                    }

                    for (int y = 0; y < TextureSize.y; y++)
                    {
                        int index = x + y * TextureSize.x;
                        pixels[index] = color;
                    }
                }
            }
            else
            {
                for (int y = 0; y < TextureSize.y; y++)
                {
                    Color color = Color.magenta;
                    if (y < anchors[anchors.Length - 1].time)
                    {
                        color = anchors[anchors.Length - 1].color;
                    }
                    else if (y > anchors[0].time)
                    {
                        color = anchors[0].color;
                    }

                    for (int a = anchors.Length - 1; a > 0; a--)
                    {
                        if (y.WithinRange((int)anchors[a].time, (int)anchors[a - 1].time))
                        {
                            color = (Vector4)Anchor.Lerp(anchors[a], anchors[a - 1], y, lerpFunc);
                            break;
                        }
                    }

                    for (int x = 0; x < TextureSize.x; x++)
                    {
                        int index = x + y * TextureSize.x;
                        pixels[index] = color;
                    }
                }
            }

            Extensions.SaveTexture(pixels, TextureSize, DynamicRange, path);
        }

        private void Save()
        {
            if (!Extensions.GetTexturePath("asset", out string path)) { return; }

            textureAsset = (GradientTexture)ScriptableObject.CreateInstance(typeof(GradientTexture));

            textureAsset.direction = direction;
            textureAsset.dynamicRange = dynamicRange;
            textureAsset.colorDefinition = colorDefinition;
            textureAsset.textureSize = textureSize;
            textureAsset.anchors = anchors;

            AssetDatabase.CreateAsset(textureAsset, path);
            AssetDatabase.ImportAsset(path);
        }
    }
}
#endif
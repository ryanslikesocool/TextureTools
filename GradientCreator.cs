// Made with <3 by Ryan Boyer http://ryanjboyer.com

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

namespace TextureTools
{
    public class GradientCreator : EditorWindow
    {
        private Direction direction = Direction.Horizontal;
        private ColorSpace colorSpace = ColorSpace.Gamma;
        private DynamicRange dynamicRange = DynamicRange.LDR;
        private ColorDefinition colorDefinition = ColorDefinition.RGB;
        private int2 textureSize = new int2(1024, 4);
        public Anchor[] anchors = new Anchor[0];

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
            if (serializedObject == null)
            {
                serializedObject = new SerializedObject(this);
            }

            serializedObject.Update();

            direction = (Direction)EditorGUILayout.EnumPopup("Direction", direction);
            colorSpace = (ColorSpace)EditorGUILayout.EnumPopup("Color Space", colorSpace);
            dynamicRange = (DynamicRange)EditorGUILayout.EnumPopup("Dynamic Range", dynamicRange);
            colorDefinition = (ColorDefinition)EditorGUILayout.EnumPopup("Color Definition", colorDefinition);

            Vector2Int size = new Vector2Int(textureSize.x, textureSize.y);
            size = EditorGUILayout.Vector2IntField("Texture Size", size);
            textureSize = new int2(size.x, size.y);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("anchors"));

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Create Gradient"))
            {
                Create();
            }
        }

        private void Create()
        {
            if (!Extensions.GetTexturePath(dynamicRange, out string path)) { return; }

            Func<float4, float4, float, float4> lerpFunc;
            switch (colorDefinition)
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

            Anchor[] anchors = this.anchors.OrderBy(a => a.time).Select(a =>
            {
                if (direction == Direction.Vertical)
                {
                    a.time = 1 - a.time;
                }
                a.time *= math.select(textureSize.y, textureSize.x, direction == Direction.Horizontal);
                a.time = math.round(a.time);
                return a;
            }).ToArray();

            Color[] pixels = new Color[textureSize.x * textureSize.y];

            if (direction == Direction.Horizontal)
            {
                for (int x = 0; x < textureSize.x; x++)
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
                        if (WithinRange(x, (int)anchors[a].time, (int)anchors[a + 1].time))
                        {
                            color = (Vector4)LerpAnchors(anchors[a], anchors[a + 1], x, lerpFunc);
                            break;
                        }
                    }

                    for (int y = 0; y < textureSize.y; y++)
                    {
                        int index = x + y * textureSize.x;
                        pixels[index] = color;
                    }
                }
            }
            else
            {
                for (int y = 0; y < textureSize.y; y++)
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
                        if (WithinRange(y, (int)anchors[a].time, (int)anchors[a - 1].time))
                        {
                            color = (Vector4)LerpAnchors(anchors[a], anchors[a - 1], y, lerpFunc);
                            break;
                        }
                    }

                    for (int x = 0; x < textureSize.x; x++)
                    {
                        int index = x + y * textureSize.x;
                        pixels[index] = color;
                    }
                }
            }

            Extensions.SaveTexture(pixels, textureSize, dynamicRange, path);
        }

        private static bool WithinRange(int v, int x, int y) => v >= x && v <= y;

        private float4 LerpAnchors(Anchor lhs, Anchor rhs, int pixel, Func<float4, float4, float, float4> lerp)
        {
            float t = math.remap(lhs.time, rhs.time, 0, 1, pixel);
            return lerp((Vector4)lhs.color, (Vector4)rhs.color, t);
        }

        [Serializable]
        public struct Anchor
        {
            [Range(0, 1)] public float time;
            public Color color;
        }
    }
}
#endif
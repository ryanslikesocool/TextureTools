// Developed with love by Ryan Boyer http://ryanjboyer.com <3

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

namespace TextureTools.Gradient {
    [CustomEditor(typeof(GradientTexture))]
    internal class GradientCreator : Editor {
        public GradientTexture textureAsset = null;

        public Direction Direction => textureAsset.direction;
        public DynamicRange DynamicRange => textureAsset.dynamicRange;
        public ColorDefinition ColorDefinition => textureAsset.colorDefinition;
        public int2 TextureSize => textureAsset.textureSize;
        public AnchorMode AnchorMode => textureAsset.anchorMode;
        public Anchor[] Anchors => textureAsset.anchors;

        private void OnEnable() {
            textureAsset = target as GradientTexture;
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            DrawDefaultInspector();

            if (GUILayout.Button("Create Gradient", GUILayout.Height(48))) {
                Create();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void Create() {
            if (!Extensions.GetTexturePath(DynamicRange, out string path)) { return; }

            Func<float4, float4, float, float4> lerpFunc;
            switch (ColorDefinition) {
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

            Anchor[] anchors = Anchors.OrderBy(a => AnchorMode == AnchorMode.Percent ? a.time : a.pixel).Select(a => {
                if (Direction == Direction.Vertical) {
                    if (AnchorMode == AnchorMode.Percent) {
                        a.time = 1 - a.time;
                    } else {
                        a.pixel = TextureSize.y - a.pixel;
                    }
                }
                if (AnchorMode == AnchorMode.Percent) {
                    a.time *= math.select(TextureSize.y, TextureSize.x, Direction == Direction.Horizontal);
                    a.time = math.round(a.time);
                } else {
                    a.time = a.pixel;
                }
                return a;
            }).ToArray();

            Color[] pixels = new Color[TextureSize.x * TextureSize.y];

            if (Direction == Direction.Horizontal) {
                for (int x = 0; x < TextureSize.x; x++) {
                    Color color = Color.magenta;
                    if (x < anchors[0].time) {
                        color = anchors[0].color;
                    } else if (x > anchors[anchors.Length - 1].time) {
                        color = anchors[anchors.Length - 1].color;
                    }

                    for (int a = 0; a < anchors.Length - 1; a++) {
                        if (x.WithinRange((int)anchors[a].time, (int)anchors[a + 1].time)) {
                            color = (Vector4)Anchor.Lerp(anchors[a], anchors[a + 1], x, lerpFunc);
                            break;
                        }
                    }

                    for (int y = 0; y < TextureSize.y; y++) {
                        int index = x + y * TextureSize.x;
                        pixels[index] = color;
                    }
                }
            } else {
                for (int y = 0; y < TextureSize.y; y++) {
                    Color color = Color.magenta;
                    if (y < anchors[anchors.Length - 1].time) {
                        color = anchors[anchors.Length - 1].color;
                    } else if (y > anchors[0].time) {
                        color = anchors[0].color;
                    }

                    for (int a = anchors.Length - 1; a > 0; a--) {
                        if (y.WithinRange((int)anchors[a].time, (int)anchors[a - 1].time)) {
                            color = (Vector4)Anchor.Lerp(anchors[a], anchors[a - 1], y, lerpFunc);
                            break;
                        }
                    }

                    for (int x = 0; x < TextureSize.x; x++) {
                        int index = x + y * TextureSize.x;
                        pixels[index] = color;
                    }
                }
            }

            Extensions.SaveTexture(pixels, TextureSize, DynamicRange, path);
        }
    }
}
#endif
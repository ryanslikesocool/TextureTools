// Developed with love by Ryan Boyer http://ryanjboyer.com <3

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEditor;

namespace TextureTools {
    internal static class Extensions {
        internal static bool WithinRange(this int v, int x, int y) => v >= x && v <= y;

        #region File
        internal static bool GetTexturePath(DynamicRange dynamicRange, out string path) {
            string extension = dynamicRange == DynamicRange.HDR ? "exr" : "png";
            return GetTexturePath(extension, out path);
        }

        internal static bool GetTexturePath(string extension, out string path) {
            path = EditorUtility.SaveFilePanelInProject("Save Texture", "New Texture", extension, "Save the texture");
            return path.Length > 0;
        }

        internal static void SaveTexture(Color[] pixels, int2 size, DynamicRange dynamicRange, string path) {
            Texture2D texture = new Texture2D(size.x, size.y, DefaultFormat.HDR, TextureCreationFlags.None);
            texture.SetPixels(pixels);
            texture.Apply();

            byte[] encodedTex;
            if (dynamicRange == DynamicRange.HDR) {
                encodedTex = texture.EncodeToEXR();
            } else {
                encodedTex = texture.EncodeToPNG();
            }

            using (FileStream stream = File.Open(path, FileMode.OpenOrCreate)) {
                stream.Write(encodedTex, 0, encodedTex.Length);
            }
        }
        #endregion

        #region Color
        internal static float3 LerpHSV(float3 lhs, float3 rhs, float t) {
            float h = 0;
            float d = rhs.x - lhs.x;
            if (lhs.x > rhs.x) {
                float h3 = rhs.x;
                rhs.x = lhs.x;
                lhs.x = h3;
                d = -d;
                t = 1 - t;
            }
            if (d > 0.5f) {
                lhs.x = lhs.x + 1;
                h = (lhs.x + t * (rhs.x - lhs.x)) % 1;
            }
            if (d <= 0.5f) {
                h = lhs.x + t * d;
            }
            return new float3
            (
                h,
                lhs.y + t * (rhs.y - lhs.y),
                lhs.z + t * (rhs.z - lhs.z)
            );
        }

        internal static float3 RGBtoHSV(float3 rgb) {
            float r = rgb.x;
            float g = rgb.y;
            float b = rgb.z;

            float min = math.min(r, math.min(g, b));
            float max = math.max(r, math.max(g, b));
            float deltaMax = max - min;

            float h = 0, s = 0;
            float v = max;

            if (deltaMax != 0) {
                s = deltaMax / max;

                float over3 = 0.333f;
                float over6 = over3 * 0.5f;

                float deltaR = (((max - r) * over6) + (deltaMax * 0.5f)) / deltaMax;
                float deltaG = (((max - g) * over6) + (deltaMax * 0.5f)) / deltaMax;
                float deltaB = (((max - b) * over6) + (deltaMax * 0.5f)) / deltaMax;

                if (r == max) {
                    h = deltaB - deltaG;
                } else if (g == max) {
                    h = over3 + deltaR - deltaB;
                } else if (b == max) {
                    h = over3 * 2 + deltaG - deltaR;
                }

                if (h < 0) {
                    h += 1;
                }
                if (h > 1) {
                    h -= 1;
                }
            }

            return new float3(h, s, v);
        }

        internal static float3 HSVtoRGB(float3 hsv) {
            float h = hsv.x;
            float s = hsv.y;
            float v = hsv.z;
            float r = v, g = v, b = v;

            if (s != 0) {
                float var_h = h * 6;
                if (var_h == 6) {
                    var_h = 0;
                }
                float i = math.floor(var_h);
                float v1 = v * (1 - s);
                float v2 = v * (1 - s * (var_h - i));
                float v3 = v * (1 - s * (1 - (var_h - i)));

                switch (i) {
                    case 0:
                        r = v;
                        g = v3;
                        b = v1;
                        break;
                    case 1:
                        r = v2;
                        g = v;
                        b = v1;
                        break;
                    case 2:
                        r = v1;
                        g = v;
                        b = v3;
                        break;
                    case 3:
                        r = v1;
                        g = v2;
                        b = v;
                        break;
                    case 4:
                        r = v3;
                        g = v1;
                        b = v;
                        break;
                    default:
                        r = v;
                        g = v1;
                        b = v2;
                        break;
                }
            }

            return new float3(r, g, b);
        }

        internal static float3 RGBtoHCL(float3 rgb) {
            float r = rgb.x;
            float g = rgb.y;
            float b = rgb.z;

            r = math.select(r / 12.92f, math.pow((r + 0.055f) / 1.055f, 2.4f), r > 0.04045f) * 100;
            g = math.select(g / 12.92f, math.pow((g + 0.055f) / 1.055f, 2.4f), g > 0.04045f) * 100;
            b = math.select(b / 12.92f, math.pow((b + 0.055f) / 1.055f, 2.4f), b > 0.04045f) * 100;

            float h = r * 0.4124f + g * 0.3576f + b * 0.1805f;
            float c = r * 0.2126f + g * 0.7152f + b * 0.0722f;
            float l = r * 0.0193f + g * 0.1192f + b * 0.9505f;

            return new float3(h, c, l);
        }

        internal static float3 HCLtoRGB(float3 hcl) {
            float x = hcl.x / 100f;
            float y = hcl.y / 100f;
            float z = hcl.z / 100f;

            float r = x * 3.2406f + y * -1.5372f + z * -0.4986f;
            float g = x * -0.9689f + y * 1.8758f + z * 0.0415f;
            float b = x * 0.0557f + y * -0.2040f + z * 1.0570f;

            float exp = 1f / 2.4f;

            r = math.select(12.92f * r, 1.055f * math.pow(r, exp) - 0.055f, r > 0.0031308f);
            g = math.select(12.92f * g, 1.055f * math.pow(g, exp) - 0.055f, g > 0.0031308f);
            b = math.select(12.92f * b, 1.055f * math.pow(b, exp) - 0.055f, b > 0.0031308f);

            return new float3(r, g, b);
        }

        internal static float4 LerpHSV(float4 rgbA, float4 rgbB, float t) => new float4(HSVtoRGB(LerpHSV(RGBtoHSV(rgbA.xyz), RGBtoHSV(rgbB.xyz), t)), math.lerp(rgbA.w, rgbB.w, t));

        internal static float4 LerpHCL(float4 rgbA, float4 rgbB, float t) => new float4(HCLtoRGB(math.lerp(RGBtoHCL(rgbA.xyz), RGBtoHCL(rgbB.xyz), t)), math.lerp(rgbA.w, rgbB.w, t));
        #endregion
    }
}
#endif
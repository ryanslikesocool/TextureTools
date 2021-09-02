// Made with <3 by Ryan Boyer http://ryanjboyer.com

#if UNITY_EDITOR
using UnityEngine;

namespace TextureTools
{
    internal enum Size2D
    {
        [InspectorName("8")] _8 = 8,
        [InspectorName("16")] _16 = 16,
        [InspectorName("32")] _32 = 32,
        [InspectorName("64")] _64 = 64,
        [InspectorName("128")] _128 = 128,
        [InspectorName("256")] _256 = 256,
        [InspectorName("512")] _512 = 512,
        [InspectorName("1024")] _1024 = 1024,
        [InspectorName("2048")] _2048 = 2048
    }

    internal enum Size3D
    {
        [InspectorName("8")] _8 = 8,
        [InspectorName("16")] _16 = 16,
        [InspectorName("32")] _32 = 32,
        [InspectorName("64")] _64 = 64,
        [InspectorName("128")] _128 = 128
    }

    internal enum Dimensionality
    {
        [InspectorName("2D")] _2D = 2,
        [InspectorName("3D")] _3D = 3
    }

    internal enum Channels
    {
        R,
        RG,
        RGB,
        RGBA
    }

    internal enum DynamicRange
    {
        LDR,
        HDR
    }

    internal enum Direction
    {
        Horizontal,
        Vertical
    }

    internal enum ColorDefinition
    {
        RGB,
        HSV,
        HCL
    }

    internal enum AnchorMode
    {
        Percent,
        Pixel
    }
}
#endif
// Made with <3 by Ryan Boyer http://ryanjboyer.com

#if UNITY_EDITOR
using UnityEngine;
using Unity.Mathematics;

namespace TextureTools.Gradient
{
    [CreateAssetMenu(menuName = "ifelse/TextureTools/Gradient Texture")]
    internal class GradientTexture : ScriptableObject
    {
        public Direction direction = Direction.Horizontal;
        public ColorSpace colorSpace = ColorSpace.Gamma;
        public DynamicRange dynamicRange = DynamicRange.LDR;
        public ColorDefinition colorDefinition = ColorDefinition.RGB;
        public int2 textureSize = new int2(1024, 4);
        public Anchor[] anchors = new Anchor[0];
    }
}
#endif
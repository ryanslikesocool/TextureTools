// Developed with love by Ryan Boyer http://ryanjboyer.com <3

#if UNITY_EDITOR
using UnityEngine;
using Unity.Mathematics;

namespace TextureTools.Gradient {
    [CreateAssetMenu(menuName = "Developed With Love/TextureTools/Gradient Texture")]
    internal class GradientTexture : ScriptableObject {
        public Direction direction = Direction.Horizontal;
        public DynamicRange dynamicRange = DynamicRange.LDR;
        public ColorDefinition colorDefinition = ColorDefinition.RGB;
        public int2 textureSize = new int2(1024, 4);
        public Anchor[] anchors = new Anchor[0];
        public AnchorMode anchorMode = AnchorMode.Percent;
    }
}
#endif
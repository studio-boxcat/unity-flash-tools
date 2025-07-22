using UnityEngine;
using UnityEngine.Assertions;

namespace FT
{
    public static class MaterialConfigurator
    {
        private static int _tintShaderPropBacking;
        private static int _tintShaderProp
        {
            get
            {
                if (_tintShaderPropBacking is 0)
                    _tintShaderPropBacking = Shader.PropertyToID("_Tint");
                Assert.AreNotEqual(0, _tintShaderPropBacking);
                return _tintShaderPropBacking;
            }
        }

        public static void SetTint(MaterialPropertyBlock mpb, Color color)
            => mpb.SetColor(_tintShaderProp, color);

        public static void SetTexture(MaterialPropertyBlock mpb, Texture2D texture)
            => mpb.SetMainTex(texture);
    }
}
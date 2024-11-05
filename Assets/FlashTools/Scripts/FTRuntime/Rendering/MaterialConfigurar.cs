using UnityEngine;
using UnityEngine.Assertions;

namespace FTRuntime
{
    public static class MaterialConfigurator
    {
        static int _tintShaderPropBacking;
        static int _tintShaderProp
        {
            get
            {
                if (_tintShaderPropBacking is 0)
                    _tintShaderPropBacking = Shader.PropertyToID("_Tint");
                Assert.AreNotEqual(0, _tintShaderPropBacking);
                return _tintShaderPropBacking;
            }
        }

        static int _mainTexShaderPropBacking;
        static int _mainTexShaderProp
        {
            get
            {
                if (_mainTexShaderPropBacking is 0)
                    _mainTexShaderPropBacking = Shader.PropertyToID("_MainTex");
                Assert.AreNotEqual(0, _mainTexShaderPropBacking);
                return _mainTexShaderPropBacking;
            }
        }

        public static void SetTint(MaterialPropertyBlock mpb, Color color)
            => mpb.SetColor(_tintShaderProp, color);

        public static void SetTexture(MaterialPropertyBlock mpb, Texture2D texture)
            => mpb.SetTexture(_mainTexShaderProp, texture);
    }
}
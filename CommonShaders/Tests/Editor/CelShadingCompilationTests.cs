using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace FakeMG.CommonShaders.Tests
{
    public sealed class CelShadingCompilationTests
    {
        private const string SHADER_GUID = "995bc292d3004c54684cff05a1663cc0";
        private Shader _shader;
        private Material _material;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(SHADER_GUID));
            Assert.That(_shader, Is.Not.Null);
            _material = new Material(_shader);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_material);
        }

        [TestCase("")]
        [TestCase("_MAIN_LIGHT_SHADOWS")]
        [TestCase("_MAIN_LIGHT_SHADOWS_CASCADE")]
        [TestCase("_MAIN_LIGHT_SHADOWS_SCREEN")]
        public void MainLightShadowModes_CompileAllPasses(string shadowKeyword)
        {
            if (shadowKeyword.Length > 0)
            {
                _material.EnableKeyword(shadowKeyword);
            }

            for (int passIndex = 0; passIndex < _material.passCount; passIndex++)
            {
                ShaderUtil.CompilePass(_material, passIndex, true);
            }

            foreach (ShaderMessage message in ShaderUtil.GetShaderMessages(_shader))
            {
                Assert.That(message.severity, Is.Not.EqualTo(ShaderCompilerMessageSeverity.Error), message.message);
            }
        }

        #endregion
    }
}

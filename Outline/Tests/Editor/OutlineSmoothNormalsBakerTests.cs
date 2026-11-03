using System;
using System.Collections.Generic;
using FakeMG.Outline.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.Outline.Tests
{
    public sealed class OutlineSmoothNormalsBakerTests
    {
        private Mesh _sourceMesh;
        private Mesh _bakedMesh;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _sourceMesh = new Mesh
            {
                vertices = new[] { Vector3.zero, Vector3.zero, Vector3.one },
                normals = new[] { Vector3.right, Vector3.up, Vector3.forward }
            };
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_bakedMesh);
            UnityEngine.Object.DestroyImmediate(_sourceMesh);
        }

        [Test]
        public void SplitVertices_ReceiveTheSameOutlineNormalWithoutChangingShadingNormals()
        {
            _bakedMesh = OutlineSmoothNormalsBaker.CreateMeshWithSmoothOutlineNormals(_sourceMesh);
            var smoothNormals = new List<Vector3>();
            _bakedMesh.GetUVs(3, smoothNormals);

            Assert.That(smoothNormals[0], Is.EqualTo((Vector3.right + Vector3.up).normalized));
            Assert.That(smoothNormals[1], Is.EqualTo(smoothNormals[0]));
            Assert.That(_bakedMesh.normals, Is.EqualTo(_sourceMesh.normals));
            Assert.That(_sourceMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord3), Is.False);
        }

        [Test]
        public void ExistingUv4_IsRejectedRatherThanOverwritten()
        {
            _sourceMesh.SetUVs(3, new List<Vector3> { Vector3.one, Vector3.one, Vector3.one });

            Assert.Throws<InvalidOperationException>(CreateBakedMesh);
        }

        [Test]
        public void MissingNormals_AreRejected()
        {
            _sourceMesh.normals = Array.Empty<Vector3>();

            Assert.Throws<InvalidOperationException>(CreateBakedMesh);
        }

        [Test]
        public void OpposingNormals_KeepTheirOriginalDirectionInsteadOfProducingZeroNormals()
        {
            _sourceMesh.normals = new[] { Vector3.right, Vector3.left, Vector3.forward };

            _bakedMesh = OutlineSmoothNormalsBaker.CreateMeshWithSmoothOutlineNormals(_sourceMesh);
            var smoothNormals = new List<Vector3>();
            _bakedMesh.GetUVs(3, smoothNormals);

            Assert.That(smoothNormals[0], Is.EqualTo(Vector3.right));
            Assert.That(smoothNormals[1], Is.EqualTo(Vector3.left));
        }

        #endregion

        #region Private Methods

        private void CreateBakedMesh()
        {
            _bakedMesh = OutlineSmoothNormalsBaker.CreateMeshWithSmoothOutlineNormals(_sourceMesh);
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FakeMG.Outline.Editor
{
    public static class OutlineSmoothNormalsBaker
    {
        #region Public Methods

        public static Mesh CreateMeshWithSmoothOutlineNormals(Mesh sourceMesh)
        {
            if (!sourceMesh.isReadable)
            {
                throw new InvalidOperationException("Enable Read/Write on the source mesh before baking outline normals.");
            }

            if (sourceMesh.HasVertexAttribute(VertexAttribute.TexCoord3))
            {
                throw new InvalidOperationException("UV4 is already occupied. Outline normals must not overwrite another feature's data.");
            }

            Vector3[] positionsMeters = sourceMesh.vertices;
            Vector3[] normals = sourceMesh.normals;
            if (normals.Length != positionsMeters.Length)
            {
                throw new InvalidOperationException("The source mesh must contain a normal for every vertex.");
            }

            var normalsByPosition = new Dictionary<Vector3, HashSet<Vector3>>();
            for (int vertexIndex = 0; vertexIndex < positionsMeters.Length; vertexIndex++)
            {
                if (!normalsByPosition.TryGetValue(positionsMeters[vertexIndex], out HashSet<Vector3> uniqueNormals))
                {
                    uniqueNormals = new HashSet<Vector3>();
                    normalsByPosition.Add(positionsMeters[vertexIndex], uniqueNormals);
                }

                uniqueNormals.Add(normals[vertexIndex]);
            }

            var smoothNormalsByPosition = new Dictionary<Vector3, Vector3>();
            foreach (KeyValuePair<Vector3, HashSet<Vector3>> entry in normalsByPosition)
            {
                Vector3 smoothNormal = Vector3.zero;
                foreach (Vector3 normal in entry.Value)
                {
                    smoothNormal += normal;
                }

                smoothNormalsByPosition.Add(entry.Key, smoothNormal.normalized);
            }

            var smoothNormals = new List<Vector3>(positionsMeters.Length);
            for (int vertexIndex = 0; vertexIndex < positionsMeters.Length; vertexIndex++)
            {
                Vector3 smoothNormal = smoothNormalsByPosition[positionsMeters[vertexIndex]];
                smoothNormals.Add(smoothNormal.sqrMagnitude > 0f ? smoothNormal : normals[vertexIndex]);
            }

            Mesh bakedMesh = UnityEngine.Object.Instantiate(sourceMesh);
            bakedMesh.name = sourceMesh.name + " Outline";
            bakedMesh.SetUVs(3, smoothNormals);
            return bakedMesh;
        }

        [MenuItem("Assets/FakeMG/Outline/Create Mesh With Smooth Outline Normals", true)]
        public static bool CanCreateOutlineMesh()
        {
            return Selection.activeObject is Mesh;
        }

        [MenuItem("Assets/FakeMG/Outline/Create Mesh With Smooth Outline Normals")]
        public static void CreateOutlineMeshAsset()
        {
            Mesh sourceMesh = (Mesh)Selection.activeObject;
            string sourcePath = AssetDatabase.GetAssetPath(sourceMesh);
            string destinationPath = EditorUtility.SaveFilePanelInProject("Save outline mesh", sourceMesh.name + " Outline", "asset",
                "Save an outline-only mesh. Assign it to a separate child MeshFilter; keep the model's original mesh.",
                Path.GetDirectoryName(sourcePath));
            if (string.IsNullOrEmpty(destinationPath))
            {
                return;
            }

            try
            {
                Mesh bakedMesh = CreateMeshWithSmoothOutlineNormals(sourceMesh);
                AssetDatabase.CreateAsset(bakedMesh, destinationPath);
                Selection.activeObject = bakedMesh;
            }
            catch (Exception exception)
            {
                FakeMG.Framework.Echo.Error(exception.ToString());
            }
        }

        #endregion
    }
}

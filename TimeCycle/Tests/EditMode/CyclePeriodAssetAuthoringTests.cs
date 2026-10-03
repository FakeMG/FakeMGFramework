using System;
using FakeMG.TimeCycle.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FakeMG.TimeCycle.Tests.EditMode
{
    public sealed class CyclePeriodAssetAuthoringTests
    {
        private string _testRootPath;
        private string _assetFolderPath;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            string folderName = "_TimeCycleAuthoringTests_" + Guid.NewGuid().ToString("N");
            _testRootPath = "Assets/" + folderName;
            AssetDatabase.CreateFolder("Assets", folderName);
            AssetDatabase.CreateFolder(_testRootPath, "Editor");
            _assetFolderPath = _testRootPath + "/Editor";
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(_testRootPath);

        [Test]
        public void DisplayAndFileRenamesPreservePeriodAssetAndGuid()
        {
            CyclePeriodSO originalSO = CyclePeriodAssetAuthoring.CreateOrUpdatePeriodSO(_assetFolderPath, "dawn", "Dawn");
            string originalPath = AssetDatabase.GetAssetPath(originalSO);
            string originalGuid = AssetDatabase.AssetPathToGUID(originalPath);
            Assert.That(AssetDatabase.RenameAsset(originalPath, "Renamed Dawn"), Is.Empty);

            CyclePeriodSO renamedSO = CyclePeriodAssetAuthoring.CreateOrUpdatePeriodSO(_assetFolderPath, "dawn", "Morning");

            Assert.That(renamedSO, Is.SameAs(originalSO));
            Assert.That(renamedSO.PeriodId, Is.EqualTo(new CyclePeriodId("dawn")));
            Assert.That(renamedSO.ItemName, Is.EqualTo("Morning"));
            Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(renamedSO)), Is.EqualTo(originalGuid));
            Assert.That(AssetDatabase.FindAssets("t:CyclePeriodSO", new[] { _assetFolderPath }).Length, Is.EqualTo(1));
        }

        [Test]
        public void ExistingDuplicateIdentitiesAreRejected()
        {
            CyclePeriodAssetAuthoring.CreateOrUpdatePeriodSO(_assetFolderPath, "dawn", "Dawn");
            var duplicateSO = ScriptableObject.CreateInstance<CyclePeriodSO>();
            duplicateSO.ConfigureForEditor("dawn", "Duplicate Dawn");
            AssetDatabase.CreateAsset(duplicateSO, _assetFolderPath + "/Duplicate.asset");

            Assert.Throws<InvalidOperationException>(
                () => CyclePeriodAssetAuthoring.CreateOrUpdatePeriodSO(_assetFolderPath, "dawn", "Morning"));
        }

        #endregion
    }
}

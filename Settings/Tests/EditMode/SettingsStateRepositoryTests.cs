using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.Settings.Tests
{
    public sealed class SettingsStateRepositoryTests
    {
        private const string VOLUME_ID = "test.volume";
        private const string VSYNC_ID = "test.vsync";

        private readonly List<Object> _createdObjects = new();
        private SettingsStateRepository _repository;
        private SliderSettingSO _volumeSettingSO;
        private BoolSettingSO _vSyncSettingSO;
        private List<SettingDefinitionSO> _changedSettingSOs;
        private int _editCount;

        [SetUp]
        public void SetUp()
        {
            _volumeSettingSO = CreateSetting<SliderSettingSO>(VOLUME_ID, 0.8f);
            _vSyncSettingSO = CreateSetting<BoolSettingSO>(VSYNC_ID, true);
            _repository = new SettingsStateRepository();
            _changedSettingSOs = new List<SettingDefinitionSO>();
            _editCount = 0;
            _repository.OnSettingChanged += RecordChange;
            _repository.OnSettingsEdited += CountEdit;
        }

        [TearDown]
        public void TearDown()
        {
            _repository.OnSettingChanged -= RecordChange;
            _repository.OnSettingsEdited -= CountEdit;
            foreach (Object createdObject in _createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void GetValue_NeverSet_ReturnsDefault()
        {
            Assert.That(_repository.GetValue(_volumeSettingSO), Is.EqualTo(0.8f));
        }

        [Test]
        public void SetValue_NewValue_StoresItAndRaisesChangeAndEdit()
        {
            _repository.SetValue(_volumeSettingSO, 0.3f);

            Assert.That(_repository.GetValue(_volumeSettingSO), Is.EqualTo(0.3f));
            Assert.That(_changedSettingSOs, Is.EqualTo(new SettingDefinitionSO[] { _volumeSettingSO }));
            Assert.That(_editCount, Is.EqualTo(1));
        }

        [Test]
        public void SetValue_SameValue_RaisesNothing()
        {
            _repository.SetValue(_volumeSettingSO, 0.8f);

            Assert.That(_changedSettingSOs, Is.Empty);
            Assert.That(_editCount, Is.Zero);
        }

        [Test]
        public void CaptureSnapshot_ThenRestoreIntoNewRepository_RoundTripsValues()
        {
            _repository.SetValue(_volumeSettingSO, 0.25f);
            _repository.SetValue(_vSyncSettingSO, false);
            var restoredRepository = new SettingsStateRepository();

            restoredRepository.RestoreSnapshot(_repository.CaptureSnapshot());

            Assert.That(restoredRepository.GetValue(_volumeSettingSO), Is.EqualTo(0.25f));
            Assert.That(restoredRepository.GetValue(_vSyncSettingSO), Is.False);
        }

        [Test]
        public void RestoreSnapshot_UsedSetting_RaisesChangeButNoEdit()
        {
            _repository.GetValue(_volumeSettingSO);

            _repository.RestoreSnapshot(SnapshotOf((VOLUME_ID, "0.5")));

            Assert.That(_repository.GetValue(_volumeSettingSO), Is.EqualTo(0.5f));
            Assert.That(_changedSettingSOs, Is.EqualTo(new SettingDefinitionSO[] { _volumeSettingSO }));
            Assert.That(_editCount, Is.Zero);
        }

        [Test]
        public void RestoreSnapshot_SettingNotUsedYet_AppliesOnFirstUse()
        {
            _repository.RestoreSnapshot(SnapshotOf((VOLUME_ID, "0.5")));

            Assert.That(_repository.GetValue(_volumeSettingSO), Is.EqualTo(0.5f));
        }

        [Test]
        public void CaptureSnapshot_SettingNotUsedYet_KeepsItsSavedValue()
        {
            _repository.RestoreSnapshot(SnapshotOf((VOLUME_ID, "0.5")));

            SettingDataSnapshot snapshot = _repository.CaptureSnapshot();

            Assert.That(snapshot.Values[VOLUME_ID], Is.EqualTo("0.5"));
        }

        [Test]
        public void RestoreSnapshot_SettingMissing_ResetsItToDefault()
        {
            _repository.SetValue(_volumeSettingSO, 0.1f);

            _repository.RestoreSnapshot(SnapshotOf());

            Assert.That(_repository.GetValue(_volumeSettingSO), Is.EqualTo(0.8f));
        }

        /// <remarks>Regression guard: a toggle saved as a 0/1 slider before it became a bool must fall back to its default.</remarks>
        [Test]
        public void RestoreSnapshot_InvalidValue_WarnsAndResetsToDefault()
        {
            _repository.SetValue(_vSyncSettingSO, false);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(VSYNC_ID));

            _repository.RestoreSnapshot(SnapshotOf((VSYNC_ID, "1")));

            Assert.That(_repository.GetValue(_vSyncSettingSO), Is.True);
        }

        [Test]
        public void RestoreDefaults_ChangedSetting_ResetsAndRaisesChange()
        {
            _repository.SetValue(_volumeSettingSO, 0.1f);
            _changedSettingSOs.Clear();

            _repository.RestoreDefaults();

            Assert.That(_repository.GetValue(_volumeSettingSO), Is.EqualTo(0.8f));
            Assert.That(_changedSettingSOs, Is.EqualTo(new SettingDefinitionSO[] { _volumeSettingSO }));
        }

        [Test]
        public void GetValue_TwoSettingsShareAnIdButNotAType_Throws()
        {
            var clashingSettingSO = CreateSetting<BoolSettingSO>(VOLUME_ID, false);
            _repository.GetValue(_volumeSettingSO);

            Assert.Throws<System.InvalidOperationException>(() => _repository.GetValue(clashingSettingSO));
        }

        private static SettingDataSnapshot SnapshotOf(params (string settingId, string savedText)[] savedValues)
        {
            var snapshot = new SettingDataSnapshot();
            foreach ((string settingId, string savedText) in savedValues)
            {
                snapshot.Values.Add(settingId, savedText);
            }

            return snapshot;
        }

        private TSetting CreateSetting<TSetting>(string settingId, object defaultValue) where TSetting : ScriptableObject
        {
            var settingSO = ScriptableObject.CreateInstance<TSetting>();
            _createdObjects.Add(settingSO);
            var serializedSetting = new SerializedObject(settingSO);
            serializedSetting.FindProperty("_settingId").stringValue = settingId;
            SerializedProperty defaultValueProperty = serializedSetting.FindProperty("_defaultValue");
            if (defaultValue is bool boolValue)
            {
                defaultValueProperty.boolValue = boolValue;
            }
            else
            {
                defaultValueProperty.floatValue = (float)defaultValue;
            }

            serializedSetting.ApplyModifiedPropertiesWithoutUndo();
            return settingSO;
        }

        private void RecordChange(SettingDefinitionSO changedSettingSO)
        {
            _changedSettingSOs.Add(changedSettingSO);
        }

        private void CountEdit()
        {
            _editCount++;
        }
    }
}

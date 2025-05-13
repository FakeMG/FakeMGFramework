using System;
using UnityEngine;

namespace FakeMG.FakeMGFramework.SaveLoad.Advanced {
    public class ExampleSaveableComponent : Saveable {
        [SerializeField] private int health = 100;
        [SerializeField] private Vector3 position;
        [SerializeField] private BoxCollider boxCollider;

        public override object CaptureState() {
            return new PlayerData {
                health = health,
                position = position,
                boxCollider = boxCollider
            };
        }

        public override void RestoreState(object data) {
            if (data is PlayerData playerData) {
                health = playerData.health;
                position = playerData.position;
                boxCollider = playerData.boxCollider;
            }
        }
    }

    [Serializable]
    public class PlayerData {
        public int health;
        public Vector3 position;
        public BoxCollider boxCollider;
    }
}
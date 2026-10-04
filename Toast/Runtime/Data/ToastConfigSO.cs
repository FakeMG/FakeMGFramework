using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Toast
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TOAST + "/ToastConfigSO")]
    public class ToastConfigSO : ScriptableObject
    {
        [Serializable]
        public struct ToastTypeColor
        {
            public ToastType Type;
            public Color TextColor;
        }

        [field: SerializeField] public float DisplayDurationSeconds { get; private set; } = 3f;
        [field: SerializeField] public int MaxVisibleCount { get; private set; } = 5;
        [field: SerializeField] public float SpacingPixels { get; private set; } = 8f;
        [field: SerializeField] public int PoolMaxSize { get; private set; } = 8;
        [field: SerializeField] public ToastStackDirection StackDirection { get; private set; } = ToastStackDirection.Up;

        // 0 disables the corresponding clamp.
        [field: SerializeField] public float MaxToastHeightPixels { get; private set; }
        [field: SerializeField] public int MaxLineCount { get; private set; } = 3;

        [SerializeField]
        private ToastTypeColor[] _typeColors =
        {
            new() { Type = ToastType.Info, TextColor = Color.white },
            new() { Type = ToastType.Success, TextColor = Color.green },
            new() { Type = ToastType.Warning, TextColor = Color.yellow },
            new() { Type = ToastType.Error, TextColor = Color.red }
        };

        public Color GetColorFor(ToastType type)
        {
            foreach (ToastTypeColor typeColor in _typeColors)
            {
                if (typeColor.Type == type)
                {
                    return typeColor.TextColor;
                }
            }

            Echo.Error($"No color configured for toast type '{type}' in '{name}'. Falling back to white.", context: this);
            return Color.white;
        }

        private void OnValidate()
        {
            if (PoolMaxSize < MaxVisibleCount)
            {
                Echo.Warning(
                    $"PoolMaxSize ({PoolMaxSize}) must be >= MaxVisibleCount ({MaxVisibleCount}) in '{name}'. Clamping.",
                    context: this);
                PoolMaxSize = MaxVisibleCount;
            }
        }
    }
}

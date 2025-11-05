using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FakeMG.Framework
{
    public static class Echo
    {
        // === Global toggle ===
        // Add/remove LOGGER_ENABLED in Project Settings > Player > Scripting Define Symbols

        [Conditional("LOGGER_ENABLED")]
        [HideInCallstack]
        public static void Log(string message, bool systemEnabled = true, Object context = null,
            string customColor = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            if (!systemEnabled) return;

            string scriptName = System.IO.Path.GetFileNameWithoutExtension(file);
            string color = string.IsNullOrEmpty(customColor) ? "white" : customColor;

            string prefix = context != null
                ? $"<color={color}>[{scriptName}][{context.name}]</color>"
                : $"<color={color}>[{scriptName}]</color>";

            UnityEngine.Debug.Log($"{prefix} {message}", context);
        }

        [Conditional("LOGGER_ENABLED")]
        [HideInCallstack]
        public static void Warning(string message, bool systemEnabled = true, Object context = null,
            string customColor = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            if (!systemEnabled) return;

            string scriptName = System.IO.Path.GetFileNameWithoutExtension(file);
            string color = string.IsNullOrEmpty(customColor) ? "yellow" : customColor;

            string prefix = context != null
                ? $"<color={color}>[{scriptName}][{context.name}]</color>"
                : $"<color={color}>[{scriptName}]</color>";

            UnityEngine.Debug.LogWarning($"{prefix} {message}", context);
        }

        [Conditional("LOGGER_ENABLED")]
        [HideInCallstack]
        public static void Error(string message, bool systemEnabled = true, Object context = null,
            string customColor = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            if (!systemEnabled) return;

            string scriptName = System.IO.Path.GetFileNameWithoutExtension(file);
            string color = string.IsNullOrEmpty(customColor) ? "red" : customColor;

            string prefix = context != null
                ? $"<color={color}>[{scriptName}][{context.name}]</color>"
                : $"<color={color}>[{scriptName}]</color>";

            UnityEngine.Debug.LogError($"{prefix} {message}", context);
        }
    }
}

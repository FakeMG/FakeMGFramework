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
        public static void Log(ref bool systemEnabled, string message, Object context = null,
            string customColor = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            if (!systemEnabled) return;

            string scriptName = System.IO.Path.GetFileNameWithoutExtension(file);
            string objectName = context != null ? context.name : "null";

            string color = string.IsNullOrEmpty(customColor) ? "white" : customColor;
            string prefix = $"<color={color}>[{scriptName}][{objectName}]</color>";

            UnityEngine.Debug.Log($"{prefix} {message}", context);
        }

        [Conditional("LOGGER_ENABLED")]
        public static void Warning(ref bool systemEnabled, string message, Object context = null,
            string customColor = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            if (!systemEnabled) return;

            string scriptName = System.IO.Path.GetFileNameWithoutExtension(file);
            string objectName = context != null ? context.name : "null";

            string color = string.IsNullOrEmpty(customColor) ? "yellow" : customColor;
            string prefix = $"<color={color}>[{scriptName}][{objectName}]</color>";

            UnityEngine.Debug.LogWarning($"{prefix} {message}", context);
        }

        [Conditional("LOGGER_ENABLED")]
        public static void Error(bool systemEnabled, string message, Object context = null,
            string customColor = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            if (!systemEnabled) return;

            string scriptName = System.IO.Path.GetFileNameWithoutExtension(file);
            string objectName = context != null ? context.name : "null";

            string color = string.IsNullOrEmpty(customColor) ? "red" : customColor;
            string prefix = $"<color={color}>[{scriptName}][{objectName}]</color>";

            UnityEngine.Debug.LogError($"{prefix} {message}", context);
        }
    }
}

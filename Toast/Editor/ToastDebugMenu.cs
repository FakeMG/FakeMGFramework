using FakeMG.Framework;
using UnityEditor;
using UnityEngine;

namespace FakeMG.Toast.Editor
{
    /// <summary>Play-mode debug entries for manually triggering toasts.</summary>
    public static class ToastDebugMenu
    {
        [MenuItem(FakeMGEditorMenus.TOAST + "/Show Info Toast")]
        private static void ShowInfoToast()
        {
            ShowToast("This is an info toast.");
        }

        [MenuItem(FakeMGEditorMenus.TOAST + "/Show Success Toast")]
        private static void ShowSuccessToast()
        {
            ShowToast("Slime fed successfully!", ToastType.Success);
        }

        [MenuItem(FakeMGEditorMenus.TOAST + "/Show Error Toast")]
        private static void ShowErrorToast()
        {
            ShowToast("Something went wrong.", ToastType.Error);
        }

        [MenuItem(FakeMGEditorMenus.TOAST + "/Show Long Wrapping Toast")]
        private static void ShowLongWrappingToast()
        {
            ShowToast(
                "This is a very long toast message used to verify that text wrapping works correctly, " +
                "that the visible height is clamped to the configured maximum line count, and that any " +
                "overflowing text is truncated with an ellipsis instead of growing without limit and " +
                "covering a large part of the screen, which would violate the toast system constraints.",
                ToastType.Warning);
        }

        [MenuItem(FakeMGEditorMenus.TOAST + "/Show Custom Color Toast")]
        private static void ShowCustomColorToast()
        {
            ToastManager toastManager = FindToastManager();
            if (toastManager == null) return;
            toastManager.Show("Custom cyan toast.", Color.cyan);
        }

        [MenuItem(FakeMGEditorMenus.TOAST + "/Show Six Toasts")]
        private static void ShowSixToasts()
        {
            ToastManager toastManager = FindToastManager();
            if (toastManager == null) return;
            for (int i = 1; i <= 6; i++)
            {
                toastManager.Show($"Stacked toast number {i}");
            }
        }

        [MenuItem(FakeMGEditorMenus.TOAST + "/Toggle TimeScale Zero")]
        private static void ToggleTimeScaleZero()
        {
            Time.timeScale = Time.timeScale == 0f ? 1f : 0f;
            Echo.Log($"Time.timeScale = {Time.timeScale}");
        }

        [MenuItem(FakeMGEditorMenus.TOAST + "/Log Toast State")]
        private static void LogToastState()
        {
            ToastManager toastManager = FindToastManager();
            if (toastManager == null) return;

            var containerTransform = toastManager.transform.Find("Toast Container") as RectTransform;
            if (containerTransform == null)
            {
                Echo.Warning("Toast Container child not found under ToastManager.");
                return;
            }

            Echo.Log($"Container children: {containerTransform.childCount}, " +
                      $"anchoredPosition: {containerTransform.anchoredPosition}");
            foreach (Transform child in containerTransform)
            {
                var view = child.GetComponent<ToastView>();
                var canvasGroup = child.GetComponent<CanvasGroup>();
                var rect = child as RectTransform;
                Echo.Log($"'{child.name}' active={child.gameObject.activeSelf} " +
                          $"alpha={canvasGroup.alpha:F2} scale={rect.localScale} " +
                          $"anchoredPos={rect.anchoredPosition} size={rect.sizeDelta} " +
                          $"worldPos={rect.position}");
            }
        }

        [MenuItem(FakeMGEditorMenus.TOAST + "/Clear All Toasts")]
        private static void ClearAllToasts()
        {
            ToastManager toastManager = FindToastManager();
            if (toastManager == null) return;
            toastManager.ClearAll();
        }

        private static void ShowToast(string text, ToastType type = ToastType.Info)
        {
            ToastManager toastManager = FindToastManager();
            if (toastManager == null) return;
            toastManager.Show(text, type);
        }

        private static ToastManager FindToastManager()
        {
            if (!Application.isPlaying)
            {
                Echo.Warning("Debug menu only works in play mode.");
                return null;
            }

            ToastManager toastManager = Object.FindFirstObjectByType<ToastManager>();
            if (toastManager == null)
            {
                Echo.Warning("No ToastManager found in loaded scenes.");
            }

            return toastManager;
        }
    }
}

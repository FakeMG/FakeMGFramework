// #if UNITY_EDITOR
// using TMPro;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UI;

// namespace FakeMG.Framework.UI.Editor
// {
//     public static class ScaleButtonCreator
//     {
//         [MenuItem("GameObject/UI/Scale Button", false, 2001)]
//         public static void CreateScaleButton(MenuCommand menuCommand)
//         {
//             GameObject parent = menuCommand.context as GameObject;

//             // Create main button GameObject
//             GameObject buttonGameObject = new("Scale Button");
//             GameObjectUtility.SetParentAndAlign(buttonGameObject, parent);

//             // Add RectTransform and set default size
//             RectTransform rectTransform = buttonGameObject.AddComponent<RectTransform>();
//             rectTransform.sizeDelta = new Vector2(130f, 50f);

//             // Add Image component for raycast target
//             Image buttonImage = buttonGameObject.AddComponent<Image>();
//             buttonImage.color = new Color(1f, 1f, 1f, 0f); // Transparent but still raycastable

//             // Create visual child GameObject
//             GameObject visualGameObject = new("Visual");
//             GameObjectUtility.SetParentAndAlign(visualGameObject, buttonGameObject);

//             RectTransform visualRect = visualGameObject.AddComponent<RectTransform>();
//             visualRect.anchorMin = Vector2.zero;
//             visualRect.anchorMax = Vector2.one;
//             visualRect.sizeDelta = Vector2.zero;
//             visualRect.anchoredPosition = Vector2.zero;

//             // Add visual image
//             Image visualImage = visualGameObject.AddComponent<Image>();
//             visualImage.color = Color.white;

//             // Create text child GameObject
//             GameObject textGameObject = new("Text");
//             GameObjectUtility.SetParentAndAlign(textGameObject, visualGameObject);

//             RectTransform textRect = textGameObject.AddComponent<RectTransform>();
//             textRect.anchorMin = Vector2.zero;
//             textRect.anchorMax = Vector2.one;
//             textRect.sizeDelta = Vector2.zero;
//             textRect.anchoredPosition = Vector2.zero;

//             // Add TextMeshProUGUI component
//             TextMeshProUGUI textComponent = textGameObject.AddComponent<TextMeshProUGUI>();
//             textComponent.text = "Button";
//             textComponent.fontSize = 18f;
//             textComponent.color = Color.black;
//             textComponent.alignment = TextAlignmentOptions.Center;

//             // Add ButtonScaler component and configure it
//             ButtonScaler scaleButton = buttonGameObject.AddComponent<ButtonScaler>();
//             scaleButton.TargetTransform = visualGameObject.transform;

//             // Register undo and select the created object
//             Undo.RegisterCreatedObjectUndo(buttonGameObject, "Create Scale Button");
//             Selection.activeGameObject = buttonGameObject;
//         }

//         // Validate that we can only create UI elements under a Canvas
//         [MenuItem("GameObject/UI/Scale Button", true)]
//         public static bool ValidateCreateScaleButton()
//         {
//             return Selection.activeGameObject == null ||
//                    Selection.activeGameObject.GetComponentInParent<Canvas>() != null;
//         }
//     }
// }
// #endif
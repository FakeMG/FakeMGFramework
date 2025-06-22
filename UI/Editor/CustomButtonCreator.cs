using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.FakeMGFramework.UI.Editor
{
    public static class CustomButtonCreator
    {
        [MenuItem("GameObject/UI/Custom Button", false, 2001)]
        public static void CreateCustomButton(MenuCommand menuCommand)
        {
            GameObject parent = menuCommand.context as GameObject;

            // Create main button GameObject
            GameObject buttonGameObject = new GameObject("Custom Button");
            GameObjectUtility.SetParentAndAlign(buttonGameObject, parent);

            // Add RectTransform and set default size
            RectTransform rectTransform = buttonGameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(130f, 50f);

            // Add Image component for raycast target
            Image buttonImage = buttonGameObject.AddComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0f); // Transparent but still raycastable

            // Create visual child GameObject
            GameObject visualGameObject = new GameObject("Visual");
            GameObjectUtility.SetParentAndAlign(visualGameObject, buttonGameObject);

            RectTransform visualRect = visualGameObject.AddComponent<RectTransform>();
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.sizeDelta = Vector2.zero;
            visualRect.anchoredPosition = Vector2.zero;

            // Add visual image
            Image visualImage = visualGameObject.AddComponent<Image>();
            visualImage.color = Color.white;

            // Create text child GameObject
            GameObject textGameObject = new GameObject("Text");
            GameObjectUtility.SetParentAndAlign(textGameObject, visualGameObject);

            RectTransform textRect = textGameObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            // Add TextMeshProUGUI component
            TextMeshProUGUI textComponent = textGameObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = "Button";
            textComponent.fontSize = 18f;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.Center;

            // Add CustomButton component and configure it
            CustomButton customButton = buttonGameObject.AddComponent<CustomButton>();

            // Use reflection to set the visual field since it's serialized
            SerializedObject serializedButton = new SerializedObject(customButton);
            SerializedProperty visualProperty = serializedButton.FindProperty("visual");
            visualProperty.objectReferenceValue = visualGameObject.transform;
            serializedButton.ApplyModifiedProperties();

            // Register undo and select the created object
            Undo.RegisterCreatedObjectUndo(buttonGameObject, "Create Custom Button");
            Selection.activeGameObject = buttonGameObject;
        }

        // Validate that we can only create UI elements under a Canvas
        [MenuItem("GameObject/UI/Custom Button", true)]
        public static bool ValidateCreateCustomButton()
        {
            return Selection.activeGameObject == null ||
                   Selection.activeGameObject.GetComponentInParent<Canvas>() != null;
        }
    }
}
using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct Optional<T>
{
    [SerializeField] private bool enabled;
    [SerializeField] private T value;

    public Optional(T initialValue, bool startEnabled = true)
    {
        enabled = startEnabled;
        value = initialValue;
    }

    public bool Enabled => enabled;
    public T Value => value;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Optional<>))]
public class OptionalPropertyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty valueProperty = property.FindPropertyRelative("value");
        return EditorGUI.GetPropertyHeight(valueProperty);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty valueProperty = property.FindPropertyRelative("value");
        SerializedProperty enabledProperty = property.FindPropertyRelative("enabled");

        position.width -= 24;
        EditorGUI.BeginDisabledGroup(!enabledProperty.boolValue);
        EditorGUI.PropertyField(position, valueProperty, label, true);
        EditorGUI.EndDisabledGroup();

        position.x += position.width + 24;
        position.width = position.height = EditorGUI.GetPropertyHeight(enabledProperty);
        position.x -= position.width;
        EditorGUI.PropertyField(position, enabledProperty, GUIContent.none);
    }
}
#endif
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for the SnapAnchorMonoBehaviour class.
/// This makes the 'position' field read-only in the inspector, as it's driven by the transform.
/// </summary>
[CustomEditor(typeof(SnapAnchorMonoBehaviour))]
public class SnapAnchorMonoBehaviourEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Get the target script instance.
        SnapAnchorMonoBehaviour myTarget = (SnapAnchorMonoBehaviour)target;

        // Update the serialized 'position' field from the transform's actual position.
        // This ensures the inspector always shows the correct, up-to-date value.
        SerializedProperty positionProp = serializedObject.FindProperty("position");
        positionProp.vector3Value = myTarget.transform.position;

        SerializedProperty rotationProp = serializedObject.FindProperty("yRotation");
        rotationProp.floatValue = myTarget.transform.eulerAngles.y;

        // Begin a disabled GUI group to make the position field read-only.
        GUI.enabled = false;
        EditorGUILayout.PropertyField(positionProp);
        EditorGUILayout.PropertyField(rotationProp);
        GUI.enabled = true; // Re-enable GUI for subsequent fields.

        // Draw the rest of the properties (i.e., the boundingBox).
        EditorGUILayout.PropertyField(serializedObject.FindProperty("boundingBox"));

        // Apply any changes to the serialized object.
        serializedObject.ApplyModifiedProperties();
    }
}
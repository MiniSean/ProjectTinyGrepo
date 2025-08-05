using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// A custom editor for the ResourceUpgradableMonoBehaviour.
/// It provides a rich, informative Inspector GUI that visualizes the current
/// upgrade progress with clear labels and progress bars.
/// </summary>
[CustomEditor(typeof(ResourceUpgradableMonoBehaviour))]
public class ResourceUpgradableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields (for _upgradeLevels and _currentLevel).
        base.OnInspectorGUI();

        // Get a reference to the component we are inspecting.
        var upgradable = (ResourceUpgradableMonoBehaviour)target;

        // Add some space for visual separation.
        EditorGUILayout.Space(10);
        
        // --- Current Level Display ---
        GUI.backgroundColor = new Color(0.8f, 0.8f, 1f); // Light blue
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        Rect levelRect = EditorGUILayout.GetControlRect();
        EditorGUI.ProgressBar(levelRect, (float)upgradable.CurrentLevel / upgradable.MaxLevel, $"Level: {upgradable.CurrentLevel} / {upgradable.MaxLevel}");
        
        EditorGUILayout.Space(5);

        // --- Upgrade Requirements Visualization ---
        if (upgradable.CurrentLevel < upgradable.MaxLevel)
        {
            EditorGUILayout.LabelField("Next Upgrade Requirements", EditorStyles.boldLabel);

            // Get the requirements for the next level.
            IResourceRequirements requirements = upgradable.GetUpgradeRequirements();

            if (requirements.Requirements.Any())
            {
                GUI.backgroundColor = new Color(0.8f, 1f, 0.8f); // Light green
                foreach (IResourceRequirement req in requirements.Requirements)
                {
                    // For each requirement, draw a progress bar.
                    float progress = 1f - ((float)req.AmountMissing / req.TotalAmount);
                    Rect progressRect = EditorGUILayout.GetControlRect();
                    
                    int haveAmount = req.TotalAmount - req.AmountMissing;
                    
                    EditorGUI.ProgressBar(progressRect, progress, $"{req.ResourceType}: {haveAmount} / {req.TotalAmount}");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No requirements specified for the next level.", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Max level reached.", MessageType.Info);
        }

        GUI.enabled = true; // Always re-enable GUI.
    }
}

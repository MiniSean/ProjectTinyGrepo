using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using UnityEditor.Rendering.Universal.ShaderGUI;

public class LitWorldspaceTiledGUI : ShaderGUI
{
    // A private field to hold an instance of the internal LitShader GUI.
    // We will create this instance using Reflection.
    private ShaderGUI m_LitShaderGUI = null;

    // A boolean to store the open/closed state of our custom foldout section.
    private bool m_CustomOptionsFoldout = true;
    
    /// <summary>
    /// This is the main method for drawing a custom shader GUI.
    /// </summary>
    /// <param name="materialEditor">The MaterialEditor that is currently drawing the inspector.</param>
    /// <param name="properties">An array of all properties found in the shader.</param>
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // On the first run, create an instance of the internal LitShader GUI.
        if (m_LitShaderGUI == null)
        {
            // The full name of the internal class we want to instantiate.
            string litShaderGuiTypeName = "UnityEditor.Rendering.Universal.ShaderGUI.LitShader";

            // Find the type using its name. We need to search across all loaded assemblies.
            Type type = Type.GetType($"{litShaderGuiTypeName}, Unity.RenderPipelines.Universal.Editor");
            if (type != null)
            {
                // If the type was found, create an instance of it.
                m_LitShaderGUI = (ShaderGUI)Activator.CreateInstance(type);
            }
        }

        // If we successfully created the LitShader GUI instance, delegate the drawing to it.
        if (m_LitShaderGUI != null)
        {
            m_LitShaderGUI.OnGUI(materialEditor, properties);
        }
        else
        {
            // As a fallback if reflection fails, just draw the default inspector.
            base.OnGUI(materialEditor, properties);
        }

        // --- After the standard GUI is drawn, we draw our custom properties ---

        // Find our custom property from the full list of properties.
        MaterialProperty tilingDensityProp = FindProperty("_TilingDensity", properties, false);

        // If our custom property was found, we can draw it.
        if (tilingDensityProp != null)
        {
            // Add a small vertical space for visual separation.
            EditorGUILayout.Space();

            // Begin a foldable header group. The return value is the new state (true if open).
            m_CustomOptionsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_CustomOptionsFoldout, "Custom Options");

            // Only draw the contents if the foldout is open.
            if (m_CustomOptionsFoldout)
            {
                // Use the material editor to draw our property field.
                materialEditor.ShaderProperty(tilingDensityProp, tilingDensityProp.displayName);
            }

            // End the header group.
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}

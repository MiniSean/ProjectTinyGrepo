using UnityEngine;
using UnityEngine.UI;
using TMPro; // Make sure TextMeshPro is imported in your project
using System.Text;

/// <summary>
/// A component that visualizes the status of an IResourceUpgradable component
/// on the same GameObject. It dynamically creates a world-space canvas and text
/// display, and subscribes to events to keep the UI updated.
/// </summary>
[RequireComponent(typeof(ResourceUpgradableMonoBehaviour))]
public class UpgradableStatusUI : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("The vertical offset of the UI from the object's pivot.")]
    [SerializeField] private float _verticalOffset = 2.0f;
    [Tooltip("The font size for the status text.")]
    [SerializeField] private float _fontSize = 8f;
    [Tooltip("The width of the UI panel.")]
    [SerializeField] private float _panelWidth = 200f;
    [Tooltip("The height of the UI panel.")]
    [SerializeField] private float _panelHeight = 100f;

    // --- Private Fields ---
    private ResourceUpgradableMonoBehaviour _targetUpgradable;
    private TextMeshProUGUI _statusText;
    private Canvas _canvas;
    private Camera _mainCamera;

    private void Awake()
    {
        // Get a reference to the component we are visualizing.
        _targetUpgradable = GetComponent<ResourceUpgradableMonoBehaviour>();
        _mainCamera = Camera.main;

        // Programmatically create the UI elements.
        CreateWorldSpaceCanvas();
    }

    private void OnEnable()
    {
        // Subscribe to the events hosted by the target component.
        _targetUpgradable.OnUpgraded += HandleUpgrade;
        _targetUpgradable.OnRequirementsUpdated += UpdateUI;

        // Perform an initial update to set the UI to the correct starting state.
        UpdateUI();
    }

    private void OnDisable()
    {
        // Always unsubscribe from events when the component is disabled or destroyed.
        _targetUpgradable.OnUpgraded -= HandleUpgrade;
        _targetUpgradable.OnRequirementsUpdated -= UpdateUI;
    }

    private void LateUpdate()
    {
        // Ensure the world-space canvas always faces the camera for readability.
        if (_canvas != null && _mainCamera != null)
        {
            _canvas.transform.LookAt(
                transform.position + _mainCamera.transform.rotation * Vector3.forward,
                _mainCamera.transform.rotation * Vector3.up
            );
        }
    }

    /// <summary>
    /// Event handler for when the target component is upgraded.
    /// </summary>
    private void HandleUpgrade(int newLevel)
    {
        UpdateUI();
    }

    /// <summary>
    /// Rebuilds and displays the status text based on the target's current state.
    /// </summary>
    private void UpdateUI()
    {
        if (_targetUpgradable == null || _statusText == null) return;

        // Use a StringBuilder for efficient string construction.
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"<b>Level: {_targetUpgradable.CurrentLevel} / {_targetUpgradable.MaxLevel}</b>");
        // sb.AppendLine("-----------");

        if (_targetUpgradable.CurrentLevel >= _targetUpgradable.MaxLevel)
        {
            sb.AppendLine("MAX LEVEL REACHED");
        }
        else
        {
            IResourceRequirements requirements = _targetUpgradable.GetUpgradeRequirements();
            foreach (var req in requirements.Requirements)
            {
                int haveAmount = req.TotalAmount - req.AmountMissing;
                sb.AppendLine($"{req.ResourceType}: {haveAmount} / {req.TotalAmount}");
            }
        }
        
        // Assign the final string to the TextMeshPro component.
        _statusText.text = sb.ToString();
    }

    /// <summary>
    /// Creates and configures all necessary UI objects (Canvas, Text) at runtime.
    /// </summary>
    private void CreateWorldSpaceCanvas()
    {
        // Create Canvas GameObject
        GameObject canvasGO = new GameObject($"{this.name}_StatusCanvas");
        canvasGO.transform.SetParent(this.transform);
        canvasGO.transform.localPosition = new Vector3(0, _verticalOffset, 0);

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(_panelWidth, _panelHeight);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Create Text GameObject
        GameObject textGO = new GameObject("StatusText");
        textGO.transform.SetParent(canvasGO.transform);

        _statusText = textGO.AddComponent<TextMeshProUGUI>();
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.fontSize = _fontSize;
        _statusText.color = Color.black;
        _statusText.textWrappingMode = TextWrappingModes.NoWrap;

        // --- Create Custom Material to Ignore Depth Test ---
        // Find the base shader for TextMeshPro's UI text.
        Shader baseShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
        if (baseShader != null)
        {
            // Create a new material instance from the base shader.
            Material customMaterial = new Material(baseShader);
            // Set the material's render queue to "Overlay". This is a high value
            // that hints to the renderer to draw it later.
            customMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay;
            // This is the critical step: we are setting the ZTest property.
            // "Always" means the pixel will be drawn regardless of the depth buffer's content.
            customMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
            
            // Assign the custom material to our text component.
            _statusText.material = customMaterial;
        }
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.localPosition = Vector3.zero;
        textRect.sizeDelta = new Vector2(_panelWidth, _panelHeight);
    }
}

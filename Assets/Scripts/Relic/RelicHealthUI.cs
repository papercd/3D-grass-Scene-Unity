using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Health bar for the Relic - displays above the Relic in world space
/// </summary>
[RequireComponent(typeof(Canvas))]
public class RelicHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RelicController relicController;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image healthBackgroundImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private GameObject warningIcon;

    [Header("Display Settings")]
    [SerializeField] private bool showNumericHP = true;
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private Vector3 uiOffset = new Vector3(0, 3f, 0);

    [Header("Billboard Settings")]
    [Tooltip("Drag your CameraController (or whatever rotates) here")]
    [SerializeField] private Transform billboardTarget;
    [SerializeField] private bool autoFindCamera = true;

    [Header("Visual Settings")]
    [SerializeField] private Gradient healthColorGradient;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool pulseOnCritical = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.2f;

    private Canvas canvas;
    private float targetFillAmount = 1f;
    private float currentFillAmount = 1f;
    private bool isCritical = false;

    void Awake()
    {
        canvas = GetComponent<Canvas>();

        // Setup canvas for world space UI
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        // Auto-find billboard target if not assigned
        if (billboardTarget == null && autoFindCamera)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Try parent first (for CameraController setups), then camera itself
                billboardTarget = cam.transform.parent != null ? cam.transform.parent : cam.transform;
                Debug.Log($"✅ RelicHealthUI: Auto-found billboard target '{billboardTarget.name}'");
            }
        }

        // Find Relic if not assigned
        if (relicController == null)
        {
            relicController = FindObjectOfType<RelicController>();
        }

        // Setup default gradient if none assigned
        if (healthColorGradient == null || healthColorGradient.colorKeys.Length == 0)
        {
            healthColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.red, 0f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.green, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            healthColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    void OnEnable()
    {
        if (relicController != null)
        {
            relicController.OnHealthChanged += UpdateHealthBar;
            relicController.OnHealthPercentChanged += UpdateHealthPercent;
            relicController.OnCriticalHealth += OnCriticalHealth;
            relicController.OnDestroyed += OnRelicDestroyed;
        }
    }

    void OnDisable()
    {
        if (relicController != null)
        {
            relicController.OnHealthChanged -= UpdateHealthBar;
            relicController.OnHealthPercentChanged -= UpdateHealthPercent;
            relicController.OnCriticalHealth -= OnCriticalHealth;
            relicController.OnDestroyed -= OnRelicDestroyed;
        }
    }

    void Start()
    {
        // Position UI above Relic
        if (relicController != null)
        {
            transform.position = relicController.transform.position + uiOffset;
        }

        // Initial update
        if (relicController != null)
        {
            UpdateHealthBar(relicController.CurrentHP, relicController.MaxHP);
        }

        // Hide warning icon initially
        if (warningIcon != null)
            warningIcon.SetActive(false);
    }

    void Update()
    {
        // Smooth fill animation
        if (healthFillImage != null)
        {
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
            healthFillImage.fillAmount = currentFillAmount;
        }

        // Critical health pulse effect
        if (isCritical && pulseOnCritical && healthFillImage != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            healthFillImage.transform.localScale = Vector3.one * pulse;

            // Pulse warning icon too
            if (warningIcon != null)
            {
                warningIcon.transform.localScale = Vector3.one * pulse;
            }
        }
    }

    void LateUpdate()
    {
        // Billboard - face camera/controller
        if (billboardTarget != null)
        {
            transform.rotation = billboardTarget.rotation;
        }

        // Update position to follow Relic
        if (relicController != null)
        {
            transform.position = relicController.transform.position + uiOffset;
        }
    }

    void UpdateHealthBar(float currentHP, float maxHP)
    {
        targetFillAmount = currentHP / maxHP;

        // Update color based on health percentage
        if (healthFillImage != null)
        {
            healthFillImage.color = healthColorGradient.Evaluate(targetFillAmount);
        }

        // Update text
        UpdateHealthText(currentHP, maxHP);
    }

    void UpdateHealthPercent(float healthPercent)
    {
        // This is an alternative update method using normalized health
        targetFillAmount = healthPercent;

        if (healthFillImage != null)
        {
            healthFillImage.color = healthColorGradient.Evaluate(healthPercent);
        }
    }

    void UpdateHealthText(float currentHP, float maxHP)
    {
        if (healthText == null) return;

        string text = "";

        if (showNumericHP)
        {
            text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
        }

        if (showPercentage)
        {
            float percent = (currentHP / maxHP) * 100f;
            if (showNumericHP)
                text += $" ({percent:F0}%)";
            else
                text = $"{percent:F0}%";
        }

        healthText.text = text;
    }

    void OnCriticalHealth()
    {
        isCritical = true;

        // Show warning icon
        if (warningIcon != null)
        {
            warningIcon.SetActive(true);
        }

        Debug.Log("⚠️ Relic Health UI: Critical warning displayed!");
    }

    void OnRelicDestroyed()
    {
        // Fade out or show destroyed state
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = 0;
        }

        if (healthText != null)
        {
            healthText.text = "DESTROYED";
            healthText.color = Color.red;
        }

        Debug.Log("💀 Relic Health UI: Destroyed state");
    }

    // Public method to manually refresh UI
    public void RefreshUI()
    {
        if (relicController != null)
        {
            UpdateHealthBar(relicController.CurrentHP, relicController.MaxHP);
        }
    }

    // Editor helper
    [ContextMenu("Test Critical State")]
    void TestCriticalState()
    {
        OnCriticalHealth();
    }
}
using UnityEngine;
using UnityEngine.UI;

namespace NABHI.Chakras.UI
{
    /// <summary>
    /// UI para mostrar la barra de energia del personaje.
    /// </summary>
    public class EnergyBarUI : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private EnergySystem energySystem;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text energyText;

        [Header("Configuracion Visual")]
        [SerializeField] private Color fullColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private Color lowColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private float lowEnergyThreshold = 0.25f;

        [Header("Animacion")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private bool pulseWhenLow = true;
        [SerializeField] private float pulseSpeed = 3f;

        private float displayedFill;
        private float targetFill;

        private void Start()
        {
            if (energySystem == null)
                energySystem = FindObjectOfType<EnergySystem>();

            if (energySystem != null)
            {
                energySystem.OnEnergyChanged += OnEnergyChanged;
                UpdateDisplay(energySystem.CurrentEnergy, energySystem.MaxEnergy);
            }
        }

        private void OnDestroy()
        {
            if (energySystem != null)
            {
                energySystem.OnEnergyChanged -= OnEnergyChanged;
            }
        }

        private void Update()
        {
            // Animar fill suavemente
            displayedFill = Mathf.Lerp(displayedFill, targetFill, smoothSpeed * Time.deltaTime);

            if (fillImage != null)
            {
                fillImage.fillAmount = displayedFill;

                // Color basado en nivel
                Color targetColor = Color.Lerp(lowColor, fullColor, displayedFill);
                fillImage.color = targetColor;

                // Pulso cuando esta bajo
                if (pulseWhenLow && displayedFill < lowEnergyThreshold)
                {
                    float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                    Color pulseColor = Color.Lerp(lowColor, Color.white, pulse * 0.3f);
                    fillImage.color = pulseColor;
                }
            }
        }

        private void OnEnergyChanged(float current, float max)
        {
            UpdateDisplay(current, max);
        }

        private void UpdateDisplay(float current, float max)
        {
            targetFill = current / max;

            if (energyText != null)
            {
                energyText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }
    }
}

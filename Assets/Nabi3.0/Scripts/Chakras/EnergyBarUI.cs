using NABHI.Chakras.DebugTools;
using UnityEngine;
using UnityEngine.UI;

public class EnergyBarUI : MonoBehaviour
{
    public Slider energySlider;
    public ChakraDebugController energySystem;

    void Start()
    {
        // Inicializar barra
        energySlider.maxValue = energySystem.maxEnergy;
        energySlider.value = energySystem.currentEnergy;
    }

    void Update()
    {
        // Actualizar barra constantemente
        energySlider.value = energySystem.currentEnergy;
    }
}

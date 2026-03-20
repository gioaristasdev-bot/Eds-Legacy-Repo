using UnityEngine;
using System;

namespace NABHI.Chakras
{
    /// <summary>
    /// Sistema de energia del personaje.
    /// Los Chakras consumen energia al activarse o mantenerse activos.
    /// </summary>
    public class EnergySystem : MonoBehaviour
    {
        [Header("Configuracion de Energia")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float currentEnergy = 100f;

        [Header("Regeneracion")]
        [SerializeField] private float energyRegenRate = 5f;
        [SerializeField] private float regenDelay = 2f;
        [SerializeField] private bool regenWhileUsingChakra = false;

        private float lastEnergyUseTime;
        private bool isUsingEnergy;

        // Eventos
        public event Action<float, float> OnEnergyChanged; // current, max
        public event Action OnEnergyDepleted;
        public event Action OnEnergyFull;

        // Propiedades publicas
        public float MaxEnergy => maxEnergy;
        public float CurrentEnergy => currentEnergy;
        public float EnergyPercent => currentEnergy / maxEnergy;
        public bool HasEnergy => currentEnergy > 0;
        public bool IsFullEnergy => currentEnergy >= maxEnergy;

        private void Update()
        {
            HandleEnergyRegeneration();
        }

        private void HandleEnergyRegeneration()
        {
            if (!regenWhileUsingChakra && isUsingEnergy)
                return;

            if (Time.time < lastEnergyUseTime + regenDelay)
                return;

            if (currentEnergy < maxEnergy)
            {
                AddEnergy(energyRegenRate * Time.deltaTime);
            }
        }

        /// <summary>
        /// Consume energia. Retorna true si habia suficiente energia.
        /// </summary>
        public bool ConsumeEnergy(float amount)
        {
            if (currentEnergy < amount)
            {
                return false;
            }

            currentEnergy -= amount;
            currentEnergy = Mathf.Max(0, currentEnergy);
            lastEnergyUseTime = Time.time;

            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

            if (currentEnergy <= 0)
            {
                OnEnergyDepleted?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// Consume energia por segundo (para chakras continuos).
        /// Llamar en Update. Retorna true mientras haya energia.
        /// </summary>
        public bool ConsumeEnergyPerSecond(float ratePerSecond)
        {
            isUsingEnergy = true;
            float amount = ratePerSecond * Time.deltaTime;
            return ConsumeEnergy(amount);
        }

        /// <summary>
        /// Indica que se dejo de usar energia (para regeneracion)
        /// </summary>
        public void StopUsingEnergy()
        {
            isUsingEnergy = false;
        }

        /// <summary>
        /// Agrega energia (pickups, descanso, etc)
        /// </summary>
        public void AddEnergy(float amount)
        {
            float previousEnergy = currentEnergy;
            currentEnergy += amount;
            currentEnergy = Mathf.Min(currentEnergy, maxEnergy);

            if (currentEnergy != previousEnergy)
            {
                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            }

            if (currentEnergy >= maxEnergy && previousEnergy < maxEnergy)
            {
                OnEnergyFull?.Invoke();
            }
        }

        /// <summary>
        /// Restaura toda la energia
        /// </summary>
        public void RestoreFullEnergy()
        {
            currentEnergy = maxEnergy;
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            OnEnergyFull?.Invoke();
        }

        /// <summary>
        /// Aumenta la energia maxima (mejoras permanentes)
        /// </summary>
        public void IncreaseMaxEnergy(float amount)
        {
            maxEnergy += amount;
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }

        /// <summary>
        /// Verifica si hay suficiente energia para una accion
        /// </summary>
        public bool HasEnoughEnergy(float amount)
        {
            return currentEnergy >= amount;
        }
    }
}

using UnityEngine;
using System;
using Debug = UnityEngine.Debug;

namespace NABHI.Chakras
{
    /// <summary>
    /// Tipos de Chakra disponibles en el juego
    /// </summary>
    public enum ChakraType
    {
        None = 0,
        Float = 1,              // Chakra 1 - Corona (Rosa)
        Invisibility = 2,       // Chakra 2 - Tercer Ojo (Azul)
        Tremor = 3,             // Chakra 3 - Corazon (Verde)
        EchoSense = 4,          // Chakra 4 - Sacro (Naranja)
        RemoteHack = 5,         // Chakra 5 - Plexo Solar (Amarillo)
        EMP = 6,                // Chakra 6 - Garganta (Azul Claro)
        Telekinesis = 7,        // Chakra 7 opcion 1 - Raiz (Rojo)
        GravityPulse = 8        // Chakra 7 opcion 2 - Raiz (Rojo)
    }

    /// <summary>
    /// Modo de activacion del Chakra
    /// </summary>
    public enum ChakraActivationMode
    {
        Instant,        // Se activa una vez y tiene efecto inmediato
        Continuous,     // Se mantiene activo mientras se presiona el boton
        Contextual      // Requiere un objetivo valido para activarse
    }

    /// <summary>
    /// Clase base abstracta para todos los Chakras.
    /// Cada Chakra hereda de esta clase e implementa su comportamiento especifico.
    /// </summary>
    public abstract class ChakraBase : MonoBehaviour
    {
        [Header("Identificacion")]
        [SerializeField] protected ChakraType chakraType;
        [SerializeField] protected string chakraName;
        [SerializeField] protected Color chakraColor = Color.white;

        [Header("Configuracion")]
        [SerializeField] protected ChakraActivationMode activationMode;
        [SerializeField] protected float energyCostPerSecond = 10f;     // Para continuos
        [SerializeField] protected float energyCostPerUse = 20f;        // Para instantaneos
        [SerializeField] protected float cooldown = 0f;

        [Header("Estado")]
        [SerializeField] protected bool isUnlocked = false;

        // Referencias
        protected EnergySystem energySystem;
        protected ChakraSystem chakraSystem;

        // Estado interno
        protected bool isActive;
        protected float cooldownTimer;
        protected bool manualEnergyManagement = false; // Si true, la subclase maneja el consumo de energia

        // Eventos
        public event Action<ChakraBase> OnChakraActivated;
        public event Action<ChakraBase> OnChakraDeactivated;
        public event Action<ChakraBase> OnChakraUnlocked;

        // Propiedades publicas
        public ChakraType Type => chakraType;

        // Indica si el jugador tiene desbloqueado el poder
        public bool unlocked = true;

        public string Name => chakraName;
        public Color Color => chakraColor;
        public ChakraActivationMode ActivationMode => activationMode;
        public bool IsUnlocked => isUnlocked;
        public bool IsActive => isActive;
        public bool IsOnCooldown => cooldownTimer > 0;
        public float CooldownRemaining => cooldownTimer;
        public float CooldownPercent => cooldown > 0 ? cooldownTimer / cooldown : 0;

        protected virtual void Awake()
        {
            energySystem = GetComponentInParent<EnergySystem>();
            chakraSystem = GetComponentInParent<ChakraSystem>();
        }

        protected virtual void Update()
        {
            // Actualizar cooldown
            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
            }

            // Si es continuo y esta activo, consumir energia (a menos que la subclase lo maneje)
            if (isActive && activationMode == ChakraActivationMode.Continuous && !manualEnergyManagement)
            {
                if (!energySystem.ConsumeEnergyPerSecond(energyCostPerSecond))
                {
                    // Sin energia, desactivar
                    Deactivate();
                }
            }
        }

        /// <summary>
        /// Intenta activar el Chakra
        /// </summary>
        public virtual bool TryActivate()
        {
            if (!CanActivate())
                return false;

            return Activate();
        }

        /// <summary>
        /// Verifica si el Chakra puede activarse
        /// </summary>
        public virtual bool CanActivate()
        {
            if (!isUnlocked)
                return false;

            if (IsOnCooldown)
                return false;

            // Verificar energia segun modo
            if (activationMode == ChakraActivationMode.Instant)
            {
                if (!energySystem.HasEnoughEnergy(energyCostPerUse))
                    return false;
            }
            else if (activationMode == ChakraActivationMode.Continuous)
            {
                if (!energySystem.HasEnergy)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Activa el Chakra (implementar en clases derivadas)
        /// </summary>
        protected virtual bool Activate()
        {
            if (activationMode == ChakraActivationMode.Instant)
            {
                // Consumir energia una vez
                if (!energySystem.ConsumeEnergy(energyCostPerUse))
                    return false;

                // Iniciar cooldown
                cooldownTimer = cooldown;
            }

            isActive = true;
            OnActivate();
            OnChakraActivated?.Invoke(this);

            Debug.Log($"[Chakra] {chakraName} activado");
            return true;
        }

        /// <summary>
        /// Desactiva el Chakra
        /// </summary>
        public virtual void Deactivate()
        {
            if (!isActive)
                return;

            isActive = false;
            energySystem.StopUsingEnergy();
            OnDeactivate();
            OnChakraDeactivated?.Invoke(this);

            // Cooldown para chakras continuos al desactivar
            if (activationMode == ChakraActivationMode.Continuous && cooldown > 0)
            {
                cooldownTimer = cooldown;
            }

            Debug.Log($"[Chakra] {chakraName} desactivado");
        }

        /// <summary>
        /// Desbloquea el Chakra (progresion del juego)
        /// </summary>
        public virtual void Unlock()
        {
            if (isUnlocked)
                return;

            isUnlocked = true;
            OnUnlock();
            OnChakraUnlocked?.Invoke(this);

            Debug.Log($"[Chakra] {chakraName} DESBLOQUEADO!");
        }

        /// <summary>
        /// Bloquea el Chakra (para testing o eventos especiales)
        /// </summary>
        public virtual void Lock()
        {
            if (isActive)
                Deactivate();

            isUnlocked = false;
        }

        // Metodos abstractos que cada Chakra debe implementar
        protected abstract void OnActivate();
        protected abstract void OnDeactivate();

        // Metodos virtuales opcionales
        protected virtual void OnUnlock() { }

        /// <summary>
        /// Para chakras contextuales: verifica si hay un objetivo valido
        /// </summary>
        public virtual bool HasValidTarget()
        {
            return true;
        }

        /// <summary>
        /// Obtiene informacion para mostrar en UI
        /// </summary>
        public virtual string GetDescription()
        {
            return $"{chakraName}: {GetChakraDescription()}";
        }

        protected abstract string GetChakraDescription();
    }
}

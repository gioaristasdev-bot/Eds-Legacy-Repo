using UnityEngine;

namespace NABHI.Chakras.DebugTools
{
    /// <summary>
    /// Controlador de debug para probar chakras desde el Inspector.
    /// Permite seleccionar, activar y desbloquear chakras sin necesidad de UI.
    /// </summary>
    public class ChakraDebugController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private ChakraSystem chakraSystem;
        [SerializeField] private EnergySystem energySystem;

        [Header("Seleccion de Chakra")]
        [Tooltip("Chakra actualmente seleccionado")]
        [SerializeField] private ChakraType selectedChakra = ChakraType.None;

        [Header("Estado de Desbloqueo")]
        [SerializeField] private bool chakra1_Float = false;
        [SerializeField] private bool chakra2_Invisibility = false;
        [SerializeField] private bool chakra3_Tremor = false;
        [SerializeField] private bool chakra4_EchoSense = false;
        [SerializeField] private bool chakra5_RemoteHack = false;
        [SerializeField] private bool chakra6_EMP = false;
        [SerializeField] private bool chakra7a_Telekinesis = false;
        [SerializeField] private bool chakra7b_GravityPulse = false;

        [Header("Acciones Rapidas")]
        [SerializeField] private bool unlockAllChakras = false;
        [SerializeField] private bool lockAllChakras = false;
        [SerializeField] private bool refillEnergy = false;

        [Header("Estado Actual (Solo Lectura)")]
        [SerializeField] private ChakraType currentlyActive = ChakraType.None;
        [SerializeField] public float currentEnergy = 0f;
        [SerializeField] public float maxEnergy = 100f;

        // Cache para detectar cambios
        private ChakraType lastSelectedChakra = ChakraType.None;
        private bool[] lastUnlockState = new bool[8];

        private void Awake()
        {
            if (chakraSystem == null)
                chakraSystem = GetComponent<ChakraSystem>();

            if (energySystem == null)
                energySystem = GetComponent<EnergySystem>();
        }

        private void Start()
        {
            // Sincronizar estado inicial desde el sistema
            SyncFromSystem();
        }

        private void Update()
        {
            if (chakraSystem == null) return;

            // Detectar cambios en seleccion
            if (selectedChakra != lastSelectedChakra)
            {
                if (selectedChakra != ChakraType.None && chakraSystem.IsChakraUnlocked(selectedChakra))
                {
                    chakraSystem.SelectChakra(selectedChakra);
                }
                lastSelectedChakra = selectedChakra;
            }

            // Detectar cambios en estado de desbloqueo
            CheckUnlockChanges();

            // Acciones rapidas
            HandleQuickActions();

            // Actualizar estado de solo lectura
            UpdateReadOnlyState();
        }

        private void CheckUnlockChanges()
        {
            bool[] currentState = new bool[]
            {
                chakra1_Float,
                chakra2_Invisibility,
                chakra3_Tremor,
                chakra4_EchoSense,
                chakra5_RemoteHack,
                chakra6_EMP,
                chakra7a_Telekinesis,
                chakra7b_GravityPulse
            };

            ChakraType[] types = new ChakraType[]
            {
                ChakraType.Float,
                ChakraType.Invisibility,
                ChakraType.Tremor,
                ChakraType.EchoSense,
                ChakraType.RemoteHack,
                ChakraType.EMP,
                ChakraType.Telekinesis,
                ChakraType.GravityPulse
            };

            for (int i = 0; i < currentState.Length; i++)
            {
                if (currentState[i] != lastUnlockState[i])
                {
                    if (currentState[i])
                    {
                        chakraSystem.UnlockChakra(types[i]);
                    }
                    else
                    {
                        var chakra = chakraSystem.GetChakra(types[i]);
                        if (chakra != null)
                        {
                            chakra.Lock();
                        }
                    }
                    lastUnlockState[i] = currentState[i];
                }
            }
        }

        private void HandleQuickActions()
        {
            if (unlockAllChakras)
            {
                unlockAllChakras = false;
                UnlockAll();
            }

            if (lockAllChakras)
            {
                lockAllChakras = false;
                LockAll();
            }

            if (refillEnergy)
            {
                refillEnergy = false;
                if (energySystem != null)
                {
                    energySystem.RestoreFullEnergy();
                }
            }
        }

        private void UpdateReadOnlyState()
        {
            currentlyActive = chakraSystem.ActiveChakra;
            selectedChakra = chakraSystem.SelectedChakra;

            if (energySystem != null)
            {
                currentEnergy = energySystem.CurrentEnergy;
                maxEnergy = energySystem.MaxEnergy;
            }
        }

        private void SyncFromSystem()
        {
            if (chakraSystem == null) return;

            chakra1_Float = chakraSystem.IsChakraUnlocked(ChakraType.Float);
            chakra2_Invisibility = chakraSystem.IsChakraUnlocked(ChakraType.Invisibility);
            chakra3_Tremor = chakraSystem.IsChakraUnlocked(ChakraType.Tremor);
            chakra4_EchoSense = chakraSystem.IsChakraUnlocked(ChakraType.EchoSense);
            chakra5_RemoteHack = chakraSystem.IsChakraUnlocked(ChakraType.RemoteHack);
            chakra6_EMP = chakraSystem.IsChakraUnlocked(ChakraType.EMP);
            chakra7a_Telekinesis = chakraSystem.IsChakraUnlocked(ChakraType.Telekinesis);
            chakra7b_GravityPulse = chakraSystem.IsChakraUnlocked(ChakraType.GravityPulse);

            lastUnlockState = new bool[]
            {
                chakra1_Float,
                chakra2_Invisibility,
                chakra3_Tremor,
                chakra4_EchoSense,
                chakra5_RemoteHack,
                chakra6_EMP,
                chakra7a_Telekinesis,
                chakra7b_GravityPulse
            };

            selectedChakra = chakraSystem.SelectedChakra;
            lastSelectedChakra = selectedChakra;
        }

        [ContextMenu("Unlock All Chakras")]
        public void UnlockAll()
        {
            chakra1_Float = true;
            chakra2_Invisibility = true;
            chakra3_Tremor = true;
            chakra4_EchoSense = true;
            chakra5_RemoteHack = true;
            chakra6_EMP = true;
            chakra7a_Telekinesis = true;
            chakra7b_GravityPulse = true;

            if (chakraSystem != null)
            {
                chakraSystem.UnlockAllChakras();
            }

            SyncFromSystem();
        }

        [ContextMenu("Lock All Chakras")]
        public void LockAll()
        {
            chakra1_Float = false;
            chakra2_Invisibility = false;
            chakra3_Tremor = false;
            chakra4_EchoSense = false;
            chakra5_RemoteHack = false;
            chakra6_EMP = false;
            chakra7a_Telekinesis = false;
            chakra7b_GravityPulse = false;

            if (chakraSystem != null)
            {
                chakraSystem.LockAllChakras();
            }

            SyncFromSystem();
        }

        [ContextMenu("Refill Energy")]
        public void RefillEnergy()
        {
            if (energySystem != null)
            {
                energySystem.RestoreFullEnergy();
            }
        }
    }
}

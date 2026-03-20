using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Debug = UnityEngine.Debug;
using NABHI.Chakras.Abilities;

namespace NABHI.Chakras
{
    /// <summary>
    /// Sistema principal de gestion de Chakras.
    /// Maneja la seleccion, activacion y control de todos los Chakras del personaje.
    /// </summary>
    public class ChakraSystem : MonoBehaviour
    {
        public void ActivateChakra(int id)
        {
            Debug.Log("Activando Chakra: " + id);

            switch (id)
            {
                case 0:
                    Debug.Log("Poder 1 activado");
                    break;

                case 1:
                    Debug.Log("Poder 2 activado");
                    break;

                case 2:
                    Debug.Log("Poder 3 activado");
                    break;

                case 3:
                    Debug.Log("Poder 4 activado");
                    break;
            }
        }


        [Header("Referencias")]
        [SerializeField] private EnergySystem energySystem;

        [Header("Input")]
        // Botón SELECT del gamepad (abrir menú)
        [SerializeField] private KeyCode gamepadSelectButton = KeyCode.JoystickButton6;

        // Botón A (activar chakra)
        [SerializeField] private KeyCode gamepadActivateButton = KeyCode.JoystickButton0;

        // Botón B (cerrar menú)
        [SerializeField] private KeyCode gamepadCancelButton = KeyCode.JoystickButton1;
        [Tooltip("Tecla para activar/desactivar chakra (press)")]
        [SerializeField] private KeyCode activateKey = KeyCode.E;
        [Tooltip("Tecla para abrir rueda de seleccion (hold)")]
        [SerializeField] private KeyCode wheelKey = KeyCode.Tab;
        [Tooltip("Tecla para cambio rapido de chakra")]
        [SerializeField] private KeyCode quickSwitchKey = KeyCode.LeftAlt;
        [Tooltip("Tiempo minimo de hold para abrir rueda (evita conflicto con press)")]
        [SerializeField] private float wheelHoldTime = 0.2f;

        private float wheelKeyHoldTimer = 0f;
        private bool wheelKeyWasHeld = false;

        [Header("Configuracion")]
        [Tooltip("Tiempo de slow-motion al abrir la rueda (0 = sin slow-mo)")]
        [SerializeField] private float wheelSlowMotionScale = 0.3f;

        [Header("Debug")]
        [Tooltip("Mostrar GUI de debug en pantalla (desactivar si usas ChakraDebugController)")]
        [SerializeField] private bool debugMode = false;

        // Chakras registrados
        private Dictionary<ChakraType, ChakraBase> chakras = new Dictionary<ChakraType, ChakraBase>();
        private List<ChakraType> unlockedChakraOrder = new List<ChakraType>();

        // Estado
        private ChakraType selectedChakra = ChakraType.None;
        private ChakraType activeChakra = ChakraType.None;
        private bool isWheelOpen;
        private float originalTimeScale;

        // Eventos
        public event Action<ChakraType> OnChakraSelected;
        public event Action<ChakraType> OnChakraActivated;
        public event Action<ChakraType> OnChakraDeactivated;
        public event Action<bool> OnWheelToggled;
        public event Action<ChakraType> OnChakraUnlocked;

        // Propiedades publicas
        public ChakraType SelectedChakra => selectedChakra;
        public ChakraType ActiveChakra => activeChakra;
        public bool IsWheelOpen => isWheelOpen;
        public bool HasActiveChakra => activeChakra != ChakraType.None;
        public IReadOnlyList<ChakraType> UnlockedChakras => unlockedChakraOrder;
        public EnergySystem Energy => energySystem;



        private void Awake()
        {
            if (energySystem == null)
                energySystem = GetComponent<EnergySystem>();

            // Registrar todos los chakras hijos
            RegisterChakras();
        }

        void Start()
        {
            ActivateAllChakras();
        }

        void ActivateAllChakras()
        {
            foreach (var chakra in chakras)
            {
                chakra.Value.unlocked = true;
            }
        }
        private void Update()
        {
            HandleInput();

        }

        private void RegisterChakras()
        {
            ChakraBase[] chakraComponents = GetComponentsInChildren<ChakraBase>(true);

            foreach (var chakra in chakraComponents)
            {
                if (!chakras.ContainsKey(chakra.Type))
                {
                    chakras[chakra.Type] = chakra;

                    // Suscribirse a eventos
                    chakra.OnChakraUnlocked += HandleChakraUnlocked;

                    if (chakra.IsUnlocked && !unlockedChakraOrder.Contains(chakra.Type))
                    {
                        unlockedChakraOrder.Add(chakra.Type);
                    }

                    if (debugMode)
                        Debug.Log($"[ChakraSystem] Registrado: {chakra.Type} - Desbloqueado: {chakra.IsUnlocked}");
                }
            }

            // Ordenar por tipo
            unlockedChakraOrder.Sort((a, b) => ((int)a).CompareTo((int)b));

            // Seleccionar el primer chakra desbloqueado
            if (unlockedChakraOrder.Count > 0 && selectedChakra == ChakraType.None)
            {
                SelectChakra(unlockedChakraOrder[0]);
            }
        }

        private void HandleInput()
        {
            // === RUEDA DE SELECCION (Hold wheelKey) ===
            if (Input.GetKey(wheelKey))
            {
                wheelKeyHoldTimer += Time.unscaledDeltaTime;

                // Abrir rueda despues de holdTime
                if (wheelKeyHoldTimer >= wheelHoldTime && !isWheelOpen)
                {
                    OpenWheel();
                    wheelKeyWasHeld = true;
                }
            }

            if (Input.GetKeyUp(wheelKey))
            {
                if (isWheelOpen)
                {
                    CloseWheel();
                }
                wheelKeyHoldTimer = 0f;
                wheelKeyWasHeld = false;
            }

            // Abrir rueda con SELECT del gamepad
            if (Input.GetKeyDown(gamepadSelectButton))
            {
                OpenWheel();
            }

            if (Input.GetKeyUp(gamepadSelectButton))
            {
                CloseWheel();
            }

            // Mientras la rueda esta abierta, manejar seleccion
            if (isWheelOpen)
            {
                HandleWheelSelection();
                return; // No procesar activacion mientras la rueda esta abierta
            }

            // === ACTIVAR/DESACTIVAR CHAKRA (Press activateKey) ===
            if (Input.GetKeyDown(activateKey) || Input.GetKeyDown(gamepadActivateButton))
            {
                ToggleOrActivateChakra();
            }

            // === CAMBIO RAPIDO ===
            if (Input.GetKeyDown(quickSwitchKey))
            {
                QuickSwitchToNextChakra();
            }

            if (isWheelOpen && Input.GetKeyDown(gamepadCancelButton))
            {
                CloseWheel();
            }
        }

        /// <summary>
        /// Logica de toggle para chakras: press activa, press desactiva
        /// </summary>
        private void ToggleOrActivateChakra()
        {
            // Si hay un chakra continuo activo
            if (activeChakra != ChakraType.None)
            {
                var currentChakra = GetChakra(activeChakra);
                if (currentChakra != null && currentChakra.ActivationMode == ChakraActivationMode.Continuous)
                {
                    // Si es el mismo chakra seleccionado, desactivar (toggle off)
                    if (activeChakra == selectedChakra)
                    {
                        DeactivateCurrentChakra();
                        return;
                    }
                    else
                    {
                        // Si es diferente, desactivar el actual primero
                        DeactivateCurrentChakra();
                    }
                }
            }

            // Activar el chakra seleccionado
            TryActivateSelectedChakra();
        }

        private void HandleWheelSelection()
        {
            // Usar mouse o analog para seleccionar en la rueda
            Vector2 input = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            if (input.magnitude > 0.3f)
            {
                // Calcular angulo y mapear a chakra
                float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                int chakraCount = unlockedChakraOrder.Count;
                if (chakraCount > 0)
                {
                    float segmentSize = 360f / chakraCount;
                    int index = Mathf.FloorToInt(angle / segmentSize);
                    index = Mathf.Clamp(index, 0, chakraCount - 1);

                    ChakraType newSelection = unlockedChakraOrder[index];
                    if (newSelection != selectedChakra)
                    {
                        SelectChakra(newSelection);
                    }
                }
            }

            // Tambien permitir seleccion con teclas numericas
            for (int i = 0; i < Mathf.Min(9, unlockedChakraOrder.Count); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectChakra(unlockedChakraOrder[i]);
                }
            }
        }

        #region Wheel Control

        public void OpenWheel()
        {
            if (isWheelOpen) return;
            if (unlockedChakraOrder.Count == 0) return;

            isWheelOpen = true;

            // Slow motion
            if (wheelSlowMotionScale > 0 && wheelSlowMotionScale < 1)
            {
                originalTimeScale = Time.timeScale;
                Time.timeScale = wheelSlowMotionScale;
            }

            OnWheelToggled?.Invoke(true);

            if (debugMode)
                Debug.Log("[ChakraSystem] Rueda abierta");
        }

        public void CloseWheel()
        {
            if (!isWheelOpen) return;

            isWheelOpen = false;

            // Restaurar tiempo
            if (wheelSlowMotionScale > 0 && wheelSlowMotionScale < 1)
            {
                Time.timeScale = originalTimeScale;
            }

            OnWheelToggled?.Invoke(false);

            if (debugMode)
                Debug.Log($"[ChakraSystem] Rueda cerrada - Seleccionado: {selectedChakra}");
        }

        #endregion

        #region Chakra Selection

        public void SelectChakra(ChakraType type)
        {
            if (type == ChakraType.None) return;
            if (!chakras.ContainsKey(type)) return;
            if (!chakras[type].IsUnlocked) return;

            selectedChakra = type;
            OnChakraSelected?.Invoke(type);

            if (debugMode)
                Debug.Log($"[ChakraSystem] Chakra seleccionado: {type}");
        }

        public void QuickSwitchToNextChakra()
        {
            if (unlockedChakraOrder.Count <= 1) return;

            int currentIndex = unlockedChakraOrder.IndexOf(selectedChakra);
            int nextIndex = (currentIndex + 1) % unlockedChakraOrder.Count;

            SelectChakra(unlockedChakraOrder[nextIndex]);
        }

        public void QuickSwitchToPreviousChakra()
        {
            if (unlockedChakraOrder.Count <= 1) return;

            int currentIndex = unlockedChakraOrder.IndexOf(selectedChakra);
            int prevIndex = currentIndex - 1;
            if (prevIndex < 0) prevIndex = unlockedChakraOrder.Count - 1;

            SelectChakra(unlockedChakraOrder[prevIndex]);
        }

        #endregion

        #region Chakra Activation

        public bool TryActivateSelectedChakra()
        {
            return TryActivateChakra(selectedChakra);
        }

        public bool TryActivateChakra(ChakraType type)
        {
            if (type == ChakraType.None) return false;
            if (!chakras.ContainsKey(type)) return false;

            // Bloquear activación si hay un chakra ejecutando una acción bloqueante (ej: Tremor)
            if (IsAnyChakraBlocking())
            {
                if (debugMode)
                    Debug.Log("[ChakraSystem] No se puede activar chakra - hay una acción bloqueante en progreso");
                return false;
            }

            var chakra = chakras[type];

            // Si hay un chakra activo diferente, desactivarlo primero
            if (activeChakra != ChakraType.None && activeChakra != type)
            {
                DeactivateCurrentChakra();
            }

            if (chakra.TryActivate())
            {
                // Solo marcar como activo si es continuo
                if (chakra.ActivationMode == ChakraActivationMode.Continuous)
                {
                    activeChakra = type;
                }

                OnChakraActivated?.Invoke(type);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verifica si algún chakra está bloqueando otras acciones
        /// </summary>
        private bool IsAnyChakraBlocking()
        {
            // Verificar Tremor
            if (chakras.TryGetValue(ChakraType.Tremor, out ChakraBase tremorBase))
            {
                if (tremorBase is ChakraTremor tremor && tremor.IsExecutingTremor)
                {
                    return true;
                }
            }

            // Aquí se pueden añadir más chakras bloqueantes en el futuro

            return false;
        }

        public void DeactivateCurrentChakra()
        {
            if (activeChakra == ChakraType.None) return;

            if (chakras.ContainsKey(activeChakra))
            {
                chakras[activeChakra].Deactivate();
            }

            ChakraType deactivated = activeChakra;
            activeChakra = ChakraType.None;
            OnChakraDeactivated?.Invoke(deactivated);
        }

        #endregion

        #region Chakra Management

        public ChakraBase GetChakra(ChakraType type)
        {
            return chakras.ContainsKey(type) ? chakras[type] : null;
        }

        public T GetChakra<T>() where T : ChakraBase
        {
            foreach (var chakra in chakras.Values)
            {
                if (chakra is T typedChakra)
                    return typedChakra;
            }
            return null;
        }

        public bool IsChakraUnlocked(ChakraType type)
        {
            return chakras.ContainsKey(type) && chakras[type].IsUnlocked;
        }

        public void UnlockChakra(ChakraType type)
        {
            if (!chakras.ContainsKey(type)) return;

            chakras[type].Unlock();
        }

        private void HandleChakraUnlocked(ChakraBase chakra)
        {
            if (!unlockedChakraOrder.Contains(chakra.Type))
            {
                unlockedChakraOrder.Add(chakra.Type);
                unlockedChakraOrder.Sort((a, b) => ((int)a).CompareTo((int)b));
            }

            // Si no hay chakra seleccionado, seleccionar este
            if (selectedChakra == ChakraType.None)
            {
                SelectChakra(chakra.Type);
            }

            OnChakraUnlocked?.Invoke(chakra.Type);
        }

        /// <summary>
        /// Desbloquea todos los chakras (para testing)
        /// </summary>
        [ContextMenu("Debug: Unlock All Chakras")]
        public void UnlockAllChakras()
        {
            foreach (var chakra in chakras.Values)
            {
                chakra.Unlock();
            }
        }

        /// <summary>
        /// Bloquea todos los chakras (para testing)
        /// </summary>
        [ContextMenu("Debug: Lock All Chakras")]
        public void LockAllChakras()
        {
            foreach (var chakra in chakras.Values)
            {
                chakra.Lock();
            }
            unlockedChakraOrder.Clear();
            selectedChakra = ChakraType.None;
            activeChakra = ChakraType.None;
        }

        #endregion

        #region UI Integration

        /// <summary>
        /// Informacion de un chakra para mostrar en UI
        /// </summary>
        public struct ChakraUIInfo
        {
            public ChakraType Type;
            public string Name;
            public Color Color;
            public bool IsUnlocked;
            public bool IsSelected;
            public bool IsActive;
            public bool IsOnCooldown;
            public float CooldownPercent;
            public string Description;
        }

        /// <summary>
        /// Obtiene informacion de todos los chakras para la UI
        /// </summary>
        public List<ChakraUIInfo> GetAllChakrasInfo()
        {
            var infoList = new List<ChakraUIInfo>();

            // Orden definido para la rueda (segun posicion en el cuerpo)
            ChakraType[] displayOrder = new ChakraType[]
            {
                ChakraType.Float,           // Corona
                ChakraType.Invisibility,    // Tercer Ojo
                ChakraType.EMP,             // Garganta
                ChakraType.Tremor,          // Corazon
                ChakraType.RemoteHack,      // Plexo Solar
                ChakraType.EchoSense,       // Sacro
                ChakraType.Telekinesis,     // Raiz (opcion 1)
                ChakraType.GravityPulse     // Raiz (opcion 2)
            };

            foreach (var type in displayOrder)
            {
                if (chakras.TryGetValue(type, out ChakraBase chakra))
                {
                    infoList.Add(new ChakraUIInfo
                    {
                        Type = type,
                        Name = chakra.Name,
                        Color = chakra.Color,
                        IsUnlocked = chakra.IsUnlocked,
                        IsSelected = selectedChakra == type,
                        IsActive = chakra.IsActive,
                        IsOnCooldown = chakra.IsOnCooldown,
                        CooldownPercent = chakra.CooldownPercent,
                        Description = chakra.GetDescription()
                    });
                }
            }

            return infoList;
        }

        /// <summary>
        /// Obtiene informacion de un chakra especifico
        /// </summary>
        public ChakraUIInfo? GetChakraInfo(ChakraType type)
        {
            if (!chakras.TryGetValue(type, out ChakraBase chakra))
                return null;

            return new ChakraUIInfo
            {
                Type = type,
                Name = chakra.Name,
                Color = chakra.Color,
                IsUnlocked = chakra.IsUnlocked,
                IsSelected = selectedChakra == type,
                IsActive = chakra.IsActive,
                IsOnCooldown = chakra.IsOnCooldown,
                CooldownPercent = chakra.CooldownPercent,
                Description = chakra.GetDescription()
            };
        }

        /// <summary>
        /// Selecciona un chakra desde la UI (llamado por clicks en la rueda)
        /// </summary>
        public void SelectChakraFromUI(ChakraType type)
        {
            SelectChakra(type);
        }

        /// <summary>
        /// Activa el chakra seleccionado desde la UI
        /// </summary>
        /// <summary>
        /// Activa el chakra seleccionado desde la UI
        /// </summary>
        public void ActivateFromUI()
        {
            ToggleOrActivateChakra();
        }

        /// <summary>
        /// Activa el chakra seleccionado desde menus externos
        /// </summary>
        public void ActivateSelectedChakra()
        {
            TryActivateSelectedChakra();
        }

        /// <summary>
        /// Abre la rueda desde la UI
        /// </summary>
        public void OpenWheelFromUI()
        {
            OpenWheel();
        }

        /// <summary>
        /// Cierra la rueda desde la UI
        /// </summary>
        public void CloseWheelFromUI()
        {
            CloseWheel();
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!debugMode) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("=== CHAKRA SYSTEM ===");
            GUILayout.Label($"Energia: {energySystem?.CurrentEnergy:F1} / {energySystem?.MaxEnergy}");
            GUILayout.Label($"Seleccionado: {selectedChakra}");
            GUILayout.Label($"Activo: {activeChakra}");
            GUILayout.Label($"Rueda abierta: {isWheelOpen}");
            GUILayout.Space(10);
            GUILayout.Label("Chakras desbloqueados:");
            foreach (var type in unlockedChakraOrder)
            {
                var chakra = GetChakra(type);
                string status = chakra.IsActive ? " [ACTIVO]" : "";
                string cd = chakra.IsOnCooldown ? $" (CD: {chakra.CooldownRemaining:F1}s)" : "";
                GUILayout.Label($"  - {type}{status}{cd}");
            }
            GUILayout.Space(10);
            GUILayout.Label("Controles:");
            GUILayout.Label($"  [{activateKey}] Activar/Desactivar chakra");
            GUILayout.Label($"  [{wheelKey}] (Hold) Abrir rueda");
            GUILayout.Label($"  [{quickSwitchKey}] Cambio rapido");
            GUILayout.EndArea();
        }

        #endregion
    }

}

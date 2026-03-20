using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NABHI.Chakras.UI
{
    /// <summary>
    /// UI de la rueda de seleccion de Chakras.
    /// Muestra los chakras desbloqueados en un patron circular
    /// y permite seleccionar cual usar.
    /// </summary>
    public class ChakraWheelUI : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private ChakraSystem chakraSystem;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform wheelContainer;
        [SerializeField] private RectTransform selectorIndicator;

        [Header("Prefabs")]
        [SerializeField] private GameObject chakraSlotPrefab;

        [Header("Configuracion")]
        [SerializeField] private float wheelRadius = 150f;
        [SerializeField] private float slotSize = 60f;
        [SerializeField] private float selectedScale = 1.3f;
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color selectedColor = Color.yellow;

        [Header("Animacion")]
        [SerializeField] private float animationSpeed = 10f;

        // Estado
        private Dictionary<ChakraType, ChakraSlotUI> slots = new Dictionary<ChakraType, ChakraSlotUI>();
        private bool isVisible;
        private ChakraType hoveredChakra = ChakraType.None;

        // Orden de los chakras en la rueda (segun posicion del cuerpo)
        private readonly ChakraType[] chakraOrder = new ChakraType[]
        {
            ChakraType.Float,           // Corona (arriba)
            ChakraType.Invisibility,    // Tercer Ojo
            ChakraType.EMP,             // Garganta
            ChakraType.Tremor,          // Corazon
            ChakraType.RemoteHack,      // Plexo Solar
            ChakraType.EchoSense,       // Sacro
            ChakraType.Telekinesis,     // Raiz (opcion 1)
            ChakraType.GravityPulse     // Raiz (opcion 2)
        };

        private void Awake()
        {
            if (chakraSystem == null)
                chakraSystem = FindObjectOfType<ChakraSystem>();

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            // Crear slots
            CreateSlots();

            // Ocultar inicialmente
            SetVisible(false);
        }

        private void OnEnable()
        {
            if (chakraSystem != null)
            {
                chakraSystem.OnWheelToggled += HandleWheelToggled;
                chakraSystem.OnChakraSelected += HandleChakraSelected;
                chakraSystem.OnChakraUnlocked += HandleChakraUnlocked;
            }
        }

        private void OnDisable()
        {
            if (chakraSystem != null)
            {
                chakraSystem.OnWheelToggled -= HandleWheelToggled;
                chakraSystem.OnChakraSelected -= HandleChakraSelected;
                chakraSystem.OnChakraUnlocked -= HandleChakraUnlocked;
            }
        }

        private void Update()
        {
            if (isVisible)
            {
                UpdateHoveredSlot();
                UpdateSlotVisuals();
            }
        }

        private void CreateSlots()
        {
            if (wheelContainer == null || chakraSlotPrefab == null)
            {
                // Crear slots programaticamente si no hay prefab
                CreateSlotsWithoutPrefab();
                return;
            }

            // Limpiar slots existentes
            foreach (Transform child in wheelContainer)
            {
                Destroy(child.gameObject);
            }

            slots.Clear();

            // Crear un slot por cada tipo de chakra
            int totalSlots = chakraOrder.Length;
            for (int i = 0; i < totalSlots; i++)
            {
                ChakraType type = chakraOrder[i];

                // Calcular posicion en circulo
                float angle = (360f / totalSlots) * i - 90f; // -90 para empezar arriba
                float rad = angle * Mathf.Deg2Rad;
                Vector2 position = new Vector2(
                    Mathf.Cos(rad) * wheelRadius,
                    Mathf.Sin(rad) * wheelRadius
                );

                // Crear slot
                GameObject slotObj = Instantiate(chakraSlotPrefab, wheelContainer);
                slotObj.name = $"ChakraSlot_{type}";

                RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = position;
                rectTransform.sizeDelta = Vector2.one * slotSize;

                ChakraSlotUI slotUI = slotObj.GetComponent<ChakraSlotUI>();
                if (slotUI == null)
                    slotUI = slotObj.AddComponent<ChakraSlotUI>();

                slotUI.Initialize(type, GetChakraColor(type), GetChakraName(type));
                slots[type] = slotUI;
            }

            UpdateSlotsUnlockState();
        }

        private void CreateSlotsWithoutPrefab()
        {
            if (wheelContainer == null)
            {
                // Crear container
                GameObject containerObj = new GameObject("WheelContainer");
                containerObj.transform.SetParent(transform);
                wheelContainer = containerObj.AddComponent<RectTransform>();
                wheelContainer.anchoredPosition = Vector2.zero;
            }

            slots.Clear();

            int totalSlots = chakraOrder.Length;
            for (int i = 0; i < totalSlots; i++)
            {
                ChakraType type = chakraOrder[i];

                // Calcular posicion
                float angle = (360f / totalSlots) * i - 90f;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 position = new Vector2(
                    Mathf.Cos(rad) * wheelRadius,
                    Mathf.Sin(rad) * wheelRadius
                );

                // Crear slot simple
                GameObject slotObj = new GameObject($"ChakraSlot_{type}");
                slotObj.transform.SetParent(wheelContainer);

                RectTransform rect = slotObj.AddComponent<RectTransform>();
                rect.anchoredPosition = position;
                rect.sizeDelta = Vector2.one * slotSize;

                Image img = slotObj.AddComponent<Image>();
                img.color = GetChakraColor(type);

                // Texto
                GameObject textObj = new GameObject("Label");
                textObj.transform.SetParent(slotObj.transform);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = new Vector2(slotSize * 2, 20);

                Text text = textObj.AddComponent<Text>();
                text.text = GetChakraName(type);
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 12;
                text.color = Color.white;

                ChakraSlotUI slotUI = slotObj.AddComponent<ChakraSlotUI>();
                slotUI.Initialize(type, GetChakraColor(type), GetChakraName(type));

                slots[type] = slotUI;
            }

            UpdateSlotsUnlockState();
        }

        private void UpdateHoveredSlot()
        {
            // Obtener direccion del mouse desde el centro de la rueda
            Vector2 mousePos = Input.mousePosition;
            Vector2 wheelCenter = wheelContainer.position;
            Vector2 direction = mousePos - wheelCenter;

            if (direction.magnitude < 30f)
            {
                // Muy cerca del centro, no hover
                hoveredChakra = ChakraType.None;
                return;
            }

            // Calcular angulo
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            angle = (angle + 90f) % 360f; // Ajustar para que 0 sea arriba

            // Determinar que slot esta siendo apuntado
            int totalSlots = chakraOrder.Length;
            float segmentSize = 360f / totalSlots;
            int index = Mathf.FloorToInt(angle / segmentSize);
            index = Mathf.Clamp(index, 0, totalSlots - 1);

            ChakraType newHovered = chakraOrder[index];

            if (newHovered != hoveredChakra)
            {
                hoveredChakra = newHovered;

                // Si esta desbloqueado, seleccionarlo
                if (chakraSystem != null && chakraSystem.IsChakraUnlocked(hoveredChakra))
                {
                    chakraSystem.SelectChakra(hoveredChakra);
                }
            }
        }

        private void UpdateSlotVisuals()
        {
            foreach (var kvp in slots)
            {
                ChakraType type = kvp.Key;
                ChakraSlotUI slot = kvp.Value;

                bool isUnlocked = chakraSystem != null && chakraSystem.IsChakraUnlocked(type);
                bool isSelected = chakraSystem != null && chakraSystem.SelectedChakra == type;
                bool isHovered = type == hoveredChakra;

                slot.UpdateVisual(isUnlocked, isSelected, isHovered, selectedScale, animationSpeed);
            }

            // Actualizar indicador de seleccion
            UpdateSelectorIndicator();
        }

        private void UpdateSelectorIndicator()
        {
            if (selectorIndicator == null) return;
            if (chakraSystem == null || chakraSystem.SelectedChakra == ChakraType.None) return;

            if (slots.TryGetValue(chakraSystem.SelectedChakra, out ChakraSlotUI selectedSlot))
            {
                Vector3 targetPos = selectedSlot.transform.position;
                selectorIndicator.position = Vector3.Lerp(
                    selectorIndicator.position,
                    targetPos,
                    animationSpeed * Time.unscaledDeltaTime
                );
            }
        }

        private void UpdateSlotsUnlockState()
        {
            if (chakraSystem == null) return;

            foreach (var kvp in slots)
            {
                bool isUnlocked = chakraSystem.IsChakraUnlocked(kvp.Key);
                kvp.Value.SetUnlocked(isUnlocked);
            }
        }

        #region Event Handlers

        private void HandleWheelToggled(bool isOpen)
        {
            SetVisible(isOpen);
        }

        private void HandleChakraSelected(ChakraType type)
        {
            // Visual feedback ya se maneja en UpdateSlotVisuals
        }

        private void HandleChakraUnlocked(ChakraType type)
        {
            if (slots.TryGetValue(type, out ChakraSlotUI slot))
            {
                slot.SetUnlocked(true);
                slot.PlayUnlockAnimation();
            }
        }

        #endregion

        #region Visibility

        public void SetVisible(bool visible)
        {
            isVisible = visible;

            if (wheelContainer != null)
            {
                wheelContainer.gameObject.SetActive(visible);
            }

            if (selectorIndicator != null)
            {
                selectorIndicator.gameObject.SetActive(visible);
            }
        }

        #endregion

        #region Helpers

        private Color GetChakraColor(ChakraType type)
        {
            switch (type)
            {
                case ChakraType.Float: return new Color(1f, 0.4f, 0.7f);         // Rosa
                case ChakraType.Invisibility: return new Color(0.2f, 0.4f, 1f);   // Azul
                case ChakraType.Tremor: return new Color(0.2f, 0.8f, 0.4f);       // Verde
                case ChakraType.EchoSense: return new Color(1f, 0.6f, 0.2f);      // Naranja
                case ChakraType.RemoteHack: return new Color(1f, 0.9f, 0.2f);     // Amarillo
                case ChakraType.EMP: return new Color(0.4f, 0.8f, 1f);            // Azul claro
                case ChakraType.Telekinesis: return new Color(0.9f, 0.2f, 0.2f);  // Rojo
                case ChakraType.GravityPulse: return new Color(0.9f, 0.2f, 0.2f); // Rojo
                default: return Color.white;
            }
        }

        private string GetChakraName(ChakraType type)
        {
            switch (type)
            {
                case ChakraType.Float: return "Levitacion";
                case ChakraType.Invisibility: return "Invisibilidad";
                case ChakraType.Tremor: return "Temblor";
                case ChakraType.EchoSense: return "Eco Sensitivo";
                case ChakraType.RemoteHack: return "Hacker";
                case ChakraType.EMP: return "PEM";
                case ChakraType.Telekinesis: return "Telecinesis";
                case ChakraType.GravityPulse: return "Pulso Gravit.";
                default: return "???";
            }
        }

        #endregion
    }

    /// <summary>
    /// Componente para cada slot individual en la rueda
    /// </summary>
    public class ChakraSlotUI : MonoBehaviour
    {
        private ChakraType chakraType;
        private Color chakraColor;
        private string chakraName;

        private Image backgroundImage;
        private Image iconImage;
        private Text nameText;

        private bool isUnlocked;
        private float currentScale = 1f;

        public void Initialize(ChakraType type, Color color, string name)
        {
            chakraType = type;
            chakraColor = color;
            chakraName = name;

            backgroundImage = GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = color;
            }

            nameText = GetComponentInChildren<Text>();
            if (nameText != null)
            {
                nameText.text = name;
            }
        }

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;

            if (backgroundImage != null)
            {
                Color c = chakraColor;
                c.a = unlocked ? 1f : 0.3f;
                backgroundImage.color = c;
            }
        }

        public void UpdateVisual(bool unlocked, bool selected, bool hovered, float selectedScale, float animSpeed)
        {
            // Escala
            float targetScale = 1f;
            if (selected) targetScale = selectedScale;
            else if (hovered) targetScale = 1.1f;

            currentScale = Mathf.Lerp(currentScale, targetScale, animSpeed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.one * currentScale;

            // Color
            if (backgroundImage != null)
            {
                Color targetColor = chakraColor;
                if (!unlocked)
                {
                    targetColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
                else if (selected)
                {
                    targetColor = Color.Lerp(chakraColor, Color.white, 0.3f);
                }

                backgroundImage.color = Color.Lerp(backgroundImage.color, targetColor, animSpeed * Time.unscaledDeltaTime);
            }
        }

        public void PlayUnlockAnimation()
        {
            // Animacion simple de escala
            StartCoroutine(UnlockAnimationRoutine());
        }

        private System.Collections.IEnumerator UnlockAnimationRoutine()
        {
            float duration = 0.5f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Escala que crece y vuelve
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f;
                transform.localScale = Vector3.one * scale;

                yield return null;
            }

            transform.localScale = Vector3.one;
        }
    }
}

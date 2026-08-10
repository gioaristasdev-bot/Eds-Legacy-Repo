using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using NABHI.Chakras;
using NABHI.Character;

namespace NABHI.Chakras.UI
{
    /// <summary>
    /// Selector rapido de chakras con LB + stick derecho.
    /// - Mantener LB abre el panel y activa slow-mo.
    /// - Inclinar el stick derecho resalta el chakra en esa direccion.
    /// - Sin stick (deadzone): Telekinesis seleccionada por defecto.
    /// - Soltar LB confirma la seleccion y cierra el panel.
    /// </summary>
    public class ChakraQuickSelect : MonoBehaviour
    {
        [Header("Sistema")]
        [SerializeField] private ChakraSystem chakraSystem;
        [SerializeField] private CharacterController2D characterController;

        [Header("Panel Visual")]
        [Tooltip("Padre que contiene todo el menu (ej: ChakraMenuPanel_V2). Se activa al abrir.")]
        [SerializeField] private GameObject parentPanel;
        [Tooltip("Unico hijo del padre que debe mostrarse (ej: PanelChakras). Sus hermanos se ocultan.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Slots del panel — arrastrar cada hijo de PanelChakras")]
        [SerializeField] private Transform slotRemoteHack;    // arriba derecha  UP-RIGHT
        [SerializeField] private Transform slotEchoSense;     // derecha          RIGHT
        [SerializeField] private Transform slotFloat;         // abajo derecha   DOWN-RIGHT
        [SerializeField] private Transform slotEMP;           // arriba izquierda UP-LEFT
        [SerializeField] private Transform slotTremor;        // izquierda        LEFT
        [SerializeField] private Transform slotInvisibility;  // abajo izquierda DOWN-LEFT
        [SerializeField] private Transform slotTelekinesis;   // centro           CENTER

        [Header("Input")]
        [Tooltip("LB del mando Xbox = JoystickButton4")]
        [SerializeField] private KeyCode holdButton = KeyCode.JoystickButton4;
        [SerializeField] private string rightStickHAxis = "RightStickHorizontal";
        [SerializeField] private string rightStickVAxis = "RightStickVertical";
        [Tooltip("Magnitud minima del stick para registrar direccion")]
        [SerializeField] private float stickDeadzone = 0.25f;

        [Header("Comportamiento")]
        [Tooltip("Escala de tiempo mientras el selector esta abierto (0.2 = slow-mo estilo GTA)")]
        [SerializeField] private float slowMoScale = 0.2f;
        [Tooltip("Escala del slot destacado")]
        [SerializeField] private float highlightScale = 1.25f;

        // Estado interno
        private bool isOpen = false;
        private ChakraType hoveredType = ChakraType.None;
        private Transform hoveredSlot = null;
        private float originalTimeScale = 1f;

        // Hermanos de panelRoot que ocultamos al abrir (para restaurarlos al cerrar)
        private readonly List<GameObject> hiddenSiblings = new List<GameObject>();

        // Image de fondo de panelRoot: se deshabilita en modo selector rapido
        private Image panelRootImage;
        // Image del padre: alpha 0 en modo selector rapido, restaurado al cerrar
        private Image parentPanelImage;
        private Color parentPanelOriginalColor;

        private void Start()
        {
            if (chakraSystem == null)
                chakraSystem = FindFirstObjectByType<ChakraSystem>();
            if (characterController == null)
                characterController = FindFirstObjectByType<CharacterController2D>();

            if (parentPanel != null)
                parentPanel.SetActive(false);

            if (panelRoot != null)
                panelRootImage = panelRoot.GetComponent<Image>();

            if (parentPanel != null)
            {
                parentPanelImage = parentPanel.GetComponent<Image>();
                if (parentPanelImage != null)
                    parentPanelOriginalColor = parentPanelImage.color;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(holdButton) && !isOpen)
                Open();
            else if (Input.GetKeyUp(holdButton) && isOpen)
                Close();

            if (isOpen)
            {
                // Evitar que el EventSystem navegue botones con el stick izquierdo
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                    EventSystem.current.SetSelectedGameObject(null);

                HandleStickInput();
            }
        }

        // ── Abrir ───────────────────────────────────────────────────────────
        private void Open()
        {
            isOpen = true;

            // 1. Activar el padre para que el hijo sea visible
            if (parentPanel != null)
            {
                parentPanel.SetActive(true);

                // 2. Ocultar todos los hermanos de panelRoot dentro del padre
                hiddenSiblings.Clear();
                foreach (Transform child in parentPanel.transform)
                {
                    if (child.gameObject == panelRoot) continue;
                    if (!child.gameObject.activeSelf) continue;
                    child.gameObject.SetActive(false);
                    hiddenSiblings.Add(child.gameObject);
                }
            }

            // 3. Asegurar que panelRoot esta activo y ocultar fondos
            panelRoot?.SetActive(true);
            if (panelRootImage != null) panelRootImage.enabled = false;
            if (parentPanelImage != null)
            {
                var c = parentPanelOriginalColor;
                c.a = 0f;
                parentPanelImage.color = c;
            }

            originalTimeScale = Time.timeScale;
            Time.timeScale = slowMoScale;

            characterController?.SetInputBlocked(true);
            chakraSystem?.SetExternalMenuOpen(true);

            // Telekinesis es la seleccion por defecto (stick en reposo)
            SetHovered(ChakraType.Telekinesis, slotTelekinesis);
        }

        // ── Cerrar ──────────────────────────────────────────────────────────
        private void Close()
        {
            isOpen = false;

            // Confirmar seleccion
            if (hoveredType != ChakraType.None && chakraSystem != null
                && chakraSystem.IsChakraUnlocked(hoveredType))
            {
                chakraSystem.SelectChakraFromUI(hoveredType);

                if (hoveredType == ChakraType.Float)
                    chakraSystem.TryActivateSelectedChakra();
                else
                    chakraSystem.DeactivateCurrentChakra();
            }

            ClearHover();

            // Restaurar fondos y hermanos antes de desactivar el padre
            if (panelRootImage != null) panelRootImage.enabled = true;
            if (parentPanelImage != null) parentPanelImage.color = parentPanelOriginalColor;
            foreach (var sibling in hiddenSiblings)
                if (sibling != null) sibling.SetActive(true);
            hiddenSiblings.Clear();

            parentPanel?.SetActive(false);

            Time.timeScale = originalTimeScale;
            chakraSystem?.SetExternalMenuOpen(false);
            characterController?.SetInputBlocked(false);
            characterController?.SkipInputForFrames(1);
        }

        // ── Input del stick derecho ─────────────────────────────────────────
        private void HandleStickInput()
        {
            float h = TryGetAxis(rightStickHAxis);
            float v = TryGetAxis(rightStickVAxis);
            Vector2 stick = new Vector2(h, v);

            ChakraType newType;
            Transform newSlot;

            if (stick.magnitude < stickDeadzone)
            {
                newType = ChakraType.Telekinesis;
                newSlot = slotTelekinesis;
            }
            else
            {
                float angle = Mathf.Atan2(stick.y, stick.x) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;
                GetChakraForAngle(angle, out newType, out newSlot);
            }

            if (newType != hoveredType)
                SetHovered(newType, newSlot);
        }

        // ── Mapeo angulo → chakra ───────────────────────────────────────────
        //
        //   EMP (90-150)      RemoteHack (30-90)
        //   Tremor (150-210)  Telekinesis  EchoSense (330-30)
        //   Invisibility (210-270)  Float (270-330)
        //
        private void GetChakraForAngle(float angle, out ChakraType type, out Transform slot)
        {
            if (angle >= 330f || angle < 30f)
            {
                type = ChakraType.EchoSense;
                slot = slotEchoSense;
            }
            else if (angle < 90f)
            {
                type = ChakraType.RemoteHack;
                slot = slotRemoteHack;
            }
            else if (angle < 150f)
            {
                type = ChakraType.EMP;
                slot = slotEMP;
            }
            else if (angle < 210f)
            {
                type = ChakraType.Tremor;
                slot = slotTremor;
            }
            else if (angle < 270f)
            {
                type = ChakraType.Invisibility;
                slot = slotInvisibility;
            }
            else
            {
                type = ChakraType.Float;
                slot = slotFloat;
            }
        }

        // ── Highlight visual ────────────────────────────────────────────────
        private void SetHovered(ChakraType type, Transform slot)
        {
            ClearHover();

            hoveredType = type;
            hoveredSlot = slot;

            if (hoveredSlot != null)
                hoveredSlot.localScale = Vector3.one * highlightScale;

            if (chakraSystem != null && chakraSystem.IsChakraUnlocked(type))
                chakraSystem.SelectChakra(type);
        }

        private void ClearHover()
        {
            if (hoveredSlot != null)
                hoveredSlot.localScale = Vector3.one;

            hoveredSlot = null;
            hoveredType = ChakraType.None;
        }

        private float TryGetAxis(string axisName)
        {
            try { return Input.GetAxisRaw(axisName); }
            catch { return 0f; }
        }
    }
}

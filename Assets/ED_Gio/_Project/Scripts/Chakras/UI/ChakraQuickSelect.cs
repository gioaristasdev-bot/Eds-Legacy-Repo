using UnityEngine;
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

        [Header("Panel Visual (ChakraMenuPanel_V2 existente)")]
        [SerializeField] private GameObject panelRoot;

        [Header("Slots del panel — arrastrar cada hijo de PanelChakras")]
        [SerializeField] private Transform slotRemoteHack;    // arriba derecha  ↗
        [SerializeField] private Transform slotEchoSense;     // derecha          →
        [SerializeField] private Transform slotFloat;         // abajo derecha   ↘
        [SerializeField] private Transform slotEMP;           // arriba izquierda ↖
        [SerializeField] private Transform slotTremor;        // izquierda        ←
        [SerializeField] private Transform slotInvisibility;  // abajo izquierda ↙
        [SerializeField] private Transform slotTelekinesis;   // centro           ●

        [Header("Input")]
        [Tooltip("LB del mando Xbox = JoystickButton4")]
        [SerializeField] private KeyCode holdButton = KeyCode.JoystickButton4;
        [SerializeField] private string rightStickHAxis = "RightStickHorizontal";
        [SerializeField] private string rightStickVAxis = "RightStickVertical";
        [Tooltip("Magnitud minima del stick para registrar direccion")]
        [SerializeField] private float stickDeadzone = 0.25f;

        [Header("Comportamiento")]
        [Tooltip("Escala de tiempo mientras la rueda esta abierta (0.2 = slow-mo estilo GTA)")]
        [SerializeField] private float slowMoScale = 0.2f;
        [Tooltip("Escala del slot destacado")]
        [SerializeField] private float highlightScale = 1.25f;

        // ── Estado interno ──────────────────────────────────────────────────
        private bool isOpen = false;
        private ChakraType hoveredType = ChakraType.None;
        private Transform hoveredSlot = null;
        private float originalTimeScale = 1f;

        // ── Inicializacion ──────────────────────────────────────────────────
        private void Start()
        {
            if (chakraSystem == null)
                chakraSystem = FindObjectOfType<ChakraSystem>();
            if (characterController == null)
                characterController = FindObjectOfType<CharacterController2D>();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        // ── Loop principal ──────────────────────────────────────────────────
        private void Update()
        {
            if (Input.GetKeyDown(holdButton) && !isOpen)
                Open();
            else if (Input.GetKeyUp(holdButton) && isOpen)
                Close();

            if (isOpen)
                HandleStickInput();
        }

        // ── Abrir / Cerrar ──────────────────────────────────────────────────
        private void Open()
        {
            isOpen = true;

            panelRoot?.SetActive(true);

            originalTimeScale = Time.timeScale;
            Time.timeScale = slowMoScale;

            characterController?.SetInputBlocked(true);
            chakraSystem?.SetExternalMenuOpen(true);

            // Telekinesis es la seleccion por defecto (stick en reposo)
            SetHovered(ChakraType.Telekinesis, slotTelekinesis);
        }

        private void Close()
        {
            isOpen = false;

            // Confirmar seleccion si el chakra esta desbloqueado
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

            Time.timeScale = originalTimeScale;
            panelRoot?.SetActive(false);

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
                // Sin direccion clara → Telekinesis (centro)
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
        // Los 6 sectores de 60° empiezan desde la derecha (0°) en sentido antihorario.
        //
        //          EMP (90°–150°)   RemoteHack (30°–90°)
        //  Tremor (150°–210°)  ●Telekinesis  EchoSense (330°–30°)
        //      Invisibility (210°–270°)  Float (270°–330°)
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

            // Preview en ChakraSystem (sin activar)
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

        // ── Helper para ejes ────────────────────────────────────────────────
        private float TryGetAxis(string axisName)
        {
            try { return Input.GetAxisRaw(axisName); }
            catch { return 0f; }
        }
    }
}

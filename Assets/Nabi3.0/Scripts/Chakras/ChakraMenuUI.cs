using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NABHI.Chakras;
using NABHI.Character;

public class ChakraMenuUI : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject chakraMenuPanel;
    public ChakraSystem chakraSystem;
    public CharacterController2D characterController;

    [Header("Primer bot�n seleccionado")]
    public GameObject firstSelectedButton;

    [Header("Botones de Chakras")]
    public Button floatButton;
    public Button invisibilityButton;
    public Button tremorButton;
    public Button echoSenseButton;
    public Button remoteHackButton;
    public Button empButton;
    public Button telekinesisButton;
    public Button gravityPulseButton;

    bool menuOpen = false;

    void Start()
    {
        chakraMenuPanel.SetActive(false);

        // Auto-buscar CharacterController2D si no fue asignado en el Inspector
        if (characterController == null)
        {
            characterController = FindObjectOfType<CharacterController2D>();
            if (characterController == null)
                Debug.LogWarning("[ChakraMenuUI] No se encontro CharacterController2D en la escena.");
        }

        // Asignar funciones a los botones
        floatButton.onClick.AddListener(() => SelectChakra(ChakraType.Float));
        invisibilityButton.onClick.AddListener(() => SelectChakra(ChakraType.Invisibility));
        tremorButton.onClick.AddListener(() => SelectChakra(ChakraType.Tremor));
        echoSenseButton.onClick.AddListener(() => SelectChakra(ChakraType.EchoSense));
        remoteHackButton.onClick.AddListener(() => SelectChakra(ChakraType.RemoteHack));
        empButton.onClick.AddListener(() => SelectChakra(ChakraType.EMP));
        telekinesisButton.onClick.AddListener(() => SelectChakra(ChakraType.Telekinesis));
        gravityPulseButton.onClick.AddListener(() => SelectChakra(ChakraType.GravityPulse));
    }

    void Update()
    {
        // Abrir men� con TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMenu();
        }

        // Abrir men� con SELECT del gamepad
        if (Input.GetKeyDown(KeyCode.JoystickButton6))
        {
            ToggleMenu();
        }

        // Cerrar men� con B / Cancel
        if (menuOpen && Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        menuOpen = !menuOpen;

        chakraMenuPanel.SetActive(menuOpen);

        // Notificar a ChakraSystem para bloquear/desbloquear su input de activacion (boton B)
        // Evita que B cierre el menu Y active/desactive el chakra al mismo tiempo
        chakraSystem.SetExternalMenuOpen(menuOpen);

        if (menuOpen)
        {
            // Bloquear TODA accion del personaje mientras el menu esta abierto.
            // Evita saltos, disparos u otras acciones con los botones del menu.
            characterController?.SetInputBlocked(true);
            Time.timeScale = 0f;

            // Seleccionar primer bot�n autom�ticamente
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
        else
        {
            // Desbloquear input + 1 frame de gracia para absorber el boton de cierre
            characterController?.SetInputBlocked(false);
            characterController?.SkipInputForFrames(1);
            Time.timeScale = 1f;
        }
    }

    void SelectChakra(ChakraType type)
    {
        chakraSystem.SelectChakraFromUI(type);

        // Solo Float se activa automaticamente desde el menu (modo vuelo persistente).
        // El resto se activa con B/E despues de cerrar el menu.
        if (type == ChakraType.Float)
        {
            chakraSystem.TryActivateSelectedChakra();
        }
        else
        {
            // Al seleccionar cualquier otro chakra, desactivar el activo actual (ej: Float).
            // No tiene sentido volar y ademas tener Invisibilidad/Tremor listo al mismo tiempo.
            chakraSystem.DeactivateCurrentChakra();
        }

        ToggleMenu();
    }
}
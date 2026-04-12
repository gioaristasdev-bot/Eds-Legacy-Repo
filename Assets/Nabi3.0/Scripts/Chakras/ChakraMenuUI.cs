using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using NABHI.Chakras;
using NABHI.Character;

public class ChakraMenuUI : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject chakraMenuPanel;
    public ChakraSystem chakraSystem;
    public CharacterController2D characterController;

    [Header("Primer boton seleccionado")]
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

    [Header("Feedback Visual")]
    public TextMeshProUGUI chakraTitleText;
    public TextMeshProUGUI chakraDescriptionText;
    public Image chakraSymbolImage;   // Imagen del simbolo del chakra (debajo del titulo)
    public Image capsulaImage;        // Cambia de color segun el chakra
    // Orden: 0=Float, 1=Invisibility, 2=Tremor, 3=EchoSense, 4=RemoteHack, 5=EMP, 6=Telekinesis, 7=GravityPulse
    public GameObject[] edImages;

    [Header("Datos por Chakra")]
    // Orden: Float, Invisibility, Tremor, EchoSense, RemoteHack, EMP, Telekinesis, GravityPulse
    public ChakraVisualData[] chakraData;

    [System.Serializable]
    public class ChakraVisualData
    {
        public string title;
        [TextArea(2, 4)] public string description;
        public Color color;
        public Sprite symbol;
    }

    bool menuOpen = false;

    // Para detectar cambio de seleccion con gamepad en Update
    GameObject lastSelectedButton;

    // Mapeo boton -> tipo
    Button[] buttonOrder;
    ChakraType[] typeOrder;

    void Start()
    {
        chakraMenuPanel.SetActive(false);

        if (characterController == null)
        {
            characterController = FindObjectOfType<CharacterController2D>();
            if (characterController == null)
                Debug.LogWarning("[ChakraMenuUI] No se encontro CharacterController2D en la escena.");
        }

        // Tabla de botones y sus chakras (mismo orden que chakraData)
        buttonOrder = new Button[]
        {
            floatButton, invisibilityButton, tremorButton, echoSenseButton,
            remoteHackButton, empButton, telekinesisButton, gravityPulseButton
        };
        typeOrder = new ChakraType[]
        {
            ChakraType.Float, ChakraType.Invisibility, ChakraType.Tremor, ChakraType.EchoSense,
            ChakraType.RemoteHack, ChakraType.EMP, ChakraType.Telekinesis, ChakraType.GravityPulse
        };

        // Click: seleccionar chakra y cerrar menu
        for (int i = 0; i < buttonOrder.Length; i++)
        {
            int idx = i; // captura por valor
            if (buttonOrder[idx] != null)
                buttonOrder[idx].onClick.AddListener(() => SelectChakra(typeOrder[idx]));
        }

        // Hover con mouse: preview sin cerrar
        for (int i = 0; i < buttonOrder.Length; i++)
        {
            int idx = i;
            if (buttonOrder[idx] != null)
                AddPointerEnterTrigger(buttonOrder[idx], typeOrder[idx]);
        }

        // Inicializar datos por defecto si el array esta vacio o sin titulo en el primer elemento
        if (chakraData == null || chakraData.Length == 0 ||
            string.IsNullOrEmpty(chakraData[0].title))
            InitDefaultChakraData();

        if (chakraTitleText == null)
            Debug.LogWarning("[ChakraMenuUI] chakraTitleText no asignado en Inspector.");
        if (chakraDescriptionText == null)
            Debug.LogWarning("[ChakraMenuUI] chakraDescriptionText no asignado en Inspector.");
    }

    void AddPointerEnterTrigger(Button button, ChakraType type)
    {
        var trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener((_) => UpdateVisuals(type));
        trigger.triggers.Add(entry);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.JoystickButton6))
            ToggleMenu();

        if (menuOpen && Input.GetKeyDown(KeyCode.JoystickButton1))
            ToggleMenu();

        // Preview con gamepad: detectar cambio de boton seleccionado
        if (menuOpen)
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected != lastSelectedButton)
            {
                lastSelectedButton = selected;
                for (int i = 0; i < buttonOrder.Length; i++)
                {
                    if (buttonOrder[i] != null && buttonOrder[i].gameObject == selected)
                    {
                        UpdateVisuals(typeOrder[i]);
                        break;
                    }
                }
            }
        }
    }

    void ToggleMenu()
    {
        menuOpen = !menuOpen;
        chakraMenuPanel.SetActive(menuOpen);
        chakraSystem.SetExternalMenuOpen(menuOpen);

        if (menuOpen)
        {
            lastSelectedButton = null;
            characterController?.SetInputBlocked(true);
            Time.timeScale = 0f;

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);

            // Mostrar el chakra actualmente seleccionado al abrir
            var current = chakraSystem.SelectedChakra;
            if (current != ChakraType.None)
                UpdateVisuals(current);
        }
        else
        {
            characterController?.SetInputBlocked(false);
            characterController?.SkipInputForFrames(1);
            Time.timeScale = 1f;
        }
    }

    void SelectChakra(ChakraType type)
    {
        chakraSystem.SelectChakraFromUI(type);

        // Float se activa automaticamente. El resto se activa con B/E despues de cerrar.
        if (type == ChakraType.Float)
            chakraSystem.TryActivateSelectedChakra();
        else
            chakraSystem.DeactivateCurrentChakra();

        ToggleMenu();
    }

    void UpdateVisuals(ChakraType type)
    {
        int index = ChakraTypeToIndex(type);
        if (index < 0 || chakraData == null || index >= chakraData.Length) return;

        var data = chakraData[index];

        if (chakraTitleText != null)
            chakraTitleText.text = data.title;

        if (chakraDescriptionText != null)
            chakraDescriptionText.text = data.description;

        if (chakraSymbolImage != null && data.symbol != null)
            chakraSymbolImage.sprite = data.symbol;

        if (capsulaImage != null)
            capsulaImage.color = data.color;

        if (edImages != null)
        {
            for (int i = 0; i < edImages.Length; i++)
            {
                if (edImages[i] != null)
                    edImages[i].SetActive(i == index);
            }
        }
    }

    int ChakraTypeToIndex(ChakraType type)
    {
        switch (type)
        {
            case ChakraType.Float:        return 0;
            case ChakraType.Invisibility: return 1;
            case ChakraType.Tremor:       return 2;
            case ChakraType.EchoSense:    return 3;
            case ChakraType.RemoteHack:   return 4;
            case ChakraType.EMP:          return 5;
            case ChakraType.Telekinesis:  return 6;
            case ChakraType.GravityPulse: return 7;
            default:                      return -1;
        }
    }

    void InitDefaultChakraData()
    {
        chakraData = new ChakraVisualData[]
        {
            new ChakraVisualData
            {
                title = "FLOAT",
                description = "Activa la levitacion. ED flota y puede alcanzar zonas inaccesibles.",
                color = new Color(1f, 0.45f, 0.78f)
            },
            new ChakraVisualData
            {
                title = "INVISIBILITY",
                description = "ED se vuelve invisible para los enemigos durante un breve periodo.",
                color = new Color(0.25f, 0.41f, 0.95f)
            },
            new ChakraVisualData
            {
                title = "TREMOR",
                description = "Genera una onda sismica que aturde a los enemigos cercanos.",
                color = new Color(0.20f, 0.80f, 0.30f)
            },
            new ChakraVisualData
            {
                title = "ECHO SENSE",
                description = "Percepcion extrasensorial: revela enemigos y objetos ocultos.",
                color = new Color(1f, 0.50f, 0.10f)
            },
            new ChakraVisualData
            {
                title = "REMOTE HACK",
                description = "Hackea dispositivos y puertas a distancia sin necesidad de contacto.",
                color = new Color(1f, 0.90f, 0.10f)
            },
            new ChakraVisualData
            {
                title = "EMP",
                description = "Pulso electromagnetico que desactiva temporalmente sistemas electronicos.",
                color = new Color(0.30f, 0.82f, 1f)
            },
            new ChakraVisualData
            {
                title = "TELEKINESIS",
                description = "Mueve objetos y enemigos con la mente a distancia.",
                color = new Color(0.88f, 0.10f, 0.10f)
            },
            new ChakraVisualData
            {
                title = "GRAVITY PULSE",
                description = "Emite una onda gravitacional que repele todo lo que rodea a ED.",
                color = new Color(0.75f, 0.10f, 0.22f)
            },
        };
    }
}

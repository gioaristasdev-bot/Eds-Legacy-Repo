using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NABHI.Chakras;

public class ChakraMenuUI : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject chakraMenuPanel;
    public ChakraSystem chakraSystem;

    [Header("Primer botón seleccionado")]
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
        // Abrir menú con TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMenu();
        }

        // Abrir menú con SELECT del gamepad
        if (Input.GetKeyDown(KeyCode.JoystickButton6))
        {
            ToggleMenu();
        }

        // Cerrar menú con B / Cancel
        if (menuOpen && Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        menuOpen = !menuOpen;

        chakraMenuPanel.SetActive(menuOpen);

        if (menuOpen)
        {
            Time.timeScale = 0f;

            // Seleccionar primer botón automáticamente
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    void SelectChakra(ChakraType type)
    {
        chakraSystem.SelectChakraFromUI(type);
        chakraSystem.TryActivateSelectedChakra();

        ToggleMenu();
    }
}
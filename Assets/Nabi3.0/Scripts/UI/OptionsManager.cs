using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class OptionsManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Brightness")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Image brightnessOverlay;

    [Header("Language")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private void Start()
    {
        LoadSettings();
        AddListeners();
    }

    #region AUDIO

    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    #endregion

    #region BRIGHTNESS

    public void SetBrightness(float value)
    {
        if (brightnessOverlay != null)
        {
            brightnessOverlay.color = new Color(0, 0, 0, 1 - value);
        }

        PlayerPrefs.SetFloat("Brightness", value);
    }

    #endregion

    #region LANGUAGE

    public void SetLanguage(int index)
    {
        PlayerPrefs.SetInt("Language", index);
        ApplyLanguage(index);
    }

    private void ApplyLanguage(int index)
    {
        switch (index)
        {
            case 0:
                Debug.Log("Español");
                break;
            case 1:
                Debug.Log("English");
                break;
        }
    }

    #endregion

    #region LOAD & LISTENERS

    private void AddListeners()
    {
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        brightnessSlider.onValueChanged.AddListener(SetBrightness);
        languageDropdown.onValueChanged.AddListener(SetLanguage);
    }

    private void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);
        float brightness = PlayerPrefs.GetFloat("Brightness", 1f);
        int language = PlayerPrefs.GetInt("Language", 0);

        masterSlider.value = master;
        musicSlider.value = music;
        sfxSlider.value = sfx;
        brightnessSlider.value = brightness;
        languageDropdown.value = language;

        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
        SetBrightness(brightness);
        ApplyLanguage(language);
    }

    #endregion
}

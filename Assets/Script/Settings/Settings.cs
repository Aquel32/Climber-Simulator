using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class Settings : MonoBehaviour
{
    public static Settings Instance;
    private void Awake() { Instance = this; }

    public AudioMixer mixer;

    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToogle;
    public Slider volumeSlider;

    Resolution[] resolutions;

    public GameObject uiGroup;



    public bool displayFPSCounter;
    public float timer, refresh, avgFramerate;
    string display = "{0} FPS";
    public TextMeshProUGUI m_Text;

    private void Start()
    {
        DontDestroyOnLoad(transform.parent.gameObject);
        CloseSettingsMenu();

        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void Update()
    {
        if (displayFPSCounter)
        {
            //Change smoothDeltaTime to deltaTime or fixedDeltaTime to see the difference
            float timelapse = Time.smoothDeltaTime;
            timer = timer <= 0 ? refresh : timer -= timelapse;

            if (timer <= 0) avgFramerate = (int)(1f / timelapse);
            m_Text.text = string.Format(display, avgFramerate.ToString());
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetVolume(float volume)
    {
        mixer.SetFloat("volume", volume);
    }
    
    public void SetSensivity(float sensitivity)
    {
        if(PlayerCamera.Instance != null)
        {
            PlayerCamera.Instance.rotationSpeed = sensitivity;
            PlayerCamera.Instance.rotationSpeed = sensitivity;
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void OpenSettingsMenu()
    {
        uiGroup.SetActive(true);
    }

    public void CloseSettingsMenu()
    {
        uiGroup.SetActive(false);
    }

    public void SetFPSCounter(bool state)
    {
        displayFPSCounter = state;
        m_Text.gameObject.SetActive(state);
    }
}

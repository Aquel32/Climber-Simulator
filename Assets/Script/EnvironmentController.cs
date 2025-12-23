using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class EnvironmentController : MonoBehaviourPunCallbacks
{
    public Light sun;

    public float dayLengthInMinutes = 5.0f;
    public float sunRotationY = 170.0f;

    [Range(0, 1)]
    public float currentTimeOfDay = 0.25f;

    public Volume volume;
    private PhysicallyBasedSky pbsSky;
    private Fog fog;
    public float nightSkyExposure = 55f;

    [Header("Weather")]
    public WeatherState[] weatherStates;

    public float minWeatherTime = 30.0f;
    public float maxWeatherTime = 120.0f;

    private float weatherTimer;
    private int currentWeatheIndex = -1;
    private WeatherState currentWeather;

    [System.Serializable]
    public class WeatherState
    {
        public string name;

        public Gradient sunColor;
        public Gradient ambientColor;
        public Gradient fogColor;

        public AnimationCurve sunIntensity;
        public float fogDensity;

        [HideInInspector]
        public GameObject weatherParticleInstance;
    }


    void Start()
    {
        if (volume == null)
        {
            Debug.LogError("Global Volume is not assigned in the Inspector!");
            return;
        }

        if (!volume.profile.TryGet<PhysicallyBasedSky>(out pbsSky))
        {
            Debug.LogError("Could not find 'PhysicallyBasedSky' component on the Volume Profile.");
        }

        if (!volume.profile.TryGet<Fog>(out fog))
        {
            Debug.LogError("Could not find 'Fog' component on the Volume Profile.");
        }

        // Safety check
        //if (weatherStates.Length == 0)
        //{
        //    enabled = false;
        //    return;
        //}

        //// Instantiate and hide all weather particle prefabs
        //foreach (var state in weatherStates)
        //{

        //}

        //weatherTimer = Random.Range(minWeatherTime, maxWeatherTime);
        //SetWeather(0);
    }

    void Update()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            UpdateTimeOfDay();
        }

        HandleTimeOfDay();
        //HandleWeather();
    }

    void UpdateTimeOfDay()
    {
        currentTimeOfDay += (Time.deltaTime / (dayLengthInMinutes * 60.0f));
        currentTimeOfDay %= 1.0f;

        photonView.RPC("UpdateTimeOfDayRPC", RpcTarget.AllBuffered, currentTimeOfDay);
    }

    [PunRPC]
    public void UpdateTimeOfDayRPC(float newTimeOfDay)
    {
        currentTimeOfDay = newTimeOfDay;
    }

    void HandleTimeOfDay()
    {
        float sunAngle = (currentTimeOfDay * 360.0f) - 90.0f;
        sun.transform.localRotation = Quaternion.Euler(sunAngle, sunRotationY, 0);


        UpdateLighting(currentTimeOfDay);
    }

    //void HandleWeather()
    //{
    //    weatherTimer -= Time.deltaTime;
    //    if (weatherTimer <= 0)
    //    {
    //        int nextWeatherIndex = Random.Range(0, weatherStates.Length);
    //        if (nextWeatherIndex == currentWeatheIndex)
    //        {
    //            nextWeatherIndex = (nextWeatherIndex + 1) % weatherStates.Length;
    //        }

    //        SetWeather(nextWeatherIndex);

    //        weatherTimer = Random.Range(minWeatherTime, maxWeatherTime);
    //    }
    //}

    void SetWeather(int index)
    {
        if (index < 0 || index >= weatherStates.Length) return;

        //if (currentWeather != null && currentWeather.weatherParticleInstance != null)
        //{
        //    currentWeather.weatherParticleInstance.SetActive(false);
        //}

        currentWeatheIndex = index;
        currentWeather = weatherStates[currentWeatheIndex];

        //if (currentWeather.weatherParticleInstance != null)
        //{
        //    currentWeather.weatherParticleInstance.SetActive(true);
        //}

        UpdateLighting(currentTimeOfDay);
    }

    void UpdateLighting(float timePercent)
    {
        float nightModifier = Math.Clamp(((Math.Abs(timePercent - 0.5f) * 30) - 7f), 0, 1);

        pbsSky.spaceEmissionMultiplier.value = nightModifier * nightSkyExposure;
        pbsSky.spaceRotation.value = sun.transform.localRotation.eulerAngles;

        float color = (256f + (128f * -nightModifier))/256f;
        fog.tint.value = new Color(color, color, color, 1);
        

        //if (currentWeather == null) return;

        //RenderSettings.ambientLight = currentWeather.ambientColor.Evaluate(timePercent);
        //RenderSettings.fogColor = currentWeather.fogColor.Evaluate(timePercent);
        //RenderSettings.fogDensity = currentWeather.fogDensity;

        //sun.color = currentWeather.sunColor.Evaluate(timePercent);
        //sun.intensity = currentWeather.sunIntensity.Evaluate(timePercent);

    }
}
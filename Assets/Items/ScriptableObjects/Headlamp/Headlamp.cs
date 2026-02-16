using Photon.Pun;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Headlamp : MonoBehaviourPunCallbacks, IGearScript
{
    public Light light;
    bool state = false;

    public void Deinitialize()
    {
        print("DEINITALIZE HEADLAMP");
        state = false;
        UpdateState();
    }

    public void Initialize()
    {
        print("INITIALIZE HEADLAMP");
        state = false;
        UpdateState();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            state = !state;
            UpdateState();
        }
    }

    void UpdateState()
    {
        light.intensity = state ? 4 : 0;
    }
}

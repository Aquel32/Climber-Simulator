using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallSystem : MonoBehaviour
{
    public static FallSystem Instance;

    float chance;
    private float baseChance;

    private float toolModifier;
    private float heightModifier;
    private float steepnessModifier;

    public float customChance;

    bool arresting = false;

    private void Awake()
    {
        baseChance = 0;

        toolModifier = 1;
        heightModifier = 1;
        steepnessModifier = 1;

        Instance = this;

        if(PlayerMovement.Instance == null )
        {
            enabled = false;
            return;
        }

    }

    private void Start()
    {
        StartCoroutine(TryFallCycle());
    }

    private void Update()
    {
        chance = Mathf.Clamp((baseChance * toolModifier * heightModifier * steepnessModifier) + customChance, 0, 100);
    }

    public void SetBaseChance(int value)
    {
        baseChance = value;
    }

    public void IncreaseToolModifier(float value)
    {
        toolModifier += value/100;
    }

    public void DecreaseToolModifier(float value)
    {
        toolModifier -= value/100;
    }

    public void SetSteepnessModifier(float value)
    {
        steepnessModifier = 1 + (value / 90) * (value / 90);
    }

    public void SetHeightModifier(float value)
    {
        heightModifier = 1 + (value / 220);
    }


    IEnumerator TryFallCycle()
    {
        while (true) 
        {
            yield return new WaitForSeconds(1f);

            if (PlayerMovement.Instance.moving == true && TryToFall())
            {
                print("POŒLIZGNIECIE");
                PlayerMovement.Instance.SetRagdollState(true);

                yield return new WaitForSeconds(Random.Range(1.5f, 2.5f) * steepnessModifier * heightModifier);

                PlayerMovement.Instance.SetRagdollState(false);
            }
        }

    }

    public void TryArrest()
    {
        if (!PlayerMovement.Instance.IsRagdoll) return;
        if (arresting) return;

        StartCoroutine(Arrest());
    }

    IEnumerator Arrest()
    {
        arresting = true;

        yield return new WaitForSeconds(Random.Range(.3f, .7f) * steepnessModifier * heightModifier);

        PlayerMovement.Instance.SetRagdollState(false);
        arresting = false;
    }

    bool TryToFall()
    {
        float rng = Random.Range(0, 100);

        return rng < chance;
    }
}

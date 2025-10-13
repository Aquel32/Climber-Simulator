using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractible
{
    public void Interact();
}

public class Interactor : MonoBehaviour
{
    public static Interactor Instance;
    
    public Transform rayOrigin;
    [SerializeField] private float interactionDistance;

    private void Awake()
    {
        Instance = this;

        if (rayOrigin == null)
        {
            //Debug.LogError("Required rayOrigin reference is missing! Disabling script.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (rayOrigin == null) return;

        //if (UiManager.Instance.somePanelTurnedOn) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, interactionDistance))
            {
                if(hitInfo.collider.gameObject.TryGetComponent(out IInteractible interactObj))
                {
                    interactObj.Interact();
                }

                print(hitInfo.collider.gameObject);
            }
        }
    }
}

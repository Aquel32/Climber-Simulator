using Photon.Pun;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Belt : MonoBehaviourPunCallbacks, IGearScript
{
    public LayerMask ropeLayer;

    private Transform playerCamera;
    private List<RopeGenerator> playerRopes = new List<RopeGenerator>();
    private List<RopeGenerator> worldRopes = new List<RopeGenerator>();
    public RopeGenerator ropeGeneratorPrefab;

    public float distance = 1.5f;
    Vector3 lastPosition;

    public void Deinitialize()
    {
        print("DEINITALIZE BELT");

        if (playerRopes.Count == 0) return;

        while (playerRopes.Count > 0)
        {
            DetachLast();
        }
    }

    public void DetachLast()
    {
        Detach(playerRopes.Count - 1);
    }

    public void Detach(RopeGenerator rope)
    {
        int index = worldRopes.IndexOf(rope);

        Detach(index);
    }

    public void Detach(int index)
    {
        FallSystem.Instance.IncreaseToolModifier(75);

        GameObject temp = playerRopes[index].gameObject;
        playerRopes.RemoveAt(index);
        worldRopes.RemoveAt(index);

        PhotonNetwork.Destroy(temp);
    }

    public void Initialize()
    {
        playerCamera = PlayerCamera.Instance.cameraTransform;
        lastPosition = Player.myPlayer.playerObject.transform.position;

        print("INITIALIZE BELT");
    }

    void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, 2, ropeLayer) && hit.collider.TryGetComponent<RopeGenerator>(out RopeGenerator ropeGenerator))
        {
            if (Input.GetKeyDown(KeyCode.E) && playerRopes.Contains(ropeGenerator) == false)
            {
                if(worldRopes.Contains(ropeGenerator) == false)
                {
                    worldRopes.Add(ropeGenerator);
                    playerRopes.Add(PhotonNetwork.Instantiate(ropeGeneratorPrefab.name, transform.position, Quaternion.identity).GetComponent<RopeGenerator>());

                    playerRopes[playerRopes.Count - 1].enabled = true;
                    playerRopes[playerRopes.Count-1].startPoint = transform;

                    FallSystem.Instance.DecreaseToolModifier(75);
                }
                else
                {
                    Detach(ropeGenerator);
                }
            }
        }

        if (playerRopes.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.O))
        {
            DetachLast();
        }

        float maxDistance = 0;
        int index = 0;

        for (int i = 0; i < playerRopes.Count; i++)
        {
            Vector3 onWorldRopePoint = worldRopes[i].GetClosestPointOnRope(transform.position);
            playerRopes[i].endPointVector = onWorldRopePoint;

            float thisDistance = Vector3.Distance(transform.position, onWorldRopePoint);
            if (thisDistance > maxDistance)
            {
                index = i;
                maxDistance = thisDistance;
            }
        }

        if (maxDistance > distance)
        {
            Player.myPlayer.playerObject.transform.position = lastPosition;
        }
        else
        {
            lastPosition = Player.myPlayer.playerObject.transform.position;
        }
    }
}

using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Belt : MonoBehaviour, IGearScript
{
    public LayerMask ropeLayer;

    private Transform playerCamera;
    private List<RopeGenerator> playerRopes = new List<RopeGenerator>();
    private List<RopeGenerator> worldRopes = new List<RopeGenerator>();
    public RopeGenerator ropeGeneratorPrefab;

    public int distance = 3;
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
        GameObject temp = playerRopes[playerRopes.Count-1].gameObject;
        playerRopes.RemoveAt(playerRopes.Count-1);
        worldRopes.RemoveAt(worldRopes.Count-1);
        Destroy(temp);
    }

    public void Detach(RopeGenerator rope)
    {
        int index = worldRopes.IndexOf(rope);

        GameObject temp = playerRopes[index].gameObject;
        playerRopes.RemoveAt(index);
        worldRopes.RemoveAt(index);
        Destroy(temp);
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
            if (Input.GetKeyDown(KeyCode.Mouse0) && playerRopes.Contains(ropeGenerator) == false)
            {
                if(worldRopes.Contains(ropeGenerator) == false)
                {
                    worldRopes.Add(ropeGenerator);
                    playerRopes.Add(Instantiate(ropeGeneratorPrefab, transform.position, Quaternion.identity));

                    playerRopes[playerRopes.Count-1].startPoint = transform;
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
            return;
        }

        for (int i = 0; i < playerRopes.Count; i++)
        {
            playerRopes[i].endPointVector = worldRopes[i].GetClosestPointOnRope(transform.position);

            if (Vector3.Distance(transform.position, worldRopes[i].transform.position) > distance)
            {
                Player.myPlayer.playerObject.transform.position = lastPosition;
            }
            else
            {
                lastPosition = Player.myPlayer.playerObject.transform.position;
            }
        }
    }
}

using Photon.Pun;
using UnityEngine;

public class PlacedIceScrew : MonoBehaviour
{
    public RopeGenerator ropeGeneratorPrefab;
    private RopeGenerator generator;

    public void AttachRope()
    {
        generator = PhotonNetwork.Instantiate(ropeGeneratorPrefab.gameObject.name, transform.position, Quaternion.identity).GetComponent<RopeGenerator>();
        generator.enabled = true;
        ModifyAttachment(transform, Player.myPlayer.playerObject.transform);
    }

    public void ModifyAttachment(Transform startPoint, Transform endPoint)
    {
        generator.startPoint = startPoint;
        generator.endPoint = endPoint;
    }

    public void ModifyEnd(Transform endPoint)
    {
        generator.endPoint = endPoint;
    }

    public void DetachRope()
    {
        GameObject temp = generator.gameObject;

        PhotonNetwork.Destroy(temp);
        generator = null;
    }
}

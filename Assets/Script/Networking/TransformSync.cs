using UnityEngine;
using Photon.Pun;

public class TransformSync : MonoBehaviourPunCallbacks, IPunObservable
{
    public float moveSpeed;
    public float rotationSpeed;

    public Vector3 networkPosition;
    public Quaternion networkRotation;


    public void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, networkPosition, Time.fixedDeltaTime * moveSpeed);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, networkRotation, Time.fixedDeltaTime * rotationSpeed);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(this.transform.localPosition);
            stream.SendNext(this.transform.localRotation);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementSync : MonoBehaviourPunCallbacks, IPunObservable
{
    public Rigidbody rb;

    public float rotationSpeed = 1000f;
    public float moveSpeed = 100f;

    public Vector3 networkPosition;
    public Quaternion networkRotation;

    public void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            rb.position = Vector3.MoveTowards(rb.position, networkPosition, Time.fixedDeltaTime * moveSpeed);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, networkRotation, Time.fixedDeltaTime * rotationSpeed);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(this.rb.position);
            stream.SendNext(this.rb.rotation);
            stream.SendNext(this.rb.linearVelocity);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            rb.linearVelocity = (Vector3)stream.ReceiveNext();

            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += (this.rb.linearVelocity * lag);
        }
    }
}

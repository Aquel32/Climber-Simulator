using Photon.Pun;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SyncRigWeight : MonoBehaviourPunCallbacks, IPunObservable
{
    public float networkWeight;

    private TwoBoneIKConstraint rig;

    void Start()
    {
        rig = GetComponent<TwoBoneIKConstraint>();
    }

    public void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            rig.weight = networkWeight;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rig.weight);
        }
        else
        {
            networkWeight = (float)stream.ReceiveNext();
        }
    }
}

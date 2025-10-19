using System.Collections;
using UnityEngine;

public class RopeTest : MonoBehaviour
{
    public Transform start, end;
    Vector3 lastPlayerPosition;
    public float distance = 3;
    private void Start()
    {
        start.position = transform.position;
        //Transform current = transform.Find("joint1");
        ////current.GetComponent<Rigidbody>().isKinematic = true;

        //do
        //{
        //    //current.GetComponent<Rigidbody>().isKinematic = true;
        //    current = current.GetChild(0);
        //}
        //while (current.childCount != 0);

        //last = current;
        ////last.GetComponent<FixedJoint>().connectedBody = Player.myPlayer.playerObject.GetComponent<Rigidbody>();
        ////last.GetComponent<Rigidbody>().isKinematic = true;
        lastPlayerPosition = Player.myPlayer.playerObject.transform.position;

        StartCoroutine(startAfterDelay());
    }

    private void Update()
    {
        if(Player.myPlayer == null) return;

        Vector3 currentPosition = Player.myPlayer.playerObject.transform.position;
        if (Vector3.Distance(currentPosition, transform.position) > distance)
        {
            Player.myPlayer.playerObject.transform.position = lastPlayerPosition;
        }

        lastPlayerPosition = Player.myPlayer.playerObject.transform.position;
        end.position = lastPlayerPosition;
        end.LookAt(transform.position);
        start.LookAt(lastPlayerPosition);
    }

    IEnumerator startAfterDelay()
    {
        yield return new WaitForSeconds(3);
        Transform current = transform.Find("joint1");
        do
        {
            current.GetComponent<Collider>().enabled = true;
            current = current.GetChild(0);
        }
        while (current.childCount != 0);
    }
}

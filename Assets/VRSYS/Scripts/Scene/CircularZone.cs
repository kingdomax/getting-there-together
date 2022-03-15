using Photon.Pun;
using UnityEngine;

namespace Vrsys
{
    public class CircularZone : MonoBehaviourPunCallbacks, IPunObservable
    {
        // EXPOSED MEMBER
        [HideInInspector]
        public GameObject NavigatorJumpingPoint;
        [HideInInspector]
        public GameObject PassengerJumpingPoint;

        // PRIVATE MEMBER
        private SceneState _sceneState;
        private GameObject _navigator;
        private GameObject _passenger;
        private LineRenderer _groupLine;

        // STATE
        private Vector3 receivedScale = Vector3.one;

        public override void OnEnable()
        {
            NavigatorJumpingPoint = transform.Find("Navigator Jumping Point")?.gameObject;
            PassengerJumpingPoint = transform.Find("Passenger Jumping Point")?.gameObject;

            _sceneState = GameObject.Find("Scene Management")?.GetComponent<SceneState>();
            _navigator = PhotonView.Find(_sceneState.GetNavigator())?.gameObject;
            _passenger = PhotonView.Find(_sceneState.GetPassenger())?.gameObject;
            _groupLine = GetComponent<LineRenderer>();

            // setup
            _sceneState.SetCircularZone(photonView.ViewID);
            _groupLine.enabled = true;
            NavigatorJumpingPoint.GetComponent<MeshRenderer>().material.color = _navigator.GetComponent<LineRenderer>().material.color;
            PassengerJumpingPoint.GetComponent<MeshRenderer>().material.color = _passenger.GetComponent<LineRenderer>().material.color;

            base.OnEnable();
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting && photonView.IsMine)
            {
                stream.SendNext(transform.lossyScale);
            }
            else if (stream.IsReading)
            {
                receivedScale = (Vector3)stream.ReceiveNext();
            }
        }

        void Update()
        {
            if (!photonView.IsMine && (transform.localScale - receivedScale).magnitude > 0.001) 
            {
                transform.localScale = Vector3.Lerp(transform.localScale, receivedScale, Time.deltaTime);
            }

            _groupLine.SetPosition(0, _navigator.GetComponent<AvatarHMDAnatomy>().body.transform.position + new Vector3(0, -1.3f, 0));
            _groupLine.SetPosition(1, _passenger.GetComponent<AvatarHMDAnatomy>().body.transform.position + new Vector3(0, -1.3f, 0));
        }

        public override void OnDisable()
        {
            _sceneState.SetCircularZone(-1);
            _groupLine.enabled = false;
            PassengerJumpingPoint.SetActive(false);

            base.OnDisable();
        }
    }
}

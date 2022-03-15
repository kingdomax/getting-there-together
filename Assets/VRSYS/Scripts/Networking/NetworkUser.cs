using TMPro;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using System.Collections.Generic;

namespace Vrsys
{
    // Handles ViewingSetup instantiation and global properties of the local user.
    [RequireComponent(typeof(AvatarAnatomy))]
    public class NetworkUser : MonoBehaviourPunCallbacks, IPunObservable
    {
        // SETTING
        [Tooltip("If true, a TMP_Text element will be searched in child components and a text will be set equal to photonView.Owner.NickName. Note, this feature may create unwanted results if the GameObject, which contains this script, holds any other TMP_Text fields but the actual NameTag.")]
        public bool setNameTagToNickname = true;
        [Tooltip("The spawn position of this NetworkUser")]
        public Vector3 spawnPosition = Vector3.zero;
        public List<string> tags = new List<string>();

        // EXPOSED MEMBERS
        public static GameObject localGameObject; // this user
        public static GameObject localHead;
        public static NetworkUser localNetworkUser { get { return localGameObject.GetComponent<NetworkUser>(); } }

        // MEMBERS
        [SerializeField]
        [Tooltip("The viewing prefab to instantiate for the local user. For maximum support, this should contain a ViewingSetupAnatomy script at root level, which supports the AvatarAnatomy attached to gameObject.")]
        private GameObject viewingSetup; // Desktop or HMD view as preffab
        [HideInInspector]
        public AvatarAnatomy avatarAnatomy { get; private set; } // easy access to model object
        [HideInInspector]
        public ViewingSetupAnatomy viewingSetupAnatomy { get; private set; } // easy access to view object (use this for narvigation connected with model object)
        private SceneState sceneState;

        // STATE
        private Vector3 receivedScale = Vector3.one;
        private bool hasPendingScaleUpdate { get { return (transform.localScale - receivedScale).magnitude > 0.001; } }
        public LineRenderer lineRenderer;
        private GameObject circularZoneObj;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting && photonView.IsMine)
            {
                stream.SendNext(viewingSetup.transform.lossyScale);
                stream.SendNext(lineRenderer?.enabled ?? false);
            }
            else if (stream.IsReading)
            {
                receivedScale = (Vector3)stream.ReceiveNext();
                lineRenderer.enabled = (bool)stream.ReceiveNext();
            }
        }

        private void Update()
        {
            if (photonView.IsMine) { return; }
            
            if (hasPendingScaleUpdate) { transform.localScale = Vector3.Lerp(transform.localScale, receivedScale, Time.deltaTime); }

            var myRole = sceneState.GetNavigationRole(photonView.ViewID);
            if (lineRenderer.enabled && myRole != NavigationRole.Observer) // locally render other's line renderer
            {
                var circularZone = GetCurrentCircularZoneObj().GetComponent<CircularZone>();
                var rightHand = ((AvatarHMDAnatomy)avatarAnatomy).handRight;
                DrawQuadraticBezierCurve(
                    lineRenderer,
                    rightHand.transform.position,
                    rightHand.transform.position + rightHand.transform.TransformDirection(new Vector3(0, 1.5f, 1.5f)),
                    myRole == NavigationRole.Navigator ? circularZone.NavigatorJumpingPoint.transform.position : circularZone.PassengerJumpingPoint.transform.position);
            }
        }

        public override void OnDisable()
        {
            sceneState.RomoveUserFromList(photonView.ViewID);
            base.OnDisable();
        }

        private void Awake()
        {
            avatarAnatomy = GetComponent<AvatarAnatomy>();
            sceneState = GameObject.Find("Scene Management").GetComponent<SceneState>();

            // Only owner user
            if (photonView.IsMine)
            {
                NetworkUser.localGameObject = gameObject;
                NetworkUser.localHead = avatarAnatomy.head;

                InitializeViewing();
                InitializeAvatar();
            }

            // All user
            if (PhotonNetwork.IsConnected)
            {
                var nameTagTextComponent = avatarAnatomy.nameTag.GetComponentInChildren<TMP_Text>();
                if (nameTagTextComponent && setNameTagToNickname)
                {
                    nameTagTextComponent.text = photonView.Owner.NickName;
                }
                gameObject.name = photonView.Owner.NickName + (photonView.IsMine ? " [Local User]" : " [External User]");
                sceneState.AppendUserToList(photonView.ViewID);
            }
        }

        private void InitializeViewing()
        {
            //Check whcih platform is running
            if (viewingSetup == null) { throw new System.ArgumentNullException("Viewing Setup must not be null for local NetworkUser."); }

            viewingSetup = Instantiate(viewingSetup);
            viewingSetup.transform.position = spawnPosition;
            viewingSetup.transform.SetParent(gameObject.transform, false); // Avatar/ViewSetup
            viewingSetup.name = "Viewing Setup";

            viewingSetupAnatomy = viewingSetup.GetComponentInChildren<ViewingSetupAnatomy>();
            if (viewingSetupAnatomy)
            {
                // Make avatar's head, handleft, handright to be child of viewAnatomy's heaad, handleft, handright
                avatarAnatomy.ConnectFrom(viewingSetupAnatomy); // Avatar / ViewSetup / viewhead --> avatarhead, viewhand --> avatarhand
            }
            else
            {
                Debug.LogWarning("Your Viewing Setup Prefab does not contain a '" + typeof(ViewingSetupAnatomy).Name + "' Component. This can lead to unexpected behavior.");
            }
        }

        private void InitializeAvatar()
        {
            var networkSetting = GameObject.Find("__NETWORKING__").transform.Find("Network Setup").gameObject;
            Color clr = ParseColorFromPrefs(networkSetting.GetComponent<NetworkSetup>().userColor);
            photonView.RPC("SetColor", RpcTarget.AllBuffered, new object[] { new Vector3(clr.r, clr.g, clr.b) });
        }

        private Color ParseColorFromPrefs(PrefabColor col)
        {
            switch (col)
            {
                case PrefabColor.Blue: return new Color(0f, 0f, 1f);
                case PrefabColor.Red: return new Color(1f, 0f, 0f);
                case PrefabColor.Default: return new Color(.6f, .6f, .6f);
            }
            return new Color(.6f, .6f, .6f);
        }

        private GameObject GetCurrentCircularZoneObj()
        {
            if (circularZoneObj == null || circularZoneObj.GetPhotonView().ViewID != sceneState.GetCircularZone())
            {
                circularZoneObj = PhotonView.Find(sceneState.GetCircularZone()).gameObject;
            }
            return circularZoneObj;
        }

        public static void DrawLinearLine(LineRenderer line, Vector3 initialPoint, Vector3 endPoint)
        {
            line.positionCount = 2;
            line.SetPosition(0, initialPoint);
            line.SetPosition(1, endPoint);
        }

        public static void DrawQuadraticBezierCurve(LineRenderer line, Vector3 initialPoint, Vector3 intermediatePoint, Vector3 endPoint)
        {
            line.positionCount = 200;
            float t = 0f;
            Vector3 B = new Vector3(0, 0, 0);
            for (int i = 0; i < line.positionCount; i++)
            {
                B = (1 - t) * (1 - t) * initialPoint + 2 * (1 - t) * t * intermediatePoint + t * t * endPoint;
                line.SetPosition(i, B);
                t += (1 / (float)line.positionCount);
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation, bool withRotation) 
            => photonView.RPC("Teleport", RpcTarget.All, position, rotation, withRotation);

        public void ToggleLineRenderer(bool isOn)
            => photonView.RPC("ToggleLineRenderer", RpcTarget.All, isOn);

        [PunRPC]
        public void Teleport(Vector3 position, Quaternion rotation, bool withRotation, PhotonMessageInfo info)
        {
            if (photonView.IsMine) // Let owner teleport his viewAvatar and other user just receive from OnPhotonSerializeView
            {
                viewingSetupAnatomy.Teleport(position, rotation, withRotation);
            }
        }

        [PunRPC]
        public void ToggleLineRenderer(bool isOn, PhotonMessageInfo info)
        {
            if (photonView.IsMine) // Let owner toggle and other user just receive from OnPhotonSerializeView
            {
                lineRenderer.enabled = isOn;
            }
        }

        [PunRPC]
        public void TogglePassengerPoint(bool isOn)
        {
            GetCurrentCircularZoneObj().GetComponent<CircularZone>().PassengerJumpingPoint.SetActive(isOn);
        }

        [PunRPC]
        public void SetFormingStage(int navigator, int passenger)
        {
            sceneState.SetFormingStage(navigator, passenger);
        }

        [PunRPC]
        public void SetPerformingStage()
        {
            sceneState.SetPerformingStage();
        }

        [PunRPC]
        public void SetAdjourningStage()
        {
            if (sceneState.GetCircularZone() != -1)
            {
                var circularZoneObj = GetCurrentCircularZoneObj();
                circularZoneObj.SetActive(false);
                if (circularZoneObj.GetPhotonView().IsMine) { PhotonNetwork.Destroy(circularZoneObj); }
            }
            sceneState.SetAdjourningStage();
        }

        [PunRPC]
        void SetColor(Vector3 color)
        {
            avatarAnatomy.SetColor(new Color(color.x, color.y, color.z));
            lineRenderer.material.color = new Color(color.x, color.y, color.z);
        }

        [PunRPC]
        void SetName(string name)
        {
            gameObject.name = name + (photonView.IsMine ? " [Local User]" : " [External User]");
            var nameTagTextComponent = avatarAnatomy.nameTag.GetComponentInChildren<TMP_Text>();
            if (nameTagTextComponent && setNameTagToNickname)
            {
                nameTagTextComponent.text = name;
            }
        }

        [PunRPC]
        void AnnouceMessage(string message)
        {
            Debug.Log(message);
        }
    }
}

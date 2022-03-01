using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;

namespace Vrsys
{
    [RequireComponent(typeof(AvatarAnatomy))]
    public class NetworkUser : MonoBehaviourPunCallbacks, IPunObservable
    {
        public static GameObject localGameObject;
        public static GameObject localHead;
        public static NetworkUser localNetworkUser
        {
            get
            {
                return localGameObject.GetComponent<NetworkUser>();
            }
        }

        [Tooltip("The viewing prefab to instantiate for the local user. For maximum support, this should contain a ViewingSetupAnatomy script at root level, which supports the AvatarAnatomy attached to gameObject.")]
        [SerializeField]
        private GameObject viewingSetup;

        [Tooltip("If true, a TMP_Text element will be searched in child components and a text will be set equal to photonView.Owner.NickName. Note, this feature may create unwanted results if the GameObject, which contains this script, holds any other TMP_Text fields but the actual NameTag.")]
        public bool setNameTagToNickname = true;

        public enum PrefabColor
        {
            Red,
            Blue,
            Default
        }
        public PrefabColor color = PrefabColor.Default;

        [Tooltip("The spawn position of this NetworkUser")]
        public Vector3 spawnPosition = Vector3.zero;

        public List<string> tags = new List<string>();

        [HideInInspector]
        public AvatarAnatomy avatarAnatomy { get; private set; }

        [HideInInspector]
        public ViewingSetupAnatomy viewingSetupAnatomy { get; private set; }

        private Vector3 receivedScale = Vector3.one;

        private bool hasPendingScaleUpdate
        {
            get
            {
                return (transform.localScale - receivedScale).magnitude > 0.001;
            }
        }

        private void Awake()
        {
            avatarAnatomy = GetComponent<AvatarAnatomy>();
            if (photonView.IsMine)
            {
                NetworkUser.localGameObject = gameObject;
                NetworkUser.localHead = avatarAnatomy.head;
                
                InitializeAvatar();
                InitializeViewing();
                //HideHandsInFavorOfControllers();
            }
            if (PhotonNetwork.IsConnected)
            {
                gameObject.name = photonView.Owner.NickName + (photonView.IsMine ? " [Local User]" : " [External User]");
                var nameTagTextComponent = avatarAnatomy.nameTag.GetComponentInChildren<TMP_Text>();
                if (nameTagTextComponent && setNameTagToNickname)
                {
                    nameTagTextComponent.text = photonView.Owner.NickName;
                }
            }
        }

        private void Update()
        {
            if (!photonView.IsMine && hasPendingScaleUpdate)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, receivedScale, Time.deltaTime);
            }
        }

        private void InitializeAvatar()
        {
            //avatarAnatomy.nameTag.SetActive(false);
            //Color clr = ParseColorFromPrefs(new Color(.6f, .6f, .6f));
            Color clr = ParseColorFromPrefs(color);
            photonView.RPC("SetColor", RpcTarget.AllBuffered, new object[] { new Vector3(clr.r, clr.g, clr.b) });
        }

        private void InitializeViewing()
        {
            //Check whcih platform is running
            if (viewingSetup == null)
            {
                throw new System.ArgumentNullException("Viewing Setup must not be null for local NetworkUser.");
            }

            viewingSetup = Instantiate(viewingSetup);
            viewingSetup.transform.position = spawnPosition;
            viewingSetup.transform.SetParent(gameObject.transform, false);
            viewingSetup.name = "Viewing Setup";

            viewingSetupAnatomy = viewingSetup.GetComponentInChildren<ViewingSetupAnatomy>();
            if (viewingSetupAnatomy)
            {
                avatarAnatomy.ConnectFrom(viewingSetupAnatomy);
            }
            else
            {
                Debug.LogWarning("Your Viewing Setup Prefab does not contain a '" + typeof(ViewingSetupAnatomy).Name + "' Component. This can lead to unexpected behavior.");
            }
        }

        private void HideHandsInFavorOfControllers()
        {
            AvatarHMDAnatomy ahmda = GetComponent<AvatarHMDAnatomy>();
            if (ahmda != null)
            {
                ahmda.handRight.SetActive(false);
                ahmda.handLeft.SetActive(false);
            }
        }

        [PunRPC]
        void SetColor(Vector3 color)
        {
            avatarAnatomy.SetColor(new Color(color.x, color.y, color.z));
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

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting && photonView.IsMine)
            {
                stream.SendNext(viewingSetup.transform.lossyScale);
            }
            else if (stream.IsReading)
            {
                receivedScale = (Vector3)stream.ReceiveNext();
            }
        }

        public Color ParseColorFromPrefs(PrefabColor col)
        {
            switch (col)
            {
                case PrefabColor.Blue: return new Color(0f, 0f, 1f);
                case PrefabColor.Red: return new Color(1f, 0f, 0f);
                case PrefabColor.Default: return new Color(.6f, .6f, .6f);
            }
            return new Color(.6f, .6f, .6f);
        }

        public static Color ParseColorFromPrefs(Color fallback)
        {
            Color color;
            switch (PlayerPrefs.GetString("UserColor"))
            {
                case "ColorBlack": color = new Color(.2f, .2f, .2f); break;
                case "ColorRed": color = new Color(1f, 0f, 0f); break;
                case "ColorGreen": color = new Color(0f, 1f, 0f); break;
                case "ColorBlue": color = new Color(0f, 0f, 1f); break;
                case "ColorPink": color = new Color(255f / 255f, 192f / 255f, 203 / 255f); break;
                case "ColorWhite": color = new Color(1f, 1f, 1f); break;
                default: color = fallback; break;
            }
            return color;
        }
    }
}

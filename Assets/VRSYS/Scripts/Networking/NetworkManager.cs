using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Vrsys
{
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        private string logTag;

        public bool verbose = false;

        public bool applyPositionToAddedUserPrefabs = true;

        [Tooltip("Class will attempt to instantiate PlayerPrefs.GetString(\"UserPrefabDir\", \"\") + \" / \" + PlayerPrefs.GetString(\"UserPrefabName\", \"\"). If values are not set, fallback will be used.")]
        public GameObject fallbackUserPrefab;
        [Tooltip("Class will attempt to instantiate PlayerPrefs.GetString(\"UserPrefabDir\", \"\") + \" / \" + PlayerPrefs.GetString(\"UserPrefabName\", \"\"). If values are not set, fallback will be used.")]
        public string fallbackUserPrefabResourceDirectory = "UserPrefabs";

        private string userPrefabPath 
        {
            get 
            {
                string p = PlayerPrefs.GetString("UserPrefabDir", "") + "/" + PlayerPrefs.GetString("UserPrefabName", "");
                return (p == "/" && fallbackUserPrefab != null) ? fallbackUserPrefabResourceDirectory + "/" + fallbackUserPrefab.name : p;
            }
        }

        void Start()
        {
            logTag = GetType().Name;

            if (verbose)
            {
                Debug.Log(logTag + ": User prefab path: " + userPrefabPath);
            }

            if (!NetworkUser.localGameObject)
            {
                InstantiateUser();
            }

            if (PhotonNetwork.IsMasterClient)
            {
                // Since the master client already called OnJoinedRoom() in the launcher scene, we have to call it again here; 
                // other clients will directly trigger the OnJoinedRoom() callback in this class
                OnJoinedRoom();
            }
        }

        private void InstantiateUser()
        {
            GameObject userInstance = PhotonNetwork.Instantiate(userPrefabPath, Vector3.zero, Quaternion.identity, 0);
            if(applyPositionToAddedUserPrefabs)
                userInstance.transform.position = gameObject.transform.position;
        }

        public override void OnJoinedRoom()
        {
            string connectedRoomName = PhotonNetwork.CurrentRoom.Name;
            if (verbose)
            {
                Debug.Log(logTag + ": Successfully connected to room " + connectedRoomName + ". Have fun!");
                Debug.Log(logTag + ": There are " + (PhotonNetwork.CurrentRoom.PlayerCount - 1) + " other participants in this room.");
            }
        }

        public override void OnLeftRoom()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public override void OnPlayerEnteredRoom(Player other)
        {
            if (verbose)
            {
                Debug.Log(logTag + ": " + other.NickName + " has entered the room.");
            }
        }

        public override void OnPlayerLeftRoom(Player other)
        {
            if (verbose)
            {
                Debug.Log(logTag + ": " + other.NickName + " has left the room.");
            }
        }
    }

}
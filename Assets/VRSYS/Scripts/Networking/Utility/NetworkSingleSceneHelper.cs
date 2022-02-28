using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Vrsys
{
    public class NetworkSingleSceneHelper : MonoBehaviourPunCallbacks
    {
        public GameObject networkLobbyMenu;
        public GameObject networkManager;
        public GameObject mainCamera;
        public List<GameObject> objectsToActivate;
        public List<GameObject> objectsToDeactivate;

        void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = GameObject.Find("Main Camera");
            }
        }

        public override void OnJoinedRoom()
        {
            networkManager.transform.parent = null;
            networkManager.SetActive(true);
            this.gameObject.SetActive(false);
            mainCamera.SetActive(false);

            foreach (GameObject go in objectsToActivate)
            {
                go.SetActive(true);
            }

            foreach (GameObject go in objectsToDeactivate)
            {
                go.SetActive(false);
            }
        }
    }
}
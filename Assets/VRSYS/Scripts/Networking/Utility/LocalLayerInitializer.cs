using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vrsys
{
    public class LocalLayerInitializer : MonoBehaviourPunCallbacks
    {
        public int layer;

        public List<GameObject> targets = new List<GameObject>();

        public bool recurseChildren = true; 

        private void Awake()
        {
            if (photonView.IsMine)
            {
                if (targets.Count > 0)
                {
                    foreach (var go in targets)
                    {
                        if (recurseChildren)
                            SetLayerRecursively(go, layer);
                        else
                            go.layer = layer;
                    }
                }
                else
                {
                    if (recurseChildren)
                        SetLayerRecursively(gameObject, layer);
                    else
                        gameObject.layer = layer;
                }
            }
            Destroy(this);
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach(Transform transform in go.transform)
            {
                SetLayerRecursively(transform.gameObject, layer);
            }
        }
    }
}

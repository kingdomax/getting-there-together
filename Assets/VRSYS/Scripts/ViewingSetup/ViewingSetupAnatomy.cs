using UnityEngine;

namespace Vrsys
{
    // Provide easy access to its own children component (desktop view)
    public class ViewingSetupAnatomy : MonoBehaviour
    {
        public GameObject childAttachmentRoot;
        public GameObject mainCamera;

        private void Awake()
        {
            ParseComponents();
        }

        protected virtual void ParseComponents()
        {
            if (childAttachmentRoot == null)
            {
                childAttachmentRoot = gameObject;
            }
            if (mainCamera == null)
            {
                mainCamera = transform.Find("Main Camera").gameObject;
            }
        }

        // To accommodate both desktop and HMD user
        public virtual void Teleport(Vector3 position, Quaternion rotation)
        {
            Debug.Log("Hi! ViewDesktopAnatomy");
            mainCamera.transform.position = position;
            mainCamera.transform.rotation = rotation;
        }
    }
}

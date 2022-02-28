using UnityEngine;

namespace Vrsys
{
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
    }
}

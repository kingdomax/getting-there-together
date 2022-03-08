using UnityEngine;

namespace Vrsys
{
    // Provide easy access to its own children component (HMD view)
    public class ViewingSetupHMDAnatomy : ViewingSetupAnatomy
    {
        public GameObject leftController;
        public GameObject rightController;

        protected override void ParseComponents()
        {
            if (childAttachmentRoot == null)
            {
                childAttachmentRoot = transform.Find("Camera Offset").gameObject;
            }
            if (mainCamera == null)
            {
                mainCamera = transform.Find("Camera Offset/Main Camera").gameObject;
            }
            if (leftController == null)
            {
                leftController = transform.Find("Camera Offset/Left Controller").gameObject;
            }
            if (rightController == null)
            {
                rightController = transform.Find("Camera Offset/Right Controller").gameObject;
            }
        }

        public override void Teleport(Vector3 position, Quaternion rotation)
        {
            childAttachmentRoot.transform.position = position;
            childAttachmentRoot.transform.rotation = rotation;
        }
    }
}

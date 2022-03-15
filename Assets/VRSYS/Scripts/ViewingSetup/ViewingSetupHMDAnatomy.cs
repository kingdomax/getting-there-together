using UnityEngine;

namespace Vrsys
{
    // Provide easy access to its own children component (HMD view)
    // ViewingSetup / Camera Offset / Main Camera,Left Controller,Right Controller
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

        public override void Teleport(Vector3 position, Quaternion rotation, bool withRotation)
        {
            var bufferHeight = 0.5f;
            transform.position = new Vector3(position.x, position.y+bufferHeight, position.z);
            transform.rotation = withRotation ? rotation : childAttachmentRoot.transform.rotation;
        }
    }
}

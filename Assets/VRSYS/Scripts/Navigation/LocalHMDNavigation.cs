using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Vrsys
{
    public class LocalHMDNavigation : MonoBehaviourPunCallbacks
    {
        [Tooltip("Translation Velocity [m/sec]")]
        [Range(0.1f, 10.0f)]
        public float translationVelocity = 3.0f;

        [Tooltip("Rotation Velocity [degree/sec]")]
        [Range(1.0f, 10.0f)]
        public float rotationVelocity = 2.0f;

        ViewingSetupHMDAnatomy viewingSetupHmd;
        private XRController controller;

        // Start is called before the first frame update
        void Start()
        {
            // This script should only compute for the local user
            if (!photonView.IsMine)
                Destroy(this);
        }

        // Update is called once per frame
        void Update()
        {
            // Only calculate & apply input if local user fully instantiated
            if (EnsureViewingSetup() && EnsureController())
            {
                MapInput(CalcTranslationInput(), CalcRotationInput());
            }
        }

        bool EnsureViewingSetup()
        {
            if (viewingSetupHmd == null)
            {
                if (NetworkUser.localNetworkUser != null)
                {
                    var viewingSetup = NetworkUser.localNetworkUser.viewingSetupAnatomy;
                    if (viewingSetup is ViewingSetupHMDAnatomy)
                    {
                        viewingSetupHmd = (ViewingSetupHMDAnatomy)viewingSetup;
                    }
                }
            }
            return viewingSetupHmd != null;
        }

        bool EnsureController()
        {
            if (controller == null)
            {
                controller = viewingSetupHmd.rightController.GetComponent<XRController>();
            }
            return controller != null;
        }

        private Vector3 CalcTranslationInput()
        {
            float trigger;
            controller.inputDevice.TryGetFeatureValue(CommonUsages.trigger, out trigger);
            trigger = trigger > 0.1 ? trigger : 0.0f;
            var dir = viewingSetupHmd.rightController.transform.forward;
            dir.y = 0;
            return dir.normalized * trigger * translationVelocity * Time.deltaTime;
        }

        private Vector3 CalcRotationInput()
        {
            Vector2 joystick;
            controller.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystick);
            return new Vector3(0, joystick.x * 0.25f * rotationVelocity, 0);
        }

        private void MapInput(Vector3 translationInput, Vector3 rotationInput) 
        {
            viewingSetupHmd.childAttachmentRoot.transform.position += translationInput;
            viewingSetupHmd.childAttachmentRoot.transform.rotation *= Quaternion.Euler(rotationInput);
        }
    }
}

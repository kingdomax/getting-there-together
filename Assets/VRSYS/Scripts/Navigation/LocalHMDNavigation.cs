using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Vrsys
{
    // Scripts that will only compute for the local user (owning the PhotonView in their parent hierarchy). 
    public class LocalHMDNavigation : MonoBehaviourPunCallbacks
    {
        [Tooltip("Translation Velocity [m/sec]")]
        [Range(0.1f, 10.0f)]
        public float translationVelocity = 3.0f;
        [Tooltip("Rotation Velocity [degree/sec]")]
        [Range(1.0f, 10.0f)]
        public float rotationVelocity = 2.0f;

        ViewingSetupHMDAnatomy _viewingSetupHmd;
        private XRController _controller;
        private SceneState _sceneState;

        void Start()
        {
            // This script should only compute for the local user
            if (!photonView.IsMine)
                Destroy(this);
        }

        void Update()
        {
            // Only calculate & apply input if local user fully instantiated
            if (EnsureViewingSetup() && EnsureController() && EnsureGroupNavStage())
            {
                MapInput(CalcTranslationInput(), CalcRotationInput());
            }
        }

        bool EnsureViewingSetup()
        {
            if (_viewingSetupHmd == null && NetworkUser.localNetworkUser != null)
            {
                var viewingSetup = NetworkUser.localNetworkUser.viewingSetupAnatomy;
                if (viewingSetup is ViewingSetupHMDAnatomy)
                {
                    _viewingSetupHmd = (ViewingSetupHMDAnatomy)viewingSetup;
                }
            }
            return _viewingSetupHmd != null;
        }

        bool EnsureController()
        {
            if (_controller == null) { _controller = _viewingSetupHmd.rightController.GetComponent<XRController>(); }
            return _controller != null;
        }

        bool EnsureGroupNavStage()
        {
            if (_sceneState == null) { _sceneState = GameObject.Find("Scene Management").GetComponent<SceneState>(); }
            return _sceneState.GetNavigationStage() == NavigationStage.Adjourning;
            // return true;
        }

        private Vector3 CalcTranslationInput()
        {
            float trigger;
            _controller.inputDevice.TryGetFeatureValue(CommonUsages.trigger, out trigger);
            trigger = trigger > 0.1 ? trigger : 0.0f;
            var dir = _viewingSetupHmd.rightController.transform.forward;
            dir.y = 0;
            return dir.normalized * trigger * translationVelocity * Time.deltaTime;
        }

        private Vector3 CalcRotationInput()
        {
            Vector2 joystick;
            _controller.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystick);
            return new Vector3(0, joystick.x * 0.25f * rotationVelocity, 0);
        }

        private void MapInput(Vector3 translationInput, Vector3 rotationInput) 
        {
            _viewingSetupHmd.transform.position += translationInput;
            _viewingSetupHmd.transform.rotation *= Quaternion.Euler(rotationInput);
        }
    }
}

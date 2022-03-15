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

        void Awake()
        {
            _sceneState = GameObject.Find("Scene Management").GetComponent<SceneState>();
        }

        void Update()
        {
            if (EnsureViewingSetup() && EnsureController())
            {
                var stage = _sceneState.GetNavigationStage();
                var role = _sceneState.GetNavigationRole(photonView.ViewID);

                if (stage == NavigationStage.Adjourning) { CalcTranslationInput(); }
                if (stage != NavigationStage.Performing || role != NavigationRole.Navigator) { CalcRotationInput(); }
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

        private void CalcTranslationInput()
        {
            _controller.inputDevice.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
            
            var dir = _viewingSetupHmd.rightController.transform.forward;
            dir.y = 0;
            trigger = trigger > 0.1 ? trigger : 0.0f;

            _viewingSetupHmd.transform.position += (dir.normalized * trigger * translationVelocity * Time.deltaTime);
        }

        private void CalcRotationInput()
        {
            _controller.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystick);
            _viewingSetupHmd.transform.rotation *= Quaternion.Euler(new Vector3(0, joystick.x * 0.25f * rotationVelocity, 0));
        }
    }
}

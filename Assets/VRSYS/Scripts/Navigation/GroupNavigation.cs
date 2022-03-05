using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Vrsys
{
    public class GroupNavigation : MonoBehaviourPunCallbacks
    {
        // SETTING
        public LayerMask layerMask;

        // STAGE
        private bool _oneTimeSetup;
        enum NavigationStage { Forming, Performing, Adjourning }
        private NavigationStage _currentStage;

        // GENERAL MEMBERS
        private ViewingSetupHMDAnatomy _viewingSetupHmd;
        private GameObject _xrRig;
        private GameObject _camera;
        private GameObject _rightHand;
        private XRController _controller;
        private LineRenderer _formingLine;

        // FORMING MEMBERS
        private GameObject _formingIntersectionPoint;
        private GameObject _formingPassengerPreview;

        void Start()
        {
            if (!photonView.IsMine) { Destroy(this); } // This script should only compute for the local user
        }

        void Update()
        {
            if (!AreDevicesReady()) { return; }

            InitializeBot();
            Forming();
            Performing();
            Adjourning();
        }

        private bool AreDevicesReady()
        {
            if (NetworkUser.localNetworkUser == null) { return false; }
            
            if (_viewingSetupHmd == null && NetworkUser.localNetworkUser.viewingSetupAnatomy is ViewingSetupHMDAnatomy)
            {
                _viewingSetupHmd = (ViewingSetupHMDAnatomy) NetworkUser.localNetworkUser.viewingSetupAnatomy;
            }

            if (_viewingSetupHmd != null && !_oneTimeSetup)
            {
                _oneTimeSetup = true;
                _currentStage = NavigationStage.Adjourning;

                _xrRig = _viewingSetupHmd.childAttachmentRoot;
                _camera = _viewingSetupHmd.mainCamera;
                _rightHand = _viewingSetupHmd.rightController;
                _controller = _rightHand.GetComponent<XRController>();
                
                _formingLine = _rightHand.GetComponent<LineRenderer>();
                _formingIntersectionPoint = GameObject.Find("Forming Intersection Point");
                _formingPassengerPreview = Instantiate(Resources.Load("UserPrefabs/Avatars/AvatarHMD-PosePreview"), Vector3.zero, Quaternion.identity) as GameObject;
                var shirtModel = _formingPassengerPreview.transform.Find("Head/Body/shirtMale/default").gameObject;
                var headModel = _formingPassengerPreview.transform.Find("Head/HeadModel/Head/HeadMesh").gameObject;
                var arrowModel = _formingPassengerPreview.transform.Find("arrow_ring (2)/default").gameObject;
                shirtModel.GetComponent<MeshRenderer>().material.color = Color.white;
                headModel.GetComponent<MeshRenderer>().material.color = Color.white;
                arrowModel.GetComponent<MeshRenderer>().material.color = Color.white;
                _formingPassengerPreview.SetActive(false);
            }

            return _viewingSetupHmd != null;
        }

        private void InitializeBot()
        {

        }

        private void Forming()
        {
            _controller.inputDevice.TryGetFeatureValue(CommonUsages.grip, out float gripper);

            // render ray and intersection point
            var isRayHit = false;
            _formingLine.enabled = gripper > 0.00001f;
            if (_formingLine.enabled)
            {
                isRayHit = Physics.Raycast(_rightHand.transform.position,
                                        _rightHand.transform.TransformDirection(Vector3.forward),
                                        out RaycastHit hit, 7f, layerMask);

                _formingIntersectionPoint.SetActive(isRayHit);
                _formingIntersectionPoint.transform.position = hit.point;
                _formingLine.SetPosition(0, _rightHand.transform.position);
                _formingLine.SetPosition(1, isRayHit ? hit.point : _rightHand.transform.position + _rightHand.transform.TransformDirection(Vector3.forward) * 7);
            }
            else
            {
                _formingIntersectionPoint.SetActive(false);
            }

            // select passenger position to be form as a group
            if (!_formingPassengerPreview.activeInHierarchy && isRayHit && gripper > 0.99f)
            {
                _formingPassengerPreview.SetActive(true);
                _formingPassengerPreview.transform.position = new Vector3(
                    _formingIntersectionPoint.transform.position.x,
                    _formingIntersectionPoint.transform.position.y,
                    _formingIntersectionPoint.transform.position.z);
            }

            // while the grip is fully pressed update the passenger direction to face the current intersection
            if (_formingPassengerPreview.activeInHierarchy)
            {
                var lookRotation = Quaternion.LookRotation(_formingLine.GetPosition(1) - _formingPassengerPreview.transform.position);
                _formingPassengerPreview.transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);

                // teleport passenger & all indicator disappear
                if (gripper < 0.00001f)
                {
                    _formingPassengerPreview.SetActive(false);
                    
                    var finalRotation = _formingPassengerPreview.transform.rotation * (Quaternion.Inverse(_camera.transform.rotation) * _xrRig.transform.rotation);
                    transform.rotation = Quaternion.Euler(0, finalRotation.eulerAngles.y, 0);
                    _xrRig.transform.position = new Vector3(
                        _formingPassengerPreview.transform.position.x,
                        _formingPassengerPreview.transform.position.y + 0.5f,
                        _formingPassengerPreview.transform.position.z);
                }
            }
        }


        private void Performing()
        {

        }

        private void Adjourning()
        {

        }
    }
}

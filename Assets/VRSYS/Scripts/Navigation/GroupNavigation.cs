using System;
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

        // GENERAL MEMBERS
        private bool _oneTimeSetup;
        private SceneState _sceneState;
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

            var currentStage = _sceneState.GetNavigationStage();
            if (currentStage == NavigationStage.Adjourning) { Forming(); }
            if (currentStage == NavigationStage.Forming || currentStage == NavigationStage.Performing) { Performing(); }
            if (currentStage == NavigationStage.Forming || currentStage == NavigationStage.Performing) { Adjourning(); }

            ResetPos();
            InitializeBot();

            Test(); // todo-moch: need to delete
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
                _sceneState = GameObject.Find("Scene Management").GetComponent<SceneState>();

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

                // all indicator disappear & teleport passenger and set _currentStage
                if (gripper < 0.00001f)
                {
                    _formingPassengerPreview.SetActive(false);

                    var passenger = _sceneState.GetAnotherUser(gameObject);
                    if (passenger != null)
                    {
                        var passengerHeadRotation = passenger.GetComponent<AvatarAnatomy>().head.transform.rotation; // read from object's avatar anatomy
                        var rotationOffset = _formingPassengerPreview.transform.rotation * (Quaternion.Inverse(passengerHeadRotation) * passenger.transform.rotation);
                        var rotation = Quaternion.Euler(0, rotationOffset.eulerAngles.y, 0);
                        var position = new Vector3(
                            _formingPassengerPreview.transform.position.x,
                            _formingPassengerPreview.transform.position.y + 0.5f,
                            _formingPassengerPreview.transform.position.z);
                        // _xrRig.transform.position = position;
                        // _xrRig.transform.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
                        Debug.Log($"object caller: {gameObject.name}");
                        passenger.GetComponent<NetworkUser>().Teleport(position, rotation); // write to object's view anatomy

                        photonView.RPC("SetFormingStage", RpcTarget.All, gameObject, passenger);
                    }

                    Debug.Log("No passenger in scene to do forming");
                }
            }
        }

        private void Performing()
        {
            var myRole = _sceneState.GetNavigationRole(gameObject);
            if (myRole == NavigationRole.Navigator) { Performing_Navigator(); }
            if (myRole == NavigationRole.Passenger) { Performing_Passenger(); }
        }

        private void Performing_Navigator()
        {
            // 1. Select navigator position #b
            // 2. Select passenger position, 4 or 8 direction, how far #b
            // 3. Select passenger rotation
            // 4. Select gap between passenger and navigator #b
            // 5. Group Teleportation

            // Circular zone might be photon.instantiate and serialization position ? brcause it will update all the time
        }

        private void Performing_Passenger()
        {
            // 1. Ability for passenger to cancel #b
        }

        private void Adjourning()
        {
            _controller.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool secondaryButton);
            if (secondaryButton && _sceneState.GetNavigationRole(gameObject) == NavigationRole.Navigator)
            {
                _sceneState.SetAdjourningStage();
            }
        }

        private void InitializeBot()
        {

        }

        private void ResetPos()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                _xrRig.transform.localPosition = Vector3.zero;
                _xrRig.transform.localRotation = Quaternion.identity;
            }
        }

        private void Test()
        {
            Action ForceTeleport = () =>
            {
                var position = new Vector3(_xrRig.transform.position.x, _xrRig.transform.position.y, _xrRig.transform.position.z);
                var rotation = _xrRig.transform.rotation;
                _sceneState.GetAnotherUser(gameObject).GetComponent<NetworkUser>().Teleport(position, rotation);
            };

            if (Input.GetKeyDown(KeyCode.F1))
            {
                photonView.RPC("UpdateMessageToAllClient", RpcTarget.All);
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                Debug.Log(_sceneState.GetLocalMessage());
            }
        }
    }
}

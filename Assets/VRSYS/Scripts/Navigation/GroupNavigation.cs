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
        private AvatarHMDAnatomy _avatarHMDAnatomy;
        private GameObject _xrRig;
        private GameObject _camera;
        private GameObject _rightHand;
        private XRController _controller;
        private LineRenderer _lineRenderer;

        // FORMING MEMBERS
        private GameObject _passenger;
        private GameObject _formingPoint;
        private GameObject _formingPassengerPreview;

        // PERFORMING MEMBERS
        private GameObject _circularZone;
        private GameObject _navigatorJumpingPoint;
        private GameObject _passengerJumpingPoint;
        private bool _navigatorJumpingConfirmation;

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

            ResetEverything();
            Test(); // todo-moch: need to delete
        }

        private bool AreDevicesReady()
        {
            if (NetworkUser.localNetworkUser == null) { return false; }
            
            if (_viewingSetupHmd == null && NetworkUser.localNetworkUser.viewingSetupAnatomy is ViewingSetupHMDAnatomy)
            {
                _viewingSetupHmd = (ViewingSetupHMDAnatomy) NetworkUser.localNetworkUser.viewingSetupAnatomy;
                _avatarHMDAnatomy = GetComponent<AvatarHMDAnatomy>();
            }

            if (_viewingSetupHmd != null && !_oneTimeSetup)
            {
                _oneTimeSetup = true;
                _sceneState = GameObject.Find("Scene Management").GetComponent<SceneState>();

                _xrRig = _viewingSetupHmd.childAttachmentRoot;
                _camera = _viewingSetupHmd.mainCamera;
                _rightHand = _viewingSetupHmd.rightController;
                _controller = _rightHand.GetComponent<XRController>();
                
                _lineRenderer = GetComponent<LineRenderer>();
                _lineRenderer.material.color = _avatarHMDAnatomy.body.GetComponentInChildren<MeshRenderer>().material.color;

                _formingPoint = GameObject.Find("Forming Intersection Point");
                _formingPoint.GetComponent<MeshRenderer>().material.color = _lineRenderer.material.color;
                _formingPassengerPreview = Instantiate(Resources.Load("UserPrefabs/Avatars/AvatarHMD-PosePreview"), Vector3.zero, Quaternion.identity) as GameObject;
                var shirtModel = _formingPassengerPreview.transform.Find("Head/Body/shirtMale/default").gameObject;
                var headModel = _formingPassengerPreview.transform.Find("Head/HeadModel/Head/HeadMesh").gameObject;
                var arrowModel = _formingPassengerPreview.transform.Find("arrow_ring (2)/default").gameObject;
                shirtModel.GetComponent<MeshRenderer>().material.color = Color.white;
                headModel.GetComponent<MeshRenderer>().material.color = Color.white;
                arrowModel.GetComponent<MeshRenderer>().material.color = Color.white;
                _formingPassengerPreview.SetActive(false);

                // todo-moch: to be remove
                _circularZone = Instantiate(Resources.Load("UserPrefabs/CircularZone"), Vector3.zero, Quaternion.identity) as GameObject;
                _navigatorJumpingPoint = _circularZone.transform.Find("Navigator Jumping Point").gameObject;
                _passengerJumpingPoint = _circularZone.transform.Find("Passenger Jumping Point").gameObject;
                _navigatorJumpingPoint.GetComponent<MeshRenderer>().material.color = _lineRenderer.material.color;
                _passengerJumpingPoint.GetComponent<MeshRenderer>().material.color = Color.red;
                //_circularZone.SetActive(false);
                _circularZone.SetActive(true); // should be trigger
                _lineRenderer.enabled = true; // should be trigger
            }

            return _viewingSetupHmd != null;
        }

        private void Forming()
        {
            _controller.inputDevice.TryGetFeatureValue(CommonUsages.grip, out float gripper);

            // render ray and intersection point
            var isRayHit = false;
            _lineRenderer.enabled = gripper > 0.00001f;
            if (_lineRenderer.enabled)
            {
                isRayHit = Physics.Raycast(_rightHand.transform.position,
                                        _rightHand.transform.TransformDirection(Vector3.forward),
                                        out RaycastHit hit, 7f, layerMask);

                _formingPoint.SetActive(isRayHit);
                _formingPoint.transform.position = hit.point;
                _lineRenderer.SetPosition(0, _rightHand.transform.position);
                _lineRenderer.SetPosition(1, isRayHit ? hit.point : _rightHand.transform.position + _rightHand.transform.TransformDirection(Vector3.forward) * 7);
            }
            else
            {
                _formingPoint.SetActive(false);
            }

            // select passenger position to be form as a group
            if (!_formingPassengerPreview.activeInHierarchy && isRayHit && gripper > 0.99f)
            {
                _formingPassengerPreview.SetActive(true);
                _formingPassengerPreview.transform.position = new Vector3(
                    _formingPoint.transform.position.x,
                    _formingPoint.transform.position.y,
                    _formingPoint.transform.position.z);
            }

            // while the grip is fully pressed update the passenger direction to face the current intersection
            if (_formingPassengerPreview.activeInHierarchy)
            {
                var lookRotation = Quaternion.LookRotation(_lineRenderer.GetPosition(1) - _formingPassengerPreview.transform.position);
                _formingPassengerPreview.transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);

                // all indicator disappear & teleport passenger and set _currentStage
                if (gripper < 0.00001f)
                {
                    _formingPassengerPreview.SetActive(false);

                    var passengerId = _sceneState.GetAnotherUser(photonView.ViewID);
                    _passenger = PhotonView.Find(passengerId)?.gameObject ?? null;
                    Debug.Log($"[FORMING] passenger: {_passenger?.name ?? "none"}");
                    if (_passenger != null)
                    {
                        var passengerHeadRotation = _passenger.GetComponent<AvatarAnatomy>().head.transform.rotation; // read from object's avatar anatomy
                        var rotationOffset = _formingPassengerPreview.transform.rotation * (Quaternion.Inverse(passengerHeadRotation) * _passenger.transform.rotation);
                        var rotation = Quaternion.Euler(0, rotationOffset.eulerAngles.y, 0);
                        _passenger.GetComponent<NetworkUser>().Teleport(_formingPassengerPreview.transform.position, rotation, true); // write to object's view anatomy
                        // _viewingSetupHmd.Teleport(_formingPassengerPreview.transform.position, rotation, true);

                        photonView.RPC("SetFormingStage", RpcTarget.All, photonView.ViewID, passengerId);
                    }
                }
            }
        }

        private void Performing()
        {
            var myRole = _sceneState.GetNavigationRole(photonView.ViewID);
            //var myRole = NavigationRole.Navigator;
            if (myRole == NavigationRole.Navigator) { Performing_Navigator(); }
            if (myRole == NavigationRole.Passenger) { Performing_Passenger(); }
        }

        private void Performing_Navigator()
        {
            // 1) Make circular functionality work #SATURDAY
            // > connect linerenderer with Navigator Jumping Point, use same color as user #done
            // > connect linerenderer with Passenger Jumping Point, use same color as user #done
            // > use joystick to control Passenger Jumping Point with proper speed, script attch to passenger ball #done
            // > display point when collide with direction cube --> change color of direction cube #done

            // render navigator ray
            _lineRenderer.SetPosition(0, _rightHand.transform.position); 
            _lineRenderer.SetPosition(1, _navigatorJumpingPoint.transform.position);
            if (!_navigatorJumpingConfirmation)
            {
                // render circular zone depend on hand position
                _circularZone.transform.position = _rightHand.transform.position + _rightHand.transform.TransformDirection(Vector3.forward) * 7; // todo-moch: be able to adjust position
                _circularZone.transform.rotation = Quaternion.Euler(0, _rightHand.transform.rotation.eulerAngles.y, 0);

                // confirm circular zone -> prepare passenger selection stuff
                _controller.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButton);
                if (primaryButton)
                {
                    Debug.Log("[PERFORMING] confirm navigator jumping position");
                    _navigatorJumpingConfirmation = true;

                    _passengerJumpingPoint.SetActive(true);
                    // _passengerLine.SetActive(true);
                }
            }

            if (_navigatorJumpingConfirmation)
            {
                // render passenger ray
                //_passengerLine.SetPosition(0, _passenger.rightHand.transform.position);
                //_passengerLine.SetPosition(1, _passengerJumpingPoint.transform.position);

                // render passenger jumping position by adjusting x and z of passengerPoint object
                _controller.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystick); // joystick is x,y = 0-->1
                _passengerJumpingPoint.transform.localPosition = new Vector3(
                    _navigatorJumpingPoint.transform.localPosition.x + (joystick.x * 2.3f),
                    _navigatorJumpingPoint.transform.localPosition.y,
                    _navigatorJumpingPoint.transform.localPosition.z + (joystick.y * 2.3f)
                );

                // confirm passenger position -> group teleport -> prepare navigator selection stuff
                var isValidPosition = _passengerJumpingPoint.GetComponent<PropagateColor>().IsColliding();
                _controller.inputDevice.TryGetFeatureValue(CommonUsages.grip, out float gripper);
                if (isValidPosition && gripper > 0.99f)
                {
                    Debug.Log("[PERFORMING] confirm passenger jumping position");
                    _navigatorJumpingConfirmation = false;

                    Debug.Log("[PERFORMING] group jumping");
                    _viewingSetupHmd.Teleport(_navigatorJumpingPoint.transform.position, Quaternion.identity, false);
                    _passenger.GetComponent<NetworkUser>().Teleport(_passengerJumpingPoint.transform.position, Quaternion.identity, false);

                    _passengerJumpingPoint.SetActive(false);
                    // _passengerLine.SetActive(false); // remove comment
                }
            }

            // 3) Make circular zone and line as distributed object #SUNDAY
            // > spawn this prefab as distributed object, photon.instantiate(prefab) and serialization position
            // > make line as distributed object

            // 4) Ability for passenger to cancel (primary button)
        }

        private void Performing_Passenger()
        {
            // 1. Ability for passenger to cancel (primary button)
        }

        private void Adjourning()
        {
            _controller.inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButton);
            if (secondaryButton && _sceneState.GetNavigator() == photonView.ViewID)
            {
                // todo-moch: maybe need to reset stuff and state of circular zone
                Debug.Log("[ADJOURNING] group terminated");
                photonView.RPC("SetAdjourningStage", RpcTarget.All);
            }
        }

        private void ResetEverything()
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
                //_sceneState.GetAnotherUser(photonView.ViewID)..GetComponent<NetworkUser>().Teleport(position, rotation);
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

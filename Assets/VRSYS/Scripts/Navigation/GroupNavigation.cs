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
        private GameObject _camera;
        private GameObject _rightHand;
        private XRController _controller;
        private LineRenderer _lineRenderer;

        // FORMING MEMBERS
        private int _passengerId = -1;
        private GameObject _passenger;
        private GameObject _formingPoint;
        private GameObject _formingPassengerPreview;

        // PERFORMING MEMBERS
        private float _circularDistance = 7f;
        private GameObject _circularZone;
        private GameObject _navigatorJumpingPoint;
        private GameObject _passengerJumpingPoint;

        void Start()
        {
            if (!photonView.IsMine) { Destroy(this); } // This script should only compute for the local user
        }

        void Update()
        {
            if (!AreDevicesReady()) 
            { 
                return; 
            }

            var currentStage = _sceneState.GetNavigationStage();
            var myRole = _sceneState.GetNavigationRole(photonView.ViewID);
            if (currentStage == NavigationStage.Adjourning) { Forming(); }
            if (currentStage != NavigationStage.Adjourning) { Performing(currentStage, myRole); }
            if (currentStage != NavigationStage.Adjourning) { Adjourning(myRole); }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                photonView.RPC("SetAdjourningStage", RpcTarget.All);
                _viewingSetupHmd.Teleport(new Vector3(76f, 22f, 31f), Quaternion.identity, true);
            }
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

                _camera = _viewingSetupHmd.mainCamera;
                _rightHand = _viewingSetupHmd.rightController;
                _controller = _rightHand.GetComponent<XRController>();
                _lineRenderer = GetComponent<LineRenderer>();

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
                isRayHit = Physics.Raycast(_rightHand.transform.position, _rightHand.transform.TransformDirection(Vector3.forward), out RaycastHit hit, 7f, layerMask);

                _formingPoint.SetActive(isRayHit);
                _formingPoint.transform.position = hit.point;
                NetworkUser.DrawLinearLine(_lineRenderer, _rightHand.transform.position, isRayHit ? hit.point : _rightHand.transform.position + _rightHand.transform.TransformDirection(Vector3.forward) * 7);
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

                // teleport passenger and prepare next stage
                if (gripper < 0.00001f)
                {
                    _formingPassengerPreview.SetActive(false);

                    _passengerId = _sceneState.GetAnotherUser(photonView.ViewID);
                    _passenger = PhotonView.Find(_passengerId)?.gameObject ?? null;
                    photonView.RPC("AnnouceMessage", RpcTarget.All, $"[FORMING] passenger: {_passenger?.name ?? "none"}");
                    if (_passenger != null)
                    {
                        photonView.RPC("SetFormingStage", RpcTarget.All, photonView.ViewID, _passengerId);
                        var passengerHeadRotation = _passenger.GetComponent<AvatarAnatomy>().head.transform.rotation; // read from object's avatar anatomy
                        var rotationOffset = _formingPassengerPreview.transform.rotation * (Quaternion.Inverse(passengerHeadRotation) * _passenger.transform.rotation);
                        var rotation = Quaternion.Euler(0, rotationOffset.eulerAngles.y, 0);
                        _passenger.GetComponent<NetworkUser>().Teleport(_formingPassengerPreview.transform.position, rotation, true); // write to object's view anatomy

                        // prepare next stage
                        _lineRenderer.enabled = true;
                        _circularZone = PhotonNetwork.Instantiate("UserPrefabs/CircularZone", Vector3.zero, Quaternion.identity);
                        _navigatorJumpingPoint = _circularZone.GetComponent<CircularZone>().NavigatorJumpingPoint;
                        _passengerJumpingPoint = _circularZone.GetComponent<CircularZone>().PassengerJumpingPoint;
                    }
                }
            }
        }

        private void Performing(NavigationStage stage, NavigationRole myRole)
        {
            // var myRole = NavigationRole.Navigator; // todo-moch-test
            if (myRole == NavigationRole.Navigator) { Performing_Navigator(stage); }
            if (myRole == NavigationRole.Passenger) { Performing_Passenger(stage); }
        }

        private void Performing_Navigator(NavigationStage stage)
        {
            // render navigator ray
            NetworkUser.DrawQuadraticBezierCurve(
                _lineRenderer,
                _rightHand.transform.position,
                _rightHand.transform.position + _rightHand.transform.TransformDirection(new Vector3(0, 1.5f, 1.5f)),
                _navigatorJumpingPoint.transform.position);

            if (stage != NavigationStage.Performing)
            {
                // render circular zone depend on hand position
                _circularZone.transform.rotation = Quaternion.Euler(0, _rightHand.transform.rotation.eulerAngles.y, 0);
                _circularZone.transform.position = _rightHand.transform.position + _rightHand.transform.TransformDirection(Vector3.forward) * _circularDistance;

                // controling circular zone
                _controller.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystick); // joystick is x,y = 0-->1
                _circularDistance += (joystick.y * 0.1f);

                // confirm circular zone -> prepare passenger selection stuff
                _controller.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButton);
                if (primaryButton)
                {
                    photonView.RPC("AnnouceMessage", RpcTarget.All, "[PERFORMING] confirm navigator's jumping position");
                    photonView.RPC("SetPerformingStage", RpcTarget.All);

                    photonView.RPC("TogglePassengerPoint", RpcTarget.All, true);
                    _passenger.GetComponent<NetworkUser>().ToggleLineRenderer(true);
                }
            }

            if (stage == NavigationStage.Performing)
            {
                // render passenger jumping position by adjusting x and z of passengerPoint object
                _controller.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystick); // joystick is x,y = 0-->1
                _passengerJumpingPoint.transform.localPosition = new Vector3(
                    _navigatorJumpingPoint.transform.localPosition.x + (joystick.x * 2.6f),
                    _navigatorJumpingPoint.transform.localPosition.y,
                    _navigatorJumpingPoint.transform.localPosition.z + (joystick.y * 2.6f)
                );

                // confirm passenger position -> group teleport -> prepare navigator selection stuff
                var isValidPosition = _passengerJumpingPoint.GetComponent<PassengerPoint>().IsColliding();
                _controller.inputDevice.TryGetFeatureValue(CommonUsages.grip, out float gripper);
                if (isValidPosition && gripper > 0.99f)
                {
                    photonView.RPC("AnnouceMessage", RpcTarget.All, "[PERFORMING] confirm passenger's jumping position");
                    photonView.RPC("SetFormingStage", RpcTarget.All, photonView.ViewID, _passengerId);

                    photonView.RPC("AnnouceMessage", RpcTarget.All, "[PERFORMING] group jumping");
                    _viewingSetupHmd.Teleport(_navigatorJumpingPoint.transform.position, Quaternion.identity, false);
                    _passenger.GetComponent<NetworkUser>().Teleport(_passengerJumpingPoint.transform.position, Quaternion.identity, false);

                    photonView.RPC("TogglePassengerPoint", RpcTarget.All, false);
                    _passenger.GetComponent<NetworkUser>().ToggleLineRenderer(false);
                }
            }
        }

        private void Performing_Passenger(NavigationStage stage)
        {
            if (stage != NavigationStage.Performing) { return; }

            // handle null or object reference mis-match
            if (_circularZone == null || _circularZone.GetPhotonView().ViewID != _sceneState.GetCircularZone())
            {
                _circularZone = PhotonView.Find(_sceneState.GetCircularZone()).gameObject;
                _navigatorJumpingPoint = _circularZone.GetComponent<CircularZone>().NavigatorJumpingPoint;
                _passengerJumpingPoint = _circularZone.GetComponent<CircularZone>().PassengerJumpingPoint;
            }

            // render passenger ray
            NetworkUser.DrawQuadraticBezierCurve(
                _lineRenderer,
                _rightHand.transform.position,
                _rightHand.transform.position + _rightHand.transform.TransformDirection(new Vector3(0, 1.5f, 1.5f)),
                _passengerJumpingPoint.transform.position);

            // ability for passenger to cancel
            _controller.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButton);
            if (primaryButton)
            {
                photonView.RPC("AnnouceMessage", RpcTarget.All, "[PERFORMING] passenger reset performing --> forming part");
                
                photonView.RPC("SetFormingStage", RpcTarget.All, _sceneState.GetNavigator(), photonView.ViewID);
                photonView.RPC("TogglePassengerPoint", RpcTarget.All, false);
                _lineRenderer.enabled = false;
            }
        }

        private void Adjourning(NavigationRole myRole)
        {
            _controller.inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButton);
            if (secondaryButton && myRole == NavigationRole.Navigator)
            {
                photonView.RPC("AnnouceMessage", RpcTarget.All, "[ADJOURNING] group terminated");
                photonView.RPC("SetAdjourningStage", RpcTarget.All);
            }
        }
    }
}

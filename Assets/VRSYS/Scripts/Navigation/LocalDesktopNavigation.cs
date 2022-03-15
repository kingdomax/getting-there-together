using Photon.Pun;
using UnityEngine;

namespace Vrsys
{
    public class LocalDesktopNavigation : MonoBehaviourPunCallbacks
    {
        public KeyCode walkForwardKey = KeyCode.W;
        public KeyCode walkBackwardKey = KeyCode.S;
        public KeyCode walkLeftKey = KeyCode.A;
        public KeyCode walkRightKey = KeyCode.D;

        [Tooltip("Translation Velocity [m/sec]")]
        [Range(0.1f, 10.0f)]
        public float translationVelocity = 3.0f;

        [Tooltip("Rotation Velocity [degree/sec]")]
        [Range(1.0f, 10.0f)]
        public float rotationVelocity = 5.0f;

        private Vector3 rotInput = Vector3.zero;

        ViewingSetupAnatomy viewingSetup;

        private void Start()
        {
            // This script should only compute for the local user
            if (!photonView.IsMine)
            {
                Destroy(this);
            }
        }

        void Update()
        {
            // Only calculate & apply input if local user fully instantiated
            if (EnsureViewingSetup())
            {
                MapInput(CalcTranslationInput(), CalcRotationInput());
            }
        }

        bool EnsureViewingSetup()
        {
            if (viewingSetup == null)
            {
                if (NetworkUser.localNetworkUser != null)
                {
                    viewingSetup = NetworkUser.localNetworkUser.viewingSetupAnatomy;
                }
            }
            return viewingSetup != null;
        }

        private Vector3 CalcTranslationInput()
        {
            Vector3 transInput = Vector3.zero;

            // foward input
            transInput.z += Input.GetKey(walkForwardKey) ? 1.0f : 0.0f;
            transInput.z -= Input.GetKey(walkBackwardKey) ? 1.0f : 0.0f;
            transInput.x += Input.GetKey(walkRightKey) ? 1.0f : 0.0f;
            transInput.x -= Input.GetKey(walkLeftKey) ? 1.0f : 0.0f;

            return transInput * translationVelocity * Time.deltaTime;
        }

        private Vector3 CalcRotationInput()
        {
            float inputY = Input.GetAxis("Mouse X");
            float inputX = Input.GetAxis("Mouse Y");

            // head rot input
            rotInput.y += inputY * rotationVelocity;

            // pitch rot input
            rotInput.x -= inputX * rotationVelocity;
            rotInput.x = Mathf.Clamp(rotInput.x, -80, 80);

            return rotInput;
        }

        private void MapInput(Vector3 transInput, Vector3 rotInput)
        {
            // map translation input
            if (transInput.magnitude > 0.0f)
            {
                viewingSetup.mainCamera.transform.Translate(transInput);
            }

            // map rotation input
            if (rotInput.magnitude > 0.0f)
            {
                viewingSetup.mainCamera.transform.localRotation = Quaternion.Euler(rotInput.x, rotInput.y, 0.0f);
            }
        }
    }
}

using UnityEngine;

namespace Vrsys 
{
    public class AvatarHMDAnatomy : AvatarAnatomy
    {
        public GameObject body;
        public GameObject handLeft;
        public GameObject handRight;

        protected override void ParseComponents()
        {
            base.ParseComponents();

            if (body == null)
            {
                body = transform.Find("Head/Body")?.gameObject;
            }
            if (handLeft == null)
            {
                handLeft = transform.Find("HandLeft")?.gameObject;
            }
            if (handRight == null)
            {
                handRight = transform.Find("HandRight")?.gameObject;
            }
        }

        public override void ConnectFrom(ViewingSetupAnatomy viewingSetup)
        {
            if (viewingSetup is ViewingSetupHMDAnatomy)
            {
                var hmd = viewingSetup as ViewingSetupHMDAnatomy;

                head.transform.position = Vector3.zero;
                head.transform.rotation = Quaternion.identity;
                head.transform.SetParent(hmd.mainCamera.transform, false);

                handLeft.transform.position = Vector3.zero;
                handLeft.transform.rotation = Quaternion.identity;
                handLeft.transform.SetParent(hmd.leftController.transform, false);

                handRight.transform.position = Vector3.zero;
                handRight.transform.rotation = Quaternion.identity;
                handRight.transform.SetParent(hmd.rightController.transform, false);
            }
            else 
            {
                throw new System.ArgumentException("Incompatible viewing setup. Connection only supported with '" + typeof(ViewingSetupHMDAnatomy).Name + "'.");
            }
        }

        public override void SetColor(Color color)
        {
            if (body == null)
            {
                Debug.LogWarning("Cannot change avatar color. Body not set.");
                return;
            }

            var renderer = body.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
            else
            {
                Debug.LogWarning("Cannot change avatar color. No renderer set in children of body.");
            }
        }

        public override Color? GetColor()
        {
            if (body == null) 
            {
                return null;
            }

            var renderer = body.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                return renderer.material.color;
            }

            return null;
        }
    }
}

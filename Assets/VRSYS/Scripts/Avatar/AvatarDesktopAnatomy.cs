using UnityEngine;

namespace Vrsys
{
    public class AvatarDesktopAnatomy : AvatarAnatomy
    {
        public GameObject body;

        protected override void ParseComponents()
        {
            base.ParseComponents();
        
            if (body == null)
            {
                body = transform.Find("Head/Body")?.gameObject;
            }
        }

        public override void ConnectFrom(ViewingSetupAnatomy viewingSetup)
        {
            head.transform.position = Vector3.zero;
            head.transform.rotation = Quaternion.identity;
            head.transform.SetParent(viewingSetup.mainCamera.transform, false);
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

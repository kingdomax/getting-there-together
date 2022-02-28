using System;
using UnityEngine;

namespace Vrsys 
{
    public abstract class AvatarAnatomy : MonoBehaviour
    {
        public GameObject head;
        public GameObject nameTag;

        private void Awake()
        {
            ParseComponents();
        }

        protected virtual void ParseComponents()
        {
            if (head == null)
            {
                head = transform.Find("Head")?.gameObject;
            }
            if (nameTag == null)
            {
                nameTag = transform.Find("Head/NameTag")?.gameObject;
            }
        }

        public abstract void ConnectFrom(ViewingSetupAnatomy viewingSetup);

        public abstract void SetColor(Color color);

        public abstract Color? GetColor();
    }
}

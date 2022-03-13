using UnityEngine;

namespace Vrsys
{
    public class PropagateColor : MonoBehaviour
    {
        private Color _myColor;
        private Collider _collider;
        private bool _isColliding;
        public bool IsColliding() => _isColliding;

        void OnEnable()
        {
            _isColliding = false;
            _collider = null;

            _myColor = GetComponent<MeshRenderer>().material.color;
        }

        void OnDisable()
        {
            _isColliding = false;
            if(_collider != null)
            {
                _collider.GetComponent<MeshRenderer>().material.color = Color.white;
                _collider = null;
            }

            transform.localPosition = new Vector3(0, 0.05f, -2);
        }

        // When a GameObject collides with another GameObject, Unity calls OnTriggerEnter.
        void OnTriggerEnter(Collider collider)
        {
            if (collider.tag == "CircularDirection")
            {
                _isColliding = true;
                _collider = collider;

                collider.GetComponent<MeshRenderer>().material.color = _myColor;
            }
        }

        // OnTriggerExit is called when the Collider other has stopped touching the trigger.
        void OnTriggerExit(Collider collider)
        {
            if (collider.tag == "CircularDirection")
            {
                _isColliding = false;
                _collider = null;

                collider.GetComponent<MeshRenderer>().material.color = Color.white;
            }
        }
    }
}

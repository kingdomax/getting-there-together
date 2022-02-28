using UnityEngine;

namespace Vrsys
{
    public class YawTowardsLocalHead : MonoBehaviour
    {
        void Update()
        {
            if (NetworkUser.localHead)
            {
                Vector3 lookDir = NetworkUser.localHead.transform.position - transform.position;

                if (lookDir == Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(NetworkUser.localHead.transform.forward);
                }
                else
                {
                    lookDir.y = 0.0f;
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
        }
    }
}

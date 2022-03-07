using UnityEngine;
using System.Collections.Generic;

namespace Vrsys
{
    public class SceneDirector : MonoBehaviour
    {
        // EXPOSED MEMBERS
        public static List<GameObject> AllUsers;
        
        void Start()
        {
            AllUsers = new List<GameObject>();
        }

        public static void AppendUserToList(GameObject self) => AllUsers.Add(self);
        public static GameObject GetAnotherUser(GameObject self) => AllUsers.Find(u => u.name != self.name);
    }
}

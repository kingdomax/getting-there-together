using UnityEngine;
using System.Collections.Generic;

namespace Vrsys
{
    public class SceneState : MonoBehaviour
    {
        private List<GameObject> AllUsers;
        private NavigationStage CurrentStage;
        private GameObject Navigator;
        private GameObject Passenger;

        void Start()
        {
            AllUsers = new List<GameObject>();

            CurrentStage = NavigationStage.Adjourning;
            Navigator = null;
            Passenger = null;
        }

        public void AppendUserToList(GameObject self) => AllUsers.Add(self);
        public void RomoveUserFromList(GameObject self) => AllUsers.Remove(self);
        public GameObject GetAnotherUser(GameObject self) => AllUsers.Find(u => u.name != self.name);

        public NavigationStage GetNavigationStage() => CurrentStage;
        public GameObject GetNavigator() => Navigator;
        public GameObject GetPassenger() => Passenger;

        public void SetFormingStage(GameObject self, GameObject passenger)
        {
            if (Navigator == null && Passenger == null) 
            {
                CurrentStage = NavigationStage.Forming;
                Navigator = self;
                Passenger = passenger;
            }
        }
        
        public void SetAdjourningStage(GameObject self)
        {
            if (Navigator?.name == self.name) 
            {
                CurrentStage = NavigationStage.Adjourning;
                Navigator = null;
                Passenger = null;
            }
        }
    }
}

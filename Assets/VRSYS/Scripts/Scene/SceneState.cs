using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

namespace Vrsys
{
    // No logic
    public class SceneState : MonoBehaviour
    {
        private List<GameObject> AllUsers;
        private NavigationStage CurrentStage;
        private GameObject Navigator;
        private GameObject Passenger;

        private string Message = "initial msg";// todo-moch: to be remove

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
        public NavigationRole GetNavigationRole(GameObject obj)
        {
            if (Navigator?.name == obj.name) { return NavigationRole.Navigator; }
            if (Passenger?.name == obj.name) { return NavigationRole.Passenger; }
            return NavigationRole.Observer;
        }

        public void SetFormingStage(GameObject navigator, GameObject passenger)
        {
            CurrentStage = NavigationStage.Forming;
            Navigator = navigator;
            Passenger = passenger;
        }
        
        public void SetAdjourningStage()
        {
            CurrentStage = NavigationStage.Adjourning;
            Navigator = null;
            Passenger = null;
        }




        // todo-moch: to be removed
        public string GetLocalMessage() => Message;
        public void UpdateMessage(string overrideMsg) => Message = overrideMsg;
    }
}

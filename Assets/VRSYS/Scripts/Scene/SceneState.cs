using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Vrsys
{
    // No logic, only data-driven class
    public class SceneState : MonoBehaviour
    {
        private int CircularZoneID;
        private NavigationStage CurrentStage;
        private Dictionary<int, NavigationRole> AllUsers;

        void Start()
        {
            CircularZoneID = -1;
            CurrentStage = NavigationStage.Adjourning;
            // CurrentStage = NavigationStage.Forming; // todo-moch-test
            AllUsers = new Dictionary<int, NavigationRole>();
        }

        public NavigationStage GetNavigationStage() => CurrentStage;
        public int GetCircularZone() => CircularZoneID;
        public void SetCircularZone(int id) => CircularZoneID = id;
        public void RomoveUserFromList(int userId) => AllUsers.Remove(userId);
        public void AppendUserToList(int userId) => AllUsers.Add(userId, NavigationRole.Observer);
        public int GetAnotherUser(int myId) => AllUsers.Where(u => u.Key != myId)?.FirstOrDefault().Key ?? -1;
        public int GetNavigator() => AllUsers.Where(u => u.Value == NavigationRole.Navigator)?.FirstOrDefault().Key ?? -1;
        public int GetPassenger() => AllUsers.Where(u => u.Value == NavigationRole.Passenger)?.FirstOrDefault().Key ?? -1;
        public NavigationRole GetNavigationRole(int userId) => AllUsers.TryGetValue(userId, out NavigationRole role) ? role : NavigationRole.Observer;

        public void SetFormingStage(int navigator, int passenger)
        {
            CurrentStage = NavigationStage.Forming;
            AllUsers[navigator] = NavigationRole.Navigator;
            AllUsers[passenger] = NavigationRole.Passenger;
        }

        public void SetPerformingStage()
        {
            CurrentStage = NavigationStage.Performing;
        }

        public void SetAdjourningStage()
        {
            CircularZoneID = -1;
            CurrentStage = NavigationStage.Adjourning;
            foreach (var key in AllUsers.Keys.ToList())
            {
                AllUsers[key] = NavigationRole.Observer;
            }
        }
    }
}

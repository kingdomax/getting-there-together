using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Vrsys
{
    // No logic
    public class SceneState : MonoBehaviour
    {
        private NavigationStage CurrentStage;
        private Dictionary<int, NavigationRole> AllUsers;

        private string Message = "initial msg";// todo-moch: to be remove

        void Start()
        {
            // CurrentStage = NavigationStage.Forming; // todo-moch: for testing
            CurrentStage = NavigationStage.Adjourning;
            AllUsers = new Dictionary<int, NavigationRole>();
        }

        public NavigationStage GetNavigationStage() => CurrentStage;

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
        
        public void SetAdjourningStage()
        {
            CurrentStage = NavigationStage.Adjourning;
            foreach (var key in AllUsers.Keys.ToList())
            {
                AllUsers[key] = NavigationRole.Observer;
            }
        }

        // todo-moch: to be removed
        public string GetLocalMessage() => Message;
        public void UpdateMessage(string overrideMsg) => Message = overrideMsg;
    }
}

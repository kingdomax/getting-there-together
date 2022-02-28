using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;

using TMPro;

namespace Vrsys
{
    public class VoiceManager : MonoBehaviourPunCallbacks
    {
        private static VoiceManager s_Instance = null;
        public static VoiceManager Instance
        {
            get
            {
                return s_Instance;
            }
        }
        private void Awake()
        {
            if (s_Instance != null)
            {
                Destroy(this);

                return;
            }

            s_Instance = this;
        }

        public List<string> availableMicrophones;
        public Recorder userRecorder;
        public string selectedMicrophone;
        public bool isMuted = false;
        public bool isRecording = false;

        void Start()
        {
            userRecorder = GetComponent<Recorder>();

            availableMicrophones = Microphone.devices.ToList();
            userRecorder.TransmitEnabled = !isMuted;
            if (availableMicrophones.Count > 0)
            {
                userRecorder.UnityMicrophoneDevice = availableMicrophones[0];
                userRecorder.RestartRecording();
                isRecording = true;
            }
            else
            {
                Debug.LogWarning("No available microphones detected.");
            }

            Debug.Log("Connecting Voice...");
            GetComponent<ConnectAndJoin>().RoomName = PhotonNetwork.CurrentRoom.Name;
            GetComponent<ConnectAndJoin>().ConnectNow();
            userRecorder.Init(GetComponent<VoiceConnection>());
        }

        public override void OnJoinedRoom()
        {

        }

        public List<string> GetMicrophones()
        {
            availableMicrophones = Microphone.devices.ToList();
            return availableMicrophones;
        }

        public void SetMicrophone(string microphone)
        {
            if (availableMicrophones.Contains(microphone))
            {
                selectedMicrophone = microphone;
                userRecorder.UnityMicrophoneDevice = selectedMicrophone;
                userRecorder.RestartRecording();
            }
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            userRecorder.TransmitEnabled = !isMuted;
        }

        public void StartRecording()
        {
            if (!isRecording)
            {
                userRecorder.RestartRecording();
            }
        }

        public void StopRecording()
        {
            if (isRecording)
            {
                userRecorder.StopRecording();
            }
        }

    }
}
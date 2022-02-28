using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Vrsys
{
    public class VoiceMenu : MonoBehaviour
    {
        public VoiceManager voiceManager;
        public Toggle muteToggle;
        public TMP_Dropdown microphoneDropdown;

        private void Awake()
        {
            if (voiceManager == null)
            {
                voiceManager = GameObject.Find("VoiceManager").GetComponent<VoiceManager>();
            }

            if (muteToggle == null)
            {
                muteToggle = GameObject.Find("MuteToggle").GetComponent<Toggle>();
            }

            if (voiceManager.isMuted)
            {
                muteToggle.isOn = true;
                //muteToggle.enabled = 
            }
            else
            {
                muteToggle.isOn = false;
            }
            muteToggle.onValueChanged.AddListener(delegate { ToggleMute(); });

            if (microphoneDropdown == null)
            {
                microphoneDropdown = GameObject.Find("MicrophoneDrop").GetComponent<TMP_Dropdown>();
            }
            microphoneDropdown.onValueChanged.AddListener(delegate { SelectMicrophone(); });


        }
        // Start is called before the first frame update
        void Start()
        {
            microphoneDropdown.ClearOptions();
            microphoneDropdown.AddOptions(voiceManager.GetMicrophones());
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void ToggleMute()
        {
            voiceManager.ToggleMute();
        }

        private void SelectMicrophone()
        {
            voiceManager.SetMicrophone(microphoneDropdown.options[microphoneDropdown.value].text);
            voiceManager.StartRecording();
            microphoneDropdown.RefreshShownValue();
        }
    }
}
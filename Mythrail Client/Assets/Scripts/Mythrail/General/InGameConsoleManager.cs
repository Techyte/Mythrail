using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Mythrail.General
{
    public class InGameConsoleManager : MonoBehaviour
    {
        public static InGameConsoleManager Instance;
        
        [SerializeField] private Transform parent;
        [SerializeField] private GameObject disableableParent;
        [SerializeField] private TextMeshProUGUI logObjPrefab;
        [SerializeField] private List<TextMeshProUGUI> displays;

        private bool _isOn;

        private List<MythrailConsoleMessage> _currentMessages = new List<MythrailConsoleMessage>();

        private void Awake()
        {
            Instance = this;
            
            Application.logMessageReceived += HijackLogMessage;
            SetShowConsole(false);
        }

        private void Update()
        {
            for (int i = 0; i < displays.Count; i++)
            {
                if (_currentMessages.Count >= i+1)
                {
                    displays[i].text = _currentMessages[i].message;
                    displays[i].color = _currentMessages[i].colour;
                }
                else
                {
                    displays[i].text = String.Empty;
                    displays[i].color = Color.green;
                }
            }
        }

        private void HijackLogMessage(string condition, string stackTrace, LogType type)
        {
            if (_currentMessages.Count >= displays.Count)
            {
                ClearOldestAndMoveUp();
            }
            
            Color colour = Color.green;
            
            switch (type)
            {
                case LogType.Warning:
                    colour = Color.yellow;
                    break;
                case LogType.Exception:
                    colour = Color.red;
                    break;
                case LogType.Error:
                    colour = Color.red;
                    break;
                case LogType.Assert:
                    colour = Color.red;
                    break;
            }

            MythrailConsoleMessage message = new MythrailConsoleMessage(condition, colour);
            _currentMessages.Add(message);
        }

        private void ClearOldestAndMoveUp()
        {
            Debug.Log("clearing and moving up");
            
            string lastMessage = String.Empty;
            Color lastMessageColour = Color.green;

            for (int i = _currentMessages.Count-1; i >= 0; i--)
            {
                string currentMessage = _currentMessages[i].message;
                Color currentMessageColour = _currentMessages[i].colour;

                if (i != _currentMessages.Count-1)
                {
                    _currentMessages[i].message = lastMessage;
                    _currentMessages[i].colour = lastMessageColour;
                }

                lastMessage = currentMessage;
                lastMessageColour = currentMessageColour;
            }

            _currentMessages.RemoveAt(_currentMessages.Count-1);
        }

        public void Clear()
        {
            for (int i = 0; i < _currentMessages.Count; i++)
            {
                _currentMessages[i].message = null;
            }
        }

        public void SetShowConsole(bool value)
        {
            _isOn = value;
            disableableParent.SetActive(_isOn);
        }

        private void OnApplicationQuit()
        {
            Application.logMessageReceived -= HijackLogMessage;
        }
    }

    public class MythrailConsoleMessage
    {
        public string message;
        public Color colour;

        public MythrailConsoleMessage(string message, Color colour)
        {
            this.message = message;
            this.colour = colour;
        }
    }
}
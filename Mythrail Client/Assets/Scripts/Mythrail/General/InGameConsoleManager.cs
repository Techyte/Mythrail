using System;
using System.Collections.Generic;
using Mythrail.Settings;
using TMPro;
using UnityEngine;

namespace Mythrail.General
{
    public class InGameConsoleManager : MonoBehaviour
    {
        public static InGameConsoleManager Instance;
        
        [SerializeField] private GameObject disableableParent;
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
            
            if(MythrailSettings.CompressDeveloperConsole)
            {
                for (int i = 0; i < _currentMessages.Count; i++)
                {
                    if (stackTrace == _currentMessages[i].stackTrace)
                    {
                        _currentMessages[i].calls++;
                        return;
                    }
                }
            }
            
            if (_currentMessages.Count >= displays.Count)
            {
                ClearOldestAndMoveUp();
            }

            MythrailConsoleMessage message = new MythrailConsoleMessage(condition, colour, stackTrace);
            _currentMessages.Add(message);
        }

        public void RecalculateCurrentCompression()
        {
            if(MythrailSettings.CompressDeveloperConsole)
            {
                List<MythrailConsoleMessage> consoleMessages = new List<MythrailConsoleMessage>(_currentMessages);
                List<MythrailConsoleMessage> revisedConsoleMessages = new List<MythrailConsoleMessage>();

                _currentMessages.Clear();
                
                Debug.Log(consoleMessages.Count);

                for (int i = 0; i < consoleMessages.Count; i++)
                {
                    bool valid = true;
                    for (int j = 0; j < revisedConsoleMessages.Count; j++)
                    {
                        if (revisedConsoleMessages[j].stackTrace == consoleMessages[i].stackTrace)
                        {
                            valid = false;
                        }
                    }
                    if(valid)
                    {
                        revisedConsoleMessages.Add(consoleMessages[i]);
                    }
                }

                _currentMessages = revisedConsoleMessages;
            }
        }

        private void ClearOldestAndMoveUp()
        {
            Debug.Log("clearing and moving up");
            
            string lastMessage = String.Empty;
            Color lastMessageColour = Color.green;
            string lastStackTrace = String.Empty;

            for (int i = _currentMessages.Count-1; i >= 0; i--)
            {
                string currentMessage = _currentMessages[i].message;
                Color currentMessageColour = _currentMessages[i].colour;
                string currentStackTrace = _currentMessages[i].stackTrace;

                if (i != _currentMessages.Count-1)
                {
                    _currentMessages[i].message = lastMessage;
                    _currentMessages[i].colour = lastMessageColour;
                    _currentMessages[i].stackTrace = lastStackTrace;
                }

                lastMessage = currentMessage;
                lastMessageColour = currentMessageColour;
                lastStackTrace = currentStackTrace;
            }

            _currentMessages.RemoveAt(_currentMessages.Count-1);
        }

        public void Clear()
        {
            _currentMessages.Clear();
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
        public string stackTrace;
        public int calls = 0;

        public MythrailConsoleMessage(string message, Color colour, string stackTrace)
        {
            this.message = message;
            this.colour = colour;
            this.stackTrace = stackTrace;
        }
    }
}
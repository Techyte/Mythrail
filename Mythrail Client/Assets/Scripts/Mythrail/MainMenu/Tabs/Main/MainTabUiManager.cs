using System;
using System.Collections.Generic;
using Mythrail.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mythrail.MainMenu.Tabs.Main
{
    public class MainTabUiManager : TabUiManager
    {
        [Header("Holder")]
        [SerializeField] private Transform matchHolder;
        [Space]
        
        [Header("Match Object")] 
        [SerializeField] private GameObject matchObject;
        
        private MainTab _mainTab;

        private List<GameObject> _matchButtons = new List<GameObject>();

        private void Awake()
        {
            _mainTab = (MainTab)tab;

            _mainTab.OnReceivedMatchInfo += delegate(object sender, MatchInfo[] infos)
            {
                UpdateMatches(infos);
            };
        }

        public void CreateMatchButton(string name, string creator, string code, ushort port)
        {
            GameObject newMatchObj = Instantiate(matchObject, matchHolder);

            newMatchObj.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = name;
            newMatchObj.transform.Find("Creator").GetComponent<TextMeshProUGUI>().text = creator;
            newMatchObj.transform.Find("Code").GetComponent<TextMeshProUGUI>().text = code;
            
            newMatchObj.GetComponent<Button>().onClick.AddListener(delegate
            {
                MenuNetworkManager.Singleton.JoinMatch(port);
            });
            
            _matchButtons.Add(newMatchObj);
        }

        public void UpdateMatches(MatchInfo[] matchInfos)
        {
            foreach (var button in _matchButtons)
            {
                Destroy(button);
            }

            for (int i = 0; i < matchInfos.Length; i++)
            {
                CreateMatchButton(matchInfos[i].name, matchInfos[i].creatorName, matchInfos[i].code, matchInfos[i].port);
            }
        }
    }   
}

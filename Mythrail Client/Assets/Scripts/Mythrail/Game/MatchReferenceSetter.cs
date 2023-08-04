using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mythrail.Game
{
    public class MatchReferenceSetter : MonoBehaviour
    {
        private void Awake()
        {
            UIManager manager = UIManager.Singleton;

            manager.SetCode();
            
            manager.UIHealthBar = GameObject.Find("Health").GetComponentsInChildren<Image>()[1].transform;
            manager.loadingScreen = GameObject.Find("Loading");
            manager.KillsText = GameObject.Find("KillCounter").GetComponent<TextMeshProUGUI>();
            manager.DeathsText = GameObject.Find("DeathCounter").GetComponent<TextMeshProUGUI>();
            manager.HUDUsernameDisplay = GameObject.Find("UsernameText").GetComponent<TextMeshProUGUI>();
            manager.LoadingStatusDisplay = GameObject.Find("LoadingText").GetComponent<TextMeshProUGUI>();
            manager.GunName = GameObject.Find("GunName").GetComponent<TextMeshProUGUI>();
            manager.RespawningScreen = GameObject.Find("RespawnScreen");
            manager.PlayScreen = GameObject.Find("PlayScreen");
            manager.CountdownText = GameObject.Find("CountdownText").GetComponent<TextMeshProUGUI>();
            manager.RespawnButton = GameObject.Find("RespawnButton").GetComponent<Button>();
            manager.RespawnButton.onClick.AddListener(manager.Respawn);
            manager.RespawningScreen.SetActive(false);
        }
    }   
}
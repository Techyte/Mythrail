using Mythrail.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mythrail.Game
{
    public class MatchReferenceSetter : MonoBehaviour
    {
        private void Awake()
        {
            UIManager uiManager = UIManager.Singleton;
            NetworkManager networkManager = NetworkManager.Singleton;

            uiManager.SetCode();
            
            uiManager.uiHealthBar = GameObject.Find("Health").GetComponentsInChildren<Image>()[1].transform;
            uiManager.loadingScreen = GameObject.Find("Loading");
            uiManager.killsText = GameObject.Find("KillCounter").GetComponent<TextMeshProUGUI>();
            uiManager.deathsText = GameObject.Find("DeathCounter").GetComponent<TextMeshProUGUI>();
            uiManager.hudUsernameDisplay = GameObject.Find("UsernameText").GetComponent<TextMeshProUGUI>();
            uiManager.loadingStatusDisplay = GameObject.Find("LoadingText").GetComponent<TextMeshProUGUI>();
            uiManager.gunName = GameObject.Find("GunName").GetComponent<TextMeshProUGUI>();
            uiManager.respawningScreen = GameObject.Find("RespawnScreen");
            uiManager.playScreen = GameObject.Find("PlayScreen");
            uiManager.countdownText = GameObject.Find("CountdownText").GetComponent<TextMeshProUGUI>();
            uiManager.respawnButton = GameObject.Find("RespawnButton").GetComponent<Button>();
            uiManager.respawnButton.onClick.AddListener(uiManager.Respawn);
            uiManager.respawningScreen.SetActive(false);
            networkManager.LoadedBattle();
        }
    }   
}
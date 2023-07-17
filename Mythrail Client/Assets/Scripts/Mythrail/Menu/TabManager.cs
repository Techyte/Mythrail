using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mythrail.Menu
{
public class TabManager : MonoBehaviour
{
    private static TabManager _singleton;
    public static TabManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(TabManager)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }
    
    [SerializeField] private List<TabInfo> tabs;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pressedClip;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            if(i!=0)
            {
                TabInfo info = tabs[i];

                int index = i;

                Debug.Log(i);
                info.tabButton.onClick.AddListener(delegate { OpenTab(index); });
            }
        }
    }

    public void OpenTab(int index)
    {
        if(!MenuNetworkManager.Singleton.UiManager.CanMoveMenu())
            return;
        
        PlaySound();
        
        DisableAllTabs();
        tabs[index].tabObject.SetActive(true);
    }

    public void OpenMain()
    {
        OpenTab(0);
    }

    private void PlaySound()
    {
        audioSource.PlayOneShot(pressedClip);
    }

    private void DisableAllTabs()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            tabs[i].tabObject.SetActive(false);
        }
    }
}

[System.Serializable]
public class TabInfo
{
    public GameObject tabObject;
    public Button tabButton;
}   
}
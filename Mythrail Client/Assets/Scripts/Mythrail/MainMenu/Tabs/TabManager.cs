using System.Collections.Generic;
using UnityEngine;

namespace Mythrail.MainMenu.Tabs
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
    
    [SerializeField] private List<Tab> tabs;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pressedClip;

    public List<Tab> Tabs => tabs;

    private void Awake()
    {
        Singleton = this;
        
        SetButtons();
    }

    private void SetButtons()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            if(i!=0)
            {
                Tab info = tabs[i];

                int index = i;

                info.TabButton.onClick.AddListener(delegate { OpenTab(index); });
            }
        }
    }

    public void OpenTab(int index)
    {
        if(tabs[index].RequiresCanMoveMenu)
        {
            if (!MenuNetworkManager.Singleton.UiManager.CanMoveMenu())
                return;
        }
        
        PlaySound();
        
        DisableAllTabs();
        tabs[index].TabObject.SetActive(true);
    }
    
    public void OpenTab(int index, bool checkCanMoveMenu)
    {
        if(checkCanMoveMenu)
        {
            if (!MenuNetworkManager.Singleton.UiManager.CanMoveMenu())
                return;
        }
        
        PlaySound();
        
        DisableAllTabs();
        tabs[index].TabObject.SetActive(true);
    }

    public void OpenMain()
    {
        OpenTab(0, false);
    }

    private void PlaySound()
    {
        audioSource.PlayOneShot(pressedClip);
    }

    private void DisableAllTabs()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            tabs[i].TabObject.SetActive(false);
        }
    }
}
}
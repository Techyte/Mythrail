using UnityEngine;
using UnityEngine.UI;

namespace Mythrail.MainMenu.Tabs
{
    public class Tab : MonoBehaviour
    {
        public GameObject TabObject => tabObject;
        public Button TabButton => tabButton;
        public bool RequiresCanMoveMenu => requiresCanMoveMenu;
    
        [SerializeField] private GameObject tabObject;
        [SerializeField] private Button tabButton;
        [SerializeField] private bool requiresCanMoveMenu = true;

        private void Start()
        {
            if(TabManager.Singleton.Tabs[0] != this)
            {
                tabButton.onClick.AddListener(delegate { Opened(); });
            }
        }

        protected virtual void Opened()
        {
            
        }

        private void Update()
        {
            if(IsOpen)
            {
                TabUpdate();
            }
        }

        protected virtual void TabUpdate()
        {
            
        }

        public bool IsOpen
        {
            get
            {
                if (tabObject)
                {
                    return tabObject.activeSelf;
                }
                else
                {
                    return false;
                }
            }
        }
    }   
}
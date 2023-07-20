using UnityEngine;

namespace Mythrail.MainMenu.Tabs
{
    public class TabUiManager : MonoBehaviour
    {
        [SerializeField] protected Tab tab;

        private void Update()
        {
            if (tab.IsOpen)
            {
                TabUpdate();
            }
        }

        protected virtual void TabUpdate()
        {
            
        }
    }   
}
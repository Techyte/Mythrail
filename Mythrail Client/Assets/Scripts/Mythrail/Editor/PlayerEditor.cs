using Mythrail.Multiplayer;

namespace Mythrail.Editor
{
    using UnityEditor;
    using Players;
    using UnityEngine;

    [CustomEditor(typeof(Player))]
    [CanEditMultipleObjects]
    public class PlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        
            Player player = (Player)target;
            
            if (GUILayout.Button("Kill"))
            {
                player.SendKillDevMessage();
            }

            if(player.IsLocal)
            {
                if (GUILayout.Button("Ready"))
                {
                    NetworkManager.Singleton.Ready();
                }
            }
        }
    }   
}
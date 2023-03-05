using UnityEditor;
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
            player.Died();
        }
        
        if (GUILayout.Button("TakeDamage"))
        {
            player.TakeEditorDamage(10);
        }
    }
}
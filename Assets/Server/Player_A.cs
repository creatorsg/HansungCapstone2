using Mirror;
using UnityEditor;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SyncVar] private string playerName;

    public void SetPlayerName(string name)
    {
        playerName = name;
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        if (isLocalPlayer)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Player: {playerName}");
        }
        EditorGUILayout.EndVertical();
    }
}

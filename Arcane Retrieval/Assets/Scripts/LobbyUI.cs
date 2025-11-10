//using TMPro;
//using Unity.Netcode;
//using UnityEngine;

//public class LobbyUI : MonoBehaviour
//{
//    public LobbyManager lobbyManager;
//    public Transform playerListParent;
//    public GameObject playerLabelPrefab;
//    public GameObject startButton; // only active for host

//    private void Start()
//    {
//        lobbyManager.playerNames.OnListChanged += UpdatePlayerList;

//        if (!lobbyManager.IsServer)
//            startButton.SetActive(false);
//    }

//    private void UpdatePlayerList(NetworkListEvent<string> changeEvent)
//    {
//        // Clear current list
//        foreach (Transform t in playerListParent)
//            Destroy(t.gameObject);

//        // Add labels for each player
//        foreach (var name in lobbyManager.playerNames)
//        {
//            GameObject label = Instantiate(playerLabelPrefab, playerListParent);
//            label.GetComponent<TMP_Text>().text = name;
//        }
//    }
//}

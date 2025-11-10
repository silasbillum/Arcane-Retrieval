using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    public LobbyManager lobbyManager;
    public Transform playerListParent;
    public GameObject playerLabelPrefab;
    public GameObject startButton; // only active for host

    private void Start()
    {
        lobbyManager.playerNames.OnListChanged += UpdatePlayerList;

        if (NetworkManager.Singleton.IsServer)

            startButton.SetActive(false);

        // Initial update
        RefreshPlayerList();
    }

    public void StartGame()
    {
        if (NetworkManager.Singleton.IsServer)
            return;

        UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayScene");
    }

    private void UpdatePlayerList(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        // Clear current list
        foreach (Transform t in playerListParent)
            Destroy(t.gameObject);

        // Add labels for each player
        foreach (var name in lobbyManager.playerNames)
        {
            GameObject label = Instantiate(playerLabelPrefab, playerListParent);
            label.GetComponent<TMP_Text>().text = name.ToString();
        }
    }
}

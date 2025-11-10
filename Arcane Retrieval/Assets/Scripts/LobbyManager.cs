using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField joinCodeInput;
    public Button joinButton;
    public Button startGameButton;
    public Transform playerListContent;
    public GameObject playerLabelPrefab;
    public TMP_Text debugText;

    public GameObject playerPrefab; // for GameplayScene

    public NetworkList<FixedString64Bytes> playerNames;

    public async void Start()
    {
        // Init NetworkList
        if (playerNames == null)
            playerNames = new NetworkList<FixedString64Bytes>();

        startGameButton.gameObject.SetActive(false);

        AppendDebug("Initializing Unity Services...");
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            AppendDebug("Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            AppendDebug("Signed in!");
        }

        joinButton.onClick.AddListener(OnJoinClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        playerNames.OnListChanged += (_) => RefreshPlayerList();

        // Check if player should be host
        if (NetworkSessionType.IsHost)
            StartHostRelay();
    }

    public async void StartHostRelay()
    {
        AppendDebug("Creating Relay allocation...");
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        AppendDebug("Relay Join Code: " + joinCode);
        joinCodeInput.text = joinCode;

        var relayData = new RelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

        bool success = NetworkManager.Singleton.StartHost();
        if (success)
        {
            AppendDebug("Host started!");
            startGameButton.gameObject.SetActive(true);
            AddPlayer(NetworkManager.Singleton.LocalClientId);
        }
    }

    public async void OnJoinClicked()
    {
        string code = joinCodeInput.text.ToUpper();
        if (string.IsNullOrEmpty(code)) return;

        AppendDebug("Joining host with code: " + code);

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
        var relayData = new RelayServerData(joinAllocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

        NetworkManager.Singleton.StartClient();
        AppendDebug("Client started!");
    }

    public void OnStartGameClicked()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene("GameplayScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void OnClientConnected(ulong clientId) => AddPlayer(clientId);
    public void OnClientDisconnected(ulong clientId) => RemovePlayer(clientId);

    public void AddPlayer(ulong clientId)
    {
        string name = $"Player {clientId}";
        if (!playerNames.Contains(name))
            playerNames.Add(name);
    }

    public void RemovePlayer(ulong clientId)
    {
        string name = $"Player {clientId}";
        if (playerNames.Contains(name))
            playerNames.Remove(name);
    }

    public void RefreshPlayerList()
    {
        foreach (Transform t in playerListContent)
            Destroy(t.gameObject);

        foreach (var name in playerNames)
        {
            GameObject label = Instantiate(playerLabelPrefab, playerListContent);
            label.GetComponent<TMP_Text>().text = name.ToString();
        }
    }

    public void AppendDebug(string message)
    {
        Debug.Log(message);
        if (debugText != null)
            debugText.text += message + "\n";
    }
}

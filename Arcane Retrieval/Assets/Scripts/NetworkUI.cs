using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    public TMP_Text debugText; // assign in inspector
    public TMP_InputField joinCodeInput;
    public GameObject playerPrefab;

    async void Start()
    {
        AppendDebug("Initializing Unity Services...");

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                AppendDebug("Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                AppendDebug("Signed in successfully!");
            }
            else
            {
                AppendDebug("Already signed in.");
            }
        }
        catch (AuthenticationException ex)
        {
            AppendDebug("Authentication exception: " + ex.Message);
        }
        catch (System.Exception ex)
        {
            AppendDebug("Unexpected error: " + ex.Message);
        }
    }

    private void Awake()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayerOnServer;
    }

    private void SpawnPlayerOnServer(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        GameObject playerInstance = Instantiate(playerPrefab);
        playerInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

        AppendDebug("Spawned player for client: " + clientId);
    }


    public async void StartHost()
    {
        try
        {
            AppendDebug("Creating Relay Allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            AppendDebug("Relay Join Code: " + joinCode);

            var relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            AppendDebug("Host started!");

            SpawnPlayer(NetworkManager.Singleton.LocalClientId);
        }
        catch (System.Exception e)
        {
            AppendDebug("Host Error: " + e.Message);
        }
    }

    public async void StartClient()
    {
        string joinCode = joinCodeInput.text; // get the text here
        AppendDebug("Trying to join with code: " + joinCode);

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
            AppendDebug("Client started!");

           
        }
        catch (System.Exception e)
        {
            AppendDebug("Join Error: " + e.Message);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (playerPrefab != null && NetworkManager.Singleton.IsServer)
        {
            GameObject playerInstance = Instantiate(playerPrefab);
            playerInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
            AppendDebug("Player spawned for client: " + clientId);
        }
    }


    void AppendDebug(string message)
    {
        Debug.Log(message);
        if (debugText != null)
        {
            debugText.text += message + "\n";
        }
    }
}

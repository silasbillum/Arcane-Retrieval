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
    public TMP_Text debugText;
    public TMP_InputField joinCodeInput;

    async void Start()
    {
        AppendDebug("Initializing Unity Services...");

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            AppendDebug("Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            AppendDebug("Signed in!");
        }
    }

    public async Task<string> StartHostRelay()
    {
        AppendDebug("Creating Relay allocation...");
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        AppendDebug("Relay Join Code: " + joinCode);

        var relayData = new RelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

        bool success = NetworkManager.Singleton.StartHost();
        if (success)
            AppendDebug("Host started!");
        FindObjectOfType<LobbyManager>()?.AddPlayer(NetworkManager.Singleton.LocalClientId);


        return joinCode;
    }

    public async Task StartClientRelay(string code)
    {
        AppendDebug("Joining host with code: " + code);
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

        var relayData = new RelayServerData(joinAllocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

        NetworkManager.Singleton.StartClient();
        AppendDebug("Client started!");
    }

    private void AppendDebug(string message)
    {
        Debug.Log(message);
        if (debugText != null)
            debugText.text += message + "\n";
    }
}

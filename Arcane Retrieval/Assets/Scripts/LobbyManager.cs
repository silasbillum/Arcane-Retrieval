//using Unity.Netcode;
//using UnityEngine;

//public class LobbyManager : NetworkBehaviour
//{
//    public NetworkList<string> playerNames;

//    private void Awake()
//    {
//        playerNames = new NetworkList<string>();
//    }

//    public override void OnNetworkSpawn()
//    {
//        if (IsServer)
//        {
//            // Add host player
//            playerNames.Add($"Player {NetworkManager.Singleton.LocalClientId}");
//        }

//        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
//        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
//    }

//    private void OnClientConnected(ulong clientId)
//    {
//        if (IsServer)
//        {
//            playerNames.Add($"Player {clientId}");
//        }
//    }

//    private void OnClientDisconnected(ulong clientId)
//    {
//        if (IsServer)
//        {
//            playerNames.Remove($"Player {clientId}");
//        }
//    }
//}

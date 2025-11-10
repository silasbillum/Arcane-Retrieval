using Unity.Netcode;
using UnityEngine;

public class GameplayManager : NetworkBehaviour
{
    public GameObject playerPrefab;

    private void Start()
    {
        if (!IsServer) return;

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject player = Instantiate(playerPrefab);
            player.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
        }
    }
}

using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Fusion")]
    public NetworkRunner runner;
    public NetworkPrefabRef playerPrefab;

    private async void Start()
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        if (playerPrefab == null)
        {
            Debug.LogError("❌ Player prefab not assigned!");
            return;
        }

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "Room1",
            Scene = scene,
            PlayerCount = 4 // max players
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Only the host spawns players
        if (!runner.IsServer)
            return;

        // Check if the player already has an object
        if (runner.GetPlayerObject(player) != null)
            return;

        Vector3 spawnPos = new Vector3(Random.Range(-5, 5), 1f, Random.Range(-5, 5));
        var playerObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);

        runner.SetPlayerObject(player, playerObj);

        Debug.Log($"[SPAWN] Player {player} spawned at {spawnPos}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.TryGetPlayerObject(player, out NetworkObject playerObject))
        {
            runner.Despawn(playerObject);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (runner.LocalPlayer == default)
            return;

        var data = new PlayerInputData
        {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")),
            jump = Input.GetKey(KeyCode.Space),
            run = Input.GetKey(KeyCode.LeftShift),
            crouch = Input.GetKey(KeyCode.LeftControl)
        };

        input.Set(data);
    }

    // --- Empty required callbacks ---
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}

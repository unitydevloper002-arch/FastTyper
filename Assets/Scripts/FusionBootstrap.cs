using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Fusion.Sockets;
using DG.Tweening.Core.Easing;
using System.Linq;
using static Unity.Collections.Unicode;

public class FusionBootstrap : MonoBehaviour
{
    public static FusionBootstrap Instance { get; private set; }
    public NetworkRunner runner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task StartShared(string sessionName)
    {
        Debug.Log("Starting Fusion...");

        if (runner == null)
            runner = gameObject.AddComponent<NetworkRunner>();

        var sceneMgr = gameObject.GetComponent<NetworkSceneManagerDefault>();
        if (sceneMgr == null)
            sceneMgr = gameObject.AddComponent<NetworkSceneManagerDefault>();

        if (!runner.IsRunning)
        {
            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = string.IsNullOrWhiteSpace(sessionName) ? "FastTyperRoom" : sessionName,
                SceneManager = sceneMgr
            });

            if (!result.Ok)
            {
                Debug.LogError($"Fusion start failed: {result.ShutdownReason}");
                return;
            }

            runner.AddCallbacks(new RunnerCallbacks());
            Debug.Log("Fusion started successfully.");
        }
    }

    // Method called by IFrameBridge for multiplayer mode
    public async void StartMultiplayerGame(string matchId)
    {
        Debug.Log($"[FusionBootstrap] Starting multiplayer game with match ID: {matchId}");
        
        try
        {
            await StartShared(matchId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FusionBootstrap] Failed to start multiplayer game: {e.Message}");
            
            // Notify IFrameBridge of the failure
            if (IFrameBridge.Instance != null)
            {
                IFrameBridge.Instance.AbortGameStartFailure($"Failed to start multiplayer: {e.Message}");
            }
        }
    }

    // Method called by IFrameBridge when player wants to leave
    public void DisconnectFromGame()
    {
        Debug.Log("[FusionBootstrap] Disconnecting from multiplayer game");
        
        if (runner != null && runner.IsRunning)
        {
            runner.Shutdown();
        }
    }
}

public sealed class RunnerCallbacks : INetworkRunnerCallbacks
{
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        //Debug.Log($"Player joined: {player}");

        //// Player spawn karo (FusionBridge as shared object)
        //if (runner.IsSharedModeMasterClient) // only master spawn kare
        //{
        //    if (Resources.Load<NetworkObject>("FusionBridge") is NetworkObject bridgePrefab)
        //    {
        //        runner.Spawn(bridgePrefab, Vector3.zero, Quaternion.identity, player);
        //        Debug.Log("FusionBridge spawned for player: " + player);
        //    }
        //    else
        //    {
        //        Debug.LogError("FusionBridge prefab not found in Resources!");
        //    }
        //}

        Debug.Log($"Player {player.PlayerId} joined");
        if (runner.LocalPlayer == player)
        {
           runner.Spawn(Resources.Load<NetworkObject>("FusionBridge"), Vector3.zero, Quaternion.identity, player);
        }

        if (runner.ActivePlayers.Count() >= 2)
        {
            Debug.Log("2 players joined → Starting countdown!");
            // Start the countdown when both players are ready
            if (UIManager.Instance != null)
            {
                UIManager.Instance.IsCountDownStart = true;
                UIManager.Instance.StartCoroutine(UIManager.Instance.StartCountdown_Animation());
            }
        }
    }

    // Required empty implementations
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    {
        Debug.Log($"Player left: {player}");
        
        // Find their player object
        if (runner.TryGetPlayerObject(player, out NetworkObject playerObj))
        {
            runner.Despawn(playerObj);
            Debug.Log("Player object despawned.");
        }
        
        // Check if this is during an active multiplayer game
        if (UIManager.Instance != null && UIManager.Instance.currentGameMode == GAMEMODE.MultiPlayer)
        {
            // Check if the game is currently running (not in menu or game over screen)
            bool isGameActive = UIManager.Instance.gamePlayScreen.activeSelf;
            
            if (isGameActive)
            {
                Debug.Log($"Player {player.PlayerId} left during active game - triggering win for remaining player");
                
                // Notify the remaining player they won due to opponent leaving
                UIManager.Instance.HandleOpponentDisconnect(player.PlayerId);
                
                // Also notify platform about opponent forfeit
                if (IFrameBridge.Instance != null)
                {
                    IFrameBridge.Instance.PostOpponentForfeit(player.PlayerId.ToString());
                }
            }
        }
    }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) 
    {
        Debug.Log($"Network runner shutdown: {shutdownReason}");
        
        // If we were in a multiplayer game, handle the shutdown
        if (UIManager.Instance != null && UIManager.Instance.currentGameMode == GAMEMODE.MultiPlayer)
        {
            bool isGameActive = UIManager.Instance.gamePlayScreen.activeSelf;
            
            if (isGameActive)
            {
                Debug.Log("Network shutdown during active multiplayer game");
                UIManager.Instance.HandleLocalPlayerLeave();
            }
        }
    }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) 
    {
        Debug.Log($"Disconnected from server: {reason}");
        
        // If we were in a multiplayer game, handle the disconnection
        if (UIManager.Instance != null && UIManager.Instance.currentGameMode == GAMEMODE.MultiPlayer)
        {
            bool isGameActive = UIManager.Instance.gamePlayScreen.activeSelf;
            
            if (isGameActive)
            {
                Debug.Log("Connection lost during active multiplayer game");
                UIManager.Instance.HandleLocalPlayerLeave();
            }
        }
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}

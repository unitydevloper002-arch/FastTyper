using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;

public class IFrameBridge : MonoBehaviour
{
    public static IFrameBridge Instance { get; private set; }

    // Score submission tracking
    private bool scoreSubmitted = false;
    private float submitTimeout = 5f;
    private float submitTimer = 0f;

    // Match information
    public static string MatchId { get; private set; } = string.Empty;
    public static string PlayerId { get; private set; } = string.Empty;
    public static string OpponentId { get; private set; } = string.Empty;

    // Bot difficulty from platform
    internal GAMETYPE botLevel;
    public GameType gameType;

    public bool IsTestBoat = true;

    private bool isInitialized = false;
    private bool gameModeInitialized = false;
    private bool isReplayMode = false;
    private List<string> replayStates = new List<string>();

    // WebGL external methods
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetURLParameters();

    [DllImport("__Internal")]
    private static extern void SendPostMessage(string message);

    [DllImport("__Internal")]
    private static extern void SendBuildVersion(string version);

    [DllImport("__Internal")]
    private static extern void SendGameReady();

    [DllImport("__Internal")]
    private static extern int IsMobileWeb();

    [DllImport("__Internal")]
    private static extern void SetupMessageListener();
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            gameObject.name = "IFrameBridge";
            DontDestroyOnLoad(gameObject);
            Debug.unityLogger.logEnabled = true;
            isInitialized = true;
            gameModeInitialized = false; // Reset game mode initialization
            Debug.Log("[IFrameBridge] Instance initialized");
        }
        else
        {
            Debug.LogWarning("[IFrameBridge] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (!isInitialized)
        {
            Debug.LogError("[IFrameBridge] Start called before initialization!");
            return;
        }

        try
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // In WebGL build, follow platform flow:
            // 1. Send ready signal
            // 2. Get match parameters from URL 
            // 3. Initialize appropriate mode
            Debug.Log("[IFrameBridge] Sending game ready signal...");
            SendGameReady();
            SendBuildVersion(Application.version);
            SetupMessageListener();
            Debug.Log("[IFrameBridge] Game ready signal sent successfully");

            ExtractParametersFromURL();
#else
            // In Unity Editor or Windows build, start with AI test mode
            Debug.Log(
                "[IFrameBridge] Editor/Build mode - starting with AI test parameters"
            );
            ExtractParametersFromURL();
#endif
        }
        catch (Exception e)
        {
            Debug.LogError("[IFrameBridge] Error in Start: " + e.Message);
        }
    }

    private void ExtractParametersFromURL()
    {
        try
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Get parameters from URL in WebGL build
            string json = GetURLParameters();
            if (string.IsNullOrEmpty(json))
            {
                throw new Exception("No URL parameters found");
            }
            InitParamsFromJS(json);
#else
            // Use test data in editor - CHOOSE MODE HERE:
            string json;

            if (IsTestBoat)
            {
                // FOR AI MODE TESTING (uncomment this line):
                json = "{\"matchId\":\"test_match\",\"playerId\":\"human_player\",\"opponentId\":\"a912345678\"}";
            }
            else
            {
                //FOR MULTIPLAYER MODE TESTING (comment out the line above and uncomment this line):
                json = "{\"matchId\":\"test_match\",\"playerId\":\"player1\",\"opponentId\":\"player2\"}";
            }

            InitParamsFromJS(json);
#endif
        }
        catch (Exception e)
        {
            Debug.LogError("[IFrameBridge] Error extracting URL parameters: " + e.Message);
            AbortInitializationError($"URL parameter extraction failed: {e.Message}");
        }
    }

    public bool IsMobileWebGL()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { return IsMobileWeb() == 1; } catch { return false; }
#else
        return Application.isMobilePlatform;
#endif
    }

    private void StartTestMatch()
    {
        Debug.Log("[IFrameBridge] Starting test match...");

        // CHOOSE ONE MODE FOR TESTING:

        // 1. FOR AI MODE TESTING (default):
      //  string json =
      //      "{\"matchId\":\"test_match\",\"playerId\":\"human_player\",\"opponentId\":\"b912345678\"}";

        // 2. FOR MULTIPLAYER MODE TESTING (uncomment this line and comment out the line above):
         string json = "{\"matchId\":\"test_match\",\"playerId\":\"player1\",\"opponentId\":\"player2\"}";

        IFrameBridge.Instance.InitParamsFromJS(json);
    }

    public void InitParamsFromJS(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[IFrameBridge] Received null or empty JSON!");
            AbortInitializationError("Empty JSON received");
            return;
        }

        // Prevent multiple initializations
        if (gameModeInitialized)
        {
            Debug.LogWarning(
                "[IFrameBridge] Game mode already initialized, ignoring duplicate InitParamsFromJS call"
            );
            return;
        }

        try
        {
            Debug.Log($"[IFrameBridge] Parsing match parameters from JSON: {json}");

            var data = JsonUtility.FromJson<MatchParams>(json);
            if (data == null)
            {
                throw new ArgumentException("Failed to parse JSON data");
            }

            // Validate and assign the data
            if (
                string.IsNullOrEmpty(data.matchId)
                || string.IsNullOrEmpty(data.playerId)
                || string.IsNullOrEmpty(data.opponentId)
            )
            {
                throw new ArgumentException("Required match parameters are missing or empty");
            }

            MatchId = data.matchId;
            PlayerId = data.playerId;
            OpponentId = data.opponentId;

            Debug.Log(
                $"[IFrameBridge] Match parameters set - Match ID: {MatchId}, Player ID: {PlayerId}, Opponent ID: {OpponentId}"
            );

            // Check if this is replay mode
            if (data.replay)
            {
                isReplayMode = true;
                Debug.Log("[IFrameBridge] REPLAY MODE DETECTED");
                SetupReplayMode();
                return;
            }

            // Determine game mode based on opponent ID
            bool isOpponentBot = IsBot(OpponentId);

            Debug.Log(
                $"[IFrameBridge] ===== MODE DETECTION ===== OpponentId: '{OpponentId}', IsBot: {isOpponentBot}"
            );

            if (isOpponentBot)
            {
                // AI MODE
                botLevel = GetBotLevel(OpponentId);
                gameType = GameType.Singleplayer;
                gameModeInitialized = true;

                Debug.Log(
                    $"[IFrameBridge] AI MODE INITIALIZED - Bot difficulty: {botLevel}, gameType: {gameType}"
                );

                // Initialize AI game completely separate from multiplayer
                InitializeAIMode();
            }
            else
            {
                // MULTIPLAYER MODE
                gameType = GameType.Multiplayer;
                gameModeInitialized = true;

                Debug.Log($"[IFrameBridge] MULTIPLAYER MODE INITIALIZED - gameType: {gameType}");

                // Initialize multiplayer game completely separate from AI
                InitializeMultiplayerMode();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[IFrameBridge] Error parsing match parameters: {e.Message}");
            AbortInitializationError($"Match parameter parsing failed: {e.Message}");
        }
    }

    private void SetupReplayMode()
    {
        Debug.Log("[IFrameBridge] Setting up replay mode...");
        // Listen for replay data from parent window
        // This will be handled by the OnLoadReplay method when called from JavaScript
    }

    // Called from JavaScript when replay data is received
    public void OnLoadReplay(string statesJson)
    {
        if (!isReplayMode)
        {
            Debug.LogWarning("[IFrameBridge] Received replay data but not in replay mode");
            return;
        }

        try
        {
            var replayData = JsonUtility.FromJson<ReplayData>(statesJson);
            if (replayData != null && replayData.states != null)
            {
                replayStates = new List<string>(replayData.states);
                Debug.Log($"[IFrameBridge] Loaded {replayStates.Count} replay states");
                PlayReplay(replayStates);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[IFrameBridge] Error parsing replay data: {e.Message}");
        }
    }

    private void PlayReplay(List<string> states)
    {
        Debug.Log($"[IFrameBridge] Starting replay with {states.Count} states (not implemented in this build)");
    }

    private void InitializeAIMode()
    {
        Debug.Log("[IFrameBridge] Initializing AI Mode...");

        if (UIManager.Instance != null)
        {
            // Use the new AutoStartGame method to bypass selection screens
            UIManager.Instance.AutoStartGame(GAMEMODE.SinglePlayer, botLevel);
        }
        else
        {
            Debug.LogError("[IFrameBridge] UIManager not found for AI mode!");
            AbortGameStartFailure("UIManager not found for AI mode");
        }
    }

    private void InitializeMultiplayerMode()
    {
        Debug.Log("[IFrameBridge] Initializing Multiplayer Mode...");

        if (FusionBootstrap.Instance != null)
        {
            // Use the new AutoStartGame method to bypass selection screens
            UIManager.Instance.AutoStartGame(GAMEMODE.MultiPlayer);

            // Start Fusion networking - this will be handled by the countdown system
            // The actual game start will happen when countdown completes
        }
        else
        {
            Debug.LogError("[IFrameBridge] FusionBootstrap not found for multiplayer mode!");
            AbortGameStartFailure("FusionBootstrap not found for multiplayer mode");
        }
    }

    public bool IsBot(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return false;
        // According to documentation, bot IDs start with "a9" or "b9"
        return playerId.StartsWith("a9") || playerId.StartsWith("b9");
    }

    public GAMETYPE GetBotLevel(string playerId)
    {
        if (playerId.StartsWith("a9"))
            return GAMETYPE.Easy;
        else if (playerId.StartsWith("b9"))
            return GAMETYPE.Hard;
        return GAMETYPE.Easy;
    }

    // Send match result when match ends normally
    public void PostMatchResult(string outcome, int score, int opponentScore)
    {
        Debug.Log(
            "[IFrameBridge] Match Result - Outcome: "
                + outcome
                + ", Score: "
                + score.ToString()
                + ", OpponentScore: "
                + opponentScore.ToString()
        );

        var message = new MatchResultMessage
        {
            type = "match_result",
            payload = new MatchResultPayload
            {
                matchId = MatchId,
                playerId = PlayerId,
                opponentId = OpponentId,
                outcome = outcome,
                score = score
            }
        };

        SendPostMessageToParent(message);
    }

    // Send match abort when game fails to start, player disconnects, or critical error occurs
    public void PostMatchAbort(string message, string error = "", string errorCode = "")
    {
        Debug.Log(
            "[IFrameBridge] Match Aborted - Message: "
                + message
                + ", Error: "
                + error
                + ", Code: "
                + errorCode
        );

        var abortMessage = new MatchAbortMessage
        {
            type = "match_abort",
            payload = new MatchAbortPayload
            {
                message = message,
                error = error,
                errorCode = errorCode
            }
        };

        SendPostMessageToParent(abortMessage);
    }

    // Send game state on every game interaction to store the gameplay
    public void PostGameState(string state)
    {
        Debug.Log("[IFrameBridge] Game State: " + state);

        var stateMessage = new GameStateMessage
        {
            type = "game_state",
            payload = new GameStatePayload
            {
                state = state
            }
        };

        SendPostMessageToParent(stateMessage);
    }

    private void SendPostMessageToParent(object message)
    {
        string jsonMessage = JsonUtility.ToJson(message);
        Debug.Log($"[IFrameBridge] Sending postMessage: {jsonMessage}");

#if UNITY_WEBGL && !UNITY_EDITOR
        SendPostMessage(jsonMessage);
#else
        Debug.Log($"[IFrameBridge] Editor mode - would send: {jsonMessage}");
#endif
    }

    private System.Collections.IEnumerator WaitForScoreSubmission()
    {
        while (submitTimer < submitTimeout && !scoreSubmitted)
        {
            submitTimer += Time.deltaTime;
            yield return null;
        }

        if (!scoreSubmitted)
        {
            Debug.LogWarning("[IFrameBridge] Score submission timed out");
        }
    }

    private System.Collections.IEnumerator SimulateScoreSubmission()
    {
        yield return new WaitForSeconds(0.5f);
        scoreSubmitted = true;
        Debug.Log("[IFrameBridge] Score submission simulated in editor");
    }

    // Called from JavaScript when score is submitted successfully
    public void OnScoreSubmitted()
    {
        scoreSubmitted = true;
        Debug.Log("[IFrameBridge] Score submitted successfully");
    }

    public bool IsScoreSubmitted()
    {
        return scoreSubmitted;
    }

    // Public method to reset game mode for testing
    public void ResetGameMode()
    {
        gameModeInitialized = false;
        Debug.Log("[IFrameBridge] Game mode reset for testing");
    }

    // Convenience methods for common abort scenarios
    public void AbortGameStartFailure(string reason)
    {
        PostMatchAbort($"Game failed to start: {reason}", reason, "GAME_START_FAILURE");
    }

    public void AbortPlayerDisconnect(string playerId)
    {
        PostMatchAbort($"Player {playerId} disconnected", "Player disconnected", "PLAYER_DISCONNECT");
    }

    // Handle opponent forfeit scenario
    public void PostOpponentForfeit(string opponentId)
    {
        Debug.Log($"[IFrameBridge] Opponent {opponentId} forfeited the match");

        // Use simple approach
        PostMatchAbort("Opponent left the game.", "", "");

        // Also send match result with "won"
        int myScore = UIManager.Instance != null ? UIManager.Instance.totalScore : 0;
        int opponentScore = UIManager.Instance != null ? UIManager.Instance.aiScore : 0;
        PostMatchResult("won", myScore, opponentScore);
    }

    // Handle when local player leaves (should trigger opponent win)
    public void PostPlayerForfeit()
    {
        Debug.Log("[IFrameBridge] Local player forfeited the match");

        // Use simple approach
        PostMatchAbort("You left the game.", "", "");
    }

    public void AbortCriticalError(string error, string errorCode = "CRITICAL_ERROR")
    {
        PostMatchAbort($"Critical error occurred: {error}", error, errorCode);
    }

    public void AbortConnectionError(string error)
    {
        PostMatchAbort("Connection error", error, "CONNECTION_ERROR");
    }

    public void AbortInitializationError(string error)
    {
        PostMatchAbort("Failed to initialize game", error, "INIT_ERROR");
    }

    // JavaScript callback methods
    public void OnGamePaused()
    {
        Time.timeScale = 0;
        Debug.Log("[IFrameBridge] Game paused by platform");
    }

    public void OnGameResumed()
    {
        Time.timeScale = 1;
        Debug.Log("[IFrameBridge] Game resumed by platform");
    }

    public void OnConnectionLost()
    {
        AbortConnectionError("Network connection lost");
        Debug.Log("[IFrameBridge] Connection lost");
    }

    // Additional abort scenarios
    public void OnPlayerTimeout(string playerId)
    {
        AbortPlayerDisconnect($"Player {playerId} timed out");
        Debug.Log($"[IFrameBridge] Player {playerId} timed out");
    }

    public void OnGameCrash(string error)
    {
        AbortCriticalError($"Game crashed: {error}", "GAME_CRASH");
        Debug.LogError($"[IFrameBridge] Game crash detected: {error}");
    }

    public void OnResourceLoadFailure(string resource)
    {
        AbortGameStartFailure($"Failed to load required resource: {resource}");
        Debug.LogError($"[IFrameBridge] Resource load failure: {resource}");
    }

    public void OnInvalidGameState(string state)
    {
        AbortCriticalError($"Invalid game state detected: {state}", "INVALID_STATE");
        Debug.LogError($"[IFrameBridge] Invalid game state: {state}");
    }

    // Method to handle when player wants to leave the game
    public void OnPlayerLeaveGame()
    {
        Debug.Log("[IFrameBridge] Player requested to leave the game");

        // Send forfeit message to platform
        PostPlayerForfeit();

        // Disconnect from network if in multiplayer
        if (gameType == GameType.Multiplayer && FusionBootstrap.Instance != null)
        {
            FusionBootstrap.Instance.DisconnectFromGame();
        }

        // Stop or reset the game via UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RestartGame();
        }
    }

    [Serializable]
    private class MatchParams
    {
        public string matchId = string.Empty;
        public string playerId = string.Empty;
        public string opponentId = string.Empty;
        public bool replay = false;
    }

    [Serializable]
    private class MatchResultMessage
    {
        public string type;
        public MatchResultPayload payload;
    }

    [Serializable]
    private class MatchResultPayload
    {
        public string matchId;
        public string playerId;
        public string opponentId;
        public string outcome; // 'won' or 'lost' or 'draw'
        public int score; // optional
    }

    [Serializable]
    private class MatchAbortMessage
    {
        public string type;
        public MatchAbortPayload payload;
    }

    [Serializable]
    private class MatchAbortPayload
    {
        public string message; // reason for the abort to show to user
        public string error; // error if error occurs
        public string errorCode; // for debugging purpose
    }

    [Serializable]
    private class GameStateMessage
    {
        public string type;
        public GameStatePayload payload;
    }

    [Serializable]
    private class GameStatePayload
    {
        public string state; // JSON string
    }

    [Serializable]
    private class ReplayData
    {
        public string[] states; // array of JSON strings
    }
}

public enum GameType
{
    Singleplayer,
    Multiplayer,
}

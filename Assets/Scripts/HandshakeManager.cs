using System.Collections;
using UnityEngine;

/// <summary>
/// HandshakeManager coordinates the handshake process between local game completion
/// and web platform communication, ensuring proper sequencing and timing.
/// </summary>
public class HandshakeManager : MonoBehaviour
{
    public static HandshakeManager Instance { get; private set; }

    [Header("Handshake Timing")]
    public float uiSettleDelay = 2f;           // Time to wait for UI to settle
    public float handshakeTimeout = 5f;        // Timeout for handshake confirmation
    public float additionalDisplayTime = 3f;   // Additional time after handshake
    public float finalDisplayTime = 5f;        // Final display time before completion

    [Header("Handshake State")]
    public bool isHandshakeInProgress = false;
    public bool isHandshakeCompleted = false;
    public float handshakeStartTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[HandshakeManager] Instance initialized");
        }
        else
        {
            Debug.LogWarning("[HandshakeManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Starts the handshake sequence for game completion
    /// </summary>
    /// <param name="outcome">Game outcome: "won", "lost", or "draw"</param>
    /// <param name="playerScore">Player's final score</param>
    /// <param name="opponentScore">Opponent's final score</param>
    /// <returns>Coroutine for the handshake process</returns>
    public Coroutine StartHandshakeSequence(string outcome, int playerScore, int opponentScore)
    {
        if (isHandshakeInProgress)
        {
            Debug.LogWarning("[HandshakeManager] Handshake already in progress, ignoring new request");
            return null;
        }

        Debug.Log($"[HandshakeManager] Starting handshake sequence - Outcome: {outcome}, Scores: {playerScore} vs {opponentScore}");
        
        isHandshakeInProgress = true;
        isHandshakeCompleted = false;
        handshakeStartTime = Time.time;
        
        return StartCoroutine(ExecuteHandshakeSequence(outcome, playerScore, opponentScore));
    }

    /// <summary>
    /// Executes the complete handshake sequence
    /// </summary>
    private IEnumerator ExecuteHandshakeSequence(string outcome, int playerScore, int opponentScore)
    {
        Debug.Log("[HandshakeManager] Step 1: Local game completion confirmed");
        
        // Step 2: Wait for UI to settle and game state to be stable
        Debug.Log($"[HandshakeManager] Step 2: Waiting {uiSettleDelay}s for UI to settle");
        yield return new WaitForSeconds(uiSettleDelay);
        
        // Step 3: Submit match result and wait for handshake confirmation
        Debug.Log("[HandshakeManager] Step 3: Submitting match result to platform");
        
        if (IFrameBridge.Instance != null)
        {
            // Send the result to IFrameBridge (this triggers PostMessage)
            IFrameBridge.Instance.PostMatchResult(outcome, playerScore, opponentScore);
            
            // Wait for handshake confirmation with timeout
            float waitTime = 0f;
            while (waitTime < handshakeTimeout)
            {
                waitTime += Time.deltaTime;
                yield return null;
            }
            
            Debug.Log($"[HandshakeManager] Step 3: Handshake timeout reached ({handshakeTimeout}s)");
        }
        else
        {
            Debug.LogWarning("[HandshakeManager] IFrameBridge not found - skipping score submission");
        }
        
        // Step 4: Additional display time after handshake
        Debug.Log($"[HandshakeManager] Step 4: Waiting {additionalDisplayTime}s additional display time");
        yield return new WaitForSeconds(additionalDisplayTime);
        
        // Step 5: Final display time (like MotorKick)
        Debug.Log($"[HandshakeManager] Step 5: Waiting {finalDisplayTime}s final display time");
        yield return new WaitForSeconds(finalDisplayTime);
        
        // Handshake sequence complete
        isHandshakeCompleted = true;
        isHandshakeInProgress = false;
        
        float totalHandshakeTime = Time.time - handshakeStartTime;
        Debug.Log($"[HandshakeManager] Handshake sequence completed successfully! Total time: {totalHandshakeTime:F2}s");
        
        // Notify that handshake is complete
        OnHandshakeCompleted();
    }

    /// <summary>
    /// Called when handshake sequence is completed
    /// </summary>
    private void OnHandshakeCompleted()
    {
        // You can add additional logic here when handshake completes
        // For example, enable restart button, show next game options, etc.
        Debug.Log("[HandshakeManager] Handshake completed - game can now reset or continue");
    }

    /// <summary>
    /// Checks if handshake is currently in progress
    /// </summary>
    public bool IsHandshakeInProgress()
    {
        return isHandshakeInProgress;
    }

    /// <summary>
    /// Checks if handshake has been completed
    /// </summary>
    public bool IsHandshakeCompleted()
    {
        return isHandshakeCompleted;
    }

    /// <summary>
    /// Gets the current handshake progress (0.0 to 1.0)
    /// </summary>
    public float GetHandshakeProgress()
    {
        if (!isHandshakeInProgress) return 0f;
        
        float elapsed = Time.time - handshakeStartTime;
        float totalExpectedTime = uiSettleDelay + handshakeTimeout + additionalDisplayTime + finalDisplayTime;
        
        return Mathf.Clamp01(elapsed / totalExpectedTime);
    }

    /// <summary>
    /// Resets the handshake state for a new game
    /// </summary>
    public void ResetHandshakeState()
    {
        isHandshakeInProgress = false;
        isHandshakeCompleted = false;
        handshakeStartTime = 0f;
        Debug.Log("[HandshakeManager] Handshake state reset for new game");
    }

    /// <summary>
    /// Forces completion of current handshake (for debugging or emergency use)
    /// </summary>
    public void ForceCompleteHandshake()
    {
        if (isHandshakeInProgress)
        {
            StopAllCoroutines();
            isHandshakeCompleted = true;
            isHandshakeInProgress = false;
            Debug.LogWarning("[HandshakeManager] Handshake force completed");
            OnHandshakeCompleted();
        }
    }
}

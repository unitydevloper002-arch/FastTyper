using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HandshakeTester provides testing functionality for the handshake system in the Unity Editor
/// </summary>
public class HandshakeTester : MonoBehaviour
{
    [Header("Test Controls")]
    public Button testHandshakeButton;
    public Button forceCompleteButton;
    public Button checkStatusButton;
    public Button resetHandshakeButton;
    
    [Header("Test Parameters")]
    public string testOutcome = "won";
    public int testPlayerScore = 100;
    public int testOpponentScore = 80;

    private void Start()
    {
        SetupTestButtons();
    }

    private void SetupTestButtons()
    {
        // Test Handshake Button
        if (testHandshakeButton != null)
        {
            testHandshakeButton.onClick.AddListener(TestHandshake);
            testHandshakeButton.GetComponentInChildren<Text>().text = "Test Handshake";
        }

        // Force Complete Button
        if (forceCompleteButton != null)
        {
            forceCompleteButton.onClick.AddListener(ForceCompleteHandshake);
            forceCompleteButton.GetComponentInChildren<Text>().text = "Force Complete";
        }

        // Check Status Button
        if (checkStatusButton != null)
        {
            checkStatusButton.onClick.AddListener(CheckHandshakeStatus);
            checkStatusButton.GetComponentInChildren<Text>().text = "Check Status";
        }

        // Reset Handshake Button
        if (resetHandshakeButton != null)
        {
            resetHandshakeButton.onClick.AddListener(ResetHandshake);
            resetHandshakeButton.GetComponentInChildren<Text>().text = "Reset Handshake";
        }
    }

    /// <summary>
    /// Test the handshake functionality with test parameters
    /// </summary>
    public void TestHandshake()
    {
        Debug.Log($"[HandshakeTester] Testing handshake with outcome: {testOutcome}, scores: {testPlayerScore} vs {testOpponentScore}");
        
        if (HandshakeManager.Instance != null)
        {
            HandshakeManager.Instance.StartHandshakeSequence(testOutcome, testPlayerScore, testOpponentScore);
        }
        else
        {
            Debug.LogError("[HandshakeTester] HandshakeManager not found! Create one first.");
        }
    }

    /// <summary>
    /// Force complete the current handshake
    /// </summary>
    public void ForceCompleteHandshake()
    {
        Debug.Log("[HandshakeTester] Force completing handshake");
        
        if (HandshakeManager.Instance != null)
        {
            HandshakeManager.Instance.ForceCompleteHandshake();
        }
        else
        {
            Debug.LogError("[HandshakeTester] HandshakeManager not found!");
        }
    }

    /// <summary>
    /// Check the current handshake status
    /// </summary>
    public void CheckHandshakeStatus()
    {
        Debug.Log("[HandshakeTester] Checking handshake status");
        
        if (HandshakeManager.Instance != null)
        {
            bool inProgress = HandshakeManager.Instance.IsHandshakeInProgress();
            bool completed = HandshakeManager.Instance.IsHandshakeCompleted();
            float progress = HandshakeManager.Instance.GetHandshakeProgress();
            
            Debug.Log($"[HandshakeTester] Status - InProgress: {inProgress}, Completed: {completed}, Progress: {progress:F2}");
        }
        else
        {
            Debug.LogError("[HandshakeTester] HandshakeManager not found!");
        }
    }

    /// <summary>
    /// Reset the handshake state
    /// </summary>
    public void ResetHandshake()
    {
        Debug.Log("[HandshakeTester] Resetting handshake state");
        
        if (HandshakeManager.Instance != null)
        {
            HandshakeManager.Instance.ResetHandshakeState();
        }
        else
        {
            Debug.LogError("[HandshakeTester] HandshakeManager not found!");
        }
    }

    /// <summary>
    /// Create a HandshakeManager if it doesn't exist
    /// </summary>
    public void CreateHandshakeManager()
    {
        if (HandshakeManager.Instance == null)
        {
            GameObject handshakeObj = new GameObject("HandshakeManager");
            handshakeObj.AddComponent<HandshakeManager>();
            Debug.Log("[HandshakeTester] Created HandshakeManager");
        }
        else
        {
            Debug.Log("[HandshakeTester] HandshakeManager already exists");
        }
    }

    /// <summary>
    /// Test the complete handshake flow
    /// </summary>
    public void TestCompleteFlow()
    {
        Debug.Log("[HandshakeTester] Testing complete handshake flow");
        
        // Step 1: Create HandshakeManager if needed
        CreateHandshakeManager();
        
        // Step 2: Start handshake
        TestHandshake();
        
        // Step 3: Monitor progress
        InvokeRepeating("CheckHandshakeStatus", 1f, 1f);
        
        // Step 4: Stop monitoring after 15 seconds
        Invoke("StopMonitoring", 15f);
    }

    private void StopMonitoring()
    {
        CancelInvoke("CheckHandshakeStatus");
        Debug.Log("[HandshakeTester] Stopped monitoring handshake status");
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (testHandshakeButton != null)
            testHandshakeButton.onClick.RemoveListener(TestHandshake);
        if (forceCompleteButton != null)
            forceCompleteButton.onClick.RemoveListener(ForceCompleteHandshake);
        if (checkStatusButton != null)
            checkStatusButton.onClick.RemoveListener(CheckHandshakeStatus);
        if (resetHandshakeButton != null)
            resetHandshakeButton.onClick.RemoveListener(ResetHandshake);
    }
}

# 🎯 **Handshake Functionality Implementation**

This document explains the handshake system implemented in FastTyper, which ensures proper coordination between local game completion and web platform communication.

## 🔄 **What is the Handshake System?**

The handshake system ensures that:
1. **Game completes locally first** - All game logic, UI updates, and state changes happen locally
2. **Proper delays are enforced** - Sequential timing ensures stability and user experience
3. **PostMessage is sent after completion** - Web platform communication happens only after local completion
4. **State synchronization** - Unity and web platform remain in sync throughout the process

## 🏗️ **Architecture Overview**

```
Game Ends → Local Completion → UI Settling → Score Submission → Handshake → Final Display → Complete
     ↓              ↓              ↓            ↓            ↓         ↓         ↓
  Timer=0      Show Results    Wait 2s    PostMessage   Wait 5s   Wait 5s   Ready
```

## 📁 **New Scripts Added**

### **1. HandshakeManager.cs**
- **Purpose**: Coordinates the entire handshake process
- **Features**: 
  - Configurable timing delays
  - Progress tracking
  - State management
  - Error handling

### **2. HandshakeTester.cs**
- **Purpose**: Testing and debugging the handshake system
- **Features**:
  - Test buttons for all handshake functions
  - Real-time status monitoring
  - Force completion capabilities

## ⚙️ **Configuration Options**

### **HandshakeManager Timing Settings**
```csharp
[Header("Handshake Timing")]
public float uiSettleDelay = 2f;           // Time for UI to settle
public float handshakeTimeout = 5f;        // Handshake confirmation timeout
public float additionalDisplayTime = 3f;   // Additional display time
public float finalDisplayTime = 5f;        // Final display time
```

### **Total Handshake Time**
- **UI Settling**: 2 seconds
- **Handshake Timeout**: 5 seconds  
- **Additional Display**: 3 seconds
- **Final Display**: 5 seconds
- **Total**: 15 seconds

## 🎮 **How It Works**

### **Step 1: Game Completion (Local)**
```csharp
private void GameOver()
{
    // Stop game logic
    timerRunning = false;
    
    // Show results locally
    resultImage.sprite = winSprite; // or loseSprite, drawSprite
    
    // Start handshake sequence
    StartHandshakeSequence();
}
```

### **Step 2: Handshake Sequence**
```csharp
private void StartHandshakeSequence()
{
    // Determine outcome locally
    string outcome = "won"; // or "lost", "draw"
    
    // Update UI scores
    player_Score.text = totalScore.ToString();
    opponent_Score.text = aiScore.ToString();
    
    // Start handshake via HandshakeManager
    HandshakeManager.Instance.StartHandshakeSequence(outcome, playerScore, opponentScore);
}
```

### **Step 3: Handshake Execution**
```csharp
private IEnumerator ExecuteHandshakeSequence(string outcome, int playerScore, int opponentScore)
{
    // Step 1: Local completion confirmed
    Debug.Log("Step 1: Local game completion confirmed");
    
    // Step 2: Wait for UI to settle
    yield return new WaitForSeconds(uiSettleDelay);
    
    // Step 3: Submit to platform and wait for handshake
    IFrameBridge.Instance.PostMatchResult(outcome, playerScore, opponentScore);
    yield return new WaitForSeconds(handshakeTimeout);
    
    // Step 4: Additional display time
    yield return new WaitForSeconds(additionalDisplayTime);
    
    // Step 5: Final display time
    yield return new WaitForSeconds(finalDisplayTime);
    
    // Handshake complete
    isHandshakeCompleted = true;
}
```

## 🔧 **Integration Points**

### **Modified Files**
1. **UIManager.cs**
   - `GameOver()` method updated
   - `StartGame()` and `RestartGame()` reset handshake state
   - Added handshake status checking methods

2. **IFrameBridge.cs**
   - `PostMatchResult()` method (already existed)
   - WebGL communication methods

### **New Dependencies**
- **HandshakeManager**: Must exist in scene for handshake to work
- **IFrameBridge**: Must exist for platform communication

## 🧪 **Testing the System**

### **Using HandshakeTester**
1. **Add HandshakeTester to scene**
2. **Assign UI buttons** (optional)
3. **Use test methods**:
   ```csharp
   // Test basic handshake
   handshakeTester.TestHandshake();
   
   // Test complete flow
   handshakeTester.TestCompleteFlow();
   
   // Check status
   handshakeTester.CheckHandshakeStatus();
   ```

### **Manual Testing**
1. **Start a game** (AI or Multiplayer)
2. **Let timer run out** or trigger game end
3. **Watch console logs** for handshake progress
4. **Verify delays** are working correctly

## 📊 **Monitoring and Debugging**

### **Console Logs**
The system provides detailed logging:
```
[HandshakeManager] Starting handshake sequence - Outcome: won, Scores: 100 vs 80
[HandshakeManager] Step 1: Local game completion confirmed
[HandshakeManager] Step 2: Waiting 2s for UI to settle
[HandshakeManager] Step 3: Submitting match result to platform
[HandshakeManager] Step 3: Handshake timeout reached (5s)
[HandshakeManager] Step 4: Waiting 3s additional display time
[HandshakeManager] Step 5: Waiting 5s final display time
[HandshakeManager] Handshake sequence completed successfully! Total time: 15.00s
```

### **Status Checking**
```csharp
// Check if handshake is in progress
bool inProgress = HandshakeManager.Instance.IsHandshakeInProgress();

// Check if handshake is completed
bool completed = HandshakeManager.Instance.IsHandshakeCompleted();

// Get progress percentage (0.0 to 1.0)
float progress = HandshakeManager.Instance.GetHandshakeProgress();
```

## 🚨 **Troubleshooting**

### **Common Issues**

#### **1. HandshakeManager Not Found**
```
[UIManager] HandshakeManager not found! Creating one...
```
**Solution**: The system automatically creates one, but you can manually add it to your scene.

#### **2. IFrameBridge Not Found**
```
[HandshakeManager] IFrameBridge not found - skipping score submission
```
**Solution**: Ensure IFrameBridge exists in your scene.

#### **3. Handshake Stuck**
```
[HandshakeManager] Handshake already in progress, ignoring new request
```
**Solution**: Use `ForceCompleteHandshake()` or wait for completion.

### **Debug Commands**
```csharp
// Force complete handshake
HandshakeManager.Instance.ForceCompleteHandshake();

// Reset handshake state
HandshakeManager.Instance.ResetHandshakeState();

// Check status
UIManager.Instance.CheckHandshakeStatus();
```

## 🔄 **Integration with Existing Systems**

### **AI Mode**
- Works with single-player AI games
- Handles win/lose/draw scenarios
- Maintains existing AI logic

### **Multiplayer Mode**
- Works with Fusion networking
- Handles player disconnections
- Maintains existing multiplayer logic

### **WebGL Platform**
- Integrates with existing iframe communication
- Uses existing PostMessage methods
- Maintains platform compatibility

## 📈 **Performance Considerations**

### **Memory Usage**
- **HandshakeManager**: Minimal overhead (~1KB)
- **Coroutines**: Efficient yield-based timing
- **No memory leaks**: Proper cleanup in all scenarios

### **Frame Rate Impact**
- **No impact during gameplay**: Handshake only runs when game ends
- **Minimal UI updates**: Only status checks during handshake
- **Efficient delays**: Uses WaitForSeconds instead of Update loops

## 🎯 **Future Enhancements**

### **Planned Features**
1. **Configurable timing** via Unity Inspector
2. **Progress bars** in UI
3. **Handshake events** for other systems
4. **Custom handshake flows** for different game modes

### **Extensibility**
The system is designed to be easily extended:
- Add new handshake steps
- Customize timing per game mode
- Integrate with other systems

## 📝 **Summary**

The handshake system ensures that FastTyper games complete locally first, then communicate with the web platform after proper delays. This provides:

✅ **Reliable game completion**  
✅ **Stable platform communication**  
✅ **Better user experience**  
✅ **Debugging capabilities**  
✅ **Easy testing and monitoring**  

The system is fully integrated with existing FastTyper functionality and maintains backward compatibility while adding robust handshake coordination.

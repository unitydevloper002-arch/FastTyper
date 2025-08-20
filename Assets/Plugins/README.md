# FastTyper IFrameBridge Integration

This document describes the complete IFrameBridge integration implementation for the FastTyper Unity project, enabling seamless platform integration for tournament and gaming platforms.

## 🏗️ Architecture Overview

The IFrameBridge system provides a complete communication layer between Unity WebGL builds and external gaming platforms:

```
Platform → HTML → JavaScript → C# → Game Logic
Game Logic → C# → JavaScript → HTML → Platform
```

## 📁 File Structure

```
Assets/
├── Scripts/
│   ├── IFrameBridge.cs              # Main C# bridge component
│   ├── UIManager.cs                 # Enhanced with match result integration
│   └── FusionBootstrap.cs           # Enhanced with IFrameBridge methods
├── Plugins/
│   ├── IFrameBridge.jslib           # JavaScript communication library
│   ├── test_iframe.html             # Test page for development
│   └── README.md                    # This documentation
└── WebGLTemplates/
    └── FastTyper/
        └── index.html               # WebGL template with parameter handling
```

## 🚀 Key Features

### 1. **Automatic Game Mode Detection**
- **AI Mode**: Detected when opponent ID starts with "a9" (Easy) or "b9" (Hard)
- **Multiplayer Mode**: Detected for human opponent IDs
- **Replay Mode**: Supports replay functionality via URL parameters

### 2. **Direct Game Start**
- **No Mode Selection**: Games automatically start from countdown, bypassing selection screens
- **URL-Based Configuration**: Game mode and AI difficulty determined from URL parameters
- **Seamless Experience**: Players go directly from loading to gameplay

### 3. **Platform Communication**
- **Match Results**: Sends outcomes (won/lost/draw) with scores
- **Match Aborts**: Handles early termination scenarios
- **Game States**: Real-time game state updates
- **Build Version**: Reports game version to platform

### 4. **Error Handling**
- **Connection Failures**: Comprehensive error reporting
- **Player Disconnections**: Automatic forfeit handling
- **Game Crashes**: Critical error reporting
- **Resource Failures**: Load failure notifications

### 4. **Mobile Support**
- **Mobile Detection**: Automatic mobile WebGL detection
- **Responsive Design**: Mobile-optimized WebGL template
- **Touch Controls**: Mobile-friendly interface

## 🎮 Game Modes

### AI Mode (Singleplayer)
```json
{
  "matchId": "ai_match_001",
  "playerId": "human_player",
  "opponentId": "a912345678"  // Easy AI
  // or "b912345678" for Hard AI
}
```

### Multiplayer Mode
```json
{
  "matchId": "multi_match_001",
  "playerId": "player1",
  "opponentId": "player2"
}
```

### Test Mode
```json
{
  "testMode": "ai"  // or "multiplayer"
}
```

## 🔧 Implementation Details

### 1. IFrameBridge.cs

The main C# component that handles:
- **URL Parameter Extraction**: Gets match parameters from WebGL
- **Game Mode Detection**: Automatically determines AI vs Multiplayer
- **Platform Communication**: Sends results and states to platform
- **Error Handling**: Comprehensive error reporting

#### Key Methods:
```csharp
// Initialize game with parameters from platform
public void InitParamsFromJS(string json)

// Send match results to platform
public void PostMatchResult(string outcome, int score, int opponentScore)

// Send match abort to platform
public void PostMatchAbort(string message, string error = "", string errorCode = "")

// Send game state updates
public void PostGameState(string state)
```

### 2. IFrameBridge.jslib

JavaScript library for WebGL communication:
- **URL Parameter Extraction**: Gets parameters from iframe URL
- **postMessage Communication**: Sends messages to parent window
- **Mobile Detection**: Detects mobile browsers
- **Message Listening**: Handles incoming platform messages

#### Key Functions:
```javascript
// Get URL parameters
GetURLParameters()

// Send postMessage to parent
SendPostMessage(message)

// Check if mobile
IsMobileWeb()

// Setup message listener
SetupMessageListener()
```

### 3. WebGL Template (index.html)

Custom WebGL template that:
- **Parameter Handling**: Extracts and validates URL parameters
- **Test Mode Support**: Handles test mode configurations
- **Mobile Optimization**: Mobile-responsive design
- **Loading Screen**: Professional loading experience

### 4. UIManager Integration

Enhanced UIManager with:
- **Match Result Reporting**: Sends results to platform when game ends
- **AI Mode Integration**: Proper AI difficulty handling
- **Multiplayer Integration**: Fusion networking support
- **Auto-Start System**: Bypasses mode selection screens for direct gameplay

#### Auto-Start Implementation:
```csharp
// New method for automatic game start
public void AutoStartGame(GAMEMODE gameMode, GAMETYPE aiType = GAMETYPE.Easy)

// Called by IFrameBridge to start game directly
UIManager.Instance.AutoStartGame(GAMEMODE.SinglePlayer, GAMETYPE.Hard);
```

**Flow:**
1. **URL Parameters** → IFrameBridge detects game mode
2. **AutoStartGame()** → Bypasses selection screens
3. **Countdown** → Starts immediately for AI, waits for players in multiplayer
4. **Gameplay** → Direct transition to game

## 🧪 Testing

### Test Page Usage

1. **Open test_iframe.html** in a web browser
2. **Click test mode buttons** to load different configurations:
   - AI Mode (Easy)
   - AI Mode (Hard)
   - Multiplayer Mode
   - Test Mode
3. **Use platform controls** to simulate platform interactions:
   - Pause/Resume Game
   - Simulate Timeout
   - Simulate Connection Loss
   - Load Replay Data

### URL Parameters for Testing

```bash
# AI Mode (Easy)
http://localhost:8080/index.html?matchId=ai_test&playerId=human&opponentId=a912345678

# AI Mode (Hard)
http://localhost:8080/index.html?matchId=ai_test&playerId=human&opponentId=b912345678

# Multiplayer Mode
http://localhost:8080/index.html?matchId=multi_test&playerId=player1&opponentId=player2

# Test Mode
http://localhost:8080/index.html?testMode=ai
```

## 📡 Platform Integration

### Message Types

#### From Game to Platform:
```javascript
// Game Ready
{
  "type": "game_ready",
  "payload": {
    "timestamp": 1234567890
  }
}

// Match Result
{
  "type": "match_result",
  "payload": {
    "matchId": "match_001",
    "playerId": "player1",
    "opponentId": "player2",
    "outcome": "won",
    "score": 100
  }
}

// Match Abort
{
  "type": "match_abort",
  "payload": {
    "message": "Player disconnected",
    "error": "Connection lost",
    "errorCode": "CONNECTION_ERROR"
  }
}

// Game State
{
  "type": "game_state",
  "payload": {
    "state": "{\"score\":100,\"time\":120}"
  }
}
```

#### From Platform to Game:
```javascript
// Pause Game
{
  "type": "pause_game",
  "payload": {}
}

// Resume Game
{
  "type": "resume_game",
  "payload": {}
}

// Player Timeout
{
  "type": "player_timeout",
  "payload": {
    "playerId": "opponent_player"
  }
}

// Load Replay
{
  "type": "load_replay",
  "payload": {
    "states": ["state1", "state2", "state3"]
  }
}
```

## 🔄 Build Process

### 1. WebGL Build Settings
- **Template**: Select "FastTyper" template
- **Compression**: Enable compression for smaller builds
- **Development Build**: Disable for production

### 2. Build Steps
1. **Build Settings** → **WebGL** → **Player Settings**
2. **WebGL Template**: Select "FastTyper"
3. **Build** → **Build And Run**

### 3. Deployment
- Upload build files to web server
- Ensure CORS is properly configured
- Test with platform integration

## 🐛 Troubleshooting

### Common Issues

1. **IFrameBridge not found**
   - Ensure IFrameBridge prefab is in scene
   - Check that IFrameBridge.cs is attached to GameObject

2. **URL parameters not received**
   - Verify WebGL template is selected
   - Check browser console for errors
   - Ensure parameters are properly formatted

3. **Multiplayer not working**
   - Verify FusionBootstrap is in scene
   - Check Fusion networking setup
   - Ensure proper session configuration

4. **Mobile detection issues**
   - Test on actual mobile device
   - Check user agent detection
   - Verify touch input handling

### Debug Logging

Enable debug logging by checking the console for:
- `[IFrameBridge]` messages
- `[WebGL]` messages
- `[FusionBootstrap]` messages

## 📋 Integration Checklist

- [ ] IFrameBridge.cs implemented and tested
- [ ] IFrameBridge.jslib deployed and working
- [ ] WebGL template configured
- [ ] UIManager integration complete
- [ ] FusionBootstrap methods added
- [ ] Test page working
- [ ] Mobile testing completed
- [ ] Platform communication verified
- [ ] Error handling tested
- [ ] Build process documented

## 🎯 Platform Integration Guide

For platform developers integrating FastTyper:

1. **Embed the WebGL build** in an iframe
2. **Pass match parameters** via URL query string
3. **Listen for postMessage events** from the game
4. **Send control messages** to the game as needed
5. **Handle match results** and aborts appropriately

Example platform integration:
```html
<iframe 
  src="https://your-domain.com/fasttyper/index.html?matchId=123&playerId=456&opponentId=789"
  width="800" 
  height="600">
</iframe>

<script>
window.addEventListener('message', function(event) {
  if (event.data.type === 'match_result') {
    // Handle match result
    console.log('Match ended:', event.data.payload.outcome);
  }
});
</script>
```

## 📞 Support

For questions or issues with the IFrameBridge integration:
1. Check the console logs for error messages
2. Verify all files are properly deployed
3. Test with the provided test page
4. Review this documentation for configuration details

---

**FastTyper IFrameBridge Integration** - Complete platform integration solution for tournament and gaming platforms.

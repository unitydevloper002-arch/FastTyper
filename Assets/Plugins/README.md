# FastTyper IFrameBridge Implementation

This implementation follows the Aurealone Game Development Guide for integrating multiplayer games in iframes.

## Overview

The IFrameBridge system allows the FastTyper game to be embedded in iframes and communicate with the parent platform using the browser's `postMessage` API.

## Files

- `IFrameBridge.cs` - Main Unity script that handles game initialization and communication
- `IFrameBridge.jslib` - JavaScript library for WebGL communication
- `test_iframe.html` - Test page demonstrating iframe integration

## How It Works

### 1. URL Parameter Parsing

The game receives three query parameters from the iframe URL:
- `matchId` - Unique ID for this match
- `playerId` - Current player's ID on the platform
- `opponentId` - Matched opponent's ID

Example URL:
```
https://games.yourdomain.com/fasttyper/index.html?matchId=abc123&playerId=player456&opponentId=player789
```

### 2. Game Mode Detection

The system automatically detects the game mode based on the opponent ID:
- **Bot Mode**: If `opponentId` starts with "a9" (Easy) or "b9" (Hard)
- **Multiplayer Mode**: If `opponentId` is a regular player ID

### 3. Bot Winning Probability

According to the documentation, bots have a 60% probability of winning in gameplay scenarios.

### 4. Communication with Parent Platform

The game sends three types of messages to the parent window:

#### Match Result (Normal End)
```javascript
{
    type: 'match_result',
    payload: {
        matchId: 'abc123',
        playerId: 'player456',
        opponentId: 'player789',
        outcome: 'won', // 'won' or 'lost' or 'draw'
        score: 123 // optional
    }
}
```

#### Match Abort (Error/Disconnect)
```javascript
{
    type: 'match_abort',
    payload: {
        message: "Opponent left the game.", // reason for abort
        error: "WebSocket connection lost", // error if occurs
        errorCode: "1234" // for debugging
    }
}
```

#### Game State (Periodic Updates)
```javascript
{
    type: 'game_state',
    payload: {
        state: "{}" // JSON string of current game state
    }
}
```

### 5. Replay Functionality

The game supports replay mode when accessed with `?replay=true` parameter. The parent window can send replay data:

```javascript
{
    type: 'load_replay',
    payload: {
        states: [
            '{"score": 100, "time": 30}',
            '{"score": 200, "time": 60}',
            '{"score": 300, "time": 90}'
        ]
    }
}
```

## Implementation Details

### Unity Script (IFrameBridge.cs)

Key methods:
- `InitParamsFromJS(string json)` - Initialize game with URL parameters
- `PostMatchResult(string outcome, int score, int opponentScore)` - Send match result
- `PostMatchAbort(string message, string error, string errorCode)` - Send abort message
- `PostGameState(string state)` - Send game state updates
- `OnLoadReplay(string statesJson)` - Handle replay data from parent

### JavaScript Library (IFrameBridge.jslib)

Key functions:
- `GetURLParameters()` - Extract parameters from iframe URL
- `SendPostMessage(string message)` - Send messages to parent window
- `SetupMessageListener()` - Listen for messages from parent
- `SendGameReady()` - Signal that game is ready
- `SendBuildVersion(string version)` - Send build version info

## Testing

Use the `test_iframe.html` file to test the iframe integration:

1. Open the HTML file in a web browser
2. Modify the test parameters (Match ID, Player ID, Opponent ID)
3. Check "Replay Mode" to test replay functionality
4. Watch the message log for communication between game and parent

## Integration Steps

1. **Build Settings**: Ensure your Unity project is set to WebGL platform
2. **Plugins Folder**: The `IFrameBridge.jslib` file must be in the `Assets/Plugins` folder
3. **Scene Setup**: Add the IFrameBridge component to a GameObject in your scene
4. **Build**: Build your game for WebGL
5. **Deploy**: Upload the built files to your web server
6. **Embed**: Use the iframe code in your platform:

```html
<iframe
    src="https://games.yourdomain.com/fasttyper/index.html?matchId=abc123&playerId=player456&opponentId=player789"
    width="800"
    height="600"
    sandbox="allow-scripts allow-same-origin">
</iframe>
```

## Error Handling

The system includes comprehensive error handling:
- URL parameter validation
- Network connection errors
- Game initialization failures
- Player disconnections
- Critical game errors

All errors are reported to the parent platform via `match_abort` messages with appropriate error codes.

## Bot Integration

Bots are automatically detected and initialized based on opponent ID:
- `a9` prefix = Easy bot
- `b9` prefix = Hard bot
- 60% winning probability for bots

## Security

The iframe uses appropriate sandbox attributes:
- `allow-scripts` - Required for game functionality
- `allow-same-origin` - Required for WebGL communication
- Other permissions can be added as needed

## Support

This implementation follows the official Aurealone Game Development Guide and should be compatible with the platform's requirements. For customer support issues, the replay functionality allows games to be replayed for debugging purposes.

mergeInto(LibraryManager.library, {
    // Get URL parameters from the iframe URL
    GetURLParameters: function() {
        try {
            const urlParams = new URLSearchParams(window.location.search);
            const matchId = urlParams.get('matchId') || '';
            const playerId = urlParams.get('playerId') || '';
            const opponentId = urlParams.get('opponentId') || '';
            const replay = urlParams.get('replay') === 'true';
            const testMode = urlParams.get('testMode') || '';
            
            const params = {
                matchId: matchId,
                playerId: playerId,
                opponentId: opponentId,
                replay: replay
            };
            
            // Handle test mode parameters
            if (testMode) {
                console.log('[IFrameBridge.jslib] Test mode detected:', testMode);
                switch (testMode) {
                    case "ai":
                        params.matchId = params.matchId || "test_ai";
                        params.playerId = params.playerId || "human_player";
                        params.opponentId = params.opponentId || "b912345678";
                        break;
                    case "multiplayer":
                        params.matchId = params.matchId || "test_multiplayer";
                        params.playerId = params.playerId || "human_player_1";
                        params.opponentId = params.opponentId || "human_player_2";
                        break;
                    default:
                        params.matchId = params.matchId || "test_default";
                        params.playerId = params.playerId || "human_player";
                        params.opponentId = params.opponentId || "b912345678";
                }
            }
            
            const jsonString = JSON.stringify(params);
            const bufferSize = lengthBytesUTF8(jsonString) + 1;
            const buffer = _malloc(bufferSize);
            stringToUTF8(jsonString, buffer, bufferSize);
            
            console.log('[IFrameBridge.jslib] URL Parameters:', params);
            return buffer;
        } catch (error) {
            console.error('[IFrameBridge.jslib] Error getting URL parameters:', error);
            return null;
        }
    },

    // Send postMessage to parent window
    SendPostMessage: function(message) {
        try {
            const messageStr = UTF8ToString(message);
            console.log('[IFrameBridge.jslib] Sending postMessage:', messageStr);
            
            if (window.parent && window.parent !== window) {
                window.parent.postMessage(JSON.parse(messageStr), '*');
            } else {
                console.warn('[IFrameBridge.jslib] No parent window found for postMessage');
            }
        } catch (error) {
            console.error('[IFrameBridge.jslib] Error sending postMessage:', error);
        }
    },

    // Send build version to parent
    SendBuildVersion: function(version) {
        try {
            const versionStr = UTF8ToString(version);
            console.log('[IFrameBridge.jslib] Build version:', versionStr);
            
            const message = {
                type: 'build_version',
                payload: {
                    version: versionStr
                }
            };
            
            if (window.parent && window.parent !== window) {
                window.parent.postMessage(message, '*');
            }
        } catch (error) {
            console.error('[IFrameBridge.jslib] Error sending build version:', error);
        }
    },

    // Send game ready signal
    SendGameReady: function() {
        try {
            console.log('[IFrameBridge.jslib] Sending game ready signal');
            
            const message = {
                type: 'game_ready',
                payload: {
                    timestamp: Date.now()
                }
            };
            
            if (window.parent && window.parent !== window) {
                window.parent.postMessage(message, '*');
            }
        } catch (error) {
            console.error('[IFrameBridge.jslib] Error sending game ready:', error);
        }
    },

    // Check if running on mobile web
    IsMobileWeb: function() {
        try {
            const ua = navigator.userAgent || navigator.vendor || window.opera;
            let isCoarse = false;
            try { 
                isCoarse = window.matchMedia && window.matchMedia('(pointer: coarse)').matches; 
            } catch(e) {}
            const isMobileUA = /android|iphone|ipad|ipod|iemobile|blackberry|mobile/i.test(ua);
            const isMobile = isMobileUA || isCoarse;
            console.log('[IFrameBridge.jslib] Is mobile web:', isMobile);
            return isMobile ? 1 : 0;
        } catch (error) {
            console.error('[IFrameBridge.jslib] Error checking mobile web:', error);
            return 0;
        }
    },

    // Listen for messages from parent window
    SetupMessageListener: function() {
        try {
            window.addEventListener('message', function(event) {
                console.log('[IFrameBridge.jslib] Received message from parent:', event.data);
                
                if (event.data && event.data.type === 'load_replay') {
                    // Handle replay data
                    const states = event.data.payload.states;
                    if (states && states.length > 0) {
                        const replayData = {
                            states: states
                        };
                        
                        // Call Unity method to handle replay
                        if (typeof SendMessage !== 'undefined') {
                            SendMessage('IFrameBridge', 'OnLoadReplay', JSON.stringify(replayData));
                        }
                    }
                }
                
                // Handle other message types
                if (event.data && event.data.type === 'pause_game') {
                    if (typeof SendMessage !== 'undefined') {
                        SendMessage('IFrameBridge', 'OnGamePaused');
                    }
                }
                
                if (event.data && event.data.type === 'resume_game') {
                    if (typeof SendMessage !== 'undefined') {
                        SendMessage('IFrameBridge', 'OnGameResumed');
                    }
                }
                
                if (event.data && event.data.type === 'player_timeout') {
                    if (typeof SendMessage !== 'undefined') {
                        SendMessage('IFrameBridge', 'OnPlayerTimeout', event.data.payload.playerId);
                    }
                }
                
                if (event.data && event.data.type === 'connection_lost') {
                    if (typeof SendMessage !== 'undefined') {
                        SendMessage('IFrameBridge', 'OnConnectionLost');
                    }
                }
            });
            
            console.log('[IFrameBridge.jslib] Message listener setup complete');
        } catch (error) {
            console.error('[IFrameBridge.jslib] Error setting up message listener:', error);
        }
    }
});

// Initialize message listener when script loads
if (typeof window !== 'undefined') {
    window.addEventListener('load', function() {
        if (typeof SetupMessageListener !== 'undefined') {
            SetupMessageListener();
        }
    });
}

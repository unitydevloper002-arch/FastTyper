mergeInto(LibraryManager.library, {
    // Get URL parameters from the iframe URL
    GetURLParameters: function() {
        try {
            var urlParams = new URLSearchParams(window.location.search);
            var matchId = urlParams.get('matchId');
            var playerId = urlParams.get('playerId');
            var opponentId = urlParams.get('opponentId');
            var replay = urlParams.get('replay') === 'true';
            
            var params = {
                matchId: matchId || '',
                playerId: playerId || '',
                opponentId: opponentId || '',
                replay: replay
            };
            
            var jsonString = JSON.stringify(params);
            var bufferSize = lengthBytesUTF8(jsonString) + 1;
            var buffer = _malloc(bufferSize);
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
            var messageStr = UTF8ToString(message);
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
            var versionStr = UTF8ToString(version);
            console.log('[IFrameBridge.jslib] Build version:', versionStr);
            
            var message = {
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
            
            var message = {
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
            var isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
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
                    var states = event.data.payload.states;
                    if (states && states.length > 0) {
                        var replayData = {
                            states: states
                        };
                        
                        // Call Unity method to handle replay
                        if (typeof SendMessage !== 'undefined') {
                            SendMessage('IFrameBridge', 'OnLoadReplay', JSON.stringify(replayData));
                        }
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

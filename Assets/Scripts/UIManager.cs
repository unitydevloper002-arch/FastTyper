using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using DG.Tweening;
using System;
using System.Linq;
using UnityEngine.EventSystems;

public enum GAMEMODE
{
    SinglePlayer,
    MultiPlayer
}

public enum GAMETYPE
{
    Easy,
    Hard
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Screen")]
    public GameObject selectModeScreen;
    public GameObject selectTypeScreen;
    public GameObject gamePlayScreen;
    public GameObject countdownScreen;
    public GameObject gameOverScreen;

    [Header("Word Source")]
    public bool loadWordsFromTextFile = true;
    public string resourcesWordsPath = "words"; // Assets/Resources/words.txt
    public string streamingAssetsFileName = "words.txt"; // Assets/StreamingAssets/words.txt

    [Header("Game State")]
    public GAMEMODE currentGameMode;
    public GAMETYPE currentAITypes;

    [Header("GamePlay_Area")]
    public TextMeshProUGUI timerText;
    private float timeRemaining = 90f;
    private bool timerRunning = false;
    private double endTime; // UTC timestamp when timer should finish

    public List<string> allWords = new List<string>();
    public List<Vector3> positionOfWords = new List<Vector3>();

    public GameObject wordItemPrefab;
    public Transform wordItemParent;
    public Transform wordItemParentAI; // AI column parent

    public Image gameHighLightImage;
    public Image gameHighLightImageAI;

    public Sprite correctHighlightSprite;
    public Sprite wrongHighlightSprite;
    public Sprite wordHighLightSprite;


    public Sprite rightSprite;
    public Sprite wrongSprite;

    [Header("Animation Settings")]
    public bool ignoreTimeScale = true; // If true, tweens ignore Time.timeScale

    [Header("Typing")]
    public TMP_InputField inputField;
    private bool currentWordFailed = false;
    private bool currentWordCompleted = false;
    private float typingLockUntil = 0f;

    [Header("Score")]
    public TextMeshProUGUI scoreText;
    public int totalScore = 0;
    public TextMeshProUGUI aiScoreText;
    public int aiScore = 0;

    [Header("AI Settings")]
    public Vector2 easyDelayRange = new Vector2(1.8f, 2.8f); // seconds per word (slower)
    public Vector2 hardDelayRange = new Vector2(1.2f, 2.0f); // seconds per word (still slower than before)

    // Independent indices and shift locks for Player and AI
    private int visibleStartIndexPlayer = 0;
    private int visibleStartIndexAI = 0;
    private bool isPlayerShifting = false;
    private bool isAIShifting = false;

    [Header("GameOver")]
    public Image resultImage;

    public TextMeshProUGUI player_Score;
    public TextMeshProUGUI opponent_Score; 

    public TextMeshProUGUI Player_WordTyped;
    public int Player_WordTyped_Count;
    public TextMeshProUGUI Opponent_WordTyped;
    public int Opponent_WordTyped_Count;

    public TextMeshProUGUI Player_Accuracy; 
    public int Player_Accuracy_Percentage;
    public TextMeshProUGUI Opponent_Accuracy;
    public int Opponent_Accuracy_Percentage;

    public TextMeshProUGUI Player_lettersPerSecond; 
    public int Player_lettersPerSecond_Count;
    public TextMeshProUGUI Opponent_lettersPerSecond;
    public int Opponent_lettersPerSecond_Count;

    public Sprite winSprite;
    public Sprite loseSprite;
    public Sprite drawSprite;

    public int localPlayerId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        
    }

    // New method called by IFrameBridge to automatically start the game
    public void AutoStartGame(GAMEMODE gameMode, GAMETYPE aiType = GAMETYPE.Easy)
    {
        Debug.Log($"[UIManager] Auto-starting game - Mode: {gameMode}, AI Type: {aiType}");
        
        // Set the game mode and AI type
        currentGameMode = gameMode;
        currentAITypes = aiType;
        
        // Hide all selection screens
        if (selectModeScreen != null)
            selectModeScreen.SetActive(false);
        if (selectTypeScreen != null)
            selectTypeScreen.SetActive(false);
        
        // For multiplayer mode, start Fusion networking and show waiting screen
        if (gameMode == GAMEMODE.MultiPlayer)
        {
            if (FusionBootstrap.Instance != null)
            {
                // Start Fusion networking with the match ID from IFrameBridge
                string matchId = IFrameBridge.MatchId;
                FusionBootstrap.Instance.StartMultiplayerGame(matchId);
            }
            else
            {
                Debug.LogError("[UIManager] FusionBootstrap not found for multiplayer mode!");
            }
            
            // Show waiting screen for multiplayer
            countdownScreen.SetActive(true);
            countdownText.text = "Waiting...opponent";
            countdownText.gameObject.GetComponent<CanvasGroup>().alpha = 1;
            countdownText.gameObject.transform.localScale = Vector3.one;
        }
        else
        {
            // For AI mode, start countdown immediately
            IsCountDownStart = true;
            countdownScreen.SetActive(true);
            StartCoroutine(StartCountdown_Animation());
        }
        
        Debug.Log($"[UIManager] Game auto-started successfully - Mode: {gameMode}, AI Type: {aiType}");
    }

    void Update()
    {
        if (timerRunning)
        {
            double timeRemainings = endTime - GetUnixTimeNow();

            if (timeRemainings > 0)
            {
                //timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay((float)timeRemainings);
            }
            else
            {
                timeRemaining = 0;
                timerRunning = false;
                Debug.Log("⏰ Timer Finish!");
                GameOver();
            }
        }

		// Click anywhere to refocus invisible input for typing (PC only)
		if (inputField != null && gamePlayScreen != null && gamePlayScreen.activeInHierarchy)
		{
			bool isMobile = Application.isMobilePlatform || (IFrameBridge.Instance != null && IFrameBridge.Instance.IsMobileWebGL());
			
			if (!isMobile && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
			{
				EnsureInputFocus();
			}
		}

        RunFillImage();
        RunGameFillImage();
    }

    private void UpdateTimerDisplay(float timeToDisplay)
    {
        if (timerText == null) return;
        if (timeToDisplay < 0f) timeToDisplay = 0f;
        int minutes = Mathf.FloorToInt(timeToDisplay / 60f);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    double GetUnixTimeNow()
    {
        return (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
    }

    public void StartGame()
    {
		timeRemaining = 90f;
		endTime = GetUnixTimeNow() + timeRemaining;
		timerRunning = true;

		if (inputField != null)
		{
			inputField.onValueChanged.AddListener(OnTyping);
			HideInputFieldVisuals();
			EnsureInputFocus();
			
			// Prevent device keyboard on mobile
			if (Application.isMobilePlatform || (IFrameBridge.Instance != null && IFrameBridge.Instance.IsMobileWebGL()))
			{
				inputField.shouldHideMobileInput = true;
				inputField.readOnly = true;
				inputField.interactable = false; // Completely disable interaction
				// Force hide any open keyboard
				if (TouchScreenKeyboard.isSupported)
				{
					TouchScreenKeyboard.hideInput = true;
				}
			}
		}

		// 1) Load words from file (if enabled) directly into allWords
		if (loadWordsFromTextFile)
		{
			var loaded = LoadWordsFromConfiguredSources();
			if (loaded.Count > 0)
			{
				allWords = loaded;
			}
		}
		// Ensure list exists
		if (allWords == null) allWords = new List<string>();
		Debug.Log($"[UIManager] Loaded words count: {allWords.Count}");

		if (currentGameMode == GAMEMODE.SinglePlayer)
		{
			// 2) Shuffle the list once per game start
			ShuffleListInPlace(allWords, new System.Random());

			CreateWordItem();

			// Start AI loop when gameplay starts (optional to move into StartGame)
			StartCoroutine(AILoop());
		}
		else
		{
			Debug.Log("IS_Multiplayer");
			if (FusionBootstrap.Instance.runner.IsSharedModeMasterClient)
			{
				FusionBridge.Instance.StartMultiplayerGame();
			}
		}

		// Force show keyboard when gameplay starts
		if (KeyboardManager.Instance != null)
		{
			KeyboardManager.Instance.ForceShowKeyboard();
		}
    }

    public void StartMultiplayerWords(int seed)
    {
		// Load words from file if configured and not already loaded
		if (loadWordsFromTextFile && (allWords == null || allWords.Count == 0))
		{
			var loaded = LoadWordsFromConfiguredSources();
			if (loaded.Count > 0)
			{
				allWords = loaded;
			}
		}
		if (allWords == null) allWords = new List<string>();
		Debug.Log($"[UIManager] Loaded words count (MP): {allWords.Count}");

		// Deterministic shuffle using shared seed
		ShuffleListInPlace(allWords, new System.Random(seed));
		CreateWordItem();
    }

    
    public void CreateWordItem()
    {
        if (allWords == null) allWords = new List<string>();
        visibleStartIndexPlayer = 0;
        visibleStartIndexAI = 0;

        for (int i = 0; i < positionOfWords.Count; i++)
        {
            GameObject wordItem = Instantiate(wordItemPrefab, wordItemParent);
            wordItem.name = "WordItem_" + i;
            wordItem.transform.localPosition = positionOfWords[i];

            // Animate scale to target for this slot
            Vector3 targetScale = GetTargetScaleForSlotIndex(i);
            wordItem.transform.localScale = Vector3.one * 0.8f;
            wordItem.transform.DOScale(targetScale, 0.25f).SetEase(Ease.OutBack).SetUpdate(ignoreTimeScale);

            // Assign sequential word text: child i => allWords[visibleStartIndex + i]
            AssignWordForItem(wordItem, visibleStartIndexPlayer + i);

            if (i == 0 || i == 1)
            {
                wordItem.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            }

            // Also create a mirror AI item if parent is assigned
            if (wordItemParentAI != null)
            {
                GameObject aiItem = Instantiate(wordItemPrefab, wordItemParentAI);
                aiItem.name = "AI_WordItem_" + i;
                aiItem.transform.localPosition = positionOfWords[i];
                aiItem.transform.localScale = Vector3.one * 0.8f;
                aiItem.transform.DOScale(targetScale, 0.25f).SetEase(Ease.OutBack).SetUpdate(ignoreTimeScale);
                AssignAIWordForItem(aiItem, visibleStartIndexAI + i);
                if (i == 0 || i == 1)
                {
                    aiItem.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
                }
            }
        }

        ApplyActiveMask();
    }

    public void RepeatWordItem()
    {
        GameObject wordItem = Instantiate(wordItemPrefab, wordItemParent);
        wordItem.transform.localPosition = positionOfWords[6];
        Vector3 targetScale = GetTargetScaleForSlotIndex(6);
        wordItem.transform.localScale = Vector3.one * 0.8f;
        wordItem.transform.DOScale(targetScale, 0.25f).SetEase(Ease.OutBack).SetUpdate(ignoreTimeScale);

        // Assign word based on current visibleStartIndex and this child's index
        int childIndex = wordItemParent.childCount - 1; // new child is last
        // Always set next word from model: start + index
        AssignWordForItem(wordItem, visibleStartIndexPlayer + childIndex);

        ApplyActiveMask();
    }

    public void RepeatWordItemAI()
    {
        if (wordItemParentAI == null) return;
        GameObject aiItem = Instantiate(wordItemPrefab, wordItemParentAI);
        aiItem.transform.localPosition = positionOfWords[6];
        Vector3 targetScale = GetTargetScaleForSlotIndex(6);
        aiItem.transform.localScale = Vector3.one * 0.8f;
        aiItem.transform.DOScale(targetScale, 0.25f).SetEase(Ease.OutBack).SetUpdate(ignoreTimeScale);
        int childIndex = wordItemParentAI.childCount - 1;
        // Always set next word from model: start + index
        AssignAIWordForItem(aiItem, visibleStartIndexAI + childIndex);
    }

    public void UpdateItemPosition()
    {
        // Player shift only
        gameHighLightImage.sprite = wordHighLightSprite;
        StartCoroutine(UpdateLaneRoutine(wordItemParent, true));
    }

    public void UpdateItemPositionAI()
    {
        // AI shift only
        gameHighLightImageAI.sprite = wordHighLightSprite;
        StartCoroutine(UpdateLaneRoutine(wordItemParentAI, false));
    }

    private IEnumerator UpdateLaneRoutine(Transform laneParent, bool isPlayer)
    {
        if (laneParent == null) yield break;

        if (laneParent.childCount > 0)
        {
            Destroy(laneParent.GetChild(0).gameObject);
            yield return null;
        }

        int moveCount = Mathf.Min(laneParent.childCount, positionOfWords.Count);
        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(ignoreTimeScale);
        for (int i = 0; i < moveCount; i++)
        {
            Transform child = laneParent.GetChild(i);
            Vector3 targetPos = positionOfWords[i];
            Vector3 targetScale = GetTargetScaleForSlotIndex(i);

            seq.Join(child.DOLocalMove(targetPos, 0.2f).SetEase(Ease.OutQuad).SetUpdate(ignoreTimeScale));
            seq.Join(child.DOScale(targetScale, 0.2f).SetEase(Ease.OutQuad).SetUpdate(ignoreTimeScale));
        }

        yield return seq.WaitForCompletion();

        if (isPlayer)
        {
            visibleStartIndexPlayer++;
            RepeatWordItem();
        }
        else
        {
            visibleStartIndexAI++;
            RepeatWordItemAI();
        }

        ApplyActiveMask();
    }

    private Vector3 GetTargetScaleForSlotIndex(int i)
    {
        if (i == 2) return new Vector3(2f, 2f, 2f);
        if (i == 1 || i == 3) return new Vector3(1.3f, 1.3f, 1.3f);
        return Vector3.one * 0.8f;
    }

    // Ensures items 0..4 are active, the remaining (e.g., 5 and 6) are inactive every time
    private void ApplyActiveMask()
    {
        for (int i = 0; i < wordItemParent.childCount; i++)
        {
            bool shouldBeActive = i < 5; // 0..4 true (1..5 as per user), others false
            var child = wordItemParent.GetChild(i).gameObject;
            if (child.activeSelf != shouldBeActive)
            {
                child.SetActive(shouldBeActive);
            }
        }

        if (wordItemParentAI != null)
        {
            for (int i = 0; i < wordItemParentAI.childCount; i++)
            {
                bool shouldBeActive = i < 5;
                var child = wordItemParentAI.GetChild(i).gameObject;
                if (child.activeSelf != shouldBeActive)
                {
                    child.SetActive(shouldBeActive);
                }
            }
        }
    }

    // Typing logic
    private void OnTyping(string value)
    {
        if (wordItemParent == null || wordItemParent.childCount < 3) return;
        // Block typing during lock window
        if (Time.unscaledTime < typingLockUntil) return;
        Transform center = wordItemParent.GetChild(2);
        var tmp = center.GetChild(0).GetComponent<TextMeshProUGUI>();
        if (tmp == null) return;

        string original = StripRichTags(tmp.text);
        // If first two were emptied, ensure we fetch from list mapping
        if (string.IsNullOrEmpty(original))
        {
            int index = visibleStartIndexPlayer + 2;
            if (index >= 0 && index < allWords.Count)
            {
                original = allWords[index];
            }
        }

        string typed = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(typed))
        {
            return;
        }

        if (currentWordCompleted) return;

        string typedCapped = typed.Length > original.Length ? typed.Substring(0, original.Length) : typed;

        for (int i = 0; i < typedCapped.Length; i++)
        {
            if (char.ToLowerInvariant(typedCapped[i]) != char.ToLowerInvariant(original[i]))
            {
                tmp.text = $"<color=red>{original}</color>";
                currentWordFailed = true;
                AddScore(-1);
                center.GetChild(1).gameObject.SetActive(true);
                center.GetChild(1).GetComponent<Image>().sprite = wrongSprite;
                gameHighLightImage.sprite = wrongHighlightSprite;

                if (currentGameMode == GAMEMODE.MultiPlayer)
                {
                    FusionBridge.Instance.RpcWordCompleted(localPlayerId, original, false);
                }

                // Lock typing for 0.5s until commit
                typingLockUntil = Time.unscaledTime + 0.5f;
                if (inputField != null) inputField.readOnly = true;
                AutoCommit();
                return;
            }
        }

        tmp.text = BuildPartialGreen(original, typedCapped);

        if (typedCapped.Length == original.Length)
        {
            tmp.text = $"<color=green>{original}</color>";
            AddScore(+1);
            center.GetChild(1).gameObject.SetActive(true);
            center.GetChild(1).GetComponent<Image>().sprite = rightSprite;
            gameHighLightImage.sprite = correctHighlightSprite;

            if (currentGameMode == GAMEMODE.MultiPlayer)
            {
                FusionBridge.Instance.RpcWordCompleted(localPlayerId, original, true);
            }

            currentWordCompleted = true;
            // Lock typing for 0.5s until commit
            typingLockUntil = Time.unscaledTime + 0.5f;
            if (inputField != null) inputField.readOnly = true;
            AutoCommit();
        }
    }

    public void OpponentWord(bool IsCorrect)
    {
        Transform center = wordItemParentAI.GetChild(2);
        var tmp = center.GetChild(0).GetComponent<TextMeshProUGUI>();

        // Get original (strip formatting)
        int aiIndex = visibleStartIndexAI + 2;
        string original = (aiIndex >= 0 && aiIndex < allWords.Count) ? allWords[aiIndex] : StripRichTags(tmp.text);

        if (IsCorrect)
        {
            // Mark green and score++
            tmp.text = $"<color=green>{original}</color>";
            aiScore++;
            gameHighLightImageAI.sprite = correctHighlightSprite;
            center.GetChild(1).gameObject.SetActive(true);
            center.GetChild(1).GetComponent<Image>().sprite = rightSprite;
            if (aiScoreText != null) aiScoreText.text = FormatScoreWithSpaces(aiScore);
        }
        else
        {
            // Mark red and score-- (floored at 0)
            tmp.text = $"<color=red>{original}</color>";
            aiScore = Mathf.Max(0, aiScore - 1);
            gameHighLightImageAI.sprite = wrongHighlightSprite;
            center.GetChild(1).gameObject.SetActive(true);
            center.GetChild(1).GetComponent<Image>().sprite = wrongSprite;
            if (aiScoreText != null) aiScoreText.text = FormatScoreWithSpaces(aiScore);
        }

        // Advance one word (mirror the player shift)
        if (!isAIShifting)
        {
            StartCoroutine(AICommitWord());
        }
    }

    private void CommitCurrentWord()
    {
        if (isPlayerShifting) return;
        isPlayerShifting = true;
        if (inputField != null)
        {
            inputField.text = string.Empty;
        }

        // Advance list and reset state
        currentWordFailed = false;
        currentWordCompleted = false;
        UpdateItemPosition();
        // Release the shift lock slightly after animations complete
        StartCoroutine(ReleasePlayerShiftLockAfterFrame());
        // Re-enable typing after commit and restore focus
        if (inputField != null)
        {
            inputField.readOnly = false;
            EnsureInputFocus();
        }
    }

    private void AutoCommit()
    {
        // Delay by one frame to allow the colored state to render, then commit automatically
        StartCoroutine(AutoCommitNextFrame());
    }

    private IEnumerator AutoCommitNextFrame()
    {
        //yield return null; // next frame
        yield return new WaitForSeconds(0.5f);
        CommitCurrentWord();
    }

    private IEnumerator ReleasePlayerShiftLockAfterFrame()
    {
        // two frames ensures sequence has time to complete
        yield return null;
        yield return null;
        isPlayerShifting = false;
    }

    private void GameOver()
    {
        // Stop the game
        timerRunning = false;

        // Hide gameplay screen and show game over screen
        if (gamePlayScreen != null)
            //gamePlayScreen.SetActive(false);
            if (gameOverScreen != null)
                gameOverScreen.SetActive(true);

        // Stop AI loop
        StopAllCoroutines();

        // Clear input field
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(OnTyping);
            inputField.text = string.Empty;
        }

        if (currentGameMode == GAMEMODE.SinglePlayer)
        {
            if (totalScore > aiScore)
            {
                resultImage.sprite = winSprite;
                
                // Notify platform of AI mode victory
                if (IFrameBridge.Instance != null)
                {
                    IFrameBridge.Instance.PostMatchResult("won", totalScore, aiScore);
                    player_Score.text = totalScore.ToString();
                    opponent_Score.text = aiScore.ToString();
                }
            }
            else if (totalScore < aiScore)
            {
                resultImage.sprite = loseSprite;
                
                // Notify platform of AI mode loss
                if (IFrameBridge.Instance != null)
                {
                    IFrameBridge.Instance.PostMatchResult("lost", totalScore, aiScore);
                    player_Score.text = totalScore.ToString();
                    opponent_Score.text = aiScore.ToString();
                }
            }
            else
            {
                resultImage.sprite = drawSprite;
                
                // Notify platform of AI mode draw
                if (IFrameBridge.Instance != null)
                {
                    IFrameBridge.Instance.PostMatchResult("draw", totalScore, aiScore);
                    player_Score.text = totalScore.ToString();
                    opponent_Score.text = aiScore.ToString();
                }
            }
        }
        else if (currentGameMode == GAMEMODE.MultiPlayer)
        {
            // Multiplayer game over - compare player vs opponent scores
            if (totalScore > aiScore)
            {
                resultImage.sprite = winSprite;
                
                // Notify platform of victory
                if (IFrameBridge.Instance != null)
                {
                    IFrameBridge.Instance.PostMatchResult("won", totalScore, aiScore);
                    player_Score.text = totalScore.ToString();
                    opponent_Score.text = aiScore.ToString();
                }
            }
            else if (totalScore < aiScore)
            {
                resultImage.sprite = loseSprite;
                
                // Notify platform of loss
                if (IFrameBridge.Instance != null)
                {
                    IFrameBridge.Instance.PostMatchResult("lost", totalScore, aiScore);
                    player_Score.text = totalScore.ToString();
                    opponent_Score.text = aiScore.ToString();
                }
            }
            else
            {
                resultImage.sprite = drawSprite;
                
                // Notify platform of draw
                if (IFrameBridge.Instance != null)
                {
                    IFrameBridge.Instance.PostMatchResult("draw", totalScore, aiScore);
                    player_Score.text = totalScore.ToString();
                    opponent_Score.text = aiScore.ToString();
                }
            }
        }
    }

    public void RestartGame()
    {

        gamePlayScreen.SetActive(false);
        // Reset scores
        totalScore = 0;
        aiScore = 0;
        if (scoreText != null)
            scoreText.text = FormatScoreWithSpaces(0);
        if (aiScoreText != null)
            aiScoreText.text = FormatScoreWithSpaces(0);

        // Reset timer
        timeRemaining = 90f;
        endTime = GetUnixTimeNow() + timeRemaining;
        timerRunning = false;

        // Clear all word items
        ClearAllWordItems();

        // Hide game over screen and show mode selection
        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);
        if (selectModeScreen != null)
            selectModeScreen.SetActive(true);

        // Reset game state
        currentWordFailed = false;
        visibleStartIndexPlayer = 0;
        visibleStartIndexAI = 0;
        isPlayerShifting = false;
        isAIShifting = false;

        //Reset FillImage
        IsCountDownStart = false;
        countFillImage.fillAmount = 0;

        IsGameSliderStart = false;
        gameFillImage.fillAmount = 1;
        gameFillImage_AI.fillAmount = 1;

        countdownText.text = "3";
        countdownText.gameObject.GetComponent<CanvasGroup>().alpha = 0;
        countdownText.gameObject.transform.localScale = Vector3.zero;
    }

    // Method to handle when player wants to leave the game during multiplayer
    public void LeaveGame()
    {
        if (currentGameMode == GAMEMODE.MultiPlayer && timerRunning)
        {
            HandleLocalPlayerLeave();
        }
        else
        {
            RestartGame();
        }
    }

    private void ClearAllWordItems()
    {
        // Clear player word items
        if (wordItemParent != null)
        {
            for (int i = wordItemParent.childCount - 1; i >= 0; i--)
            {
                Destroy(wordItemParent.GetChild(i).gameObject);
            }
        }

        // Clear AI word items
        if (wordItemParentAI != null)
        {
            for (int i = wordItemParentAI.childCount - 1; i >= 0; i--)
            {
                Destroy(wordItemParentAI.GetChild(i).gameObject);
            }
        }
    }

    private void AddScore(int delta)
    {
        totalScore += delta;
        if (totalScore < 0) totalScore = 0;
        if (scoreText != null)
        {
            scoreText.text = FormatScoreWithSpaces(totalScore);
        }
    }

    private static string StripRichTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return Regex.Replace(text, "<.*?>", string.Empty);
    }

    private static string BuildPartialGreen(string original, string typed)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(original.Length * 16);
        for (int i = 0; i < original.Length; i++)
        {
            if (i < typed.Length && char.ToLowerInvariant(original[i]) == char.ToLowerInvariant(typed[i]))
            {
                sb.Append("<color=green>");
                sb.Append(original[i]);
                sb.Append("</color>");
            }
            else
            {
                sb.Append(original[i]);
            }
        }
        return sb.ToString();
    }

    private List<string> LoadWordsFromConfiguredSources()
    {
        var result = new List<string>();

        // Try Resources first if path provided
        if (!string.IsNullOrWhiteSpace(resourcesWordsPath))
        {
            try
            {
                TextAsset ta = Resources.Load<TextAsset>(resourcesWordsPath);
                if (ta != null)
                {
                    ParseWordsInto(result, ta.text);
                }
            }
            catch { /* ignore and fallback */ }
        }

        // If still empty, try StreamingAssets
        if (result.Count == 0 && !string.IsNullOrWhiteSpace(streamingAssetsFileName))
        {
            try
            {
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, streamingAssetsFileName);
#if UNITY_ANDROID && !UNITY_EDITOR
                // On Android, StreamingAssets is in jar. Use UnityWebRequest to read.
                var request = UnityEngine.Networking.UnityWebRequest.Get(path);
                var op = request.SendWebRequest();
                while (!op.isDone) { }
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    ParseWordsInto(result, request.downloadHandler.text);
                }
#else
                if (System.IO.File.Exists(path))
                {
                    string content = System.IO.File.ReadAllText(path);
                    ParseWordsInto(result, content);
                }
#endif
            }
            catch { /* ignore */ }
        }

        return result;
    }

    private static void ParseWordsInto(List<string> target, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        // Add ALL entries as provided (keep duplicates and any characters)
        var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var parts = normalized.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var w = parts[i].Trim();
            if (w.Length == 0) continue;
            target.Add(w);
        }
    }

    // Fisher–Yates shuffle for deterministic ordering
    private static void ShuffleListInPlace<T>(List<T> list, System.Random rng)
    {
        if (list == null || list.Count <= 1) return;
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            if (j != i)
            {
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }

    // Tracks which index in allWords is mapped to child 0
    private int visibleStartIndex = 0;

    // Assigns allWords[wordIndex] to the TextMeshProUGUI under the given word item
    private void AssignWordForItem(GameObject item, int wordIndex)
    {
        var tmp = item.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null) return;

        if (allWords == null || wordIndex < 0 || wordIndex >= allWords.Count)
        {
            tmp.text = "";
        }
        else
        {
            tmp.text = allWords[wordIndex];
        }
    }

    // AI helpers
    private void AssignAIWordForItem(GameObject item, int wordIndex)
    {
        var tmp = item.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null) return;

        if (allWords == null || wordIndex < 0 || wordIndex >= allWords.Count)
        {
            tmp.text = "";
        }
        else
        {
            tmp.text = allWords[wordIndex];
        }
    }

    private IEnumerator AILoop()
    {
        while (true)
        {
            // wait a short random delay to simulate typing speed (slower per mode)
            float delay = (currentAITypes == GAMETYPE.Easy)
                ? UnityEngine.Random.Range(easyDelayRange.x, easyDelayRange.y)
                : UnityEngine.Random.Range(hardDelayRange.x, hardDelayRange.y);
            yield return new WaitForSecondsRealtime(delay);

            // Determine win chance and mistake chance
            float winChance = (currentAITypes == GAMETYPE.Easy) ? 0.70f : 0.95f; // per your spec
            bool isWin = UnityEngine.Random.value <= winChance;

            // Ensure AI has items
            if (wordItemParentAI == null || wordItemParentAI.childCount < 3) continue;
            Transform center = wordItemParentAI.GetChild(2);
            var tmp = center.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (tmp == null) continue;

            // Get original (strip formatting)
            int aiIndex = visibleStartIndexAI + 2;
            string original = (aiIndex >= 0 && aiIndex < allWords.Count) ? allWords[aiIndex] : StripRichTags(tmp.text);

            if (string.IsNullOrEmpty(original)) continue;

            if (isWin)
            {
                // Mark green and score++
                tmp.text = $"<color=green>{original}</color>";
                aiScore++;
                gameHighLightImageAI.sprite = correctHighlightSprite;
                center.GetChild(1).gameObject.SetActive(true);
                center.GetChild(1).GetComponent<Image>().sprite = rightSprite;
                if (aiScoreText != null) aiScoreText.text = FormatScoreWithSpaces(aiScore);
            }
            else
            {
                // Mark red and score-- (floored at 0)
                tmp.text = $"<color=red>{original}</color>";
                aiScore = Mathf.Max(0, aiScore - 1);
                gameHighLightImageAI.sprite = wrongHighlightSprite;
                center.GetChild(1).gameObject.SetActive(true);
                center.GetChild(1).GetComponent<Image>().sprite = wrongSprite;
                if (aiScoreText != null) aiScoreText.text = FormatScoreWithSpaces(aiScore);
            }

            // Advance one word (mirror the player shift)
            if (!isAIShifting)
            {
                StartCoroutine(AICommitWord());
            }
        }
    }

    private IEnumerator AICommitWord()
    {
        if (isAIShifting) yield break;
        isAIShifting = true;
        // shift AI lane only
        yield return new WaitForSeconds(0.5f);
        UpdateItemPositionAI();
        // small wait similar to player
        //yield return null;
        //yield return null;
        isAIShifting = false;
    }

    #region GAMEPLAY_SLIDER

    public Image gameFillImage;
    public Image gameFillImage_AI;
    private bool IsGameSliderStart = false;

    public void RunGameFillImage()
    {
        if (IsGameSliderStart)
        {
            if (gameFillImage.fillAmount > 0)
            {
                float speed = 1f / 300f;
                gameFillImage.fillAmount -= Time.deltaTime * speed;
                gameFillImage_AI.fillAmount -= Time.deltaTime * speed;
            }
            else
            {
                IsGameSliderStart = false;
                gameFillImage.fillAmount = 1;
                gameFillImage_AI.fillAmount = 1;
            }
        }
    }

    #endregion


    #region COUNTDOWN

    [Header("Countdown")]
    public TextMeshProUGUI countdownText;
    public Image countFillImage;
    public bool IsCountDownStart = false;

    public IEnumerator StartCountdown_Animation()
    {
        countdownText.text = "3";
        countdownText.gameObject.GetComponent<CanvasGroup>().alpha = 0;
        countdownText.gameObject.transform.localScale = Vector3.zero;

        yield return new WaitForSeconds(0.5f);
        countdownText.text = "3";
        countdownText.gameObject.GetComponent<CanvasGroup>().DOFade(1, 1f);
        countdownText.gameObject.transform.DOScale(Vector3.one, 1f).OnComplete(() =>
        {
            countdownText.gameObject.GetComponent<CanvasGroup>().alpha = 0;
            countdownText.gameObject.transform.localScale = Vector3.zero;
            countdownText.text = "2";

            countdownText.gameObject.GetComponent<CanvasGroup>().DOFade(1, 1f);
            countdownText.gameObject.transform.DOScale(Vector3.one, 1f).OnComplete(() =>
            {
                countdownText.gameObject.GetComponent<CanvasGroup>().alpha = 0;
                countdownText.gameObject.transform.localScale = Vector3.zero;
                countdownText.text = "1";

                countdownText.gameObject.GetComponent<CanvasGroup>().DOFade(1, 1f);
                countdownText.gameObject.transform.DOScale(Vector3.one, 1f).OnComplete(() =>
                {
                    countdownText.gameObject.GetComponent<CanvasGroup>().alpha = 0;
                    countdownText.gameObject.transform.localScale = Vector3.zero;
                    countdownText.text = "GO!";

                    countdownText.gameObject.GetComponent<CanvasGroup>().DOFade(1, 1f);
                    countdownText.gameObject.transform.DOScale(Vector3.one, 1f);
                });
            });

        });
    }

    public void RunFillImage()
    {
        if (IsCountDownStart)
        {
            if (countFillImage.fillAmount < 1f)
            {
                countFillImage.fillAmount += Time.deltaTime * 0.2f;
            }
            else
            {
                IsCountDownStart = false;
                countFillImage.fillAmount = 0;

                countdownScreen.SetActive(false);
                gamePlayScreen.SetActive(true);
                StartGame();
                IsGameSliderStart = true;
            }
        }
    }
    #endregion

    #region MultiPlayer

    public void MultiplayerGameStart()
    {
        IsCountDownStart = true;
        StartCoroutine(StartCountdown_Animation());
    }

    public void HandleOpponentDisconnect(int disconnectedPlayerId)
    {
        Debug.Log($"Handling opponent disconnect: Player {disconnectedPlayerId} left the game");
        
        // Stop the timer and game immediately
        timerRunning = false;
        StopAllCoroutines();
        
        // Clear input field
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(OnTyping);
            inputField.text = string.Empty;
        }
        
        // Show game over screen with win result
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);
            
        // Display win sprite since opponent left
        if (resultImage != null)
        {
            resultImage.sprite = winSprite;
            player_Score.text = totalScore.ToString();
            opponent_Score.text = aiScore.ToString();
        }
        
        Debug.Log("Player wins due to opponent disconnection - displaying win screen");
    }

    public void HandleLocalPlayerLeave()
    {
        Debug.Log("Local player is leaving the game");
        
        // Stop the timer and game immediately
        timerRunning = false;
        StopAllCoroutines();
        
        // Clear input field
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(OnTyping);
            inputField.text = string.Empty;
        }
        
        // Show game over screen with lose result (since we're leaving)
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);
            
        // Display lose sprite since we left
        if (resultImage != null)
        {
            resultImage.sprite = loseSprite;
            player_Score.text = totalScore.ToString();
            opponent_Score.text = aiScore.ToString();
        }
        
        // Notify platform that local player forfeited
        if (IFrameBridge.Instance != null)
        {
            IFrameBridge.Instance.PostPlayerForfeit();
        }
        
        Debug.Log("Local player left - displaying lose screen");
    }

    #endregion

	private void EnsureInputFocus()
	{
		if (inputField == null) return;
		if (EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(inputField.gameObject);
			inputField.ActivateInputField();
		}
	}

	private void HideInputFieldVisuals()
	{
		if (inputField == null) return;
		// Hide text caret, placeholder, and background by disabling the Graphic components
		var cg = inputField.GetComponent<CanvasGroup>();
		if (cg == null) cg = inputField.gameObject.AddComponent<CanvasGroup>();
		cg.alpha = 0f;
		cg.blocksRaycasts = false; // clicks pass through
		cg.interactable = true;    // still receives keyboard input when focused
	}

	// ===== On-screen keyboard input API =====
	public void KeyboardAppend(string key)
	{
		if (inputField == null) return;
		if (Time.unscaledTime < typingLockUntil) return;
		if (string.IsNullOrEmpty(key)) return;
		inputField.text = (inputField.text ?? string.Empty) + key;
		inputField.caretPosition = inputField.text.Length;
		EnsureInputFocus();
	}

	public void KeyboardBackspace()
	{
		if (inputField == null) return;
		if (Time.unscaledTime < typingLockUntil) return;
		string t = inputField.text ?? string.Empty;
		if (t.Length > 0)
		{
			inputField.text = t.Substring(0, t.Length - 1);
			inputField.caretPosition = inputField.text.Length;
		}
		EnsureInputFocus();
	}

	public void KeyboardSpace()
	{
		if (inputField == null) return;
		if (Time.unscaledTime < typingLockUntil) return;
		// If the word is already judged, commit; else ignore space to avoid accidental wrong
		if (currentWordCompleted || currentWordFailed)
		{
			CommitCurrentWord();
		}
	}

	public void KeyboardEnter()
	{
		KeyboardSpace();
	}

	private string FormatScoreWithSpaces(int value)
	{
		int clamped = value < 0 ? 0 : (value > 9999 ? 9999 : value);
		string s = clamped.ToString("D4");
		// 0<space=0.6em>0<space=0.9em>0<space=0.6em>0
		return string.Concat(
			s[0], "<space=0.6em>", s[1], "<space=0.9em>", s[2], "<space=0.6em>", s[3]
		);
	}

}

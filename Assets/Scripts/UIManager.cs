using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using DG.Tweening;

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

    [Header("Game State")]
    public GAMEMODE currentGameMode;
    public GAMETYPE currentAITypes;

    [Header("GamePlay_Area")]
    public TextMeshProUGUI timerText;
    private float timeRemaining = 300f;
    private bool timerRunning = false;

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

    public Sprite winSprite;
    public Sprite loseSprite;
    public Sprite drawSprite;

    void Start()
    {

    }

    void Update()
    {
        if (timerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                timerRunning = false;
                Debug.Log("⏰ Timer Finish!");
                GameOver();
            }
        }

        RunFillImage();
        RunGameFillImage();
    }

    public void StartGame()
    {
        timeRemaining = 300f;
        timerRunning = true;

        CreateWordItem();
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnTyping);
        }

        // Start AI loop when gameplay starts (optional to move into StartGame)
        StartCoroutine(AILoop());
        resultImage.transform.localScale = Vector3.zero;
    }

    void UpdateTimerDisplay(float timeToDisplay)
    {
        if (timeToDisplay < 0)
            timeToDisplay = 0;

        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void OnClickEasy()
    {
        currentAITypes = GAMETYPE.Easy;
        selectTypeScreen.SetActive(false);

        IsCountDownStart = true;
        countdownScreen.SetActive(true);
        StartCoroutine(StartCountdown_Animation());
    }

    public void OnClickHard()
    {
        currentAITypes = GAMETYPE.Hard;
        selectTypeScreen.SetActive(false);

        IsCountDownStart = true;
        countdownScreen.SetActive(true);
        StartCoroutine(StartCountdown_Animation());
    }

    public void OnClickSinglePlayer()
    {
        currentGameMode = GAMEMODE.SinglePlayer;
        selectModeScreen.SetActive(false);
        selectTypeScreen.SetActive(true);
    }

    public void OnClickMultiPlayer()
    {
        currentGameMode = GAMEMODE.MultiPlayer;
        selectModeScreen.SetActive(false);
        selectTypeScreen.SetActive(true);
    }

    private void PopulateAllWordsRandom(int targetCount)
    {
        for (int i = 0; i < targetCount; i++)
        {
            int length;
            if (currentAITypes == GAMETYPE.Easy)
            {
                // 3..5 letters (max is exclusive)
                Debug.Log(currentAITypes + "currentAITypes_1");
                length = Random.Range(3, 4);
            }
            else
            {
                // 5..8 letters (max is exclusive)
                Debug.Log(currentAITypes + "currentAITypes_2");
                length = Random.Range(4, 5);
            }
            allWords.Add(GenerateRandomWord(length));
        }
    }

    private string GenerateRandomWord(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < length; i++)
        {
            sb.Append(chars[Random.Range(0, chars.Length)]);
        }
        return sb.ToString();
    }

    public void CreateWordItem()
    {
         allWords.Clear();
        // Ensure we have words
        if (allWords == null || allWords.Count == 0)
        {
            PopulateAllWordsRandom(2000);
        }
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
            // Do not reset formatting when input becomes empty
            return;
        }

        if (currentWordFailed)
        {
            // Already failed; keep red until space
            tmp.text = $"<color=red>{original}</color>";
            return;
        }

        // Compare letter-by-letter
        for (int i = 0; i < typed.Length; i++)
        {
            if (i >= original.Length || char.ToLowerInvariant(typed[i]) != char.ToLowerInvariant(original[i]))
            {
                // mark full red and auto-commit this word, decrement score
                tmp.text = $"<color=red>{original}</color>";
                currentWordFailed = true;
                AddScore(-1);
                center.GetChild(1).gameObject.SetActive(true);
                center.GetChild(1).GetComponent<Image>().sprite = wrongSprite;
                gameHighLightImage.sprite = wrongHighlightSprite;
                AutoCommit();
                return;
            }
        }

        // Partial correct => show green letters
        tmp.text = BuildPartialGreen(original, typed);

        // If fully typed correctly, color full green (still wait for space to commit)
        if (typed.Length == original.Length)
        {
            // mark full green and auto-commit, increment score
            tmp.text = $"<color=green>{original}</color>";
            AddScore(+1);
            center.GetChild(1).gameObject.SetActive(true);
            center.GetChild(1).GetComponent<Image>().sprite = rightSprite;
            gameHighLightImage.sprite = correctHighlightSprite;
            AutoCommit();
        }
    }

    private void RefreshLaneTexts(Transform laneParent, int startIndex)
    {
        if (laneParent == null) return;
        int count = laneParent.childCount;
        for (int i = 0; i < count; i++)
        {
            var tmp = laneParent.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>();
            if (tmp == null) continue;
            if (i == 0 || i == 1)
            {
                tmp.text = "";
            }
            else
            {
                int wordIdx = startIndex + i;
                if (allWords != null && wordIdx >= 0 && wordIdx < allWords.Count)
                {
                    tmp.text = allWords[wordIdx];
                }
                else
                {
                    tmp.text = "";
                }
            }
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
        UpdateItemPosition();
        // Release the shift lock slightly after animations complete
        StartCoroutine(ReleasePlayerShiftLockAfterFrame());
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
        
        if(currentGameMode == GAMEMODE.SinglePlayer)
        {
            if(totalScore > aiScore)
            {
                resultImage.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.Linear);
                resultImage.sprite = winSprite;
            }
            else if(totalScore < aiScore)
            {
                resultImage.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.Linear);
                resultImage.sprite = loseSprite;
            }
            else
            {
                resultImage.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.Linear);
                resultImage.sprite = drawSprite;
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
            scoreText.text = "0";
        if (aiScoreText != null)
            aiScoreText.text = "0";

        // Reset timer
        timeRemaining = 300f;
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

        resultImage.transform.localScale = Vector3.zero;
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
            scoreText.text = totalScore.ToString();
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
                ? Random.Range(easyDelayRange.x, easyDelayRange.y)
                : Random.Range(hardDelayRange.x, hardDelayRange.y);
            yield return new WaitForSecondsRealtime(delay);

            // Determine win chance and mistake chance
            float winChance = (currentAITypes == GAMETYPE.Easy) ? 0.70f : 0.95f; // per your spec
            bool isWin = Random.value <= winChance;

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
                if (aiScoreText != null) aiScoreText.text = aiScore.ToString();
            }
            else
            {
                // Mark red and score-- (floored at 0)
                tmp.text = $"<color=red>{original}</color>";
                aiScore = Mathf.Max(0, aiScore - 1);
                gameHighLightImageAI.sprite = wrongHighlightSprite;
                center.GetChild(1).gameObject.SetActive(true);
                center.GetChild(1).GetComponent<Image>().sprite = wrongSprite;
                if (aiScoreText != null) aiScoreText.text = aiScore.ToString();
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
    private bool IsCountDownStart = false;

    public IEnumerator StartCountdown_Animation()
    {
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

}

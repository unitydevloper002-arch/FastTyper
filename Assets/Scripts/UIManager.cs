using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Game State")]
    public GAMEMODE currentGameMode;
    public GAMETYPE currentAITypes;

    [Header("GamePlay_Screen")]
    public TextMeshProUGUI timerText;
    private float timeRemaining = 300f;
    private bool timerRunning = false;

    public GameObject itemPrefab;

    public Transform itemParent;
    public Transform itemParent_AI;

    public List<string> allWords = new List<string>();

    public TextMeshProUGUI scoreText;
    public int totalScore = 0;

    public TextMeshProUGUI scoreText_AI;
    public int totalScore_AI = 0;

    private void Start()
    {
        // StartGame();
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
            }
        }

        ManageScrollView();
        ManageScrollView_AI();

        // 🔹 Auto-generate more words if running out
        if (currentIndex >= allWords.Count - 5 || currentIndex_AI >= allWords.Count - 5)
        {
            GenerateWordsForGame(true);
        }
    }

    public void RestartGame(bool IsBack)
    {
        // 1️⃣ Stop timer and AI coroutine
        timerRunning = false;
        StopAllCoroutines();

        // 2️⃣ Clear old items
        foreach (Transform child in itemParent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in itemParent_AI)
        {
            Destroy(child.gameObject);
        }

        // 3️⃣ Reset indexes
        currentIndex = 2;
        currentIndex_AI = 2;

        // 4️⃣ Reset scores
        totalScore = 0;
        totalScore_AI = 0;
        UpdateScoreUI();
        UpdateScoreUI_AI();

        // 5️⃣ Generate fresh words
        allWords.Clear();

        // 6️⃣ Start fresh game
        if (!IsBack)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        timeRemaining = 300f;
        timerRunning = true;

        CreateItem();
        CreateItem_AI();

        UpdateScoreUI();
        UpdateScoreUI_AI();
        SetUpScrollView();

        GenerateWordsForGame(false);
        if (allWords.Count > 0)
        {
            allWords[0] = "";
            allWords[1] = "";
        }

        // Start AI logic if SinglePlayer
        if (currentGameMode == GAMEMODE.SinglePlayer)
        {
            StartCoroutine(AI_PlayLoop());
        }
    }

    public void SetUpScrollView()
    {
        itemHeight = viewport.rect.height / 5f;
        for (int i = 0; i < content.childCount; i++)
        {
            var rt = content.GetChild(i).GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, itemHeight);
        }

        inputField.onValueChanged.AddListener(OnTyping);
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
        gamePlayScreen.SetActive(true);

        StartGame();
    }

    public void OnClickHard()
    {
        currentAITypes = GAMETYPE.Hard;
        selectTypeScreen.SetActive(false);
        gamePlayScreen.SetActive(true);

        StartGame();
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

    #region GamePlay

    [Header("Scroll Settings")]
    public ScrollRect scrollRect;
    public RectTransform viewport;
    public RectTransform content;
    public ScrollRect scrollRect_AI;
    public RectTransform viewport_AI;
    public RectTransform content_AI;
    public float scrollTime = 0.3f;
    public float itemHeight = 132f;
    private int currentIndex = 2;
    private int currentIndex_AI = 2;

    [Header("Scale Settings")]
    public float maxScale = 1.2f;
    public float minScale = 0.8f;
    public float distanceForMin = 200f;

    [Header("Input")]
    public TMP_InputField inputField;
    public TMP_InputField inputField_AI;

    private bool isScrolling = false;
    private bool isScrolling_AI = false;

    public void CreateItem()
    {
        for (int i = 0; i < allWords.Count; i++)
        {
            GameObject G = Instantiate(itemPrefab, itemParent);
            G.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = allWords[i];
        }
    }

    public void CreateItem_AI()
    {
        for (int i = 0; i < allWords.Count; i++)
        {
            GameObject G = Instantiate(itemPrefab, itemParent_AI);
            G.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = allWords[i];
        }
    }

    void ManageScrollView()
    {
        Vector3 viewportCenterLocal = viewport.rect.center;
        foreach (RectTransform item in content)
        {
            Vector3 itemLocalPos = viewport.InverseTransformPoint(item.position);
            float distance = Mathf.Abs(itemLocalPos.y - viewportCenterLocal.y);
            float t = Mathf.Clamp01(distance / distanceForMin);
            float scale = Mathf.Lerp(maxScale, minScale, t);
            item.localScale = new Vector3(scale, scale, 1f);
        }
    }

    void ManageScrollView_AI()
    {
        Vector3 viewportCenterLocal = viewport_AI.rect.center;
        foreach (RectTransform item in content_AI)
        {
            Vector3 itemLocalPos = viewport_AI.InverseTransformPoint(item.position);
            float distance = Mathf.Abs(itemLocalPos.y - viewportCenterLocal.y);
            float t = Mathf.Clamp01(distance / distanceForMin);
            float scale = Mathf.Lerp(maxScale, minScale, t);
            item.localScale = new Vector3(scale, scale, 1f);
        }
    }

    void OnTyping(string value)
    {
        if (isScrolling) return;
        if (string.IsNullOrWhiteSpace(value)) return;

        string original = GetOriginalWord(content, currentIndex);
        string typed = value.Trim();

        for (int i = 0; i < typed.Length; i++)
        {
            if (i >= original.Length || char.ToLower(typed[i]) != char.ToLower(original[i]))
            {
                content.GetChild(currentIndex).GetChild(0).GetComponent<TextMeshProUGUI>().text =
                    $"<color=red>{original}</color>";
                totalScore = Mathf.Max(0, totalScore - 1);
                UpdateScoreUI();
                inputField.text = "";
                currentIndex++;
                StartCoroutine(SmoothScrollToIndex(content, false, currentIndex));
                return;
            }
        }

        ShowPartialGreen(content, currentIndex, original, typed);

        if (typed.Length == original.Length)
        {
            content.GetChild(currentIndex).GetChild(0).GetComponent<TextMeshProUGUI>().text =
                $"<color=green>{original}</color>";
            totalScore += 1;
            UpdateScoreUI();
            inputField.text = "";
            currentIndex++;
            StartCoroutine(SmoothScrollToIndex(content, false, currentIndex));
        }
    }

    // AI loop for auto-playing
    //IEnumerator AI_PlayLoop()
    //{
    //    float winChance = currentAITypes == GAMETYPE.Easy ? 0.4f : 0.9f;

    //    while (timerRunning && currentIndex_AI < allWords.Count)
    //    {
    //        yield return new WaitForSeconds(Random.Range(2.5f, 3.5f)); // typing delay

    //        bool aiWins = Random.value <= winChance;
    //        string original = GetOriginalWord(content_AI, currentIndex_AI);

    //        if (aiWins)
    //        {
    //            content_AI.GetChild(currentIndex_AI).GetChild(0).GetComponent<TextMeshProUGUI>().text =
    //                $"<color=green>{original}</color>";
    //            totalScore_AI++;
    //            UpdateScoreUI_AI();
    //        }
    //        else
    //        {
    //            content_AI.GetChild(currentIndex_AI).GetChild(0).GetComponent<TextMeshProUGUI>().text =
    //                $"<color=red>{original}</color>";
    //            totalScore_AI = Mathf.Max(0, totalScore_AI - 1);
    //            UpdateScoreUI_AI();
    //        }

    //        currentIndex_AI++;
    //        StartCoroutine(SmoothScrollToIndex(content_AI, true, currentIndex_AI));
    //    }
    //}

    IEnumerator AI_PlayLoop()
    {
        float minDelay, maxDelay;
        float winChance;

        if (currentAITypes == GAMETYPE.Easy)
        {
            winChance = 0.4f; // ~4 out of 10 correct
            minDelay = 2.5f;
            maxDelay = 4.0f;
        }
        else // Hard
        {
            winChance = 0.9f; // ~9 out of 10 correct
            minDelay = 1.2f;
            maxDelay = 2.2f;
        }

        // Prepare planned outcomes for every word in the list
        List<bool> aiDecisions = new List<bool>();
        for (int i = 0; i < allWords.Count; i++)
        {
            aiDecisions.Add(Random.value <= winChance);
        }

        // AI plays word-by-word
        while (timerRunning && currentIndex_AI < allWords.Count)
        {
            // Ensure AI has a decision ready for this word
            while (aiDecisions.Count <= currentIndex_AI)
            {
                aiDecisions.Add(Random.value <= winChance);
            }

            bool aiWins = aiDecisions[currentIndex_AI];
            string original = GetOriginalWord(content_AI, currentIndex_AI);

            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

            if (aiWins)
            {
                content_AI.GetChild(currentIndex_AI).GetChild(0).GetComponent<TextMeshProUGUI>().text =
                    $"<color=green>{original}</color>";
                totalScore_AI++;
            }
            else
            {
                content_AI.GetChild(currentIndex_AI).GetChild(0).GetComponent<TextMeshProUGUI>().text =
                    $"<color=red>{original}</color>";
                totalScore_AI = Mathf.Max(0, totalScore_AI - 1);
            }

            UpdateScoreUI_AI();
            currentIndex_AI++;
            StartCoroutine(SmoothScrollToIndex(content_AI, true, currentIndex_AI));
        }
    }


    void ShowPartialGreen(RectTransform list, int index, string original, string typed)
    {
        string result = "";
        for (int i = 0; i < original.Length; i++)
        {
            if (i < typed.Length && char.ToLower(original[i]) == char.ToLower(typed[i]))
            {
                result += $"<color=green>{original[i]}</color>";
            }
            else
            {
                result += original[i];
            }
        }
        list.GetChild(index).GetChild(0).GetComponent<TextMeshProUGUI>().text = result;
    }

    string GetOriginalWord(RectTransform list, int index)
    {
        string raw = list.GetChild(index).GetChild(0).GetComponent<TextMeshProUGUI>().text;
        return System.Text.RegularExpressions.Regex.Replace(raw, "<.*?>", string.Empty);
    }

    IEnumerator SmoothScrollToIndex(RectTransform list, bool isAI, int index)
    {
        if (isAI) isScrolling_AI = true;
        else isScrolling = true;

        float startY = list.anchoredPosition.y;
        float targetY = (index - 2) * itemHeight;

        float elapsed = 0;
        while (elapsed < scrollTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scrollTime);
            list.anchoredPosition = new Vector2(
                list.anchoredPosition.x,
                Mathf.Lerp(startY, targetY, t)
            );
            yield return null;
        }

        if (isAI) isScrolling_AI = false;
        else isScrolling = false;
    }


    void UpdateScoreUI()
    {
        scoreText.text = totalScore.ToString();
    }

    void UpdateScoreUI_AI()
    {
        scoreText_AI.text = totalScore_AI.ToString();
    }

    private void GenerateWordsForGame(bool append = false)
    {
        int wordCount = 20; // smaller chunk so easy to add later
        List<string> newWords = new List<string>();

        if (currentAITypes == GAMETYPE.Easy)
        {
            for (int i = 0; i < wordCount; i++)
            {
                newWords.Add(GenerateRandomWord(Random.Range(3, 6))); // 3 to 5 letters
            }
        }
        else if (currentAITypes == GAMETYPE.Hard)
        {
            for (int i = 0; i < wordCount; i++)
            {
                newWords.Add(GenerateRandomWord(Random.Range(7, 11))); // 7 to 10 letters
            }
        }

        if (append)
        {
            // Append to both player's & AI's list
            foreach (string w in newWords)
            {
                // Player
                GameObject G = Instantiate(itemPrefab, itemParent);
                G.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = w;

                // AI
                GameObject G_AI = Instantiate(itemPrefab, itemParent_AI);
                G_AI.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = w;
            }
            allWords.AddRange(newWords);
        }
        else
        {
            allWords = newWords;
        }
    }

    // Utility function to create a random word
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

    #endregion
}

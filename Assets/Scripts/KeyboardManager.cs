using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KeyboardManager : MonoBehaviour
{
	public static KeyboardManager Instance { get; private set; }

	[Header("Behavior")]
	public bool showOnMobile = true;
	public bool showOnDesktop = true;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		// Delay one frame to ensure scene objects are initialized
		StartCoroutine(ShowKeyboardFlow());
	}

	private IEnumerator ShowKeyboardFlow()
	{
		yield return null;
		bool shown = TryShowKeyboard();
		if (!shown)
		{
			// Retry once more next frame in case UI created late
			yield return null;
			TryShowKeyboard();
		}
	}

	private static bool IsMobile()
	{
		// Prefer IFrameBridge when available (WebGL mobile detection), else fallback
		if (IFrameBridge.Instance != null)
		{
			return IFrameBridge.Instance.IsMobileWebGL();
		}
		return Application.isMobilePlatform;
	}

	private GameObject FindKeyboardObject()
	{
		// 1) Active object by tag/name
		GameObject kb = GameObject.FindGameObjectWithTag("OnScreenKeyboard");
		if (kb != null) return kb;
		kb = GameObject.Find("OnScreenKeyboard");
		if (kb != null) return kb;
		kb = GameObject.Find("Keyboard");
		if (kb != null) return kb;

		// 2) Inactive search in active scene hierarchy
		var scene = SceneManager.GetActiveScene();
		var roots = scene.GetRootGameObjects();
		for (int r = 0; r < roots.Length; r++)
		{
			var transforms = roots[r].GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < transforms.Length; i++)
			{
				Transform t = transforms[i];
				if (t == null) continue;
				if (t.CompareTag("OnScreenKeyboard")) return t.gameObject;
				string n = t.gameObject.name;
				if (n == "OnScreenKeyboard" || n == "Keyboard") return t.gameObject;
			}
		}
		return null;
	}

	public bool TryShowKeyboard()
	{
		bool mobile = IsMobile();
		bool shouldShow = (mobile && showOnMobile) || (!mobile && showOnDesktop);
		GameObject kb = FindKeyboardObject();

		if (!shouldShow)
		{
			if (kb != null && kb.activeSelf)
			{
				kb.SetActive(false);
				Debug.Log("[KeyboardManager] Keyboard hidden (per settings)");
			}
			return false;
		}

		if (kb != null)
		{
			if (!kb.activeSelf)
			{
				kb.SetActive(true);
				Debug.Log("[KeyboardManager] Keyboard shown (found by tag/name)");
			}
			return true;
		}

		Debug.Log("[KeyboardManager] Keyboard GameObject not found. Ensure tag 'OnScreenKeyboard' or name 'OnScreenKeyboard/Keyboard' is set.");
		return false;
	}

	// Public method to force show keyboard (called from UIManager when gameplay starts)
	public void ForceShowKeyboard()
	{
		GameObject kb = FindKeyboardObject();
		if (kb != null)
		{
			kb.SetActive(true);
			Debug.Log("[KeyboardManager] Keyboard force-shown for gameplay");
		}
		else
		{
			Debug.LogWarning("[KeyboardManager] Cannot force-show: keyboard not found");
		}
	}
}



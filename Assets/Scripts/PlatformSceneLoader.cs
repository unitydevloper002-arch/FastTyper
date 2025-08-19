using UnityEngine;
using UnityEngine.SceneManagement;

public class PlatformSceneLoader : MonoBehaviour
{
    void Awake()
    {
        // Platform check
#if UNITY_STANDALONE || UNITY_EDITOR
        // PC / Editor
        SceneManager.LoadScene(1); // PC mate Scene1 load karo
#elif UNITY_ANDROID || UNITY_IOS
            // Mobile
            SceneManager.LoadScene(2); // Mobile mate Scene2 load karo

            // Screen orientation lock
            Screen.orientation = ScreenOrientation.Portrait; // Portrait ma lock
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
#endif
    }
}

using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    public AudioSource BG_audioSource;
    public AudioSource GamePlay_audioSource;
    public AudioSource Keyboard_audioSource;

    public AudioClip wrongWord;
    public AudioClip rightWord;
    public AudioClip pressKey;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        StartBGSound();
    }

    public void StartBGSound()
    {
        BG_audioSource.Play();
    }

    public void WrongWord()
    {
        GamePlay_audioSource.clip = wrongWord;
        GamePlay_audioSource.Play();
    }

    public void RightWord()
    {
        GamePlay_audioSource.clip = rightWord;
        GamePlay_audioSource.Play();
    }

    public void KeyBoardPressKey()
    {
        Keyboard_audioSource.clip = pressKey;
        Keyboard_audioSource.Play();
    }
}

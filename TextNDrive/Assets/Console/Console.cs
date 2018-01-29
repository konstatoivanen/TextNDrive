using UnityEngine;
using UnityEngine.UI;

public class Console : MonoBehaviour
{
    public  Text        inputFeed;
    public  InputField  inputField;

    [Space(10)]
    public  Outline     flashOutline;
    public  Gradient    failGradient;
    private int         h_consoleColor = Shader.PropertyToID("_ConsoleColor");
    private float       failTimer = 1;
    private float       flashTimer = 0;

    [Space(10)]
    public AudioClip    soundType;
    public AudioClip    soundSuccess;
    public AudioClip    soundFail;
    private AudioSource src;

    private string[]    wordArray;
    private string[]    wordFeed = new string[4];

    private string      currentInput;  
    private string      prevInput;
    private string      correctInput;

    private RectTransform rTransform;
    [Space(10)]
    public  Vector2       dockedOffset;

    public  System.Action OnFail;
    public  System.Action OnSuccess;
    public  System.Action OnReload;

	void Awake ()
    {
        rTransform = GetComponent<RectTransform>();

        src = GetComponent<AudioSource>();

        LoadWordDB();

        Refresh();
	}
	
	void Update ()
    {
        //Force focus
        inputField.Select();
        inputField.ActivateInputField();

        //Shader Color
        if (failTimer < 1)      failTimer       += Time.deltaTime;
        if (flashTimer > 0)     flashTimer      -= Time.deltaTime * 4;

        Shader.SetGlobalColor(h_consoleColor, failGradient.Evaluate(failTimer));
        flashOutline.effectColor = new Color(1, 1, 1, flashTimer);

        WordMatchUpdate();
    }

    private void WordMatchUpdate()
    {
        prevInput       = currentInput;
        currentInput    = inputField.text;

        //No delta
        if (prevInput == currentInput)
            return;

        src.PlayOneShot(soundType, 0.3f);

        //Success
        if (currentInput.Trim() == correctInput.Trim())
        {
            SuccessFx();
            Refresh();
            if (OnSuccess != null) OnSuccess();
            return;
        }

        //Fail
        if (currentInput.Length > correctInput.Length)
        {
            FailFx();
            Refresh();
            if (OnFail != null) OnFail();
        }
    }

    private void    Refresh()
    {
        currentInput    = "";
        prevInput       = "";
        inputField.text = "";

        AddNewWord();
    }

    private void    LoadWordDB()
    {
        var wordFile = Resources.Load("words", typeof(TextAsset)) as TextAsset;
        wordArray = wordFile.text.Split('\n');

        for (int i = 0; i < wordFeed.Length; ++i)
            wordFeed[i] = RandomWord();

    }
    private string  RandomWord()
    {
        return wordArray[Random.Range(0, wordArray.Length)].Trim();
    }
    private void    AddNewWord()
    {
        for (int i = 0; i < wordFeed.Length -1; ++i)
            wordFeed[i] = wordFeed[i + 1];

        wordFeed[wordFeed.Length -1] = RandomWord();

        inputFeed.text = "";

        for(int i = wordFeed.Length -1; i >= 0; --i)
            inputFeed.text += wordFeed[i] + "\n";

        correctInput = wordFeed[0];
    }

    private void    FailFx()
    {
        failTimer   = 0;
        flashTimer  = 1;

        src.PlayOneShot(soundFail);
    }
    private void    SuccessFx()
    {
        flashTimer  = 1;

        src.PlayOneShot(soundSuccess);
    }

    public void Toggle(bool b)
    {
        enabled = b;

        LeanTween.move(rTransform, b ? Vector2.zero : dockedOffset, 0.5f).setEase(LeanTweenType.easeInCubic);
    }
}

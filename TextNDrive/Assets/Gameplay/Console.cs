using UnityEngine;
using UnityEngine.UI;

public class Console : MonoBehaviour
{
    public  int         wordsInBuffer;
    public  Text        inputFeed;
    public  InputField  inputField;

    [Space(10)]
    public  Outline     flashOutline;
    public  Gradient    failGradient;
    private int         h_consoleColor = Shader.PropertyToID("_ConsoleColor");
    private float       failTimer  = 1;
    private float       flashTimer = 0;

    [Space(10)]
    public AudioClip    soundType;
    public AudioClip    soundSuccess;
    public AudioClip    soundFail;
    private AudioSource src;

    private string[]    wordArray;
    private string[]    wordFeed;

    private string      currentInput;  
    private string      prevInput;
    private string      correctInput;

    private int wordsRange;

    public  System.Action OnFail;
    public  System.Action OnSuccess;
    public  System.Action OnReload;

	void Awake ()
    {
        wordFeed    = new string[wordsInBuffer];
        wordsRange  = wordsInBuffer;
        src         = GetComponent<AudioSource>();

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

        inputFeed.color                 = failGradient.Evaluate(failTimer);
        inputField.textComponent.color  = inputFeed.color;
        flashOutline.effectColor        = new Color(inputFeed.color.r, inputFeed.color.g, inputFeed.color.b, flashTimer);

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
        if (currentInput != correctInput.Substring(0, currentInput.Length))
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
        return wordArray[Random.Range(wordsRange - wordsInBuffer, wordsRange)].Trim();
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
    public  void    IncreseWordRange(int increment)
    {
        wordsRange += increment;
        wordsRange  = Mathf.Clamp(wordsRange, 0, wordArray.Length);
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
}

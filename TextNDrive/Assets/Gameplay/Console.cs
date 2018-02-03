using UnityEngine;
using UnityEngine.UI;

public class Console : MonoBehaviour
{
    public  int         wordsInBuffer;
    public  Text        inputFeed;
    public  InputField  inputField;
    public  Image       timerBar;

    [Space(10)]
    public  Outline     flashOutline;
    public  Gradient    failGradient;
    private float       m_failTimer  = 1;
    private float       m_flashTimer = 0;

    [Space(10)]
    public AudioClip    soundType;
    public AudioClip    soundSuccess;
    public AudioClip    soundFail;
    private AudioSource m_src;

    private string[]    m_wordArray;
    private string[]    m_wordFeed;

    private string      m_currentInput;  
    private string      m_prevInput;
    private string      m_correctInput;

    private float m_timeLimitTimer;
    private float m_timeLimit;
    private int   m_wordsRange;
    private bool  m_enabled = true;

    public  System.Action OnFail;
    public  System.Action OnSuccess;
    public  System.Action OnReload;

	void Awake ()
    {
        m_wordFeed    = new string[wordsInBuffer];
        m_wordsRange  = wordsInBuffer;
        m_src         = GetComponent<AudioSource>();

        LoadWordDB();
        Refresh();
	}
	void Update ()
    {
        //Force focus
        if(m_enabled)
        {
            inputField.Select();
            inputField.ActivateInputField();
            inputField.MoveTextEnd(false);
        }

        //Shader Color
        if (m_failTimer < 1)    m_failTimer       += Time.deltaTime;
        if (m_flashTimer > 0)   m_flashTimer      -= Time.deltaTime * 4;

        inputFeed.color                 = failGradient.Evaluate(m_failTimer);
        inputField.textComponent.color  = inputFeed.color;
        flashOutline.effectColor        = new Color(inputFeed.color.r, inputFeed.color.g, inputFeed.color.b, m_flashTimer);

        if (!m_enabled)
            return;

        m_prevInput       = m_currentInput;
        m_currentInput    = inputField.text;

        //Timer Bar Fill
        timerBar.fillAmount = Mathf.FloorToInt((inputTimeLeft / m_timeLimit) * 32) / 32f;

        //Ran out of time
        if(Time.time > m_timeLimitTimer && !GM.instance.practiseMode)
        {
            if (OnFail != null) OnFail();
            FailFx();
            Refresh();
        }

        //Remove spaces and tabs from input
        if (m_currentInput.Length > 0 && (m_currentInput[m_currentInput.Length - 1] == ' ' || m_currentInput[m_currentInput.Length - 1] == '\t'))
        {
            m_currentInput  = m_currentInput.Substring(0, m_currentInput.Length - 1);
            inputField.text = m_currentInput;
        }

        //No delta
        if (m_prevInput == m_currentInput)
            return;

        m_src.PlayOneShot(soundType, 0.3f);

        //Success
        if (m_currentInput.Trim() == m_correctInput.Trim())
        {
            if (OnSuccess != null) OnSuccess();
            SuccessFx();
            Refresh();
            return;
        }

        //Fail
        if (m_currentInput != m_correctInput.Substring(0, m_currentInput.Length))
        {
            if (OnFail != null) OnFail();
            FailFx();
            Refresh();
        }
    }

    private void    Refresh()
    {
        m_currentInput      = "";
        m_prevInput         = "";
        inputField.text     = "";
        m_timeLimitTimer    = Time.time + m_timeLimit;
        AddNewWord();
    }

    private void    LoadWordDB()
    {
        TextAsset wordFile = Resources.Load("words", typeof(TextAsset)) as TextAsset;
        m_wordArray = wordFile.text.Split('\n');

        for (int i = 0; i < m_wordFeed.Length; ++i)
            m_wordFeed[i] = RandomWord();

    }
    private string  RandomWord()
    {
        return m_wordArray[Random.Range(m_wordsRange - wordsInBuffer, m_wordsRange)].Trim();
    }
    private void    AddNewWord()
    {
        for (int i = 0; i < m_wordFeed.Length -1; ++i)
            m_wordFeed[i] = m_wordFeed[i + 1];

        m_wordFeed[m_wordFeed.Length -1] = RandomWord();

        inputFeed.text = "";

        for(int i = m_wordFeed.Length -1; i >= 0; --i)
            inputFeed.text += m_wordFeed[i] + "\n";

        m_correctInput = m_wordFeed[0];

        m_timeLimitTimer = Time.time + m_timeLimit;
    }
    public  void    IncreseWordRange(int increment)
    {
        m_wordsRange += increment;
        m_wordsRange  = Mathf.Clamp(m_wordsRange, 0, m_wordArray.Length);
    }

    public  void    SetMaxTime(float t)
    {
        m_timeLimit         = t;
        m_timeLimitTimer    = Time.time + t;
    }
            float   inputTimeLeft
    {
        get
        {
            return Mathf.Max(0, m_timeLimitTimer - Time.time);
        }
    }

    private void    FailFx()
    {
        m_failTimer   = 0;
        m_flashTimer  = 1;

        m_src.PlayOneShot(soundFail, 2);
    }
    private void    SuccessFx()
    {
        m_flashTimer  = 1;

        m_src.PlayOneShot(soundSuccess);
    }

    public void Disable()
    {
        m_enabled = false;
        inputField.DeactivateInputField();
        inputField.interactable = false;
        inputField.gameObject.SetActive(false);
    }
}

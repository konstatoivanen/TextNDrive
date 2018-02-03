using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;
using UnityEngine.SceneManagement;

public class GM : MonoBehaviour
{
    public static GM instance;

    public Slider           ui_volume;
    public Slider           ui_mblur;
    public Text             ui_practiceMode;
    public RectTransform    ui_background;
    public RectTransform    ui_hud_parent;
    public GameObject       ui_menu;
    public GameObject       ui_hud;
    public AudioClip        ui_sound_select;
    public AudioClip        ui_sound_enter;
    public AudioSource      ui_source;
    private float           ui_background_bottom_target = 0;

    [Space(10)]
    public float worldScaling = 1;
    public float deacceleration;
    public float failDeacceleration;
    public float speedReward;
    public float speedPenalty;
    public float copSpawnSpeed;
    public float startTimeLimit;
    public bool  practiseMode;

    [Space(10)]
    public AnimationCurve filterCurve;

    [Space(10)]
    public   float lefLanePosition;
    public   float rightLanePosition;

    [Space(10)]
    public Ent_CopCar       cop;
    public Text             score;
    public Console          console;
    public Transform        car;
    public ParticleSystem   boostFx;
    public ParticleSystem   impactFx;
    public GameObject       destructionFx;
    public GameObject       destructionHeader;
    public GameObject[]     lights;
    public float            cs_Stiffness;
    public float            cs_Damping;
    public AnimationCurve   carHoverCurve;

    [System.Serializable]
    public class RepeatLayer
    {
        public Transform[]  blocks;
        public float        spacing;
        public Vector3      offset;

        private float minPos;
        private float maxPos;
        private float position;

        public  void  Init()
        {
            minPos = offset.x;
            maxPos = offset.x + spacing * blocks.Length;
        }
        public  void  Update(float deltaPos)
        {
            position += deltaPos;

            position  = Repeat(position);

            for (int i = 0; i < blocks.Length; ++i)
            {
                blocks[i].position = offset + new Vector3(spacing * i + position, 0, 0);
                blocks[i].position = new Vector3(Repeat(blocks[i].position.x), blocks[i].position.y, blocks[i].position.z);
            }
        }
        private float Repeat(float f)
        {
           return Mathf.Repeat(f - minPos, maxPos - minPos) + minPos;
        }
    }
    public RepeatLayer[] layers;

    internal float      speed;
    private  int        m_wordCount;
    private  float      m_totalDistance;
    private  int        m_integrity = 100;
    private  bool       m_inLeftLane;
    private  bool       m_cameraAngleToggle;
    internal bool       destroyed;

    internal Vector2 s_restState;
    private  Vector2 s_state;
    private  Vector2 s_velocity;

    private AudioLowPassFilter  m_lowPassFilter;
    private AudioSource         m_source;
    private Transform           m_camera;
	
    void Start()
    {
        Application.targetFrameRate = 120;

        CombinedEffect.instance.brightness = 0;

        instance            = this;
        m_camera            = Camera.main.transform;
        m_lowPassFilter     = GetComponent<AudioLowPassFilter>();
        m_source            = GetComponent<AudioSource>();
        console.OnSuccess   = Reward;
        console.OnFail      = Penalty;
        console.SetMaxTime(startTimeLimit);
        s_restState.y       = lefLanePosition;

        for (int i = 0; i < layers.Length; ++i) layers[i].Init();

        AudioListener.volume = 0.5f;
    }
	void Update ()
    {
        if ( Input.GetKeyDown(KeyCode.Escape) && !ui_menu.activeSelf)
            UIRestart();

        ui_background.offsetMin = new Vector2(ui_background.offsetMin.x, Mathf.MoveTowards(ui_background.offsetMin.y, ui_background_bottom_target, Time.deltaTime * Screen.height * 2f));

        if(ui_hud.activeSelf) ui_hud_parent.anchoredPosition = new Vector2(ui_hud_parent.anchoredPosition.x, Mathf.Lerp(ui_hud_parent.anchoredPosition.y, 0, Time.deltaTime * 4f));

        LaneUpdate();
        CameraUpdate();
        CarSpringUpdate();

        if (destroyed) m_source.pitch = Mathf.Lerp(m_source.pitch, 0, Time.deltaTime * 0.5f);

        speed               = Mathf.MoveTowards(speed, 0, Time.deltaTime * (destroyed?failDeacceleration:deacceleration));
        m_totalDistance    += speed * Time.deltaTime;
        car.position        = new Vector3(s_state.x, carHoverCurve.Evaluate(Time.time), s_state.y);
        car.eulerAngles     = new Vector3(0, 90, Mathf.LerpAngle(car.eulerAngles.z, s_velocity.y, Time.deltaTime * 4));

        m_lowPassFilter.cutoffFrequency = Mathf.Lerp(m_lowPassFilter.cutoffFrequency,filterCurve.Evaluate(speed), Time.deltaTime);

        for (int i = 0; i < layers.Length; ++i) layers[i].Update(-speed * Time.deltaTime);

        UpdateScore();
	}

    public float kilometersPerHour
    {
        get { return Mathf.FloorToInt((speed * worldScaling) * 3.6f); }
    }
    public float travelled_meters
    {
        get { return m_totalDistance * worldScaling; }
    }
    public float travelled_decameters
    {
        get { return Mathf.FloorToInt(Mathf.Repeat((m_totalDistance * worldScaling), 1000) / 10); }
    }
    public int   travelled_kilometers
    {
        get { return Mathf.FloorToInt((m_totalDistance * worldScaling) / 1000); }
    }
    public float deltePosition
    {
        get { return speed * Time.deltaTime; }
    }

    public void Reward()
    {
        if (destroyed)
            return;

        speed += speedReward;

        if (speed > copSpawnSpeed && !cop.gameObject.activeSelf && !practiseMode)
            cop.Spawn(100, speed * 1.5f);

        console.IncreseWordRange(1);

        s_velocity.x += 10;

        boostFx.Play();

        m_wordCount++;
    }
    public void Penalty()
    {
        if (destroyed)
            return;

        integrity       -= 5;
        speed           -= speed > 0? speedPenalty : 0;
        s_velocity.x    -= speed > 0? 5 : 0;
        m_wordCount--;
    }
    public int  integrity
    {
        get
        {
            return m_integrity;
        }
        set
        {
            if (destroyed)
            {
                m_integrity = 0;
                return;
            }

            m_integrity = practiseMode? 100 : value;
            m_integrity = Mathf.Clamp(integrity, 0, 100);

            CombinedEffect.instance.noiseAmount = (2f - (m_integrity / 50f));
            CombinedEffect.instance.blood       = CombinedEffect.instance.noiseAmount * 0.125f;

            if (m_integrity > 0)
                return;

            destructionFx.SetActive(true);
            destructionHeader.SetActive(true);

            MeshRenderer mf = car.GetComponent<MeshRenderer>();

            mf.materials[2].SetColor("_EmissionColor", Color.black);
            mf.materials[3].SetColor("_EmissionColor", Color.black);

            for (int i = 0; i < lights.Length; ++i)
                lights[i].SetActive(false);

            UpdateScore();

            destroyed = true;

            console.Disable();

            CombinedEffect.instance.SetContrastTarget(2f);
        }
    }
    public void HitCar(RaycastHit hit)
    {
        if (hit.transform != car)
            return;

        impactFx.transform.position = hit.point;
        impactFx.transform.forward  = hit.normal;
        impactFx.Play();

        integrity -= 10;
    }

    void UpdateScore()
    {
        if (destroyed)
            return;

        score.text  = "Distance:  " + string.Format("{0:0}.{1:00}km", travelled_kilometers, travelled_decameters);
        score.text += "\n";
        score.text += "Speed:     " + kilometersPerHour.ToString() + "km/h";
        score.text += "\n";
        score.text += "Words:     " + m_wordCount.ToString();
        score.text += "\n";
        score.text += "Integrity: " + integrity.ToString() + "%";
    }
    void CarSpringUpdate()
    {
        s_velocity      += (s_restState -s_state) * cs_Stiffness * Time.deltaTime;  
        s_velocity      /= 1 + (cs_Damping * Time.deltaTime); 
        s_state         += s_velocity * Time.deltaTime;
    }

    void LaneUpdate()
    {
        if (destroyed)
            return;

        //Switch Lane
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        m_inLeftLane = !m_inLeftLane;

        s_velocity.y  += ((m_inLeftLane? rightLanePosition : lefLanePosition) - s_restState.y) * 2;
        s_restState.y  = m_inLeftLane ? rightLanePosition : lefLanePosition;
    }
    void CameraUpdate()
    {
        if (destroyed)
            return;

        if (!Input.GetKeyDown(KeyCode.Tab))
            return;

        m_cameraAngleToggle = !m_cameraAngleToggle;

        m_camera.position     = new Vector3(m_cameraAngleToggle? 20 : -20, 7, -35);
        m_camera.eulerAngles  = new Vector3(10, m_cameraAngleToggle ? -30 : 30, 0);
    }

    public void UIChanveVolume()
    {
        AudioListener.volume = ui_volume.value;
    }
    public void UIChanveMBlur()
    {
        CombinedEffect.instance.accumulation = ui_mblur.value;
    }
    public void UIToggleMode()
    {
        UISoundEnter();
        practiseMode = !practiseMode;
        ui_practiceMode.text = practiseMode ? "[M: PRACTISE]": "[M:NORMAL]";
    }

    public void UIStartGame()
    {
        UISoundEnter();
        StartCoroutine(StartGame_Internal());
    }
    IEnumerator StartGame_Internal()
    {
        ui_background_bottom_target = Screen.height;

        yield return new WaitForSeconds(0.5f);

        ui_menu.SetActive(false);
        ui_hud.SetActive(true);

        yield return new WaitForSeconds(0.4f);

        console.enabled = true;
    }

    public void UIQuitGame()
    {
        UISoundEnter();
        Application.Quit();
    }
    public void UIRestart()
    {
        UISoundEnter();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void UISoundEnter()
    {
        ui_source.PlayOneShot(ui_sound_enter, 2);
    }
    public void UISoundSelect()
    {
        ui_source.PlayOneShot(ui_sound_select);
    }
}

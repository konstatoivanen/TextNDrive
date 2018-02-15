using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;
using UnityEngine.SceneManagement;

public class GM : MonoBehaviour
{
    public static GM instance;

    public Text             ui_leaderboard;
    public Text             ui_namePrompt;
    public RectTransform    ui_competitiveGroup;
    public InputField       ui_plrName;
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
    private float           ui_competitiveGroup_target = 0;

    [Space(10)]
    public float worldScaling = 1;
    public float deacceleration;
    public float failDeacceleration;
    public float speedReward;
    public float speedPenalty;
    public float copSpawnSpeed;
    public float startTimeLimit;

    [Space(10)]
    public float dodgeMaxSpeed;
    public AnimationCurve dodgeAcceleration;

    public   enum  Mode { Normal, Practise, Dodge}
    internal Mode  mode;

    [Space(10)]
    public AnimationCurve filterCurve;

    [Space(10)]
    public   float lefLanePosition;
    public   float rightLanePosition;

    [Space(10)]
    public Ent_CopCar       cop;
    public Text             score;
    public Console          console;
    public ObstacleDirector obsDirector;
    public Transform        car;
    public CollisionProxy   carCollision;
    public ParticleSystem   boostFx;
    public GameObject       destructionFx;
    public GameObject       destructionHeader;
    public GameObject[]     lights;
    public float            cs_Stiffness;
    public float            cs_Damping;
    public AnimationCurve   carHoverCurve;

    [Serializable]
    public class RepeatLayer
    {
        public Transform[]  blocks;
        public float        spacing;
        public float        scaling = 1;
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
            position += deltaPos * scaling;

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
    [Space(10)]
    public RepeatLayer[] layers;

    internal float      speed;
    private  int        m_topSpeed = 0;
    private  int        m_wordCount;
    private  float      m_totalDistance;
    private  int        m_integrity = 100;
    private  bool       m_inLeftLane;
    private  float      m_laneSwitchTime;
    private  bool       m_cameraAngleToggle;
    internal bool       destroyed;

    internal Vector2 s_restState;
    private  Vector2 s_state;
    private  Vector2 s_velocity;
    private  Vector2 s_velocity_prev;

    private AudioLowPassFilter  m_lowPassFilter;
    private AudioSource         m_source;
    private Transform           m_camera;
	
    [Serializable]
    public class SaveData
    {
        public float mblur  = 0.6f;
        public float volume = 0.5f;
        public Mode  mode;

        public Score currentScore;

        public List<Score> leaderboard = new List<Score>();
    }

    [Serializable]
    public class Score
    {
        public string name;
        public float  distance;
        public int    topSpeed;

        public Score(string n, float d, int s)
        {
            name        = n;
            distance    = d;
            topSpeed    = s;
        }

        public override string ToString()
        {
            int km = Mathf.FloorToInt((distance) / 1000);
            int dm = Mathf.FloorToInt(Mathf.Repeat((distance), 1000) / 10);

            return name + " - " + string.Format("{0:0}.{1:00}km ", km, dm) + topSpeed.ToString() + "km/h";
        }

        public class ScoreComparer : IComparer<GM.Score>
        {

            public int Compare(GM.Score first, GM.Score second)
            {
                return IsHigher(first, second);
            }

            /// <summary>
            ///     Returns 1 if first comes before second in score order.
            ///     Returns -1 if second comes before first.
            ///     Returns 0 if the points are identical.
            /// </summary>
            public static int IsHigher(GM.Score first, GM.Score second)
            {
                if (first.distance == second.distance)
                    return 0;

                if (first.distance > second.distance)
                    return -1;

                return 1;
            }

        }
    }

    internal SaveData save;

    private BinaryFormatter serializer = new BinaryFormatter();
    private FileStream fileCurrent;

    void Start()
    {
        Load();

        //Application.targetFrameRate = 120;

        carCollision.collisionEvent = OnCollisionEnter;

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
    }
	void Update ()
    {
        if ( Input.GetKeyDown(KeyCode.Escape) && !ui_menu.activeSelf)
            UIRestart();

        ui_competitiveGroup.offsetMin   = new Vector2(ui_background.offsetMin.x, Mathf.MoveTowards(ui_competitiveGroup.offsetMin.y, ui_competitiveGroup_target, Time.deltaTime * Screen.height * 2f));
        ui_background.offsetMin         = new Vector2(ui_background.offsetMin.x, Mathf.MoveTowards(ui_background.offsetMin.y, ui_background_bottom_target, Time.deltaTime * Screen.height * 2f));

        CarSpringUpdate();

        if (destroyed) m_source.pitch = Mathf.Lerp(m_source.pitch, 0, Time.deltaTime * 0.5f);
       
        if(mode == Mode.Dodge && !ui_menu.activeSelf)
        {
            LaneUpdate();
            speed = Mathf.MoveTowards(speed, destroyed ? 30 : dodgeMaxSpeed, Time.deltaTime * (destroyed ? failDeacceleration : dodgeAcceleration.Evaluate(speed)));
        }

        if (mode != Mode.Dodge && ui_hud.activeSelf)
        {
            speed = Mathf.MoveTowards(speed, 0, Time.deltaTime * (destroyed ? failDeacceleration : deacceleration));
            ui_hud_parent.anchoredPosition = new Vector2(ui_hud_parent.anchoredPosition.x, Mathf.Lerp(ui_hud_parent.anchoredPosition.y, 0, Time.deltaTime * 4f));

            LaneUpdate();
            CameraUpdate();
        }

        m_totalDistance    += speed * Time.deltaTime;
        car.position        = new Vector3(s_state.x, carHoverCurve.Evaluate(Time.time), s_state.y);
        car.eulerAngles     = new Vector3(0, 90, Mathf.LerpAngle(car.eulerAngles.z, (s_velocity.y - s_velocity_prev.y) * 50, Time.deltaTime * 4));

        m_lowPassFilter.cutoffFrequency = Mathf.Lerp(m_lowPassFilter.cutoffFrequency,filterCurve.Evaluate(speed), Time.deltaTime);

        for (int i = 0; i < layers.Length; ++i) layers[i].Update(-speed * Time.deltaTime);

        UpdateScore();
	}

    public int    kilometersPerHour
    {
        get { return Mathf.FloorToInt((speed * worldScaling) * 3.6f); }
    }
    public float  travelled_meters
    {
        get { return m_totalDistance * worldScaling; }
    }
    public float  travelled_decameters
    {
        get { return Mathf.FloorToInt(Mathf.Repeat((m_totalDistance * worldScaling), 1000) / 10); }
    }
    public int    travelled_kilometers
    {
        get { return Mathf.FloorToInt((m_totalDistance * worldScaling) / 1000); }
    }
    public float  deltePosition
    {
        get { return speed * Time.deltaTime; }
    }

    public void Reward()
    {
        if (destroyed)
            return;

        speed += speedReward;

        if (speed > copSpawnSpeed && !cop.gameObject.activeSelf && mode == Mode.Normal)
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

            m_integrity = mode == Mode.Practise ? 100 : value;
            m_integrity = Mathf.Clamp(integrity, 0, 100);

            m_lowPassFilter.cutoffFrequency = 20;

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

        integrity -= 10;
    }
    
    void OnCollisionEnter(Collision c)
    {
        if (mode == Mode.Dodge)
        {
            if(Math.Abs(c.transform.position.z - s_restState.y) < 0.5f || Mathf.Abs(c.transform.position.z - s_state.y) < 2)
                integrity -= 5;
            else
                return;
        }

        if (Math.Abs(c.transform.position.z - s_restState.y) < 0.5f)
            SwitchLane(true);

        float sign = Mathf.Sign(transform.position.x - c.contacts[0].point.x);

        s_velocity.x    += 30 * sign;
        speed           += 5 * sign;

        IM.Spawn.Fx(IM.Type.fx_collision, c.contacts[0].point, Quaternion.identity);
    }

    void UpdateScore()
    {
        if (destroyed)
            return;

        if (kilometersPerHour > m_topSpeed) m_topSpeed = kilometersPerHour;

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
        s_velocity_prev  = s_velocity;
        s_velocity      += (s_restState -s_state) * cs_Stiffness * Time.deltaTime;  
        s_velocity      /= 1 + (cs_Damping * Time.deltaTime); 
        s_state         += s_velocity * Time.deltaTime;
    }
    void LaneUpdate()
    {
        if (destroyed)
            return;

        //Switch Lane
        if (!Input.GetKeyDown(KeyCode.Space) || Time.time < m_laneSwitchTime)
            return;

        m_laneSwitchTime = Time.time + 0.25f;

        SwitchLane();
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

    public void SwitchLane(bool fast = false)
    {
        m_inLeftLane  = !m_inLeftLane;
        s_restState.y = m_inLeftLane ? rightLanePosition : lefLanePosition;

        if (!fast)
            return;
        
        s_velocity.y += ((m_inLeftLane ? rightLanePosition : lefLanePosition) - s_restState.y);
        s_state       = Vector2.Lerp(s_state, s_restState, 0.5f);
    }

    public void UIChanveVolume()
    {
        AudioListener.volume = ui_volume.value;
        save.volume = ui_volume.value;
    }
    public void UIChanveMBlur()
    {
        CombinedEffect.instance.accumulation = ui_mblur.value;
        save.mblur = ui_mblur.value;
    }
    public void UIToggleMode()
    {
        UISoundEnter();

        mode = mode == Mode.Normal ? Mode.Practise : mode == Mode.Practise ? Mode.Dodge : Mode.Normal;

        ui_practiceMode.text        = mode == Mode.Normal ? "[MODE: NORMAL]" : mode == Mode.Practise ? "[MODE: PRACTISE]" : "[MODE: DODGE]";
        save.mode                   = mode;
        ui_competitiveGroup_target  = mode == Mode.Normal? 0 : Screen.height;

        m_camera.position           = new Vector3(mode == Mode.Dodge ? -10 : -20, 7, -35);
    }
    public void UIUpdateLeaderboard()
    {
        ui_leaderboard.text = "";

        if (save == null || save.leaderboard == null || save.leaderboard.Count == 0)
            return;

        save.leaderboard.Sort(new Score.ScoreComparer());

        for (int i = save.leaderboard.Count -1; i >= 0; --i)
            ui_leaderboard.text += (i == 0? "1st " : i == 1? "2nd " : i == 2? "3rd " : (i+1).ToString() + "th ") + save.leaderboard[i].ToString() + "\n";
    }
    public void UINameChange()
    {
        if (ui_plrName.text.Trim() == "")
            return;

        save.currentScore = FetchPlayer();

        if (save.currentScore == null)
        {
            save.currentScore = new Score(ui_plrName.text, 0, 0);
            save.leaderboard.Add(save.currentScore);
        }

        ui_plrName.text = "";
        ui_plrName.DeactivateInputField();

        UILoadPreviousName();
        UIUpdateLeaderboard();
    }
    public void UILoadPreviousName()
    {
        if (save == null || save.currentScore == null)
            return;

        ui_namePrompt.text  = "INSERT NAME [" + save.currentScore.name + "]";
    }

    public void UIStartGame()
    {
        //Ensure we have atleas a default player profile
        if(save.currentScore == null)
        {
            save.currentScore = new Score("Player1", 0, 0);
            save.leaderboard.Add(save.currentScore);
        }

        UISoundEnter();
        StartCoroutine(StartGame_Internal());
    }
    IEnumerator StartGame_Internal()
    {
        ui_background_bottom_target = Screen.height;

        yield return new WaitForSeconds(0.5f);

        ui_menu.SetActive(false);

        if (mode == Mode.Dodge)
        {
            obsDirector.enabled = true;
            yield break;
        }

        ui_hud.SetActive(true);

        yield return new WaitForSeconds(0.4f);

        console.enabled = true;
    }

    public void UIQuitGame()
    {
        Save();
        UISoundEnter();
        Application.Quit();
    }
    public void UIRestart()
    {
        Save();
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

    public void Save()
    {
        SaveScore();

        fileCurrent = File.Create(Application.dataPath + "/save.save");
        serializer.Serialize(fileCurrent, save);
        fileCurrent.Close();
        fileCurrent = null;
    }
    public void Load()
    {
        try
        {
            if (File.Exists(Application.dataPath + "/save.save"))
            {
                fileCurrent = File.Open(Application.dataPath + "/save.save", FileMode.Open);
                save = (SaveData)serializer.Deserialize(fileCurrent);
                fileCurrent.Close();
                fileCurrent = null;

                mode                                    = save.mode;
                ui_competitiveGroup_target              = mode == Mode.Normal ? 0 : Screen.height;
                AudioListener.volume                    = save.volume;
                CombinedEffect.instance.accumulation    = save.mblur;

                ui_mblur.value          = save.mblur;
                ui_volume.value         = save.volume;
                ui_practiceMode.text    = mode == Mode.Normal ? "[MODE: NORMAL]" : mode == Mode.Practise ? "[MODE: PRACTISE]" : "[MODE: DODGE]";
            }
            else
                save = new SaveData();
        }
        catch { save = new SaveData(); }

        UILoadPreviousName();
        UIUpdateLeaderboard();
    }

    private void  SaveScore()
    {
        if (mode != Mode.Normal)
            return;

        if (save == null || save.currentScore == null)
            return;

        if (save.currentScore.distance > m_totalDistance * worldScaling)
            return;

        save.currentScore.topSpeed = m_topSpeed;
        save.currentScore.distance = m_totalDistance * worldScaling;
    }
    private Score FetchPlayer()
    {
        if (save == null || save.leaderboard == null || save.leaderboard.Count == 0)
            return null;

        for(int i = 0; i < save.leaderboard.Count; ++i)
        {
            if (ui_plrName.text == save.leaderboard[i].name)
                return save.leaderboard[i];
        }

        return null;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GM : MonoBehaviour
{
    public static GM instance;

    public float position;
    public float worldScaling = 1;
    public float deacceleration;
    public float speedReward;
    public float speedPenalty;
    [Space(10)]
    public   AnimationCurve filterCurve;
    internal float speed;

    [Space(10)]
    public   float lefLanePosition;
    public   float rightLanePosition;

    [Space(10)]
    public Text             score;
    public Console          console;
    public Transform        car;
    public ParticleSystem   boostFx;
    public float            cs_Stiffness;
    public float            cs_Damping;
    public AnimationCurve   carHoverCurve;

    [Space(10)]
    public Transform[] blocks;
    public float spacing;
    public Vector3 offset;

    private float minPosition;
    private float maxPosition;

    private int     wordCount;
    private float   totalDistance;

    internal Vector2 s_restState;
    private  Vector2 s_state;
    private  Vector2 s_velocity;

    private AudioLowPassFilter lowPassFilter;

    private Transform m_camera;
	
    void Start()
    {
        instance            = this;
        m_camera            = Camera.main.transform;
        lowPassFilter       = GetComponent<AudioLowPassFilter>();
        console.OnSuccess   = Reward;
        console.OnFail      = Penalty;
        minPosition         = offset.x;
        maxPosition         = offset.x + spacing * (blocks.Length - 1);

        s_restState.y = lefLanePosition;
    }
	void Update ()
    {
        LaneUpdate();
        CameraUpdate();
        CarSpringUpdate();

        position         = Repeat(position);
        speed            = Mathf.MoveTowards(speed, 0, Time.deltaTime * deacceleration);
        position        -= speed * Time.deltaTime;
        totalDistance   += speed * Time.deltaTime;
        car.position     = new Vector3(s_state.x, carHoverCurve.Evaluate(Time.time), s_state.y);
        car.eulerAngles = new Vector3(0, 90, Mathf.LerpAngle(car.eulerAngles.z, s_velocity.y, Time.deltaTime * 4));

        lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, filterCurve.Evaluate(speed), Time.deltaTime);

        for (int i = 0; i < blocks.Length; ++i)
        {
            blocks[i].position = offset + new Vector3(spacing * i + position, 0, 0);
            blocks[i].position = new Vector3(Repeat(blocks[i].position.x), blocks[i].position.y, blocks[i].position.z);
        }

        UpdateScore();
	}

    float Repeat(float pos)
    {
        return Mathf.Repeat(pos - minPosition, maxPosition - minPosition) + minPosition;
    }
    public float kilometersPerHour
    {
        get { return Mathf.FloorToInt((speed * worldScaling) * 3.6f); }
    }
    public float travelled_meters
    {
        get { return totalDistance * worldScaling; }
    }
    public float travelled_decameters
    {
        get { return Mathf.FloorToInt(Mathf.Repeat((totalDistance * worldScaling), 1000) / 10); }
    }
    public int   travelled_kilometers
    {
        get { return Mathf.FloorToInt((totalDistance * worldScaling) / 1000); }
    }
    public float deltePosition
    {
        get { return speed * Time.deltaTime; }
    }

    public void Reward()
    {
        speed += speedReward;

        console.IncreseWordRange(1);

        s_velocity.x += 10;

        boostFx.Play();

        wordCount++;
    }
    public void Penalty()
    {
        speed -= speedPenalty;

        s_velocity.x -= 5;

        wordCount--;
    }

    void UpdateScore()
    {
        score.text  = "Distance:  " + string.Format("{0:0}.{1:00}km", travelled_kilometers, travelled_decameters);
        score.text += "\n";
        score.text += "Speed:     " + kilometersPerHour.ToString() + "km/h";
        score.text += "\n";
        score.text += "Words:     " + wordCount.ToString();
    }
    void CarSpringUpdate()
    {
        s_velocity      += (s_restState -s_state) * cs_Stiffness * Time.deltaTime;  
        s_velocity      /= 1 + (cs_Damping * Time.deltaTime); 
        s_state         += s_velocity * Time.deltaTime;
    }

    void LaneUpdate()
    {
        //Switch Lane
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            s_velocity.y  += (lefLanePosition - s_restState.y) * 2;
            s_restState.y  = lefLanePosition;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            s_velocity.y += (rightLanePosition - s_restState.y) * 2;
            s_restState.y = rightLanePosition;
        }
    }
    void CameraUpdate()
    {
        //Switch Camera position
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            m_camera.position     = new Vector3(-20, 7, -35);
            m_camera.eulerAngles  = new Vector3(10, 30, 0);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            m_camera.position     = new Vector3(20, 7, -35);
            m_camera.eulerAngles  = new Vector3(10, -30, 0);
        }
    }
}

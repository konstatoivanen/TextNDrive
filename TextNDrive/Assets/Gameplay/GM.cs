using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GM : MonoBehaviour
{
    public float position;
    public float deacceleration;
    public float speedReward;
    public float speedPenalty;
    [Space(10)]
    public  AnimationCurve filterCurve;
    private float speed;

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

    private float   s_state;
    private float   s_velocity;

    private AudioLowPassFilter lowPassFilter;
	
    void Start()
    {
        lowPassFilter = GetComponent<AudioLowPassFilter>();

        console.OnSuccess = Reward;
        console.OnFail = Penalty;

        minPosition = offset.x;
        maxPosition = offset.x + spacing * (blocks.Length - 1);
    }
	void Update ()
    {
        CarSpringUpdate();

        position         = Repeat(position);
        speed            = Mathf.MoveTowards(speed, 0, Time.deltaTime * deacceleration);
        position        -= speed * Time.deltaTime;
        totalDistance   += speed * Time.deltaTime;
        car.position     = new Vector3(s_state, carHoverCurve.Evaluate(Time.time), 0);

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

    public void Reward()
    {
        speed += speedReward;

        s_velocity += 10;

        boostFx.Play();

        wordCount++;
    }
    public void Penalty()
    {
        speed -= speedPenalty;

        s_velocity -= 5;

        wordCount--;
    }

    void UpdateScore()
    {
        int km  = Mathf.FloorToInt(totalDistance / 1000);
        int hm  = Mathf.FloorToInt(Mathf.Repeat(totalDistance, 1000) / 10);
        int kmh = Mathf.FloorToInt(speed * 3.6f);

        score.text  = "Distance:  " + string.Format("{0:0}.{1:00}km", km, hm);
        score.text += "\n";
        score.text += "Speed:     " + kmh.ToString() + "km/h";
        score.text += "\n";
        score.text += "Words:     " + wordCount.ToString();
    }

    void CarSpringUpdate()
    {
        s_velocity += -s_state * cs_Stiffness * Time.deltaTime;  
        s_velocity /= 1 + (cs_Damping * Time.deltaTime); 
        s_state += s_velocity * Time.deltaTime;
    }
}

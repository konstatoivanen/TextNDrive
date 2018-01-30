﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ent_CopCar : MonoBehaviour
{
    public float laneUpdateInterval;

    [Space(10)]
    public float            cs_Stiffness;
    public float            cs_Damping;
    public float            speedGain;
    public float            acceleration;
    public float            deacceleration;
    public float            tailingDistance;
    public AnimationCurve   carHoverCurve;

    private float   laneTimer;
    private Vector2 s_state;
    private Vector2 s_velocity;
    private Vector2 s_restState;

    private float m_speedCurrent;
    private float m_speedTarget;
    private float m_brakedist;
    private float m_deltaSpeed;
    private float m_distanceTotarget;
    private float m_speedMax = 50;

    void Start ()
    {
        s_restState = new Vector2(transform.position.x,  1.25f);
        laneTimer = laneUpdateInterval;
    }
	
	// Update is called once per frame
	void Update ()
    {
        LaneUpdate();
        LocomotionUpdate();
    }

    void LocomotionUpdate()
    {
        s_velocity  += (s_restState - s_state) * cs_Stiffness * Time.deltaTime;
        s_velocity  /= 1 + (cs_Damping * Time.deltaTime);
        s_state     += s_velocity * Time.deltaTime;

        //Add Player deltaposition
        s_restState.x   -= GM.instance.deltePosition;
        s_state.x       -= GM.instance.deltePosition;

        //Increase speed cap based on time passed
        m_speedMax += speedGain * Time.deltaTime;

        //Increase speed if falling behind and decrease if too close
        m_distanceTotarget  = GM.instance.transform.position.x - transform.position.x - tailingDistance;

        m_deltaSpeed        = GM.instance.speed - m_speedCurrent;

        m_brakedist         = BrakeDistance(acceleration, m_deltaSpeed);

        m_speedTarget       = GM.instance.speed + m_distanceTotarget + (m_brakedist < Mathf.Abs(m_distanceTotarget)? m_deltaSpeed : 0);

        m_speedTarget       = Mathf.Clamp(m_speedTarget, -m_speedMax, m_speedMax);

        m_speedCurrent      = Mathf.MoveTowards(m_speedCurrent, m_speedTarget, Time.deltaTime * (m_speedTarget < m_speedCurrent? deacceleration : acceleration));

        s_restState.x      += m_speedCurrent * Time.deltaTime;

        transform.position    = new Vector3(s_state.x, carHoverCurve.Evaluate(Time.time), s_state.y);
        transform.eulerAngles = new Vector3(0, 90, Mathf.LerpAngle(transform.eulerAngles.z, s_velocity.y, Time.deltaTime * 4));
    }
    void LaneUpdate()
    {
        if (Time.time < laneTimer)
            return;

        laneTimer += laneUpdateInterval;

        s_restState.y = GM.instance.s_restState.y;
    }

    float BrakeDistance(float a, float v)
    {
        return 0.5f * a * (v/a) * (v/a);
    }
}

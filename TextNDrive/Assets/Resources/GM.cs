using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GM : MonoBehaviour
{
    public float position;
    public float deacceleration;
    public float speedReward;
    public float speedPenalty;
    [Space(10)]
    public float ostTreshold;
    public float ostFadeInDuration;
    public float ostVolume;
    private float speed;

    [Space(10)]
    public Console console;
    public Transform car;
    public AnimationCurve carHoverCurve;

    [Space(10)]
    public Transform[] blocks;
    public float spacing;
    public Vector3 offset;

    private float minPosition;
    private float maxPosition;

    private AudioSource ostSrc;
	
    void Start()
    {
        ostSrc = GetComponent<AudioSource>();

        console.OnSuccess = Reward;
        console.OnFail = Penalty;

        minPosition = offset.x;
        maxPosition = offset.x + spacing * (blocks.Length - 1);
    }
	void Update ()
    {
        position = Repeat(position);

        speed = Mathf.MoveTowards(speed, 0, Time.deltaTime * deacceleration);

        position -= speed * Time.deltaTime;

        car.position = new Vector3(0, carHoverCurve.Evaluate(Time.time), 0);

        if (speed > ostTreshold && !ostSrc.isPlaying)
            ostSrc.Play();

        if (ostSrc.isPlaying)
            ostSrc.volume = Mathf.MoveTowards(ostSrc.volume, ostVolume, (1 / ostFadeInDuration) * Time.deltaTime);

        for (int i = 0; i < blocks.Length; ++i)
        {
            blocks[i].position = offset + new Vector3(spacing * i + position, 0, 0);

            blocks[i].position = new Vector3(Repeat(blocks[i].position.x), blocks[i].position.y, blocks[i].position.z);
        }

	}

    float Repeat(float pos)
    {
        return Mathf.Repeat(pos - minPosition, maxPosition - minPosition) + minPosition;
    }

    public void Reward()
    {
        speed += speedReward;
    }
    public void Penalty()
    {
        speed -= speedPenalty;
    }

}

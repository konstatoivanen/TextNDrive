using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public Text timer;

	void Update ()
    {
        if (GM.instance.destroyed)
            return;

        timer.text = timeFormated();
    }

    string timeFormated()
    {
        float t     = Time.timeSinceLevelLoad;
        int min     = Mathf.FloorToInt(t / 60);
        int sec     = Mathf.FloorToInt(t - min * 60);
        int msec    = Mathf.FloorToInt(Mathf.Repeat((t - min * 60 * sec) * 100, 100));

        return string.Format("{0:00}:{1:00}:{2:00}", min, sec, msec);
    }
}

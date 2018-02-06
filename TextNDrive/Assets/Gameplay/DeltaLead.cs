using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeltaLead : MonoBehaviour
{
    public Text timer;

    private float prevDelta;

	void Update ()
    {
        if (GM.instance.destroyed)
            return;

        timer.text = distanceToLead();
    }

    string timeFormated()
    {
        float t     = Time.timeSinceLevelLoad;
        int min     = Mathf.FloorToInt(t / 60);
        int sec     = Mathf.FloorToInt(t - min * 60);
        int msec    = Mathf.FloorToInt(Mathf.Repeat((t - min * 60 * sec) * 100, 100));

        return string.Format("{0:00}:{1:00}:{2:00}", min, sec, msec);
    }
    string distanceToLead()
    {
        float delta = GM.instance.travelled_meters - GM.instance.save.leaderboard[0].distance;

        if (delta > 0 && prevDelta < 0)
            timer.color = Color.green;

        prevDelta = delta;

        float deltaAbs = Mathf.Abs(delta);

        int dm = Mathf.FloorToInt(Mathf.Repeat((deltaAbs), 1000) / 10);
        int km = Mathf.FloorToInt((deltaAbs) / 1000);

        return (delta > 0? "+" :"-" ) + string.Format("{0:0}.{1:00}km", km, dm);
    }
}

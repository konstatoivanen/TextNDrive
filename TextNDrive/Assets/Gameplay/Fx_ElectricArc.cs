using UnityEngine;

public class Fx_ElectricArc : MonoBehaviour
{
    public LineRenderer arc;
    public int cornersCount;
    public float maxOffset;
    [Range(0.1f,10)]
    public float midRangeMult;
    public AnimationCurve fadeCurve;
    public float duration;

    Vector3 p0;
    Vector3 p1;

    Vector3[] points;
    float mult;
    float currentRange;
    float t;
    ParticleSystem.ShapeModule sh;

	void Update ()
    {
        if(t > duration)
            return;

        t += Time.deltaTime;
        arc.startColor  = new Color(1, 1, 1, fadeCurve.Evaluate(t));
        arc.endColor    = new Color(1, 1, 1, fadeCurve.Evaluate(t));
	}
    void Recalculate()
    {
        t = 0;
        arc.positionCount = cornersCount;
        points      = new Vector3[cornersCount];
        points[0]   = p0;
        mult = 1f / points.Length;

        for(int i = 1; i<points.Length-1; i++)
        {
            currentRange = i * mult;
            points[i] = Vector3.Lerp(p0, p1, currentRange) + new Vector3(Random.Range(-maxOffset, maxOffset), Random.Range(-maxOffset, maxOffset), Random.Range(-maxOffset, maxOffset)) * (-Mathf.Abs(2 * currentRange - 1) + midRangeMult);
        }

        points[points.Length - 1] = p1;

        arc.SetPositions(points);
    }
    public void Play(Vector3 from, Vector3 to)
    {
        if(!arc.useWorldSpace)
        {
            from = transform.InverseTransformPoint(from);
            to   = transform.InverseTransformPoint(to);
        }

        p0 = from;
        p1 = to;
        Recalculate();
    }
}

using UnityEngine;

public class Ent_Car : MonoBehaviour
{
    public AnimationCurve carHoverCurve;
    public float speed;

	void Update ()
    {
        transform.position += new Vector3(-GM.instance.deltePosition + speed * Time.deltaTime, 0, 0);
	}

    public void Set(float _speed)
    {
        speed = _speed;
    }
}

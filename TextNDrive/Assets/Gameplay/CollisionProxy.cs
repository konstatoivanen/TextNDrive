using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionProxy : MonoBehaviour
{
    public System.Action<Collision> collisionEvent;

    void OnCollisionEnter(Collision c)
    {
        if (collisionEvent != null) collisionEvent(c);
    }
}

using UnityEngine;

public class ObstacleDirector : MonoBehaviour
{
    public float spawnSpeed;
    public float spawnSpeedThreshold;
    public float minSpacing;
    public float maxSpacing;
    public float spawnOffset;
    public float leftLanePos;
    public float rightLanePos;

    private float m_currentLane;
    private float m_currentSpacing;
    private float m_distanceSinceSpawn;
	
	void Update ()
    {
        if(GM.instance.speed < spawnSpeedThreshold)
            return;

        m_distanceSinceSpawn += GM.instance.deltePosition - spawnSpeed * Time.deltaTime;

        if (m_distanceSinceSpawn < m_currentSpacing)
            return;

        m_distanceSinceSpawn = 0;

        m_currentSpacing    = Random.Range(minSpacing, maxSpacing);
        m_currentLane       = Random.value > 0.5? leftLanePos : rightLanePos;

        IM.Spawn.Fx_Ret(IM.Type.obstacleCar, new Vector3(spawnOffset, 1, m_currentLane), Quaternion.Euler(0, 90, 0)).GetComponent<Ent_Car>().Set(spawnSpeed);
	}
}

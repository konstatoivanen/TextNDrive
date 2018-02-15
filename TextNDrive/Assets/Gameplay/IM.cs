using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IM : MonoBehaviour
{
	public static IM Spawn;

	public enum Type
    {
        fx_collision,
        fx_boost,
        fx_impact,
        obstacleCar
    }
    private int TypeIndex(Type t)
    {
        switch (t)
        {
            case Type.fx_collision: return 0;
            case Type.fx_boost:     return 1;
            case Type.fx_impact:    return 2;
            case Type.obstacleCar:  return 3;
        }
        return 0;
    }

    [System.Serializable]
	public class InstanceData
	{
        public      GameObject      Instance;
		public      int             maxCount;
		public      AudioClip[]     sounds;

		internal    GameObject[]    Pool;
		internal    bool            hasAudioSrc = false;
		internal    AudioSource[]   srcPool;
		internal    int             index = 0;
	}

    [HideInInspector]
	public  InstanceData[]      SpawnTypes;
	private InstanceData        m_instance;
    private int                 m_Index;

	void Awake()
    {
        Spawn = this;
        InitializePools();
    }

    private void        InitializePools()
    {
        List<GameObject>    temp_List   = new List<GameObject>();
        List<AudioSource>   temp_List2  = new List<AudioSource>();
        GameObject          temp_Object;

        for (int i = 0; i < SpawnTypes.Length; ++i)
        {
            if (SpawnTypes[i].maxCount == 0)
                continue;

            bool typeHasAudioSrc = SpawnTypes[i].Instance.GetComponent<AudioSource>() != null && SpawnTypes[i].sounds.Length != 0;

            int p = 0;
            while (p < SpawnTypes[i].maxCount)
            {
                temp_Object             = null;
                temp_Object             = Instantiate(SpawnTypes[i].Instance, Vector3.zero, Quaternion.identity) as GameObject;
                temp_Object.hideFlags   = HideFlags.HideInHierarchy;
                temp_List.Add(temp_Object);

                if (typeHasAudioSrc) temp_List2.Add(temp_Object.GetComponent<AudioSource>());
                p++;
            }
            SpawnTypes[i].Pool = temp_List.ToArray();
            temp_List.Clear();

            if (typeHasAudioSrc)
            {
                SpawnTypes[i].hasAudioSrc   = true;
                SpawnTypes[i].srcPool       = temp_List2.ToArray();
                temp_List2.Clear();
            }
            p = 0;
        }
    }
    public  void        Fx (Type t, Vector3 pos, Quaternion rot, Transform parent = null)
	{
        m_instance                                         = SpawnTypes[TypeIndex(t)];
        m_instance.Pool[m_instance.index].transform.parent = parent == null ? null : parent;
        m_instance.Pool[m_instance.index].transform.SetPositionAndRotation(pos, rot);
        m_instance.Pool[m_instance.index].SetActive(true);

		if(m_instance.hasAudioSrc) m_instance.srcPool[m_instance.index].PlayFromArray(m_instance.sounds);

        m_instance.index++;
	    if(m_instance.index == m_instance.maxCount) m_instance.index = 0;

        m_instance.Pool[m_instance.index].SetActive(false);
	}
    public  GameObject  Fx_Ret (Type t, Vector3 pos, Quaternion rot, Transform parent = null)
	{
        m_instance                                         = SpawnTypes[TypeIndex(t)];
        m_instance.Pool[m_instance.index].transform.parent = parent == null ? null : parent;
        m_instance.Pool[m_instance.index].transform.SetPositionAndRotation(pos, rot);
        m_instance.Pool[m_instance.index].SetActive(true);

        if (m_instance.hasAudioSrc) m_instance.srcPool[m_instance.index].PlayFromArray(m_instance.sounds);

        int x = m_instance.index;
        m_instance.index++;
		if(m_instance.index == m_instance.maxCount) m_instance.index = 0;

        m_instance.Pool[m_instance.index].SetActive(false);
	    return m_instance.Pool[x];
	}
    public  GameObject  Fx_Ret (Type t)
    {
        m_instance = SpawnTypes[TypeIndex(t)];
        m_instance.Pool[m_instance.index].SetActive(true);

        if (m_instance.hasAudioSrc) m_instance.srcPool[m_instance.index].PlayFromArray(m_instance.sounds);

        int x = m_instance.index;
        m_instance.index++;
        if (m_instance.index == m_instance.maxCount) m_instance.index = 0;

        m_instance.Pool[m_instance.index].SetActive(false);
        return m_instance.Pool[x];
    }
}

public static class ExtensionMethods
{
    public static void PlayFromArray(this AudioSource src, AudioClip[] sounds)
    {
        src.PlayOneShot(sounds[Random.Range(0, sounds.Length)]);
    }
    public static void PlayFromArray(this AudioSource src, AudioClip[] sounds, float volumeScale)
    {
        src.PlayOneShot(sounds[Random.Range(0, sounds.Length)], volumeScale);
    }  
}

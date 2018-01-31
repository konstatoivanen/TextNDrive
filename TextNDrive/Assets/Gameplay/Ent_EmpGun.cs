using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ent_EmpGun : MonoBehaviour
{
    public float          maxDist;
    public ParticleSystem buildup;
    public ParticleSystem muzzleFlash;
    public Fx_ElectricArc arc;
    public AudioSource    source;

    private RaycastHit m_hit;
    private bool       m_hitSuccess;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            Fire();
    }
    public void Fire()
    {

        StartCoroutine(FireSequence());
    }

    public IEnumerator FireSequence()
    {
        buildup.Play();

        source.PlayOneShot(source.clip);

        yield return new WaitForSeconds(0.5f);

        muzzleFlash.Play();

        yield return new WaitForSeconds(0.25f);

        if (Physics.Raycast(transform.position, transform.forward, out m_hit, maxDist))
        {
            arc.Play(transform.position, m_hit.point);
            GM.instance.HitCar(m_hit);
        }
        else
            arc.Play(transform.position, transform.TransformPoint(0, 0, maxDist));

    }
}

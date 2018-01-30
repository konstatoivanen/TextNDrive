using UnityEngine;

[ExecuteInEditMode]
public class Fx_LightShaft : MonoBehaviour
{
    [Space(10)]
    public Color color;
    
    [Space(10)]   
    public float   widthStart     = 0;
    public float   widthEnd       = 5;
    public float   height         = 5;
    public Vector3 deltaPosition;

    [Space(10)]
    public bool  useperpendicularStartWidth;
    public float widthStartPerpendicular;

    [Space(10)]
    public float widthFadePower     = 1;
    public float heightFadePower    = 1;

    [Space(10)]
    public float fadeInDistance     = 10;
    public float fadeOutDistance    = 5;

    [Space(10)]
    public Texture  smokeTexture;
    public float    smokeStrength           = 1;
    public float    smokeTiling             = 1;
    public float    SmokeWorldPosStrength   = 0.25f;
    public float    SmokeMoveSpeed          = 0.1f;

    private LineRenderer    m_line;
    private Material        m_material = null;
    private int             h_Color = Shader.PropertyToID("_Color");

    void         Start()
    {
        MaterialUpdate();
        LineUpdate();

        if(Application.isPlaying)
            enabled = false;
    }
    void         Update()
    {
        if (Application.isPlaying)
            return;

        LineUpdate();
        MaterialUpdate();
    }
    private void LineUpdate()
    {
        if (m_line == null)
        {
            if (!(m_line = GetComponent<LineRenderer>())) m_line = gameObject.AddComponent<LineRenderer>();
            m_line.useWorldSpace        = false;
            m_line.hideFlags            = HideFlags.HideInInspector;
        }

        m_line.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_line.receiveShadows       = false;
        m_line.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        m_line.material             = m_material;
        m_line.startWidth           = widthEnd;
        m_line.endWidth             = widthEnd;
        m_line.SetPosition(0, deltaPosition);
        m_line.SetPosition(1, new Vector3(0, 0, height) + deltaPosition);
    }
    private void MaterialUpdate()
    {
        if (m_material == null)
        {
            m_material              = new Material(Shader.Find("Hidden/LightShaft"));
            m_material.hideFlags    = HideFlags.HideAndDontSave;
        }

        fadeInDistance  = Mathf.Max(fadeInDistance,  fadeOutDistance);
        fadeOutDistance = Mathf.Min(fadeOutDistance, fadeInDistance);
        widthStart      = Mathf.Clamp(widthStart, 0, widthEnd);

        m_material.SetTexture("_SmokeTex",  smokeTexture);
        m_material.SetColor("_Color",       color);
        m_material.SetFloat("_WidthS",      widthStart/widthEnd);
        m_material.SetFloat("_WidthP",      (useperpendicularStartWidth? widthStartPerpendicular : widthStart)/ widthEnd);
        m_material.SetFloat("_PowW",        widthFadePower);
        m_material.SetFloat("_PowH",        heightFadePower);
        m_material.SetFloat("_FadeIn",      fadeInDistance);
        m_material.SetFloat("_FadeOut",     fadeOutDistance);
        m_material.SetVector("_Smoke",      new Vector4(smokeStrength, smokeTiling, SmokeWorldPosStrength, SmokeMoveSpeed)); 

        m_material.SetMatrix("_WorldToLocal", transform.worldToLocalMatrix);
    }
    public  void SetColor(Color c)
    {
        if(m_material) m_material.SetColor(h_Color, c);
    }
    void         OnDestroy()
    {
        if(m_line) m_line.hideFlags = HideFlags.None;
    }
}

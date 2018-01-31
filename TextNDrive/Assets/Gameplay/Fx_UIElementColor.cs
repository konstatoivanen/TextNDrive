using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/ElementColor")]
public class Fx_UIElementColor : MonoBehaviour
{
    public Text  m_text;
    public Image m_image;

    [Space(10)]
    public  Gradient m_gradient;
    public  float    m_speed;
    private float    m_timer;

	void Update ()
    {
        m_timer += Time.deltaTime * m_speed;
        if (m_timer > 1)    m_timer         = 0;
        if (m_text)         m_text.color    = m_gradient.Evaluate(m_timer);
        if (m_image)        m_image.color   = m_gradient.Evaluate(m_timer);
	}
}

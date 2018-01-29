using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Bloom")]
#if UNITY_5_4_OR_NEWER
[ImageEffectAllowedInSceneView]
#endif
public class Bloom : MonoBehaviour
{
	[Range(0.0f, 0.4f)]
	public float bloomIntensity = 0.05f;

	public Shader shader;
	private Material material;

	public Texture2D lensDirtTexture;
	[Range(0.0f, 0.95f)]
	public float lensDirtIntensity = 0.05f;

	private bool isSupported;

	private float blurSize = 4.0f;

	public bool inputIsHDR;

    private float         m_spread;
    private RenderTexture m_downsampled;
    private RenderTexture rt;
    private RenderTexture rt2;
    private RenderTextureDescriptor m_desc;

    #region Shader hashes
        private int h_bloomIntensity    = Shader.PropertyToID("_BloomIntensity");
        private int h_lensDirtIntensity = Shader.PropertyToID("_LensDirtIntensity");
        private int h_blursize          = Shader.PropertyToID("_BlurSize");
        private int h_lensDirt          = Shader.PropertyToID("_LensDirt");
        private int h_bloom0            = Shader.PropertyToID("_Bloom0");
        private int h_bloom1            = Shader.PropertyToID("_Bloom1");
        private int h_bloom2            = Shader.PropertyToID("_Bloom2");
        private int h_bloom3            = Shader.PropertyToID("_Bloom3");
        private int h_bloom4            = Shader.PropertyToID("_Bloom4");
        private int h_bloom5            = Shader.PropertyToID("_Bloom5");
    #endregion

    void Start() 
	{
        if (!material)
        {
            material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;
        }

        isSupported = SystemInfo.supportsImageEffects && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
	}
	void OnDisable()
	{
		if(material)
			DestroyImmediate(material);
	}
	void OnRenderImage(RenderTexture source, RenderTexture destination) 
	{
		if (!isSupported)
		{
			Graphics.Blit(source, destination);
			return;
		}

		if (!material)
        {
            material            = new Material(shader);
            material.hideFlags  = HideFlags.HideAndDontSave;
        }

		#if UNITY_EDITOR
		inputIsHDR = source.format == RenderTextureFormat.ARGBHalf;
		#endif

		material.SetFloat(h_bloomIntensity,    Mathf.Exp(bloomIntensity) - 1.0f);
		material.SetFloat(h_lensDirtIntensity, Mathf.Exp(lensDirtIntensity) - 1.0f);
		source.filterMode = FilterMode.Bilinear;
      
        m_desc         = source.descriptor;
        m_desc.width  /= 2;
        m_desc.height /= 2;
        m_downsampled  = source;
        m_spread       = 1.0f;

		for (int i = 0; i < 6; ++i)
		{
			rt = RenderTexture.GetTemporary(m_desc);

			Graphics.Blit(m_downsampled, rt, material, 1);

            m_downsampled = rt;
            m_spread      = i == 2 ? 0.75f : (i > 1 ? 1.0f : 0.5f);

			for (int j = 0; j < 2; j++)
			{
				material.SetFloat(h_blursize, (blurSize * 0.5f + j) * m_spread);

				//vertical blur
				rt2   = RenderTexture.GetTemporary(m_desc);

				Graphics.Blit(rt, rt2, material, 2);
				RenderTexture.ReleaseTemporary(rt);
				rt = rt2;

				rt2 = RenderTexture.GetTemporary(m_desc);

				Graphics.Blit(rt, rt2, material, 3);
				RenderTexture.ReleaseTemporary(rt);
				rt = rt2;
			}

			switch (i)
			{
				case 0: material.SetTexture(h_bloom0, rt); break;
				case 1: material.SetTexture(h_bloom1, rt); break;
				case 2: material.SetTexture(h_bloom2, rt); break;
				case 3: material.SetTexture(h_bloom3, rt); break;
				case 4: material.SetTexture(h_bloom4, rt); break;
				case 5: material.SetTexture(h_bloom5, rt); break;
				default: break;
			}

			RenderTexture.ReleaseTemporary(rt);

            m_desc.width  /= 2;
            m_desc.height /= 2;
		}

		material.SetTexture(h_lensDirt, lensDirtTexture);

		Graphics.Blit(source, destination, material, 0);
	}
}
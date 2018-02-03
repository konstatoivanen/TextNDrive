using UnityEngine;
using UnityEngine.Rendering;

namespace UnityStandardAssets.ImageEffects
{
	[ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Image Effects/Combined Effect")]
#if UNITY_5_4_OR_NEWER
    [ImageEffectAllowedInSceneView]
#endif
    public sealed class CombinedEffect : PostProcessBase
    {
        #region Editor values

        public Vector4          radialBlur              = Vector4.zero;
        public Vector2          fishEyeIntensity        = new Vector2(0, 0);
        public AnimationCurve   flashBangFallOffCurve;

        [Space(10)]
        public bool     depthOfFieldEnabled = true;
        [Range(0,1)]
        public float    blurAmount          = 0.01f;
        public float    focalArea           = 1;
        public float    effectiveRange      = 5;

        [Space(10)]
        [Header("Vignette, Chroma, blood, contrast and saturation Parameters")]
        [Range(0, 2)]
        public  float   accumulation;
        [Range(0, 2)]
        public  float   chroma;
        [Range(0, 1)]
        public  float   vignette;
        [Range(0, 1)]
        public  float   blood;
        [Range(0, 20)]
        public  float   contrast;
        private float   contrastTarget;
        [Range(0, 1f)]
        public float    saturation;
        [Range(0.0f, 4f)]
        public  float    brightness;
        private float    brightnessTarget = 1;
        [Range(-2,2)]
        public float    noiseAmount;
        [Range(0, 5)]
        public float    noiseLuminosity;
        public Gradient pauseColor;

        [Space(10)]
        [Header("Fog Effect Parameters")]
        public float    fogPow;
        public int      fogDistance;
        public float    fogFadeRadius;
        public Color    fogColor;

        [Space(10)]
        [Header("Overlay Parameters")]
        public Color        overlayColor        = Color.white;
        public float        overlayIntensity    = 0f;
        public float        distortAmount;
        public Texture2D    overlayTexture;

        #endregion

        #region Reference variables

        private Vector2 m_displace          = Vector2.zero;
        private float   m_distortAmount     = 0;

        private float   m_pauseFadeIn;

        private RenderTexture accumTex          = null;
        private RenderTexture pauseAccumTex     = null;

        public static CombinedEffect instance;

        #endregion

        #region Shader hash ids

        private int h_radialBlur        = Shader.PropertyToID("_RadialBlur");
        private int h_DF                = Shader.PropertyToID("_DF"); //Displace Fisheye;
        private int h_SCVB              = Shader.PropertyToID("_SCVB"); // Saturation, Chroma, Vignette, Blood
        private int h_noiseALB          = Shader.PropertyToID("_NoiseALB");
        private int h_fogPOD            = Shader.PropertyToID("_FogPOD");
        private int h_fogCol            = Shader.PropertyToID("_FogColor");
        private int h_accumTex          = Shader.PropertyToID("_AccumTex");
        private int h_MCOD              = Shader.PropertyToID("_MCOD"); //Motion blur, Contrast, Overlay, Distort
        private int h_overlayColor      = Shader.PropertyToID("_OverlayColor");
        private int h_overlayTexture    = Shader.PropertyToID("_Overlay");
        private int h_AFR               = Shader.PropertyToID("_AFR"); //Blur Amount, Focal Area, Range

        private int h_pauseFadeIn   = Shader.PropertyToID("_PauseFadeIn");
        private int h_pauseColor    = Shader.PropertyToID("_PauseColor");
        private int h_pauseTime     = Shader.PropertyToID("_UnscaledTime");
        private int h_pauseAccumTex = Shader.PropertyToID("_PauseAccumTex");

        private RenderTextureDescriptor m_desc;

        #endregion

        // Reconstruction filter for shutter speed simulation
        class ReconstructionFilter
        {
            #region Predefined constants

            // The maximum length of motion blur, given as a percentage
            // of the screen height. Larger values may introduce artifacts.
            const float kMaxBlurRadius = 5;

            #endregion

            #region Public methods

            public ReconstructionFilter()
            {
                Shader shader = Shader.Find("Hidden/Reconstruction");
                if (shader.isSupported && CheckTextureFormatSupport())
                {
                    _material = new Material(shader);
                    _material.hideFlags = HideFlags.DontSave;
                }

                // Use loop unrolling on Adreno GPUs to avoid shader issues.
                _unroll = SystemInfo.graphicsDeviceName.Contains("Adreno");
            }

            public void Release()
            {
                if (_material != null) DestroyImmediate(_material);
                _material = null;
            }

            public void ProcessImage(float shutterAngle, int sampleCount, RenderTexture source, RenderTexture destination)
            {
                // If the shader isn't supported, simply blit and return.
                if (_material == null)
                {
                    Graphics.Blit(source, destination);
                    return;
                }

                // Calculate the maximum blur radius in pixels.
                int maxBlurPixels = (int)(kMaxBlurRadius * source.height / 100);

                // Calculate the TileMax size.
                // It should be a multiple of 8 and larger than maxBlur.
                int tileSize = ((maxBlurPixels - 1) / 8 + 1) * 8;

                // 1st pass - Velocity/depth packing
                // Motion vectors are scaled by an empirical factor of 1.45.
                float velocityScale = shutterAngle / 360 * 1.45f;
                _material.SetFloat(h_velocityScale, velocityScale);
                _material.SetFloat(h_maxBlurRadius, maxBlurPixels);

                RenderTexture vbuffer = GetTemporaryRT(source, 1, _packedRTFormat);
                Graphics.Blit(null, vbuffer, _material, 0);

                // 2nd pass - 1/4 TileMax filter
                RenderTexture tile4 = GetTemporaryRT(source, 4, _vectorRTFormat);
                Graphics.Blit(vbuffer, tile4, _material, 1);

                // 3rd pass - 1/2 TileMax filter
                RenderTexture tile8 = GetTemporaryRT(source, 8, _vectorRTFormat);
                Graphics.Blit(tile4, tile8, _material, 2);
                ReleaseTemporaryRT(tile4);

                // 4th pass - Last TileMax filter (reduce to tileSize)
                Vector2 tileMaxOffs = Vector2.one * (tileSize / 8.0f - 1) * -0.5f;
                _material.SetVector(h_tileMaxOffs, tileMaxOffs);
                _material.SetInt(h_tileMaxLoop, tileSize / 8);

                RenderTexture tile = GetTemporaryRT(source, tileSize, _vectorRTFormat);
                Graphics.Blit(tile8, tile, _material, 3);
                ReleaseTemporaryRT(tile8);

                // 5th pass - NeighborMax filter
                RenderTexture neighborMax = GetTemporaryRT(source, tileSize, _vectorRTFormat);
                Graphics.Blit(tile, neighborMax, _material, 4);
                ReleaseTemporaryRT(tile);

                // 6th pass - Reconstruction pass
                _material.SetInt(h_loopCount, Mathf.Clamp(sampleCount / 2, 1, 64));
                _material.SetFloat(h_maxBlurRadius, maxBlurPixels);
                _material.SetTexture(h_neighborMaxTex, neighborMax);
                _material.SetTexture(h_velocityTex, vbuffer);
                Graphics.Blit(source, destination, _material, _unroll ? 6 : 5);

                // Cleaning up
                ReleaseTemporaryRT(vbuffer);
                ReleaseTemporaryRT(neighborMax);
            }

            #endregion

            #region Private members

            private int h_velocityScale     = Shader.PropertyToID("_VelocityScale");
            private int h_maxBlurRadius     = Shader.PropertyToID("_MaxBlurRadius");
            private int h_tileMaxOffs       = Shader.PropertyToID("_TileMaxOffs");
            private int h_tileMaxLoop       = Shader.PropertyToID("_TileMaxLoop");
            private int h_loopCount         = Shader.PropertyToID("_LoopCount");
            private int h_neighborMaxTex    = Shader.PropertyToID("_NeighborMaxTex");
            private int h_velocityTex       = Shader.PropertyToID("_VelocityTex");

            private Material _material;
            private bool     _unroll;

            // Texture format for storing 2D vectors.
            private RenderTextureFormat _vectorRTFormat = RenderTextureFormat.RGHalf;

            // Texture format for storing packed velocity/depth.
            private RenderTextureFormat _packedRTFormat = RenderTextureFormat.ARGB2101010;

            private bool CheckTextureFormatSupport()
            {
                // RGHalf is not supported = Can't use motion vectors.
                if (!SystemInfo.SupportsRenderTextureFormat(_vectorRTFormat))
                    return false;

                // If 2:10:10:10 isn't supported, use ARGB32 instead.
                if (!SystemInfo.SupportsRenderTextureFormat(_packedRTFormat))
                    _packedRTFormat = RenderTextureFormat.ARGB32;

                return true;
            }

            private RenderTexture GetTemporaryRT(Texture source, int divider, RenderTextureFormat format)
            {
                var w = source.width / divider;
                var h = source.height / divider;
                var rt = RenderTexture.GetTemporary(w, h, 0, format);
                rt.filterMode = FilterMode.Point;
                return rt;
            }

            private void ReleaseTemporaryRT(RenderTexture rt)
            {
                RenderTexture.ReleaseTemporary(rt);
            }

            #endregion
        }
        private ReconstructionFilter m_reconstructionFilter;

        void Awake()
        {
            instance = this;

            shader              = Shader.Find("Hidden/CombinedEffect");

            material.SetTexture(h_overlayTexture, overlayTexture);
            material.SetVector(h_radialBlur,   radialBlur);
            material.SetVector(h_SCVB,         new Vector4(saturation, chroma, vignette, blood) );
            material.SetVector(h_noiseALB,     new Vector3(noiseAmount, noiseLuminosity , brightness));

            m_reconstructionFilter = new ReconstructionFilter();
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;

           // if(GM.NotStatic)
           //     GM.NotStatic.OnPause += OnPauseGame;
        }
		void OnRenderImage(RenderTexture src, RenderTexture dest)
        {

            if(Application.isPlaying)
            {
                m_displace       = contrast > 0? Vector2.Lerp(m_displace.normalized, Random.insideUnitCircle.normalized, Time.unscaledDeltaTime * 20) * 0.0015f: Vector2.zero;
                contrast         = Mathf.Lerp(contrast, contrastTarget, Time.unscaledDeltaTime * 0.5f);
                m_distortAmount  = Mathf.Lerp(m_distortAmount, Time.timeScale < 1 && Application.isPlaying? distortAmount : 0, Time.unscaledDeltaTime * 5);
                brightness       = Mathf.MoveTowards(brightness, brightnessTarget, Time.unscaledDeltaTime * 0.25f);
            }

            //Pass 1 Parameters
            material.SetVector( h_radialBlur,   radialBlur);
            material.SetVector( h_SCVB,         new Vector4(saturation, chroma, vignette, blood));
            material.SetVector( h_noiseALB,     new Vector3(noiseAmount, noiseLuminosity, brightness));
            material.SetVector( h_fogPOD,       new Vector4(fogPow, fogDistance, fogFadeRadius));
            material.SetColor(  h_fogCol,       fogColor);

            //Pass 2 Parameters
            material.SetTexture(h_overlayTexture,    overlayTexture);
            material.SetVector( h_DF,                new Vector4(m_displace.x, m_displace.y, fishEyeIntensity.x, fishEyeIntensity.y));
            material.SetColor(  h_overlayColor,      overlayColor);
            material.SetVector( h_MCOD,              new Vector4(accumulation, -contrast, m_distortAmount, overlayIntensity));

            //Blits
            m_desc = src.descriptor;
            m_desc.width  = 640;
            m_desc.height = Mathf.FloorToInt(640 * ((float)src.descriptor.height / src.descriptor.width));
            RenderTexture rt  = RenderTexture.GetTemporary(m_desc);
            rt.filterMode = FilterMode.Point;

            Graphics.Blit(src, rt, material, 0); //Pass 0 and optional motion blur pass Graphics.Blit(MotionBlurPass(src, dest), rt, material, 0);

            RenderTexture rt2 = RenderTexture.GetTemporary(m_desc);
            rt2.filterMode = FilterMode.Point;

            Graphics.Blit(DofPass(rt, rt2), dest, material, 1); //Pass 2 and optional dof pass

            RenderTexture.ReleaseTemporary(rt);
            RenderTexture.ReleaseTemporary(rt2);

            FlashBangUpdate(dest);

           // if (Application.isPlaying && GM.NotStatic.pauseGame) PausePass(dest);
        }

        private RenderTexture MotionBlurPass(RenderTexture src, RenderTexture dest)
        {
            if (!Application.isPlaying) //GM.options.mblur <= 0 ||
                return src;

            if (m_reconstructionFilter == null)
                m_reconstructionFilter = new ReconstructionFilter();

            m_reconstructionFilter.ProcessImage(360, 8, src, dest); //GM.options.mblur

            return dest;
        }
        private RenderTexture DofPass(RenderTexture src, RenderTexture dest)
        {
            if (!depthOfFieldEnabled)
                return src;

            material.SetVector(h_AFR, new Vector4(blurAmount, focalArea, effectiveRange));
            Graphics.Blit(src, dest, material, 2);
            return dest;
        }
        private void          PausePass(RenderTexture dst)
        {
            if (pauseAccumTex == null || pauseAccumTex.texelSize != dst.texelSize)
            {
                DestroyImmediate(pauseAccumTex);
                pauseAccumTex           = new RenderTexture(dst.descriptor);
                pauseAccumTex.hideFlags = HideFlags.HideAndDontSave;
            }

            m_pauseFadeIn   = Mathf.MoveTowards(m_pauseFadeIn, 1, Time.unscaledDeltaTime * 2);

            material.SetFloat(h_pauseFadeIn,        m_pauseFadeIn);
            material.SetFloat(h_pauseTime,          Time.unscaledTime);
            material.SetColor(h_pauseColor,         pauseColor.Evaluate(Mathf.Repeat(Time.unscaledTime * 0.02f, 1)));
            material.SetTexture(h_pauseAccumTex,    pauseAccumTex);

            RenderTexture rt = RenderTexture.GetTemporary(dst.descriptor);
            Graphics.Blit(dst, rt);
            dst.DiscardContents();
            Graphics.Blit(rt, dst, material, 3);
            Graphics.Blit(dst, pauseAccumTex);
            RenderTexture.ReleaseTemporary(rt);
        }

        private void FlashBangUpdate(RenderTexture src)
        {
            //Refresh AccumTexture on source parameter change
            if (accumTex == null || accumTex.texelSize != src.texelSize)
            {
                DestroyImmediate(accumTex);
                accumTex            = new RenderTexture(src.descriptor);
                accumTex.hideFlags  = HideFlags.HideAndDontSave;
            }

            Graphics.Blit(src, accumTex);
            material.SetTexture(h_accumTex, accumTex);
        }

        public  void SetBrightness(float f) { brightnessTarget = f; }
        public  void SetContrastTarget(float f) { contrastTarget = f; }

        void OnPauseGame(bool b)
        {
            m_pauseFadeIn = 0;
        }
    }
}
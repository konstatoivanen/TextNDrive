Shader "Custom/LightCone" 
{
    Properties 
	{
        _Color("Color", Color) = (1,1,1,0)
        _Intensity("Intensity", Range(0, 10)) = 1.0
		_FadeIn("Fade In", Range(0,10)) = 0.5
		_FadeOut("Fade Out", Range(0,10)) = 2
		_InvFade("Depth Fade", Range(0.01,3.0)) = 1.0
    }
    SubShader 
	{
        Tags { "IgnoreProjector"="True" "Queue"="Transparent" "RenderType"="Transparent" }
		Blend		One One
		AlphaTest	Greater .01
		Lighting	Off
		ZWrite		Off
		Cull		Off

		Pass 
		{   
			CGPROGRAM
			#pragma  vertex vert
			#pragma  fragment frag
			#pragma  fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"

			sampler2D			_CameraDepthTexture;
			uniform float4		_Color;
            uniform float		_Intensity, _FadeIn, _FadeOut, _InvFade;

            struct VertexInput 
			{
                float4 vertex		: POSITION;
                float4 uv			: TEXCOORD0;
            };
            struct v2f 
			{
                float4 pos			: SV_POSITION;
                float4 uv			: TEXCOORD0;
                float3 vPos			: TEXCOORD2;
				float3 cPos			: TEXCOORD3;
				float4 projPos		: TEXCOORD4;
            };

            v2f    vert (VertexInput v) 
			{
                v2f o;
				o.uv		= v.uv;
				o.pos		= UnityObjectToClipPos(v.vertex);
				o.projPos	= ComputeScreenPos(o.pos);
				COMPUTE_EYEDEPTH(o.projPos.z);

				o.vPos		= v.vertex.xyz;
				o.cPos		= mul(unity_WorldToObject, fixed4(_WorldSpaceCameraPos.xyz, 1) ).xyz;

                return o;
            }
            fixed4 frag(v2f i) : COLOR 
			{
				fixed sceneZ	= LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
				fixed fade		= saturate(_InvFade * (sceneZ - i.projPos.z));

				fixed3 n	= normalize(fixed3(normalize(i.vPos.xy),-0.5));
				fixed3 p	= normalize(i.cPos);
				fixed  d	= clamp(length(i.cPos.xy), _FadeIn, _FadeOut);

                fixed3 c	= _Color * _Intensity * (pow(abs(dot(n, p)), 2) * pow(i.uv.y, 2) / d) * fade;

                return fixed4(c,1);
            }
            ENDCG
        }
    }
}

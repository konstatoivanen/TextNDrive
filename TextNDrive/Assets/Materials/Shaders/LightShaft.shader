Shader "Hidden/LightShaft"
{
	Properties
	{
		[NoScaleOffset] _SmokeTex("Dust Texture", 2D)		= "white" {}
		_Smoke("Strength, Tiling, WorldPos, Speed", Vector) = (1,1,1,1)

		_Color("Color", Color) = (0.5,0.5,0.5,0.5)

		_WidthS("Width Start", Range(0.0,1.0))		= 1.0
		_WidthE("width End", Range(0.0,1.0))		= 1.0
		_PowW("Width Fade Power", Range(0.01,20.0))	= 2 
		_PowH("Height Fade Power", Range(0.0,20.0)) = 1

		_InvFade("Soft Particle Factor", Range(0.0,3.0)) = 1.0

		_FadeIn("FadeIn Distance", Float) = 10
		_FadeOut("FadeOut Distance", Float) = 5
	}
		Category
		{
			Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
			Blend		SrcAlpha OneMinusSrcAlpha
			AlphaTest	Greater .01
			Cull		Back
			Lighting	Off 
			ZWrite		Off

			SubShader
			{
				Pass
				{
					CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#pragma fragmentoption ARB_precision_hint_fastest
					#pragma target 3.0
					#include "UnityCG.cginc"

					sampler2D	_CameraDepthTexture, _SmokeTex;
					fixed4		_Color, _Smoke;
					fixed		_WidthS, _WidthP, _PowW, _PowH, _FadeIn, _FadeOut, _InvFade;
					float4x4	_WorldToLocal;

					struct appdata
					{
						float4 vertex	: POSITION;
						float2 uv		: TEXCOORD0;
					};
					struct v2f
					{
						float2 uv		: TEXCOORD0;
						float4 vertex	: SV_POSITION;
						float3 worldPos : TEXCOORD1;
						fixed  fadeIO   : TEXCOORD2;
						fixed  width	: TEXCOORD3;
						float4 projPos	: TEXCOORD4;
					};

					v2f    vert(appdata v)
					{
						v2f o;
						o.vertex	= UnityObjectToClipPos(v.vertex);
						o.uv		= v.uv;
						o.worldPos	= mul(unity_ObjectToWorld, v.vertex).xyz * _Smoke.z + _Smoke.w * _Time.x;
						o.projPos	= ComputeScreenPos(o.vertex);
						COMPUTE_EYEDEPTH(o.projPos.z);

						//Local camera position & forward- & up vectors
						float3 cameraPosLocal	= mul(_WorldToLocal, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;

						//Fade Out When camera is close to local forward vector
						o.fadeIO	= distance(cameraPosLocal.xy, 0);
						o.fadeIO	= clamp(o.fadeIO, _FadeOut, _FadeIn) - _FadeOut;
						o.fadeIO	= saturate( o.fadeIO / (_FadeIn - _FadeOut) );

						//Lerp between default start width and startWidth when camera is facing local up vector
						o.width		= lerp(_WidthS, _WidthP, abs(dot(normalize(cameraPosLocal.xy), float2(0,1))));

						return o;
					}
					fixed4 frag(v2f i) : COLOR
					{
						fixed4	main		= _Color;
								main.rgb   *= exp(5.0f * main.a);

						//Cone Math q:-^D
						fixed w = pow( saturate(1 - (abs(i.uv.y - 0.5) * 2) / lerp(i.width, 1, i.uv.x)) , _PowW);
						fixed h = pow(1 - i.uv.x, _PowH);

						//Smoke Alpha
						fixed s = tex2D(_SmokeTex, i.uv * _Smoke.y + i.worldPos.xy).r + tex2D(_SmokeTex, i.uv * _Smoke.y + i.worldPos.yz).g + tex2D(_SmokeTex, i.uv * _Smoke.y + i.worldPos.zx).b;
							  s = saturate(lerp(1, s, _Smoke.x));

						//Soft Particle Fade
						float sceneZ	= LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
						fixed f			= saturate(_InvFade * (sceneZ - i.projPos.z));

						main.a *= w * h  * s * f;

						return main;
					}
					ENDCG
				}
			}
		}
}

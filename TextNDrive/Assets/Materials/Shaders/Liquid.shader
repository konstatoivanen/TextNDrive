Shader "Particles/Liquid" 
{
	Properties 
	{
		_MainTex("Tintmap", 2D) = "white" {}
		_Tint("Tint", Color) = (0.5,0.5,0.5,0.5)
		_V   ("FallOffPow, Alpha Contrast, Contrast Pos, WorldPosDelta", Vector) = (1,1,0.5,0)
		_V2  ("Gain, Tint Str, TimeStr, tiling", Vector) = (1,1,1,1)
	}

	Category 
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Blend		SrcAlpha OneMinusSrcAlpha
		AlphaTest	Greater .01
		Cull		Back
		Lighting	Off 
		ZWrite		Off
		Fog{ Mode Off }

		SubShader 
		{
			GrabPass 
			{							
				Name "BASE"
				Tags { "LightMode" = "Always" }
 			}
 		
			Pass 
			{
				Name "BASE"
				Tags { "LightMode" = "Always" }
			
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#pragma multi_compile_particles
				#include "UnityCG.cginc"

				CBUFFER_START(nonUpdatedVariables)
					sampler2D	_MainTex;
					float4		_MainTex_ST, _Tint, _V, _V2;
				CBUFFER_END

				struct appdata_t 
				{
					float4 vertex	: POSITION;
					float2 texcoord	: TEXCOORD0;
					float4 color	: COLOR;
				};

				struct v2f 
				{
					float4 vertex	: POSITION;
					float2 uv		: TEXCOORD1;
					float3 worldPos : TEXCOORD3;
					float4 color	: COLOR;
				};

				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex	= UnityObjectToClipPos(v.vertex);
					o.uv		= TRANSFORM_TEX( v.texcoord, _MainTex);
					o.color		= v.color;
					o.worldPos	= mul(unity_ObjectToWorld, v.vertex).xyz;
					return o;
				}

				half4 frag( v2f i ) : COLOR
				{
					half2 uv2	= (i.uv - 0.5) * 2;
					half  f		= saturate( 1 - pow(sqrt(uv2.x*uv2.x + uv2.y*uv2.y), _V.x ) );

					half2  os	= half2(_Time.x,-_Time.x) * _V2.z;
					half4  main = tex2D(_MainTex, i.uv + i.worldPos.yz * _V.w + os) * tex2D(_MainTex, i.uv + i.worldPos.xy * _V.w + os) * tex2D(_MainTex, i.uv * _V2.w + i.worldPos.xy * _V.w + os);
					main		= half4(lerp(main.rgb * _Tint.rgb, _Tint.rgb, _V2.y) * exp(5.0f * _V2.x), saturate(((main.a * f) - _V.z) * _V.y + _V.z) );

					return main *  i.color;
				}
			ENDCG
			}
		}
	}
}

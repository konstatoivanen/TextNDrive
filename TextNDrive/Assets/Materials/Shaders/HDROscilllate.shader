Shader "Unlit/HDR Oscillate" 
{
	Properties 
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_EmissionGain ("Emission Gain", Range(0, 1)) = 0.3
		_Freq ("Oscillation Rate", Float) = 1
		_Offset("Oscillation Offset", Float) = 0
	}

	Category 
	{
		Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque" }
		Blend		Off
		ColorMask	RGB
		Cull		Back 
		Lighting	Off 
		ZWrite		On
		Fog{ Mode Off }
	
		SubShader 
		{
			Pass 
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#pragma multi_compile_particles
				#include "UnityCG.cginc"

				sampler2D _MainTex;
				float4 _MainTex_ST, _TintColor;
				float  _EmissionGain, _Freq, _Offset;

				
				struct appdata_t 
				{
					float4 vertex	: POSITION;
					fixed4 color	: COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f 
				{
					float4 vertex	: POSITION;
					float2 texcoord : TEXCOORD0;
				};

				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.texcoord	= TRANSFORM_TEX(v.texcoord,_MainTex);
					return o;
				}

				fixed4 frag(v2f i) : COLOR
				{
					float4 col = 10.0f * _TintColor * tex2D(_MainTex, i.texcoord) * (exp(_EmissionGain * 5.0) * sin(_Freq * (_Time.x + _Offset)));
					col.a	= 1;
					col.rgb = max(0, col.rgb);
					return col;
				}
				ENDCG 
			}
		}	
	}
}

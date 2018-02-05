Shader "Custom/FogPlane" 
{
	Properties 
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_InvFade ("Fade Radius", Float) = 1.0
		_PFade("Height Fade Pow", Float) = 1.0
	}

	Category 
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend		SrcAlpha One
		AlphaTest	Greater .01
		ColorMask	RGB
		Cull		Back 
		Lighting	Off 
		ZWrite		Off 
		Fog{ Mode Off }
	
		SubShader 
		{
			Pass 
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_particles
				#include "UnityCG.cginc"

				fixed4		_TintColor;

				half2  depth2dist(float2 depth)
				{
					half2  ab = half2(_ProjectionParams.z / (_ProjectionParams.z - _ProjectionParams.y), _ProjectionParams.z * _ProjectionParams.y / (_ProjectionParams.y - _ProjectionParams.z)); // zFar / ( zFar - zNear ), zFar * zNear / ( zNear - zFar )				
					return ab.y / (1.0 - depth - ab.x);
				}
			
				struct appdata_t 
				{
					float4 vertex	: POSITION;
					fixed4 color	: COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f 
				{
					float4 vertex	: POSITION;
					fixed4 color	: COLOR;
					float2 texcoord : TEXCOORD0;
					#ifdef SOFTPARTICLES_ON
					float4 projPos	: TEXCOORD1;
					#endif
				};

				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					#ifdef SOFTPARTICLES_ON
					o.projPos = ComputeScreenPos (o.vertex);
					COMPUTE_EYEDEPTH(o.projPos.z);
					#endif

					o.color		= v.color * _TintColor;
					o.texcoord = v.texcoord;
					return o;
				}

				sampler2D _CameraDepthTexture;
				float _InvFade, _PFade;
			
				fixed4 frag (v2f i) : COLOR
				{
					#ifdef SOFTPARTICLES_ON
					float sceneZ	 = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos));
					float partZ		 = i.projPos.z;
					float2 sp		 = depth2dist(half2(sceneZ, partZ));
					float fade		 = saturate ((sp.x-sp.y) / _InvFade);
					i.color.a		*= fade;
					#endif

					i.color.a		*= pow(1 - i.texcoord.y, _PFade);
				
					return i.color;
				}
				ENDCG 
			}
		}	
	}
}

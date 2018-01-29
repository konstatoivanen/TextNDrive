Shader "Hidden/CombinedEffect" 
{
	Properties{ _MainTex("Base (RGB)", 2D) = "white" {} }

	CGINCLUDE
		#include "UnityCG.cginc"
		#pragma target 3.0

		uniform sampler2D	_MainTex, _CameraDepthTexture, _AccumTex, _PauseAccumTex, _Overlay, _Mask;
		uniform float4		_MainTex_TexelSize, _RadialBlur, _SCVB, _NoiseALB, _FogColor, _OverlayColor, _MCOD, _PauseColor, _DF;
		uniform float3		_FogPOD, _AFR; // Blur amount, Focal Area, Effective Range
		uniform float		_PauseFadeIn, _UnscaledTime;

		half   noise(float3 c) { return saturate(frac(sin(dot(c.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453)); }
		half   grayscale(half3 c) { return dot(c, half3(0.3, 0.59, 0.11)); }
		half   diagonalNoise(half2 p)
		{
			float a = atan2(p.y, p.x);
			float s = floor((abs(p.x) + abs(p.y)) * 100);
			s *= sin(s * 23.4035);
			float s2 = frac(sin(s));

			float c = step(cos(_UnscaledTime * 0.1) * 0.2 + 0.6, sin(a + s + s2 * _UnscaledTime * 0.25) * 0.5 + 0.5);

			c *= s2 * .7 + 0.3;
			return c;
		}

		half   depth2dist(float depth)
		{
			half2  ab = half2(_ProjectionParams.z / (_ProjectionParams.z - _ProjectionParams.y), _ProjectionParams.z * _ProjectionParams.y / (_ProjectionParams.y - _ProjectionParams.z)); // zFar / ( zFar - zNear ), zFar * zNear / ( zNear - zFar )				
			return ab.y / (1.0 - depth - ab.x);
		}
		half2  depth2dist(float2 depth)
		{
			half2  ab = half2(_ProjectionParams.z / (_ProjectionParams.z - _ProjectionParams.y), _ProjectionParams.z * _ProjectionParams.y / (_ProjectionParams.y - _ProjectionParams.z)); // zFar / ( zFar - zNear ), zFar * zNear / ( zNear - zFar )				
			return ab.y / (1.0 - depth - ab.x);
		}

		half2  radianDirection(half radian) { return normalize(half2(cos(radian), sin(radian))); }
		half2  blurDirection(int index, half length)
		{
			length = (index / length) * 6.28;
			return radianDirection(length);
		}

		struct v2f_offset
		{
			float4 pos		: SV_POSITION;
			float2 uv		: TEXCOORD0;
			float2 uvOffset	: TEXCOORD1;
		};

		v2f_offset	vert_offset(appdata_img v)
		{
			v2f_offset o;
			o.pos	= UnityObjectToClipPos(v.vertex);
			o.uv	= MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord);

			half3 rb	= _RadialBlur.xyz * _RadialBlur.w;
			half2 res	= half2(_ScreenParams.y / _ScreenParams.x, _ScreenParams.x / _ScreenParams.y);
			o.uvOffset	= (o.uv - 0.5) * (1.0 - rb.z) + 0.5 + rb.xy * res * 0.5;

			return o;
		}
		v2f_img		vert_simple(appdata_img v)
		{
			v2f_img o;
			o.pos	= UnityObjectToClipPos(v.vertex);
			o.uv	= v.texcoord;
			return o;
		}

		float4 frag_pass1	(v2f_offset i)	: SV_Target
		{
			//Character mask
			half4 mask	= 0;
			mask.a		= tex2D(_Mask, i.uv).b;
			mask.rgb	= tex2D(_MainTex, i.uv).rgb;

			//Initialize Base Color And store original uv
			half4 main = tex2D(_MainTex, i.uv);

			//Coords
			half coordDot  = length((i.uv - 0.5) * 2.0);

			//Chromatic Distortion
			half4 offset = half4(0.5, -0.5, -0.87, 1) * pow(coordDot * _SCVB.y, 3);
			main.r		+= abs(tex2D(_MainTex, i.uv + offset.xz).r			- main.r) * 16;
			main.g		+= abs(tex2D(_MainTex, i.uv + offset.yz).g			- main.g) * 16;
			main.b      += abs(tex2D(_MainTex, i.uv + half2(0, offset.w)).b - main.b) * 16;

			//Blur Layers
			for(int j = 1; j < 17; j++) main.rgb += tex2D(_MainTex, lerp(i.uv, i.uvOffset, j * 0.0625) );
			main.rgb /= 16;

			//Fog
			half  dist		= depth2dist(tex2D(_CameraDepthTexture, i.uv).r);
			float fogAmount = saturate(pow(1.0 - distance(dist, _FogPOD.y) / _FogPOD.z, _FogPOD.x) * _FogColor.a);
			main.rgb		= lerp(main.rgb, _FogColor.rgb, fogAmount);

			//Saturation
			main.rgb = lerp(grayscale(main.rgb), main.rgb, max(_SCVB.x, mask.a));

			//Very Simple Noise function
			half3 n		= pow((noise(_Time.xyz * i.uv.xyx) - 0.5) * 2, 2) * _NoiseALB.x;
			main.rgb   *= 1 + n * saturate(1 - length(main.rgb) * _NoiseALB.y); 


			main.rgb *= 1.0 - saturate(coordDot * _SCVB.z);				//Vignetting
			main.gb  *= 1.0 - saturate(coordDot * _SCVB.w * 2); 		//Blood

			main.rgb *= _NoiseALB.z;

			main.rgb = lerp(main.rgb, lerp(main.rgb, main.rgb * main.rgb, 0.5), mask.a);

			return main;
		}

		float4 frag_pass2	(v2f_img i)		: SV_Target
		{		
			half2 coords	= (i.uv - 0.5) * 2.0;

			half3  overlay  = tex2D(_Overlay, i.uv).rgb;
			half2  bump		= -coords * overlay.r * _MCOD.w;
			half2  over		= -coords * overlay.g * _MCOD.z * 0.5;

			coords = (1 - pow(coords.yx, 2)) * coords.xy * _DF.wz * (_ScreenParams.x / _ScreenParams.y);

			half2 uvr		= i.uv - coords + over + bump * 1.05;
			half2 uvg		= i.uv - coords + over + bump * 0.95;
			half2 uvb		= i.uv - coords + over + bump;

			uvr	= 2 * abs(round(0.5 * uvr) - 0.5 * uvr);
			uvg	= 2 * abs(round(0.5 * uvg) - 0.5 * uvg);
			uvb	= 2 * abs(round(0.5 * uvb) - 0.5 * uvb);

			half4 main = fixed4(tex2D(_MainTex, uvr).r, tex2D(_MainTex, uvg).g, tex2D(_MainTex, uvb).b,1);

			//Contrast Outline effect
			half4 contrast	= half4(tex2D(_MainTex, uvb + _DF.xy).rgb, _MCOD.y + overlay.r * 5);
			main.rgb		= lerp( lerp(main.rgb, grayscale(contrast) , 0.01 * contrast.w), contrast , contrast.w);

			//Flashbang
			main = lerp(main, tex2D(_AccumTex, i.uv), _MCOD.x );

			//Overlay
			main  += overlay.g * _MCOD.z * _OverlayColor;
			main.a = 1;

			return main;
		}

		float4 frag_dof		(v2f_img i)		: SV_Target
		{
			float4 main		= tex2D(_MainTex, i.uv);
			float4 mainBlur = main;

			for (int j = 0; j < 8; j++) mainBlur += tex2D(_MainTex, i.uv + blurDirection(j, 16.0) * 0.005  * _AFR.x);
			for (int k = 0; k < 8; k++) mainBlur += tex2D(_MainTex, i.uv + blurDirection(k, 16.0) * 0.01   * _AFR.x);

			mainBlur /= 17;

			half   z		= depth2dist(tex2D(_CameraDepthTexture, i.uv).r);
			       main		= lerp(main, mainBlur, saturate((z - _AFR.z) / _AFR.y));
				   main.a	= 1;
			return main;
		}

		float4 frag_pause	(v2f_img i)		: SV_Target //Diagonal Pause Effect
		{
			half2 uv	= (i.uv  - half2(0.75,0.5)) * 0.5 * half2(_ScreenParams.x / _ScreenParams.y, 1);
			float s		= sin(_UnscaledTime * 10.) * cos(_UnscaledTime * 180 + 32);
			float ss	= (s > .1 ? s * sin(floor(uv.y * 32) / 32) * 0.4 - 0.2 : 0) * 0.2;

			float4 main			= float4( diagonalNoise(half2(uv.x + ss * floor(uv.y * 16) / 16, uv.y)), diagonalNoise(uv + 0.002), diagonalNoise(uv), 1) * _PauseColor;
			float4 background	= tex2D(_MainTex, i.uv);
			float4 accum		= tex2D(_PauseAccumTex, i.uv);

			main   = lerp(main, accum, saturate(1.0 - _PauseFadeIn * 0.2));
			main  += saturate(reflect(main,background)) * 0.2;
			main   = lerp(main, background, step(_PauseFadeIn,1 - i.uv.x));

			return main;
		}

	ENDCG

	SubShader 
	{
		ZTest	Always
		Cull	Off
		ZWrite	Off

		Pass // Pass 0
		{
			Name "Pass 0"
			CGPROGRAM
			#pragma vertex		vert_offset
			#pragma fragment	frag_pass1
			ENDCG
		}

		Pass // Pass 1
		{
			Name "Pass 1"
			CGPROGRAM
			#pragma vertex		vert_simple
			#pragma fragment	frag_pass2
			ENDCG
		}

		Pass // Depth of Field Pass
		{
			Name "Depth Of Field Pass"
			CGPROGRAM
			#pragma vertex		vert_simple
			#pragma fragment	frag_dof
			ENDCG
		}

		Pass //Pause Pass
		{
			Name "Pause Pass"
			CGPROGRAM
			#pragma vertex		vert_simple
			#pragma fragment	frag_pause
			ENDCG
		}
	}
	Fallback off
}
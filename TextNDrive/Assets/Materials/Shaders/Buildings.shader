Shader "Custom/Buildings" 
{
	Properties 
	{
		[NoScaleOffset] _MainTex("Albedo", 2D) = "white" {}
		[NoScaleOffset] _EmissionTex("Window Mask", 2D) = "black" {}
		_ColorA("Albedo Color", Color) = (1,1,1,1)
		_Color ("Window Color", Color) = (1,1,1,1)
		_ColorGlow("Underglow Color", Color) = (0,0,0,0)
		_Glow("GLow: Height, Radius, Pow, WThreshold", Vector) = (0,0,0,0)
		_Window("Window: Width, Height, Gain, Time", Vector) = (0,0,0,0)
	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM

		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		sampler2D _MainTex, _EmissionTex;

		struct Input 
		{
			float2 uv_MainTex;
			float3 worldPos;
		};

		float random(float2 uv) 
		{
			return frac(sin(dot(uv,half2(12.9898, 78.233))) * 43758.5453123);
		}

		fixed4 _Color, _ColorGlow, _Glow, _ColorA, _Window;

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			clip((IN.worldPos.y - _Glow.x));

			//Window Noise
			half2 uv	= floor(IN.uv_MainTex * _Window.zy  + floor(IN.worldPos.x* 0.01));
			float3 w	= random(uv + _Time * 0.00001 * _Window.w);
				   w.x  = lerp(0, 1, saturate((w.x - _Glow.w) / (1 - _Glow.w)) );
			       w	= w.x * _Color * _Window.z;


			fixed4 c	= tex2D (_MainTex, IN.uv_MainTex) * _ColorA;
			fixed4 e    = tex2D(_EmissionTex, IN.uv_MainTex);
			fixed  g	= saturate(1 - (clamp(IN.worldPos.y - _Glow.x, 0, _Glow.y * 2) / _Glow.y));
				   g	= saturate(pow(g, _Glow.z));

			o.Albedo	= c.rgb;
			o.Emission  = e.rgb * w + g * c.rgb * _ColorGlow * 100;

			o.Metallic	 = c.r;
			o.Smoothness = c.r;

			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

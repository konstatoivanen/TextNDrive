Shader "UI/DepthMask"
 {
	Properties
	{
		[NoScaleOffset]_MainTex("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque" "IgnoreProjector" = "True" }

		Lighting Off
		Cull Back

		CGPROGRAM
		#pragma surface surf Lambert
		sampler2D _MainTex;
		struct Input { float2 uv_MainTex; };
		void surf(Input IN, inout SurfaceOutput o) { clip(tex2D(_MainTex, IN.uv_MainTex).a - 0.01); }
		ENDCG
	}
	Fallback "Unlit"
}

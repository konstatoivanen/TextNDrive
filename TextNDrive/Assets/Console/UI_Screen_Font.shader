Shader "UI/Screen Font" 
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		[NoScaleOffset] _ScanTex("Scanline Texture", 2D) = "white" {}
		_ScanIntensity("Scanline Intensity", Float) = 1.0
		_ScanTiling("Scanline Tiling", Float) = 1.0
		_ScanSpeed("Scaline Scroll Speed", Float) = 1.0
		_Gain("Color Gain", Float) = 1.0
		_Color("Tint", Color) = (0.5,0.5,0.5,0.5)
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent +100" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
		Blend		SrcAlpha OneMinusSrcAlpha
		Cull		Off 
		Lighting	Off 
		ZWrite		Off
		Fog{ Mode Off }
 
		Pass 
		{      
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
   
			sampler2D	_MainTex, _ScanTex;
			float4		_ScanTex_ST, _ConsoleColor;
			float		_Gain, _ScanIntensity, _ScanTiling, _ScanSpeed;
   
			struct appdata 
			{
                half4 vertex	: POSITION;
                half2 texcoord	: TEXCOORD0;
                half4 color		: COLOR0;
            };
            struct v2f 
			{

                half4 pos		: SV_POSITION;
                half2 uv		: TEXCOORD0;
				half2 screenPos : TEXCOORD1;
                half4 c			: COLOR0;
            };
 
            v2f vert (appdata v)
            {
                v2f o;
                o.pos		= UnityObjectToClipPos(v.vertex );
                o.uv		= v.texcoord; 
				o.screenPos = 0.5*(o.pos.xy + 1.0);
                o.c			= v.color * _ConsoleColor;
                return o;
            }
            half4 frag( v2f i ) : COLOR
            {
				fixed4 scan = tex2D(_ScanTex, float2(i.screenPos.x, i.screenPos.y * _ScanTiling + _Time.x * _ScanSpeed));
				fixed4 main = lerp(i.c, i.c * scan, _ScanIntensity);
				main.a	   *= tex2D(_MainTex, i.uv).a;
				main.rgb   *= _Gain;
				return main;
            }
				
            ENDCG
        }

	}
}

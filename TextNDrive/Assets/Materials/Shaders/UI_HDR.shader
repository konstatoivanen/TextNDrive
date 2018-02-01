Shader "UI/HDR"
{
	Properties
	{
		[NoScaleOffset]_MainTex("Texture", 2D) = "white" {}
		[NoScaleOffset]_EmissionTex("Emisission Map", 2D) = "black" {}
		_Gain("Gain", Float) = 1.0
		_Color("Color", Color) = (0.5,0.5,0.5,0.5)
		_Color2("Emission Color", Color) = (0,0,0,0)
	}

	SubShader
	{
		Tags{ "Queue" = "Geometry" "IgnoreProjector" = "True" "RenderType" = "Opaque" "PreviewType" = "Plane" }
		Blend		Off
		Cull		Off 
		Lighting	Off 
		ZWrite		On
		ZTest		Lequal
		Fog{ Mode Off }
 
		Pass 
		{   
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
   
			sampler2D	_MainTex, _EmissionTex;
			float4	    _Color, _Color2;
			float		_Gain;
   
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
                half4 c			: COLOR0;
            };
 
            v2f vert (appdata v)
            {
                v2f o;
                o.pos		= UnityObjectToClipPos(v.vertex );
                o.uv		= v.texcoord; 
                o.c			= v.color;
                return o;
            }
            half4 frag( v2f i ) : COLOR
            {
				fixed4 main = tex2D(_MainTex, i.uv) * i.c * _Color;
				fixed4  em  = tex2D(_EmissionTex, i.uv) * i.c * _Color2 * _Gain;

				clip(main.a - 0.01);

				return main + em;
            }
				
            ENDCG
        }

	}
}

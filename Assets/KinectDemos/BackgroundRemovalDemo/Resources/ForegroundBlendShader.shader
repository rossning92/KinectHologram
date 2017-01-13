Shader "Custom/ForegroundBlendShader" 
{
	Properties
	{
		_MainTex ("MainTex", 2D) = "white" {}
        _BodyTex ("Body (RGB)", 2D) = "white" {}
        _ColorTex ("Color (RGB)", 2D) = "white" {}
        _ColorFlipH ("Horizontal Flip", Int) = 0
        _ColorFlipV ("Vertical Flip", Int) = 1
	}

	SubShader 
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			//float4 _MainTex_ST;

			uniform sampler2D _BodyTex;
			uniform sampler2D _ColorTex;
			
			uniform int _ColorFlipH;
			uniform int _ColorFlipV;


			struct v2f 
			{
			   float4 pos : SV_POSITION;
			   float2 uv : TEXCOORD0;
			};

			v2f vert (appdata_base v)
			{
			   v2f o;
			   
			   o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
			   o.uv = v.texcoord;

			   return o;
			}

			half4 frag (v2f i) : COLOR
			{
				float2 c_uv;
				c_uv.x = _ColorFlipH ? 1 - i.uv.x : i.uv.x;
				c_uv.y = _ColorFlipV ? 1 - i.uv.y : i.uv.y;

				float4 clrBack = tex2D (_MainTex, i.uv);
				float4 clrFore = tex2D (_ColorTex, c_uv);

				float w = tex2D(_BodyTex, i.uv).w;
				float4 clrOut = clrBack * (1 - w) + clrFore * w;

				return clrOut;
			}

			ENDCG
		}
	}

	FallBack "Diffuse"
}

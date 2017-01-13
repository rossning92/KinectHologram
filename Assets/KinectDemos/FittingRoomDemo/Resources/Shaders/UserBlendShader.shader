Shader "Custom/UserBlendShader" 
{
	Properties
	{
		_MainTex ("MainTex", 2D) = "white" {}
        _ColorTex ("Color (RGB)", 2D) = "white" {}
        _Threshold ("Depth Threshold", Range(0, 0.5)) = 0.1
	}

	SubShader 
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
		
			CGPROGRAM
			#pragma target 5.0
			//#pragma enable_d3d11_debug_symbols

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			//float4 _MainTex_ST;
			sampler2D _CameraDepthTexture;

			uniform sampler2D _ColorTex;
			uniform float _Threshold;

			uniform float _ColorResX;
			uniform float _ColorResY;
			uniform float _DepthResX;
			uniform float _DepthResY;

			uniform float _ColorOfsX;
			uniform float _ColorMulX;
			uniform float _ColorOfsY;
			uniform float _ColorMulY;

			uniform float _DepthFactor;

			StructuredBuffer<float2> _DepthCoords;
			StructuredBuffer<float> _DepthBuffer;


			struct v2f 
			{
			   float4 pos : SV_POSITION;
			   float2 uv : TEXCOORD0;
			   float2 uv2 : TEXCOORD1;
			   float4 scrPos : TEXCOORD2;
			};

			v2f vert (appdata_base v)
			{
			   v2f o;
			   
			   o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
			   o.uv = v.texcoord;

			   o.uv2.x = o.uv.x;
			   o.uv2.y = 1 - o.uv.y;

			   o.scrPos = ComputeScreenPos(o.pos);

			   return o;
			}

			half4 frag (v2f i) : COLOR
			{
			    float camDepth = LinearEyeDepth (tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.scrPos)).r);
				//float camDepth01 = Linear01Depth (tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.scrPos)).r);

				float2 ctUv = float2(_ColorOfsX + i.uv.x * _ColorMulX, i.uv.y /**_ColorOfsY + i.uv.y * _ColorMulY*/);
				
				int cx = (int)(ctUv.x * _ColorResX);
				int cy = (int)(ctUv.y * _ColorResY);
				int ci = (int)(cx + cy * _ColorResX);

				if (!isinf(_DepthCoords[ci].x) && !isinf(_DepthCoords[ci].y))
				{
					int dx = (int)_DepthCoords[ci].x;
					int dy = (int)_DepthCoords[ci].y;
					int di = (int)(dx + dy * _DepthResX);

					//float di_length = _DepthResX * _DepthResY;
					//if(di >= 0 && di < di_length)
					{
						float kinDepth = _DepthBuffer[di] / 1000;
						kinDepth *= _DepthFactor;
					
						if(isinf(_DepthBuffer[di]) || kinDepth < 0.1 || kinDepth > 5.0 || camDepth <= (kinDepth + _Threshold))
						{
							return tex2D(_MainTex, i.uv);
						}
						else
						{
							return tex2D(_ColorTex, ctUv);
						}
					}
				}
				
				return camDepth < 5.0 ? tex2D(_MainTex, i.uv) : tex2D(_ColorTex, ctUv);
				//return tex2D(_MainTex, i.uv);
			}

			ENDCG
		}
	}

	FallBack "Diffuse"
}

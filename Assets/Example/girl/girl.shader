﻿Shader "Girl"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				sampler2D _MainTex;
				float4 _MainTex_ST;

				struct v2f {
					float4  pos : SV_POSITION;
					float2  uv : TEXCOORD0;
				};

				v2f vert(appdata_base v)
				{
					v2f o;
					o.pos = mul(UNITY_MATRIX_MVP,v.vertex);
					o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);
					return o;
				}

				float4 frag(v2f i) : COLOR
				{
					float4 texCol = tex2D(_MainTex,i.uv);
					float4 outp = texCol;
					return outp;
				}

			ENDCG
		}
	}
}

Shader "Hidden/s_under" 
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_blend ("toBlend", 2D) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform sampler2D _blend;

			float4 frag(v2f_img i) : COLOR
			{
				float4 c = tex2D(_MainTex, i.uv);
				float4 b = tex2D(_blend, i.uv);

				float4 o = (c * c.a) + (b * (1.0 - c.a));
				o *= c.a + b.a;
				return o;
			}
			ENDCG
		}
	}
}
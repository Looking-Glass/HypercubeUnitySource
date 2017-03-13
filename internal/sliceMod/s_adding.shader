Shader "Hidden/s_adding"
/*{
	Properties{
		_MainTex("Texture to blend", 2D) = "black" {}
	}
		SubShader{
		Tags{ "Queue" = "Transparent" }
		Pass{
		Blend SrcAlpha OneMinusSrcAlpha
		SetTexture[_MainTex]{ combine texture }
	}
	}
}
*/

{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_blend("toBlend", 2D) = "white" {}
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" }
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
				//b *= b.a; //take alpha into account?

				return c + b;
			}
			ENDCG
		}
	}
}
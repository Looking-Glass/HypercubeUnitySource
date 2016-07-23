
Shader "Hidden/softOverlap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_softPercent ("softPercent", Float) = 0 //what percentage of our z depth should be considered 'soft' on each side
	}
	SubShader
	{
		// No culling or depth
		//Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;
			float _softPercent;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);

				//if(_softPercent <= 0)   //this should not be used because we can count on our component being off if this is not needed
				//	return col;
		
				float d =  SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);

				//return 1-d; //uncomment this to show the raw depth

				float mask = 1;
				if (d < _softPercent)
					mask *= d / _softPercent; //this is the darkening of the slice near 0
				else if (d > 1 - _softPercent)
					mask *= 1 - ((d - (1-_softPercent))/_softPercent); //this is the darkening of the slice near 1 

					//return mask;
				return col * mask;

			}
			ENDCG
		}
	}
}

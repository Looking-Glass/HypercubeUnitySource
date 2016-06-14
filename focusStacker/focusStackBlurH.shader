
//this is the first post process step of the edge detection mask
//it performs strictly a horizontal blur, and nothing else

Shader "Hidden/focusStackBlur_H"
{
	Properties
	{
		_MainTex ("RawMask", 2D) = "white" {}
		_PixelSizeX ("Pixel Size X", Range (0, 1)) = .01
		_ExpansionStrength ("Expansion", Range (0, 2)) = .7
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _PixelSizeX;
			float _ExpansionStrength;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			//from https://vvvv.org/documentation/tutorial-effects-neighbouring-pixels
			float4 PSHorizontalBlur(v2f In): COLOR
			{
				float4 sum = 0;
				int weightSum = 0;

				//the weights of the neighbouring pixels
				int weights[15] = {1, 2, 3, 4, 5, 6, 7, 8, 7, 6, 5, 4, 3, 2, 1};

				//we are taking 15 samples
				for (int i = 0; i < 15; i++)
				{
					//7 to the left, self and 7 to the right
					float2 cord = float2(In.uv.x + _PixelSizeX * (i-7), In.uv.y);

					//the samples are weighed according to their relation to the current pixel
					sum += tex2D(_MainTex, cord) * weights[i];

					//while going through the loop we are summing up the weights
					weightSum += weights[i];
				}

				sum /= weightSum;
				return float4(sum.rgb, 1);
			}

			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = PSHorizontalBlur(i) * _ExpansionStrength;
				return col;
			}


			ENDCG
		}
	}
}

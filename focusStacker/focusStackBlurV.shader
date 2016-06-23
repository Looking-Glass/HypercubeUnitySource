
//this is the 2nd and final pass of the edge detection mask, that outputs to screen
//it is meant to take the output of the Horizontal blur, blur it vertically, and then apply the final blend

Shader "Hidden/focusStackBlur_V"
{
	Properties
	{
		_MainTex ("RawMask", 2D) = "white" {}
		_OriginalTex ("OriginalImage", 2D) = "white" {}
		_ExpansionStrength ("Expansion", Range (0, 2)) = .7
		_PixelSizeY ("Pixel Size Y", Range (0, 1)) = .01
		_OutMod ("Output Modifier", Range (0, 1)) = 1
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

			#pragma multi_compile ___ SHOWBLUR

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
			sampler2D _OriginalTex;
			float4 _MainTex_ST;
			float _PixelSizeY;
			float _ExpansionStrength;
			float _OutMod;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			//from https://vvvv.org/documentation/tutorial-effects-neighbouring-pixels
			float4 PSVerticalalBlur(v2f In): COLOR
			{
				float4 sum = 0;
				int weightSum = 0;

				//the weights of the neighbouring pixels
				int weights[15] = {1, 2, 3, 4, 5, 6, 7, 8, 7, 6, 5, 4, 3, 2, 1};

				//we are taking 15 samples
				for (int i = 0; i < 15; i++)
				{
					//7 upwards, self and 7 downwards
					float2 cord = float2(In.uv.x, In.uv.y + _PixelSizeY * (i-7));

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
				fixed4 col =  PSVerticalalBlur(i);

				col.rgb = clamp((col.r + col.g + col.b),0,1) ;//* _ExpansionStrength;

				#ifdef SHOWBLUR
				return col;
				#endif

				col.a = col.r; //allow a useful alpha, if desired

				return col * tex2D(_OriginalTex, i.uv) * _OutMod;
			}


			ENDCG
		}
	}
}

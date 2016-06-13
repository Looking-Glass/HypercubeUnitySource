Shader "Unlit/focusStacker"
{
	Properties
	{
		_MainTex ("RawMask", 2D) = "white" {}
		_OriginalTex ("OriginalImage", 2D) = "white" {}
	//	_SampleRange ("Sample Range", Range (.0001, .05)) = .002 //ideally this should be the pixel sizes
		_ExpansionStrength ("Expansion", Range (0, 2)) = .7
		_PixelSizeX ("Pixel Size X", Range (0, 1)) = .01
		_PixelSizeY ("Pixel Size Y", Range (0, 1)) = .01
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

			#pragma multi_compile ___  RANGE_SOFT
			#pragma multi_compile ___  RANGE_2
			#pragma multi_compile ___  RANGE_3
			#pragma multi_compile ___SHOWMASK

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
			float _PixelSizeX;
			float _PixelSizeY;
			float _ExpansionStrength;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}


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
				fixed4 col = tex2D(_MainTex, i.uv);

				col = PSHorizontalBlur(i) + PSVerticalalBlur(i);

				col.rgb = (col.r + col.g + col.b) * _ExpansionStrength;
				return col * tex2D(_OriginalTex, i.uv);


				///////

				//sample 8 values adjacent to the current pixel
				float rangeX = _PixelSizeX;
				float rangeY = _PixelSizeY;
				i.uv.x -= rangeX;
				i.uv.y -= rangeY;
				float3 neighbor = tex2D(_MainTex, i.uv).rgb; //upper left

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //top middle

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //top right

				i.uv.y += rangeY;
				neighbor += tex2D(_MainTex, i.uv).rgb; //right middle

				i.uv.x -= rangeX * 2;
				neighbor += tex2D(_MainTex, i.uv).rgb; //left middle

				i.uv.y += rangeY;
				neighbor += tex2D(_MainTex, i.uv).rgb; //lower left

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //lower middle

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //lower right		

				#ifdef  RANGE_2 
				//2nd sample: sample farther out, and add this too.
				rangeX += _PixelSizeX;
				rangeY += _PixelSizeY;
				i.uv.x -= rangeX;
				i.uv.y -= rangeY;
				neighbor = tex2D(_MainTex, i.uv).rgb; //upper left

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //top middle

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //top right

				i.uv.y += rangeY;
				neighbor += tex2D(_MainTex, i.uv).rgb; //right middle

				i.uv.x -= rangeX * 2;
				neighbor += tex2D(_MainTex, i.uv).rgb; //left middle

				i.uv.y += rangeY;
				neighbor += tex2D(_MainTex, i.uv).rgb; //lower left

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //lower middle

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //lower right	
				

				#ifdef  RANGE_3
				//3rd sample: sample farther out, and add this too.
				rangeX += _PixelSizeX;
				rangeY += _PixelSizeY;
				i.uv.x -= rangeX;
				i.uv.y -= rangeY;
				neighbor = tex2D(_MainTex, i.uv).rgb; //upper left

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //top middle

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //top right

				i.uv.y += rangeY;
				neighbor += tex2D(_MainTex, i.uv).rgb; //right middle

				i.uv.x -= rangeX * 2;
				neighbor += tex2D(_MainTex, i.uv).rgb; //left middle

				i.uv.y += rangeY;
				neighbor += tex2D(_MainTex, i.uv).rgb; //lower left

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //lower middle

				i.uv.x += rangeX;
				neighbor += tex2D(_MainTex, i.uv).rgb; //lower right				
				#endif
				#endif
			
				col.rgb += (neighbor.r + neighbor.g + neighbor.b) * _ExpansionStrength;

				
				#ifdef SHOWMASK
				return col;
				#endif

				return col;
			}





			ENDCG
		}
	}
}

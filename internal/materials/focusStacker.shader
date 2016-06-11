Shader "Unlit/focusStacker"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SampleRange ("Sample Range", Range (.0001, .05)) = .002
		_SampleContrastEffect ("Range Contrast Mod", Range (0, 1)) = .4 
		_Brightness ("Sample Offset", Range (-20, 20)) = 1.5
		_Contrast ("Sample Contrast", Range (0, 500)) = 10
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
			//	float2 screenPos:TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _diffTex;
			float4 _MainTex_ST;
			float _SampleRange;
			float _SampleContrastEffect;
			float _Brightness;
			float _Contrast;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
			//	o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				float3 minimum = col.rgb;
				float3 maximum = col.rgb;

				//sample 8 values near the current pixel
				i.uv -= _SampleRange;
				float4 current = tex2D(_MainTex, i.uv); //upper left
				float3 total = current.rgb;
				minimum = min(current, minimum);
				maximum = max(current, maximum);

				i.uv[0] += _SampleRange;
				current = tex2D(_MainTex, i.uv); //top middle
				total += current.rgb;
				minimum = min(current, minimum);
				maximum = max(current, maximum);

				i.uv[0] += _SampleRange;
				current = tex2D(_MainTex, i.uv); //top right
				total += current.rgb;
				minimum = min(current, minimum);
				maximum = max(current, maximum);

				i.uv[1] += _SampleRange;
				current = tex2D(_MainTex, i.uv); //right middle
				total += current.rgb;
				minimum = min(current, minimum);
				maximum = max(current, maximum);

				i.uv[0] -= _SampleRange * 2;
				current = tex2D(_MainTex, i.uv); //left middle
				total += current.rgb;
				minimum = min(current, minimum);
				maximum = max(current, maximum);

				i.uv[1] += _SampleRange;
				current = tex2D(_MainTex, i.uv); //lower left
				total += current.rgb;
				minimum = min(current, minimum);
				maximum = max(current, maximum);

				i.uv[0] += _SampleRange;
				current= tex2D(_MainTex, i.uv); //lower middle
				total += current.rgb;
				minimum = min(current, minimum);
				maximum = max(current, maximum);

				i.uv[0] += _SampleRange;
				current = tex2D(_MainTex, i.uv); //lower right
				total += current.rgb;
				minimum = min(current, minimum);
				maximum = max(current, maximum);

				float3 diff =  (total/8) - col.rgb; //the average - color
				

				//add the effect of high difference among the samples
				diff *=  (maximum - minimum) * _SampleContrastEffect;


				//contrast + brightness
				diff.rgb = ((diff.rgb - 0.5f) * max(_Contrast, 0)) + 0.5f;
				diff.rgb += _Brightness;

				//uncomment to view raw mask
				//float4 output = col;
				//output.rgb = diff;
				//return output;

				col.rgb *= diff;
				return col;

			}
			ENDCG
		}
	}
}

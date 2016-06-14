
//this shader can be used to highlight areas of focus (contrast)
//it is intended to be used with another shader that post processes the results into a transparency mask for focus slicing

Shader "Unlit/focusStackMask"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BufferTex ("Texture", 2D) = "white" {}
		_SampleRange ("Sample Range", Range (.0001, .05)) = .002
		//_SampleContrastEffect ("Range Contrast Mod", Range (0, 1)) = .4  //usually this is not helpful
		_EdgeSuppression ("Edge Suppression", Range (0, 2)) = .97
		_Brightness ("Sample Offset", Range (4, 4)) = 1.5
		_Contrast ("Sample Contrast", Range (0, 20)) = 3
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

			#pragma multi_compile ___  DISABLE
		
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
			sampler2D _diffBuffer;
			float4 _MainTex_ST;
			float _SampleRange;
			//float _SampleContrastEffect;
			float _EdgeSuppression;
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

				#ifdef DISABLE
				return col;
				#endif

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
				//float3  sampleDifferenceMod =  (maximum - minimum) * _SampleContrastEffect;
				//sampleDifferenceMod =  clamp(sampleDifferenceMod, 0, _EdgeSuppression);
				//diff += sampleDifferenceMod;

				//edge suppression.  The edges of a thing will most likely be the highest contrast, so do a high pass on them.
				//diff *= max(sign( _EdgeSuppression - sampleDifferenceMod), 0.0);  //if (diff > _EdgeSuppression) diff *= 0;  ...a hard clamping suppression
				diff -= smoothstep(_EdgeSuppression, _EdgeSuppression + .2, diff); //a softer suppression.  the value is the softness: lower is a harder clamp

				//contrast + brightness
				diff.rgb = ((diff.rgb - 0.5f) * max(_Contrast, 0)) + 0.5f;
				diff.rgb += _Brightness;


				fixed4 output = col;
				output.rgb = diff;
				return output; //output the mask
			}
			ENDCG
		}
	}
}

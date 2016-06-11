Shader "Unlit/focusStacker"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SampleRange ("Sample Range", Range (.0001, .05)) = .002
		_SampleContrastEffect ("Range Contrast Mod", Range (0, 1)) = .4 
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
				//float3 min = col.rgb;
				//float3 max = col.rgb;

				//sample 8 values near the current pixel
				i.uv -= _SampleRange;
				float4 total = tex2D(_MainTex, i.uv); //upper left
				i.uv[0] += _SampleRange;
				//min = min()

				total += tex2D(_MainTex, i.uv); //top middle
				i.uv[0] += _SampleRange;

				total += tex2D(_MainTex, i.uv); //top right
				i.uv[1] += _SampleRange;

				total += tex2D(_MainTex, i.uv); //right middle
				i.uv[0] -= _SampleRange * 2;

				total += tex2D(_MainTex, i.uv); //left middle
				i.uv[1] += _SampleRange;

				total += tex2D(_MainTex, i.uv); //lower left
				i.uv[0] += _SampleRange;

				total += tex2D(_MainTex, i.uv); //lower middle
				i.uv[0] += _SampleRange;

				total += tex2D(_MainTex, i.uv); //lower right

				float4 diff =  (total/8) - col; //the average - color
				
			//	diff.rgb = ((diff.rgb - 0.5f) * max(5, 0)) + 0.5f;
			//	diff.rgb +=1.95;
				diff.a = 1;
				return diff;

				col.rgb *= (diff.r + diff.g + diff.b) * 5;

				//col.rgb *= diff.rgb ;

				return col;

			//	_SampleContrastEffect

				return col;
			}
			ENDCG
		}
	}
}

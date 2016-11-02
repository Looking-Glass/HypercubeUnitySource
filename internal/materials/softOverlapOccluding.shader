Shader "Hidden/softOverlapOccluding" 
{
	Properties
	{
		_MainTex("Render Input", 2D) = "white" {}
	}
	SubShader
	{
		ZTest Always Cull Off ZWrite Off Fog{ Mode Off }
		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f 
			{
				float4 pos : POSITION;
				half2 uv : TEXCOORD0;
			};

			//Our Vertex Shader 
			v2f vert(appdata_img v) 
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				return o;
			}

			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;
			float _sliceCount; //slice count

			float _softPercent;
			float _overlap;

			float4 frag(v2f IN) : COLOR
			{

				//which slice is our current target pixel on?
				float fs = floor(IN.uv.y * _sliceCount);

				float sliceDepth = (1 / _sliceCount);

				//g is for softslicing later. It is the edge of the slice.
				float g = (fs / _sliceCount) + (sliceDepth); //our slice + a full slice, this aligns it with the way the other methods function

				//this is what squeezes the render down so it's taking the slcth pixel
				IN.uv.y = frac(IN.uv.y * _sliceCount);

				//get the color
				float4 c = tex2D(_MainTex, IN.uv.xy); 

				//get the depth
				float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, IN.uv.xy);
				if (UNITY_NEAR_CLIP_VALUE == -1) //OGL will use this.
				{
					d = (d * .5) + .5;  //map  -1 to 1   into  0 to 1
				}

				//here we map 0-1 perslice
				d = (g-d)  + (sliceDepth *  _overlap);  //offset the 'start' depth of the slice, plus offset by the overlap				
				d *=  _sliceCount   / (1 + _overlap + _overlap) ; //expand to account for the overlap
				
				//float n = saturate(gdist * (_sliceCount - (_NFOD.z * _sliceCount / 2))); //kyle's original
				//return d;

				//soft slicing--------------------------------------
				//if(_softPercent <= 0)   //this should not be used because we can count on our component being off if this is not needed
				//	return col;

				float mask = 1;

				if (d < _softPercent)
					mask *= d / _softPercent; //this is the darkening of the slice near 0 (near)
				else if (d > 1 - _softPercent)
					mask *= 1 - ((d - (1 - _softPercent)) / _softPercent); //this is the darkening of the slice near 1 (far)
				//return mask;
				//end soft slicing----------------------------------------

				float4 r = c * mask;//for some reason it's (slightly) faster to combine them before returning them.
				return r;
			}

			ENDCG
		}
	}
}
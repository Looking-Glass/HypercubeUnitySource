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
			float4 _NFOD; //x: near clip, y: far clip, z: overlap, w: depth curve
			float _sliceCount; //slice count

			float linearDepth(float depthSample)
			{
				float zLinear = DECODE_EYEDEPTH(depthSample); //nice that there's a built in macro for it, after i did so much math learning -.-
				zLinear = (zLinear - _NFOD.x) / (_NFOD.y - _NFOD.x); //normalize it between the near-far planes
				return zLinear;
			}

			float4 frag(v2f IN) : COLOR
			{

				//which slice are we on?
				float fs = floor(IN.uv.y * _sliceCount);

				//g is for softslicing later. steps through each slice, defining a midpoint to measure from.
				float g = floor(IN.uv.y * _sliceCount) / _sliceCount + 1 / (_sliceCount * 2);

				//this is what folds the render down so it's taking the slcth pixel
				IN.uv.y = frac(IN.uv.y * _sliceCount);

				//get the color
				float4 c = tex2D(_MainTex, IN.uv.xy);

				//get the depth
				float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, IN.uv.xy);
				if (UNITY_NEAR_CLIP_VALUE == -1) //OGL will use this.
				{
					d = (d * .5) + .5;  //map  -1 to 1   into  0 to 1
				}

				//depth curve
				float ld = -(sign(_NFOD.x) - 1) * d + sign(_NFOD.x) * linearDepth(d); //conditional: use d if ortho, linear(d) if persp. sign of 0, -1, *-1 = 1, so if cam=ortho & near=0, just use d. otherwise, use linear(d)
				d = lerp(ld, d, _NFOD.w); //now lerp between linear depth and not.

										  //depth distance / soft slicing / the whole point of this thing
				float n = pow(saturate(1 - abs(g - d) * (_sliceCount - _NFOD.z * _sliceCount / 2)), 0.5); //abs(g-d) is "dist" of d from g(midpoint in slice). *10 so if it's <1/10th screen away, it = 1, depending on overlap (could be 1/5th screen away with overlap = 1) 1- that because we want it 1 at the closest and 0 if its too far. Then ^0.5 because for some god awful reason it wasn't linear and now it is?

				float4 r = c * n; //for some reason it's (slightly) faster to combine them before returning them.

				return r;
			}

			ENDCG
		}
	}
}
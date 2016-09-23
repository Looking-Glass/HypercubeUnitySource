Shader "Hypercube/Unlit Cutout"
{
    Properties {
        _MainTex ("Base (RGB) Transparency (A)", 2D) = "" {}
		_Color("Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha cutoff", Range (0,1)) = 0.5
		[Toggle(ENABLE_SOFTSLICING)] _softSlicingToggle ("Soft Sliced", Float) = 1
		[MaterialEnum(Off,0,Front,1,Back,2)] _Cull ("Cull", Int) = 2
    }
    SubShader {

	Cull [_Cull]

        Pass 
		{
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}

		CGPROGRAM
			#pragma vertex vert vertex:vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			#pragma shader_feature ENABLE_SOFTSLICING
			#pragma multi_compile __ SOFT_SLICING 

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				float4 color : COLOR;
			};
			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				float4 vertexColor : COLOR;
				float4 projPos : TEXCOORD1; //Screen position of pos
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			fixed _Cutoff;

			sampler2D _CameraDepthTexture;
			float _softPercent;
			half4 _blackPoint;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.vertexColor = v.color;
				o.projPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
			fixed4 col = tex2D(_MainTex, i.texcoord) * _Color * i.vertexColor;
			col += _blackPoint;

#if defined(SOFT_SLICING) && defined(ENABLE_SOFTSLICING)
				float d = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)).r;
				//return d; //uncomment this to show the raw depth

				//note: if _softPercent == 0  that is the same as hard slice.

				float mask = 1;	
									
				if (d < _softPercent)
					mask *= d / _softPercent; //this is the darkening of the slice near 0 (near)
				else if (d > 1 - _softPercent)
					mask *= 1 - ((d - (1-_softPercent))/_softPercent); //this is the darkening of the slice near 1 (far)

				//return mask;
				col.rgb *= mask;  //multiply mask after everything because _blackPoint must be included in there or we will get 'hardness' from non-black blackpoints		
#endif

				clip(col.a - _Cutoff);
				return col ;
			}
		ENDCG
        }

						// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}
    }

	Fallback "Standard"
}

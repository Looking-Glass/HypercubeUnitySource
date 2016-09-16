Shader "Hypercube/Lit" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		[Toggle(ENABLE_SOFTSLICING)] _softSlicingToggle ("Soft Sliced", Float) = 1
		 [MaterialEnum(Off,0,Front,1,Back,2)] _Cull ("Cull", Int) = 2
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull [_Cull]
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:depthVert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#include "UnityCG.cginc"

		#pragma shader_feature ENABLE_SOFTSLICING 
		#pragma multi_compile __ SOFT_SLICING 

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float4 screenPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		sampler2D _CameraDepthTexture;
		float _softPercent;
		half4 _blackPoint;

		void depthVert (inout appdata_full v, out Input data) 
		{
			data.screenPos = ComputeScreenPos(v.vertex);
			  UNITY_INITIALIZE_OUTPUT(Input,data);
		//	  float pos = length(UnityObjectToViewPos(v.vertex).xyz);
		//	  float diff = unity_FogEnd.x - unity_FogStart.x;
		//	  float invDiff = 1.0f / diff;
		//	  data.depth = clamp ((unity_FogEnd.x - pos) * invDiff, 0.0, 1.0);
	 }

		void surf (Input IN, inout SurfaceOutputStandard o)
		 {
			// Albedo comes from a texture tinted by color
			fixed4 c = (tex2D (_MainTex, IN.uv_MainTex) * _Color) + _blackPoint;

	#if defined(SOFT_SLICING) && defined(ENABLE_SOFTSLICING)
				//float d = Linear01Depth (tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)).r);
				float d = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)).r;
				//float d =  IN.depth;
				//return d; //uncomment this to show the raw depth

				//note: if _softPercent == 0  that is the same as hard slice.

				float mask = 1;	
									
				if (d < _softPercent)
					mask *= d / _softPercent; //this is the darkening of the slice near 0 (near)
				else if (d > 1 - _softPercent)
					mask *= 1 - ((d - (1-_softPercent))/_softPercent); //this is the darkening of the slice near 1 (far)

				//return mask;

				c *= mask;  //multiply mask after everything because _blackPoint must be included in the color or we will get 'hardness' from non-black blackpoints	
#endif

			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG


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
	FallBack "Diffuse"
}

Shader "Hypercube/Unlit Cutout"
{
    Properties {
        _MainTex ("Base (RGB) Transparency (A)", 2D) = "" {}
		_Color("Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha cutoff", Range (0,1)) = 0.5
		//[MaterialEnum(Off,0,On,1)] _useLighting ("Lighting", Int) = 1
		[MaterialEnum(Off,0,Front,1,Back,2)] _Cull ("Cull", Int) = 2
    }
    SubShader {

	Tags { "RenderType"="TransparentCutout" }
	Cull [_Cull]

        Pass 
		{

            AlphaTest Greater [_Cutoff]
			Lighting Off
            Material 
			{
                Diffuse [_Color]
				Ambient [_Color]
            }
            
            SetTexture [_MainTex] { combine texture * primary }
        }

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

	Fallback "Diffuse"
}

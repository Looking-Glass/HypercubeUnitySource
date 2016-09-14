Shader "Hypercube/Unlit"
{
    Properties
    {
		_Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Mod ("Brightness Mod", Range (0, 100)) = 1
        [MaterialEnum(Off,0,Front,1,Back,2)] _Cull ("Cull", Int) = 2
		//_softPercent ("softPercent", Range (0, .5)) = 0 //what percentage of our z depth should be considered 'soft' on each side
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  }
        Cull [_Cull]
        ZWrite On
 
        Pass
        {    
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
         
            #include "UnityCG.cginc"        

			#pragma multi_compile __ SOFT_SLICING
 
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				
            };
 
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float4 projPos : TEXCOORD1; //Screen position of pos
            };
 
            sampler2D _MainTex;
            float4 _MainTex_ST;
			fixed4 _Color;
            float _Mod;
			float _softPercent;
			sampler2D _CameraDepthTexture;
         
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.projPos = ComputeScreenPos(o.vertex);
                return o;
            }
         
            fixed4 frag (v2f i) : SV_Target
            {

	#ifdef SOFT_SLICING
	//soft slicing--------------------------------------
				//if(_softPercent <= 0)   //this should not be used because we can count on our shader being optimized away if this is not needed
				//float d = Linear01Depth (tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)).r);
				float d = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)).r;
				//return d; //uncomment this to show the raw depth

				//if(_softPercent <= 0)   //this should not be used because we can count on our component being off if this is not needed
				//	return col;

				float mask = 1;	
									
				if (d < _softPercent)
					mask *= d / _softPercent; //this is the darkening of the slice near 0 (near)
				else if (d > 1 - _softPercent)
					mask *= 1 - ((d - (1-_softPercent))/_softPercent); //this is the darkening of the slice near 1 (far)

				//return mask;
				_Mod *= mask;
//end soft slicing----------------------------------------
#endif

                return tex2D(_MainTex, i.uv) * _Color * _Mod;

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
	Fallback "Diffuse"
}

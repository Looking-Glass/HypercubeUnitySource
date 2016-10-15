Shader "Hypercube/Additive"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Texture", 2D) = "white" {}
		[Toggle(ENABLE_SOFTSLICING)] _softSlicingToggle("Soft Sliced", Float) = 1
		[MaterialEnum(Off,0,Front,1,Back,2)] _Cull("Cull", Int) = 2
		_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	}
		SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Cull[_Cull]
		AlphaTest Greater .01
		ColorMask RGB
		Lighting Off ZWrite Off
		Blend SrcAlpha One

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_particles
#pragma multi_compile_fog

#include "UnityCG.cginc"        

#pragma shader_feature ENABLE_SOFTSLICING
#pragma multi_compile __ SOFT_SLICING

		struct appdata
	{
		float4 vertex : POSITION;
		fixed4 color : COLOR;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		fixed4 color : COLOR;
		float2 uv : TEXCOORD0;
		UNITY_FOG_COORDS(1)
#ifdef SOFTPARTICLES_ON
			float4 projPos : TEXCOORD2;
#endif
		float projPosZ : TEXCOORD3; //Screen Z position
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	fixed4 _Color;

	float _softPercent;
	half4 _blackPoint;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
#ifdef SOFTPARTICLES_ON
		o.projPos = ComputeScreenPos(o.vertex);
		COMPUTE_EYEDEPTH(o.projPos.z);
#endif
		o.color = v.color;
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		UNITY_TRANSFER_FOG(o,o.vertex);
		o.projPosZ = o.vertex.z;
		return o;
	}

	sampler2D_float _CameraDepthTexture;
	float _InvFade;

	fixed4 frag(v2f i) : SV_Target
	{
#ifdef SOFTPARTICLES_ON
	float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
	float partZ = i.projPos.z;
	float fade = saturate(_InvFade * (sceneZ - partZ));
	i.color.a *= fade;
#endif

	fixed4 col = 2.0f * i.color * _Color * tex2D(_MainTex, i.uv);
	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0)); // fog towards black due to our blend mode

	//col += _blackPoint; //blackpoint should not be used on particles, as it will likely cause artifacts.

#if defined(SOFT_SLICING) && defined(ENABLE_SOFTSLICING)
	float d = i.projPosZ;

	if (UNITY_NEAR_CLIP_VALUE == -1) //OGL will use this.
	{
		d = (d * .5) + .5;  //map  -1 to 1   into  0 to 1
	}

	//return d; //uncomment this to show the raw depth

	//note: if _softPercent == 0  that is the same as hard slice.

	float mask = 1;

	if (d < _softPercent)
		mask *= d / _softPercent; //this is the darkening of the slice near 0 (near)
	else if (d > 1 - _softPercent)
		mask *= 1 - ((d - (1 - _softPercent)) / _softPercent); //this is the darkening of the slice near 1 (far)

															   //return mask;
	return col * mask;  //multiply mask after everything because _blackPoint must be included in there or we will get 'hardness' from non-black blackpoints		
#endif
	return col;
	}
		ENDCG
	}

		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass{
		Name "ShadowCaster"
		Tags{ "LightMode" = "ShadowCaster" }

		ZWrite On
		ZTest LEqual

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

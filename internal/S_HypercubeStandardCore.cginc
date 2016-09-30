// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

#ifndef UNITY_STANDARD_CORE_INCLUDED
#define UNITY_STANDARD_CORE_INCLUDED

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "S_HypercubeStandardInput.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityStandardBRDF.cginc"

#include "AutoLight.cginc"


float _softPercent;
half4 _blackPoint;
half _Shininess;


//-------------------------------------------------------------------------------------
// counterpart for NormalizePerPixelNormal
// skips normalization per-vertex and expects normalization to happen per-pixel
half3 NormalizePerVertexNormal (half3 n)
{
	#if (SHADER_TARGET < 30)
		return normalize(n);
	#else
		return n; // will normalize per-pixel instead
	#endif
}

half3 NormalizePerPixelNormal (half3 n)
{
	#if (SHADER_TARGET < 30)
		return n;
	#else
		return normalize(n);
	#endif
}

//-------------------------------------------------------------------------------------
UnityLight MainLight (half3 normalWorld)
{
	UnityLight l;
	#ifdef LIGHTMAP_OFF
		
		l.color = _LightColor0.rgb;
		l.dir = _WorldSpaceLightPos0.xyz;
		l.ndotl = LambertTerm (normalWorld, l.dir);
	#else
		// no light specified by the engine
		// analytical light might be extracted from Lightmap data later on in the shader depending on the Lightmap type
		l.color = half3(0.f, 0.f, 0.f);
		l.ndotl  = 0.f;
		l.dir = half3(0.f, 0.f, 0.f);
	#endif

	return l;
}

UnityLight AdditiveLight (half3 normalWorld, half3 lightDir, half atten)
{
	UnityLight l;

	l.color = _LightColor0.rgb;
	l.dir = lightDir;
	#ifndef USING_DIRECTIONAL_LIGHT
		l.dir = NormalizePerPixelNormal(l.dir);
	#endif
	l.ndotl = LambertTerm (normalWorld, l.dir);

	// shadow the light
	l.color *= atten;
	return l;
}

UnityLight DummyLight (half3 normalWorld)
{
	UnityLight l;
	l.color = 0;
	l.dir = half3 (0,1,0);
	l.ndotl = LambertTerm (normalWorld, l.dir);
	return l;
}

UnityIndirect ZeroIndirect ()
{
	UnityIndirect ind;
	ind.diffuse = 0;
	ind.specular = 0;
	return ind;
}

//-------------------------------------------------------------------------------------
// Common fragment setup
half3 WorldNormal(half4 tan2world[3])
{
	return normalize(tan2world[2].xyz);
}

#ifdef _TANGENT_TO_WORLD
	half3x3 ExtractTangentToWorldPerPixel(half4 tan2world[3])
	{
		half3 t = tan2world[0].xyz;
		half3 b = tan2world[1].xyz;
		half3 n = tan2world[2].xyz;

	#if UNITY_TANGENT_ORTHONORMALIZE
		n = NormalizePerPixelNormal(n);

		// ortho-normalize Tangent
		t = normalize (t - n * dot(t, n));

		// recalculate Binormal
		half3 newB = cross(n, t);
		b = newB * sign (dot (newB, b));
	#endif

		return half3x3(t, b, n);
	}
#else
	half3x3 ExtractTangentToWorldPerPixel(half4 tan2world[3])
	{
		return half3x3(0,0,0,0,0,0,0,0,0);
	}
#endif

#ifdef _PARALLAXMAP
	#define IN_VIEWDIR4PARALLAX(i) NormalizePerPixelNormal(half3(i.tangentToWorldAndParallax[0].w,i.tangentToWorldAndParallax[1].w,i.tangentToWorldAndParallax[2].w))
	#define IN_VIEWDIR4PARALLAX_FWDADD(i) NormalizePerPixelNormal(i.viewDirForParallax.xyz)
#else
	#define IN_VIEWDIR4PARALLAX(i) half3(0,0,0)
	#define IN_VIEWDIR4PARALLAX_FWDADD(i) half3(0,0,0)
#endif

#if UNITY_SPECCUBE_BOX_PROJECTION
	#define IN_WORLDPOS(i) i.posWorld
#else
	#define IN_WORLDPOS(i) half3(0,0,0)
#endif

#define IN_LIGHTDIR_FWDADD(i) half3(i.tangentToWorldAndLightDir[0].w, i.tangentToWorldAndLightDir[1].w, i.tangentToWorldAndLightDir[2].w)

#define FRAGMENT_SETUP(x) FragmentCommonData x = \
	FragmentSetup(i.tex, i.eyeVec, WorldNormal(i.tangentToWorldAndParallax), IN_VIEWDIR4PARALLAX(i), ExtractTangentToWorldPerPixel(i.tangentToWorldAndParallax), IN_WORLDPOS(i));

#define FRAGMENT_SETUP_FWDADD(x) FragmentCommonData x = \
	FragmentSetup(i.tex, i.eyeVec, WorldNormal(i.tangentToWorldAndLightDir), IN_VIEWDIR4PARALLAX_FWDADD(i), ExtractTangentToWorldPerPixel(i.tangentToWorldAndLightDir), half3(0,0,0));

struct FragmentCommonData
{
	half3 diffColor, specColor;
	// Note: oneMinusRoughness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
	// Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
	half oneMinusReflectivity, oneMinusRoughness;
	half3 normalWorld, eyeVec, posWorld;
	half alpha;
};

inline FragmentCommonData MetallicSetup (float4 i_tex)
{
	half2 metallicGloss = MetallicGloss(i_tex.xy);
	half metallic = metallicGloss.x;
	half oneMinusRoughness = metallicGloss.y;

	half oneMinusReflectivity;
	half3 specColor;
	half3 diffColor = DiffuseAndSpecularFromMetallic (Albedo(i_tex), metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);
	specColor = Specular(i_tex);

	FragmentCommonData o = (FragmentCommonData)0;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.oneMinusRoughness = oneMinusRoughness;
	return o;
} 

inline FragmentCommonData FragmentSetup (float4 i_tex, half3 i_eyeVec, half3 i_normalWorld, half3 i_viewDirForParallax, half3x3 i_tanToWorld, half3 i_posWorld)
{
	i_tex = Parallax(i_tex, i_viewDirForParallax);

	half alpha = Alpha(i_tex.xy);
	#if defined(_ALPHATEST_ON)
		clip (alpha - _Cutoff);
	#endif

	#ifdef _NORMALMAP
		half3 normalWorld = NormalizePerPixelNormal(mul(NormalInTangentSpace(i_tex), i_tanToWorld)); // @TODO: see if we can squeeze this normalize on SM2.0 as well
	#else
		// Should get compiled out, isn't being used in the end.
	 	half3 normalWorld = i_normalWorld;
	#endif
	
	half3 eyeVec = i_eyeVec;
	eyeVec = NormalizePerPixelNormal(eyeVec);


	FragmentCommonData o = UNITY_SETUP_BRDF_INPUT (i_tex);
	o.normalWorld = normalWorld;
	o.eyeVec = eyeVec;
	o.posWorld = i_posWorld;

	// NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	o.diffColor = PreMultiplyAlpha (o.diffColor, alpha, o.oneMinusReflectivity, /*out*/ o.alpha);
	return o;
}

inline UnityGI FragmentGI (
	float3 posWorld, 
	half occlusion, half4 i_ambientOrLightmapUV, half atten, half oneMinusRoughness, half3 normalWorld, half3 eyeVec,
	UnityLight light)
{
	UnityGIInput d;
	d.light = light;
	d.worldPos = posWorld;
	d.worldViewDir = -eyeVec;
	d.atten = atten;
	#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
		d.ambient = 0;
		d.lightmapUV = i_ambientOrLightmapUV;
	#else
		d.ambient = i_ambientOrLightmapUV.rgb;
		d.lightmapUV = 0;
	#endif
	
	{
		UnityGI o_gi;
		UNITY_INITIALIZE_OUTPUT(UnityGI, o_gi);

		#if UNITY_SHOULD_SAMPLE_SH
			#if UNITY_SAMPLE_FULL_SH_PER_PIXEL
				half3 sh = ShadeSH9(half4(normalWorld, 1.0));
			#elif (SHADER_TARGET >= 30)
				half3 sh = d.ambient + ShadeSH12Order(half4(normalWorld, 1.0));
			#else
				half3 sh = d.ambient;
			#endif
		
			o_gi.indirect.diffuse += sh;
		#endif

		#if !defined(LIGHTMAP_ON)
			o_gi.light = d.light;
			o_gi.light.color *= d.atten;

		#else
			// Baked lightmaps
			fixed4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, d.lightmapUV.xy); 
			half3 bakedColor = DecodeLightmap(bakedColorTex);
			
			#ifdef DIRLIGHTMAP_OFF
				o_gi.indirect.diffuse = bakedColor;

				#ifdef SHADOWS_SCREEN
					half3 shadowLightmapColor = bakedColorTex.rgb * d.atten;
					half3 darkerColor = min(o_gi.indirect.diffuse, shadowLightmapColor);
					o_gi.indirect.diffuse = max(darkerColor, o_gi.indirect.diffuse * d.atten);
				#endif // SHADOWS_SCREEN

			#elif DIRLIGHTMAP_COMBINED
				fixed4 bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, d.lightmapUV.xy);
				o_gi.indirect.diffuse = DecodeDirectionalLightmap (bakedColor, bakedDirTex, normalWorld);

				#ifdef SHADOWS_SCREEN
					half3 shadowLightmapColor = bakedColorTex.rgb * d.atten;
					half3 darkerColor = min(o_gi.indirect.diffuse, shadowLightmapColor);
					o_gi.indirect.diffuse = max(darkerColor, o_gi.indirect.diffuse * d.atten);
				#endif // SHADOWS_SCREEN

			#elif DIRLIGHTMAP_SEPARATE
				// Left halves of both intensity and direction lightmaps store direct light; right halves - indirect.

				// Direct
				fixed4 bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd, unity_Lightmap, d.lightmapUV.xy);
				o_gi.indirect.diffuse += DecodeDirectionalSpecularLightmap (bakedColor, bakedDirTex, normalWorld, false, 0, o_gi.light);

				// Indirect
				half2 uvIndirect = d.lightmapUV.xy + half2(0.5, 0);
				bakedColor = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, uvIndirect));
				bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd, unity_Lightmap, uvIndirect);
				o_gi.indirect.diffuse += DecodeDirectionalSpecularLightmap (bakedColor, bakedDirTex, normalWorld, false, 0, o_gi.light2);
			#endif
		#endif
		
		#ifdef DYNAMICLIGHTMAP_ON
			// Dynamic lightmaps
			fixed4 realtimeColorTex = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, d.lightmapUV.zw);
			half3 realtimeColor = DecodeRealtimeLightmap (realtimeColorTex);

			#ifdef DIRLIGHTMAP_OFF
				o_gi.indirect.diffuse += realtimeColor;

			#elif DIRLIGHTMAP_COMBINED
				half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, d.lightmapUV.zw);
				o_gi.indirect.diffuse += DecodeDirectionalLightmap (realtimeColor, realtimeDirTex, normalWorld);

			#elif DIRLIGHTMAP_SEPARATE
				half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, d.lightmapUV.zw);
				half4 realtimeNormalTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicNormal, unity_DynamicLightmap, d.lightmapUV.zw);
				o_gi.indirect.diffuse += DecodeDirectionalSpecularLightmap (realtimeColor, realtimeDirTex, normalWorld, true, realtimeNormalTex, o_gi.light3);
			#endif
		#endif
		o_gi.indirect.diffuse *= occlusion;
		
		{
			//BlinnPhongLight
			half3 h = normalize (light.dir + d.worldViewDir);
			
			float nh = max (0, dot (normalWorld, h));
			float spec = pow (nh, _Shininess*128.0);
			
			o_gi.indirect.specular = light.color * spec;
		}
		
		o_gi.indirect.specular *= occlusion;

		return o_gi;
	}
}

//-------------------------------------------------------------------------------------
half4 OutputForward (half4 output, half alphaFromSurface)
{
	#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
		output.a = alphaFromSurface;
	#else
		UNITY_OPAQUE_ALPHA(output.a);
	#endif
	return output;
}

// ------------------------------------------------------------------
//  Base forward pass (directional light, emission, lightmaps, ...)

struct VertexOutputForwardBase
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half4 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndParallax[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax]
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UV
	SHADOW_COORDS(6)
	UNITY_FOG_COORDS(7)

	// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
	#if UNITY_SPECCUBE_BOX_PROJECTION
		float3 posWorld					: TEXCOORD8;
	#endif
};

VertexOutputForwardBase vertForwardBase (VertexInput v)
{
	VertexOutputForwardBase o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	#if UNITY_SPECCUBE_BOX_PROJECTION
		o.posWorld = posWorld.xyz;
	#endif
	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	o.tex = TexCoords(v);
	o.eyeVec = half4(NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos), ComputeScreenPos(o.pos).z);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
	#ifdef _TANGENT_TO_WORLD
		float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

		float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
		o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
		o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
		o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];
	#else
		o.tangentToWorldAndParallax[0].xyz = 0;
		o.tangentToWorldAndParallax[1].xyz = 0;
		o.tangentToWorldAndParallax[2].xyz = normalWorld;
	#endif
	//We need this for shadow receving
	TRANSFER_SHADOW(o);

	// Static lightmaps
	#ifndef LIGHTMAP_OFF
		o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
		o.ambientOrLightmapUV.zw = 0;
	// Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
	#elif UNITY_SHOULD_SAMPLE_SH
		#if UNITY_SAMPLE_FULL_SH_PER_PIXEL
			o.ambientOrLightmapUV.rgb = 0;
		#elif (SHADER_TARGET < 30)
			o.ambientOrLightmapUV.rgb = ShadeSH9(half4(normalWorld, 1.0));
		#else
			// Optimization: L2 per-vertex, L0..L1 per-pixel
			o.ambientOrLightmapUV.rgb = ShadeSH3Order(half4(normalWorld, 1.0));
		#endif
		// Add approximated illumination from non-important point lights
		#ifdef VERTEXLIGHT_ON
			o.ambientOrLightmapUV.rgb += Shade4PointLights (
				unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
				unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
				unity_4LightAtten0, posWorld, normalWorld);
		#endif
	#endif

	#ifdef DYNAMICLIGHTMAP_ON
		o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif
	
	#ifdef _PARALLAXMAP
		TANGENT_SPACE_ROTATION;
		half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
		o.tangentToWorldAndParallax[0].w = viewDirForParallax.x;
		o.tangentToWorldAndParallax[1].w = viewDirForParallax.y;
		o.tangentToWorldAndParallax[2].w = viewDirForParallax.z;
	#endif
	
	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}

half4 fragForwardBase (VertexOutputForwardBase i) : SV_Target
{
	FRAGMENT_SETUP(s)
	UnityLight mainLight = MainLight (s.normalWorld);
	half atten = SHADOW_ATTENUATION(i);
	
	half occlusion = Occlusion(i.tex.xy);
	UnityGI gi = FragmentGI (
		s.posWorld, occlusion, i.ambientOrLightmapUV, atten, s.oneMinusRoughness, s.normalWorld, s.eyeVec, mainLight);
	
	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
	c.rgb += Emission(i.tex.xy);

	UNITY_APPLY_FOG(i.fogCoord, c.rgb);
	c.rgb += _blackPoint.rgb;
	#if defined(SOFT_SLICING) && defined(ENABLE_SOFTSLICING)
		float d = i.eyeVec.w;
		
		//note: if _softPercent == 0  that is the same as hard slice.
		float mask = 1;				
		if (d < _softPercent)
			mask *= d / _softPercent; //this is the darkening of the slice near 0 (near)
		else if (d > 1 - _softPercent)
			mask *= 1 - ((d - (1-_softPercent))/_softPercent); //this is the darkening of the slice near 1 (far)
		
		c.rgb *= mask;
	#endif
	return OutputForward (c, s.alpha);
}

// ------------------------------------------------------------------
//  Additive forward pass (one light per pass)
struct VertexOutputForwardAdd
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half4 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndLightDir[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:lightDir]
	LIGHTING_COORDS(5,6)
	UNITY_FOG_COORDS(7)

	// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if defined(_PARALLAXMAP)
	half3 viewDirForParallax			: TEXCOORD8;
#endif
};

VertexOutputForwardAdd vertForwardAdd (VertexInput v)
{
	VertexOutputForwardAdd o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAdd, o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	o.tex = TexCoords(v);
	o.eyeVec = half4(NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos), ComputeScreenPos(o.pos).z);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
	#ifdef _TANGENT_TO_WORLD
		float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

		float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
		o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
		o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
		o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
	#else
		o.tangentToWorldAndLightDir[0].xyz = 0;
		o.tangentToWorldAndLightDir[1].xyz = 0;
		o.tangentToWorldAndLightDir[2].xyz = normalWorld;
	#endif
	//We need this for shadow receving
	TRANSFER_VERTEX_TO_FRAGMENT(o);

	float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
	#ifndef USING_DIRECTIONAL_LIGHT
		lightDir = NormalizePerVertexNormal(lightDir);
	#endif
	o.tangentToWorldAndLightDir[0].w = lightDir.x;
	o.tangentToWorldAndLightDir[1].w = lightDir.y;
	o.tangentToWorldAndLightDir[2].w = lightDir.z;

	#ifdef _PARALLAXMAP
		TANGENT_SPACE_ROTATION;
		o.viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
	#endif
	
	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}

half4 fragForwardAdd (VertexOutputForwardAdd i) : SV_Target
{
	FRAGMENT_SETUP_FWDADD(s)

	UnityLight light = AdditiveLight (s.normalWorld, IN_LIGHTDIR_FWDADD(i), LIGHT_ATTENUATION(i));
	UnityIndirect noIndirect = ZeroIndirect ();

	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, light, noIndirect);
	
	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
	#if defined(SOFT_SLICING) && defined(ENABLE_SOFTSLICING)
		float d = i.eyeVec.w;
		
		//note: if _softPercent == 0  that is the same as hard slice.
		float mask = 1;				
		if (d < _softPercent)
			mask *= d / _softPercent; //this is the darkening of the slice near 0 (near)
		else if (d > 1 - _softPercent)
			mask *= 1 - ((d - (1-_softPercent))/_softPercent); //this is the darkening of the slice near 1 (far)
		
		c.rgb *= mask;
	#endif
	return OutputForward (c, s.alpha);
}			
			
#endif // UNITY_STANDARD_CORE_INCLUDED

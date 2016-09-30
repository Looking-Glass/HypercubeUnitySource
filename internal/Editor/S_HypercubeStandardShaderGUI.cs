using System;
using UnityEngine;

namespace UnityEditor
{
internal class S_HypercubeStandardShaderGUI : ShaderGUI
{
	public enum BlendMode
	{
		Opaque,
		Cutout,
		Fade,		// Old school alpha-blending mode, fresnel does not affect amount of transparency
		Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
	}
	
	public enum CullMode
	{
		Off,
		Front,
		Back
	}

	private static class Styles
	{
		public static GUIStyle optionsButton = "PaneOptions";
		public static GUIContent uvSetLabel = new GUIContent("UV Set");
		public static GUIContent[] uvSetOptions = new GUIContent[] { new GUIContent("UV channel 0"), new GUIContent("UV channel 1") };

		public static string emptyTootip = "";
		public static GUIContent albedoText = new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)");
		public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
		public static GUIContent specularText = new GUIContent("Specular Mask", "Specular Mask (RGB)");
		public static GUIContent shininessText = new GUIContent("Shininess", "");
		public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
		public static GUIContent heightMapText = new GUIContent("Height Map", "Height Map (G)");
		public static GUIContent occlusionText = new GUIContent("Occlusion", "Occlusion (G)");
		public static GUIContent emissionText = new GUIContent("Emission", "Emission (RGB)");
		public static GUIContent detailMaskText = new GUIContent("Detail Mask", "Mask for Secondary Maps (A)");
		public static GUIContent detailAlbedoText = new GUIContent("Detail Albedo x2", "Albedo (RGB) multiplied by 2");
		public static GUIContent detailNormalMapText = new GUIContent("Normal Map", "Normal Map");

		public static string whiteSpaceString = " ";
		public static string primaryMapsText = "Main Maps";
		public static string secondaryMapsText = "Secondary Maps";
		public static string renderingMode = "Rendering Mode";
		public static string renderingCull = "Rendering Cull";
		public static GUIContent softSlicingText = new GUIContent("Soft Slicing", "Soft Slicing");
		public static GUIContent emissiveWarning = new GUIContent ("Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive.");
		public static GUIContent emissiveColorWarning = new GUIContent ("Ensure emissive color is non-black for emission to have effect.");
		public static readonly string[] blendNames = Enum.GetNames (typeof (BlendMode));
		public static readonly string[] cullNames = Enum.GetNames (typeof (CullMode));
	}

	MaterialProperty blendMode = null;
	MaterialProperty cullMode = null;
	MaterialProperty softSlicing = null;
	MaterialProperty albedoMap = null;
	MaterialProperty albedoColor = null;
	MaterialProperty alphaCutoff = null;
	MaterialProperty specularMask = null;
	MaterialProperty specularColor = null;
	MaterialProperty shininess = null;
	MaterialProperty bumpScale = null;
	MaterialProperty bumpMap = null;
	MaterialProperty occlusionStrength = null;
	MaterialProperty occlusionMap = null;
	MaterialProperty heigtMapScale = null;
	MaterialProperty heightMap = null;
	MaterialProperty emissionScaleUI = null;
	MaterialProperty emissionColorUI = null;
	MaterialProperty emissionColorForRendering = null;
	MaterialProperty emissionMap = null;
	MaterialProperty detailMask = null;
	MaterialProperty detailAlbedoMap = null;
	MaterialProperty detailNormalMapScale = null;
	MaterialProperty detailNormalMap = null;
	MaterialProperty uvSetSecondary = null;

	MaterialEditor m_MaterialEditor;

	bool m_FirstTimeApply = true;

	public void FindProperties (MaterialProperty[] props)
	{
		blendMode = FindProperty ("_Mode", props);
		cullMode = FindProperty ("_CullMode", props);
		softSlicing = FindProperty ("_softSlicingToggle", props, false);
		albedoMap = FindProperty ("_MainTex", props);
		albedoColor = FindProperty ("_Color", props);
		alphaCutoff = FindProperty ("_Cutoff", props);
		specularMask = FindProperty ("_SpecMask", props);
		specularColor = FindProperty ("_SpecColor", props);
		shininess = FindProperty ("_Shininess", props);
		bumpScale = FindProperty ("_BumpScale", props);
		bumpMap = FindProperty ("_BumpMap", props);
		heigtMapScale = FindProperty ("_Parallax", props);
		heightMap = FindProperty("_ParallaxMap", props);
		occlusionStrength = FindProperty ("_OcclusionStrength", props);
		occlusionMap = FindProperty ("_OcclusionMap", props);
		emissionScaleUI = FindProperty ("_EmissionScaleUI", props);
		emissionColorUI = FindProperty ("_EmissionColorUI", props);
		emissionColorForRendering = FindProperty ("_EmissionColor", props);
		emissionMap = FindProperty ("_EmissionMap", props);
		detailMask = FindProperty ("_DetailMask", props);
		detailAlbedoMap = FindProperty ("_DetailAlbedoMap", props);
		detailNormalMapScale = FindProperty ("_DetailNormalMapScale", props);
		detailNormalMap = FindProperty ("_DetailNormalMap", props);
		uvSetSecondary = FindProperty ("_UVSec", props);
	}

	public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] props)
	{
		FindProperties (props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
		m_MaterialEditor = materialEditor;
		Material material = materialEditor.target as Material;

		//ShaderPropertiesGUI (material);

		// Make sure that needed keywords are set up if we're switching some existing
		// material to a standard shader.
		if (m_FirstTimeApply)
		{
			MaterialChanged(material);
			m_FirstTimeApply = false;
		}
		
		ShaderPropertiesGUI (material);
	}

	public void ShaderPropertiesGUI (Material material)
	{
		// Use default labelWidth
		EditorGUIUtility.labelWidth = 0f;

		// Detect any changes to the material
		EditorGUI.BeginChangeCheck();
		{
			BlendModePopup();
			
			CullModePopup();
			
			if (softSlicing != null)
				m_MaterialEditor.ShaderProperty(softSlicing, Styles.softSlicingText.text);

			// Primary properties
			GUILayout.Label (Styles.primaryMapsText, EditorStyles.boldLabel);
			DoAlbedoArea(material);
			DoSpecularArea(material);
			
			m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null);
			m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap, heightMap.textureValue != null ? heigtMapScale : null);
			m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);
			DoEmissionArea(material);
			m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
			EditorGUI.BeginChangeCheck();
			m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
			if (EditorGUI.EndChangeCheck())
			{
				specularMask.textureScaleAndOffset = albedoMap.textureScaleAndOffset; // Apply the main texture scale and offset to the specular mask as well
				emissionMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake
			}

			EditorGUILayout.Space();

			// Secondary properties
			GUILayout.Label(Styles.secondaryMapsText, EditorStyles.boldLabel);
			m_MaterialEditor.TexturePropertySingleLine(Styles.detailAlbedoText, detailAlbedoMap);
			m_MaterialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, detailNormalMap, detailNormalMapScale);
			m_MaterialEditor.TextureScaleOffsetProperty(detailAlbedoMap);
			m_MaterialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);
			
		}
		if (EditorGUI.EndChangeCheck())
		{
			foreach (var obj in blendMode.targets)
				MaterialChanged((Material)obj);
		}
	}

	public override void AssignNewShaderToMaterial (Material material, Shader oldShader, Shader newShader)
	{
		base.AssignNewShaderToMaterial(material, oldShader, newShader);

		if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
		{
			SetupMaterialWithCullMode(material, (CullMode)material.GetFloat("_CullMode"));
			return;
		}

		BlendMode blendMode = BlendMode.Opaque;
		if (oldShader.name.Contains("/Transparent/Cutout/"))
		{
			blendMode = BlendMode.Cutout;
		}
		else if (oldShader.name.Contains("/Transparent/"))
		{
			// NOTE: legacy shaders did not provide physically based transparency
			// therefore Fade mode
			blendMode = BlendMode.Fade;
		}
		material.SetFloat("_Mode", (float)blendMode);
		
		MaterialChanged(material);
	}

	void BlendModePopup()
	{
		EditorGUI.showMixedValue = blendMode.hasMixedValue;
		var mode = (BlendMode)blendMode.floatValue;

		EditorGUI.BeginChangeCheck();
		mode = (BlendMode)EditorGUILayout.Popup(Styles.renderingMode, (int)mode, Styles.blendNames);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
			blendMode.floatValue = (float)mode;
		}

		EditorGUI.showMixedValue = false;
	}
	
	void CullModePopup()
	{
		EditorGUI.showMixedValue = cullMode.hasMixedValue;
		var cull = (CullMode)cullMode.floatValue;

		EditorGUI.BeginChangeCheck();
		cull = (CullMode)EditorGUILayout.Popup(Styles.renderingCull, (int)cull, Styles.cullNames);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Cull");
			cullMode.floatValue = (float)cull;
		}

		EditorGUI.showMixedValue = false;
	}
	
	void DoAlbedoArea(Material material)
	{
		m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
		if (((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout))
		{
			m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel+1);
		}
	}
	
	void DoSpecularArea(Material material)
	{
		m_MaterialEditor.TexturePropertyTwoLines(Styles.specularText, specularMask, specularColor, Styles.shininessText, shininess);
	}

	void DoEmissionArea(Material material)
	{
		bool showEmissionColorAndGIControls = emissionScaleUI.floatValue > 0f;
		bool hadEmissionTexture = emissionMap.textureValue != null;

		// Do controls
		m_MaterialEditor.TexturePropertySingleLine(Styles.emissionText, emissionMap, showEmissionColorAndGIControls ? emissionColorUI : null, emissionScaleUI);

		// Set default emissionScaleUI if texture was assigned
		if (emissionMap.textureValue != null && !hadEmissionTexture && emissionScaleUI.floatValue <= 0f)
			emissionScaleUI.floatValue = 1.0f;

		// Dynamic Lightmapping mode
		if (showEmissionColorAndGIControls)
		{
			bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled(EvalFinalEmissionColor(material));
			EditorGUI.BeginDisabledGroup(!shouldEmissionBeEnabled);

			m_MaterialEditor.LightmapEmissionProperty (MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);

			EditorGUI.EndDisabledGroup();
		}

		if (!HasValidEmissiveKeyword(material))
		{
			EditorGUILayout.HelpBox(Styles.emissiveWarning.text, MessageType.Warning);
		}
	
	}

	public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
	{
		switch (blendMode)
		{
			case BlendMode.Opaque:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = -1;
				break;
			case BlendMode.Cutout:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.EnableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 2450;
				break;
			case BlendMode.Fade:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
				break;
			case BlendMode.Transparent:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
				break;
		}
	}
	
	public static void SetupMaterialWithCullMode(Material material, CullMode cullMode)
	{
		switch (cullMode)
		{
			case CullMode.Off:
				material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
				break;
			case CullMode.Front:
				material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Front);
				break;
			case CullMode.Back:
				material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Back);
				break;
		}
	}

	
	// Calculate final HDR _EmissionColor (gamma space) from _EmissionColorUI (LDR, gamma) & _EmissionScaleUI (gamma)
	static Color EvalFinalEmissionColor(Material material)
	{
		return material.GetColor("_EmissionColorUI") * material.GetFloat("_EmissionScaleUI");
	}

	static bool ShouldEmissionBeEnabled (Color color)
	{
		return color.grayscale > (0.1f / 255.0f);
	}

	static void SetMaterialKeywords(Material material)
	{
		// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
		// (MaterialProperty value might come from renderer material property block)
		SetKeyword (material, "_NORMALMAP", material.GetTexture ("_BumpMap") || material.GetTexture ("_DetailNormalMap"));
		SetKeyword (material, "_PARALLAXMAP", material.GetTexture ("_ParallaxMap"));
		SetKeyword (material, "_DETAIL_MULX2", material.GetTexture ("_DetailAlbedoMap") || material.GetTexture ("_DetailNormalMap"));

		bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled (material.GetColor("_EmissionColor"));
		SetKeyword (material, "_EMISSION", shouldEmissionBeEnabled);

		// Setup lightmap emissive flags
		MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
		if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
		{
			flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
			if (!shouldEmissionBeEnabled)
				flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

			material.globalIlluminationFlags = flags;
		}
	}

	bool HasValidEmissiveKeyword (Material material)
	{
		// Material animation might be out of sync with the material keyword.
		// So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
		// (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
		bool hasEmissionKeyword = material.IsKeywordEnabled ("_EMISSION");
		if (!hasEmissionKeyword && ShouldEmissionBeEnabled (emissionColorForRendering.colorValue))
			return false;
		else
			return true;
	}

	static void MaterialChanged(Material material)
	{
		// Clamp EmissionScale to always positive
		if (material.GetFloat("_EmissionScaleUI") < 0.0f)
			material.SetFloat("_EmissionScaleUI", 0.0f);
		
		// Apply combined emission value
		Color emissionColorOut = EvalFinalEmissionColor (material);
		material.SetColor("_EmissionColor", emissionColorOut);

		// Handle Blending modes
		SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));
		SetupMaterialWithCullMode(material, (CullMode)material.GetFloat("_CullMode"));

		SetMaterialKeywords(material);
	}

	static void SetKeyword(Material m, string keyword, bool state)
	{
		if (state)
			m.EnableKeyword (keyword);
		else
			m.DisableKeyword (keyword);
	}
}

} // namespace UnityEditor

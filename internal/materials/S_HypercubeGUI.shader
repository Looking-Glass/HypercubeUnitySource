Shader "Hypercube/GUI" { 
	Properties {
		_MainTex ("Font Texture", 2D) = "white" {}
		_Color ("Text Color", Color) = (1,1,1,1)
		[MaterialEnum(Off,0,Front,1,Back,2)] _Cull ("Cull", Int) = 2
	}
 
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Lighting Off 
		Cull [_Cull] 
		ZWrite Off 
		Fog { Mode Off }

		Blend SrcAlpha OneMinusSrcAlpha

		Pass 
		{
			Color [_Color]
			SetTexture [_MainTex] 
			{
				combine primary, texture * primary
			}
		}
	}
}
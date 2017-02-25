//Shader "Hypercube/Internal/Cast Mesh"
Shader "Hidden/Cast Mesh"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

		//_sliceBrightnessR ("R Offset", Range (-2, 2)) = 1  //TODO remove these lines, they shouldn't be public
		//_sliceBrightnessG ("G Offset", Range (-2, 2)) = 1
		//_sliceBrightnessB ("B Offset", Range (-2, 2)) = 1
  //      _hypercubeBrightnessMod ("Contrast Mod", Range (0, 100)) = 1
		//_hardwareContrastMod ("Contrast Mod", Range (0, 100)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  }
        Cull Off
        ZWrite On
 
        Pass
        {    
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
         
            #include "UnityCG.cginc"        
 
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
 
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
 
            sampler2D _MainTex;
            float4 _MainTex_ST;

			//this comes from onValidate on hypercubeCamera
			float _hypercubeBrightnessMod = 1;
			//thse come from castMesh.loadSettings
            float _hardwareContrastMod = 1;
			float _sliceBrightnessR = 1;
			float _sliceBrightnessG = 1;
			float _sliceBrightnessB = 1;
         
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
         
            fixed4 frag (v2f i) : SV_Target
            {
				float4 output = tex2D(_MainTex, i.uv) * _hypercubeBrightnessMod;
	
				output.r *= _sliceBrightnessR;
				output.g *= _sliceBrightnessG;
				output.b *= _sliceBrightnessB;
                return output * _hardwareContrastMod;
            }
            ENDCG
        }
	}
}

Shader "Hypercube/Cast Mesh"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_brightnessR ("R Offset", Range (-2, 2)) = 1
		_brightnessG ("G Offset", Range (-2, 2)) = 1
		_brightnessB ("B Offset", Range (-2, 2)) = 1
        _Mod ("Contrast Mod", Range (0, 100)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  }
        Cull Back
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
            float _Mod;
			float _brightnessR;
			float _brightnessG;
			float _brightnessB;
         
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
         
            fixed4 frag (v2f i) : SV_Target
            {
				float4 output = tex2D(_MainTex, i.uv) * _Mod;
				output.r += _brightnessR;
				output.g += _brightnessG;
				output.b += _brightnessB;
                return output;
            }
            ENDCG
        }
	}
}

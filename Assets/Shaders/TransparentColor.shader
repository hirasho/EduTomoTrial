Shader "App/TransparentColor"
{
    Properties
    {
		_Color ("Color", Color) = (1, 1, 1, 1)
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("cull", Float) = 2.0    
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("srcBlend", Float) = 1.0
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("dstBlend", Float) = 0.0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("zTest", Float) = 4.0
		[Enum(Off, 0, On, 1)] _ZWrite("zWrite", Float) = 1.0    
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Transparent" }
        LOD 100

        Pass
        {
			Blend[_SrcBlend][_DstBlend]
			ZTest[_ZTest]
			ZWrite[_ZWrite]
			Cull[_Cull]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

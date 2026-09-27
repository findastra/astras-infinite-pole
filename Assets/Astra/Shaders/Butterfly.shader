Shader "Astra/Butterfly" {
 // Claude 2026-09-26: flapping sprite-sheet butterflies. Particle colour tints the wings; a little glow for bloom.
 Properties { _MainTex("Butterfly sheet",2D)="white"{} _Glow("Glow",Range(1,4))=1.6 }
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _MainTex_ST;float _Glow;
 struct appdata{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f{float4 pos:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;}
 half4 frag(v2f i):SV_Target{half4 t=tex2D(_MainTex,i.uv);half4 c=t*i.color;c.rgb*=_Glow;return c;}
 ENDCG}}
}

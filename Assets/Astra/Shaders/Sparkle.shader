Shader "Astra/Soft Sparkle"
{
 Properties { _Color("Tint",Color)=(0.65,0.45,1,1) }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"} Blend SrcAlpha One ZWrite Off Cull Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO};
 fixed4 _Color;
 v2f vert(appdata v) {v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv*2-1; o.color=v.color*_Color; return o;}
 fixed4 frag(v2f i):SV_Target {float r=dot(i.uv,i.uv); float a=pow(saturate(1-r),3); return fixed4(i.color.rgb,i.color.a*a);}
 ENDCG
 }
 }
}

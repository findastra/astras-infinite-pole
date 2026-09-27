Shader "Astra/Infinite Pole"
{
 // Claude round 1: chrome with dense sparkles (fake pink-white studio reflection, no probes needed).
 Properties { _Opacity("Opacity",Range(0,1))=1 _Sparkle("Crystal sparkle",Range(0,4))=3  _Color("Chrome tint", Color)=(1,1,1,1) _VoidColor("Void",Color)=(0.008,0.005,0.025,1) }
 SubShader {
 Tags { "RenderType"="Transparent" "Queue"="Geometry+10" }
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Pass {
  ColorMask 0
  ZWrite On
  CGPROGRAM
  #pragma vertex depthVert
  #pragma fragment depthFrag
  #pragma multi_compile_instancing
  #include "UnityCG.cginc"
  struct dIn {float4 vertex:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct dOut {float4 pos:SV_POSITION;float y:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
  float _Opacity;
  dOut depthVert(dIn v){dOut o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.y=mul(unity_ObjectToWorld,v.vertex).y;return o;}
  fixed4 depthFrag(dOut i):SV_Target {clip(_Opacity-.99);clip(80-abs(i.y-_WorldSpaceCameraPos.y));return 0;}
  ENDCG
 }
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
 struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
 fixed4 _Color, _VoidColor; float _Opacity,_Sparkle;
 float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 v2f vert(appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.pos=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.normal=UnityObjectToWorldNormal(v.normal); return o; }
 float3 Studio(float3 r){
  float3 env=lerp(float3(.05,.03,.09),float3(1,.86,.96),smoothstep(-.35,.7,r.y));
  env+=float3(1,.97,1)*pow(saturate(1-abs(r.y-.08)*5),6)*1.6;
  env+=float3(1,.4,.78)*pow(saturate(dot(r,normalize(float3(.65,.25,-.7)))),20)*2.2;
  env+=float3(.7,.8,1)*pow(saturate(dot(r,normalize(float3(-.7,.1,.6)))),28)*1.4;
  return env;
 }
 half4 frag(v2f i):SV_Target {
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  float3 n=normalize(i.normal); float3 eye=normalize(_WorldSpaceCameraPos-i.world);
  float3 r=reflect(-eye,n);
  float fres=.6+.4*pow(1-saturate(dot(n,eye)),2);
  float3 chrome=Studio(r)*_Color.rgb*fres;
  float2 uv=float2(atan2(n.z,n.x)*20,i.world.y*160);
  float2 cell=floor(uv);float h=Hash(cell);float2 p=frac(uv)-.5;
  float crystal=pow(saturate(1-length(p)*2.2),4);
  float aa=saturate(1-length(fwidth(uv))*.3);
  float sparkle=aa*crystal*step(.35,h)*pow(.5+.5*sin(_Time.y*1.6+h*60+dot(eye,n)*22),10);
  float3 gem=lerp(float3(1,1,1),.6+.4*cos(float3(0,2,4)+h*6.28+_Time.y*.1),.45);
  chrome+=gem*sparkle*_Sparkle*4;
  float fade=smoothstep(80,120,abs(i.world.y-_WorldSpaceCameraPos.y));
  return half4(chrome,(1-fade)*_Opacity);
 }
 ENDCG
 }
 }
}

Shader "Astra/Infinite Pole"
{
 Properties { _Opacity("Opacity",Range(0,1))=1 _Sparkle("Crystal sparkle",Range(0,4))=3  _Color("Silver tint", Color)=(0.72,0.77,0.9,1) _VoidColor("Void",Color)=(0.008,0.005,0.025,1) }
 SubShader {
 Tags { "RenderType"="Transparent" "Queue"="Transparent-10" }
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
 struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
 fixed4 _Color, _VoidColor; float _Opacity,_Sparkle;
 v2f vert(appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.pos=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.normal=UnityObjectToWorldNormal(v.normal); return o; }
 fixed4 frag(v2f i):SV_Target {
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  float3 n=normalize(i.normal); float3 eye=normalize(_WorldSpaceCameraPos-i.world);
  float rim=pow(1-saturate(dot(n,eye)),3);
  float stripe=pow(saturate(dot(n,normalize(float3(-0.6,0,0.8)))),18);
  float3 silver=_Color.rgb*(0.18+0.7*rim+1.4*stripe);
  float fade=smoothstep(35,110,abs(i.world.y));
    float2 uv=float2(atan2(n.z,n.x)*12,i.world.y*95);
  float2 cell=floor(uv);float h=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
  float2 p=frac(uv)-.5;
  float crystal=pow(saturate(1-length(p)*2),5);
  float sparkle=crystal*pow(.5+.5*sin(_Time.y*1.1+h*60+dot(eye,n)*18),14);
  float3 gem=.6+.4*cos(float3(0,2,4)+h*6.28+_Time.y*.08);
  silver+=gem*sparkle*_Sparkle*5;
  return fixed4(silver,(1-fade)*_Opacity);
 }
 ENDCG
 }
 }
}


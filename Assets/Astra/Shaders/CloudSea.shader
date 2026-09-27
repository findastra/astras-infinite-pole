Shader "Astra/Cloud Sea" {
 // Claude 2026-09-26: an endless sea of soft pink cloud tops (banner look). Billowy lumps are shaded in the pixel shader
 // with gold-lit tops, magenta valleys and horizon haze that melts into the sunset sky. Slow drift, fine point glitter.
 Properties {
  _Top("Lit tops",Color)=(1,.74,.86,1)
  _Mid("Cloud body",Color)=(.98,.52,.8,1)
  _Shadow("Valleys",Color)=(.74,.38,.84,1)
  _Sun("Sun on tops",Color)=(1,.84,.52,1)
  _Horizon("Horizon haze",Color)=(1,.86,.7,1)
  _SunDir("Sun direction",Vector)=(0,.25,1,0)
  _Scale("Lump size",Float)=.05
  _Haze("Haze distance",Float)=320
  _Speed("Drift",Float)=.5
  _Glitter("Glitter",Range(0,3))=.7
 }
 SubShader {Tags {"Queue"="Geometry+1" "RenderType"="Opaque"} Cull Back
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 float4 _Top,_Mid,_Shadow,_Sun,_Horizon,_SunDir;float _Scale,_Haze,_Speed,_Glitter;
 struct appdata{float4 vertex:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f{float4 pos:SV_POSITION;float3 world:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 float H(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float H3(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float N(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(H(i),H(i+float2(1,0)),f.x),lerp(H(i+float2(0,1)),H(i+1),f.x),f.y);}
 float Billow(float2 p){float s=0,a=.55;for(int k=0;k<4;k++){s+=a*(1-abs(N(p)*2-1));p=p*2.03+float2(3.1,1.7);a*=.5;}return s;}
 float Height(float2 p){float b=Billow(p);return b*b;}
 float Glints(float3 p){float3 c=floor(p);float h=H3(c);float3 f=frac(p)-.5;float3 o=float3(H3(c+3.1),H3(c+7.7),H3(c+5.3))-.5;
  float spot=pow(saturate(1-length(f-o*.6)*2.6),4);return spot*pow(saturate(.5+.5*sin(_Time.y*1.3+h*63)),10)*step(.6,h)*saturate(1-length(fwidth(p))*.35);}
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 half4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  float2 p=i.world.xz*_Scale+_Time.y*_Speed*float2(.012,.004);
  float e=.02;float h=Height(p),hx=Height(p+float2(e,0)),hz=Height(p+float2(0,e));
  float3 n=normalize(float3(-(hx-h)/e*.35,1,-(hz-h)/e*.35));
  float3 sun=normalize(_SunDir.xyz);float3 eye=normalize(_WorldSpaceCameraPos-i.world);
  float ndl=saturate(dot(n,sun)*.6+.4);
  float3 c=lerp(_Shadow.rgb,_Mid.rgb,smoothstep(.1,.5,h));
  c=lerp(c,_Top.rgb,smoothstep(.45,.95,h)*ndl);
  c+=_Sun.rgb*pow(saturate(dot(n,normalize(float3(sun.x,.35,sun.z)))),3)*smoothstep(.35,.9,h)*.45;
  c+=_Sun.rgb*pow(saturate(dot(-eye,sun)),6)*.25;                          // back-lit glow looking toward the sun
  c+=float3(1,.96,1)*Glints(i.world*6)*_Glitter;
  float d=distance(_WorldSpaceCameraPos.xz,i.world.xz);
  c=lerp(c,_Horizon.rgb,saturate(1-exp(-d/_Haze))*.95);
  return half4(c,1);
 }
 ENDCG}}
}

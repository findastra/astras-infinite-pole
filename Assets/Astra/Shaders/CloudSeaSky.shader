Shader "Astra/Cloud Sea Sky" {
 // Claude 2026-09-26: the pink-to-gold sunset from Astra's banner. Soft wisps drift slowly; a warm glow sits on the horizon.
 Properties {
  _Zenith("Zenith",Color)=(.96,.52,.76,1)
  _Upper("Upper sky",Color)=(.99,.62,.78,1)
  _Mid("Low sky",Color)=(1,.75,.7,1)
  _Horizon("Horizon",Color)=(1,.9,.62,1)
  _Below("Below horizon",Color)=(1,.74,.86,1)
  _SunDir("Glow direction",Vector)=(0,.06,1,0)
  _Wisps("Wisps",Range(0,1))=.45
  _Exposure("Exposure",Range(0,2))=1
 }
 SubShader {Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Zenith,_Upper,_Mid,_Horizon,_Below,_SunDir;float _Wisps,_Exposure;
 struct v2f{float4 pos:SV_POSITION;float3 dir:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 struct appdata{float4 vertex:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
 float H(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float N(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(H(i),H(i+float2(1,0)),f.x),lerp(H(i+float2(0,1)),H(i+1),f.x),f.y);}
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
 half4 frag(v2f i):SV_Target{
  float3 d=normalize(i.dir);float h=d.y;float3 c;
  if(h>=0){c=lerp(_Horizon.rgb,_Mid.rgb,smoothstep(0,.14,h));c=lerp(c,_Upper.rgb,smoothstep(.1,.45,h));c=lerp(c,_Zenith.rgb,smoothstep(.4,1,h));}
  else c=lerp(_Horizon.rgb,_Below.rgb,smoothstep(0,-.25,h));
  float az=atan2(d.z,d.x);
  float w=N(float2(az*4+_Time.y*.004,h*26))*.6+N(float2(az*11-_Time.y*.006,h*70))*.4;
  c+=float3(1,.96,.92)*_Wisps*.16*smoothstep(.5,.85,w)*smoothstep(.02,.1,h)*(1-smoothstep(.3,.6,h));
  float s=saturate(dot(d,normalize(_SunDir.xyz)));
  c+=float3(1,.88,.6)*(pow(s,5)*.3+pow(s,60)*.35);
  return half4(c*_Exposure,1);
 }
 ENDCG}}
}

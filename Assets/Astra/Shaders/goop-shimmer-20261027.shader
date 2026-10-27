Shader "Astra/Goop Shimmer" {
 // 2026-10-27 (Claude): the goop star by the cinema (Astra: a big rippling goop star, "that shimmery material", little
 // blobs around it). Dark glossy goop with an oil-slick rainbow sheen at grazing angles, a wet highlight and a few glints.
 // Star mode: rings ripple outward from the centre (vertex colour red = how much a vertex may move, 0 at the rim).
 // Blob mode: the whole blob breathes and wobbles, each one on its own beat (from where it stands).
 Properties {
  _Base("Goop colour",Color)=(.09,.035,.08,1)
  _Deep("Deep colour (facing you)",Color)=(.02,.01,.03,1)
  _Sheen("Rainbow sheen",Range(0,2))=1.1
  _Gloss("Wet highlight",Range(0,3))=1.4
  _Glitter("Glints",Range(0,3))=.8
  _RippleAmp("Ripple height (m)",Range(0,.15))=.04
  _RippleFreq("Ripple rings per metre",Range(0,20))=7
  _RippleSpeed("Ripple speed",Range(0,6))=2.2
  [Toggle] _Blob("Blob mode (breathe and wobble)",Float)=0
 }
 SubShader {Tags {"Queue"="Geometry" "RenderType"="Opaque"}
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #pragma target 3.0
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;UNITY_FOG_COORDS(2) UNITY_VERTEX_OUTPUT_STEREO};
 float4 _Base,_Deep;float _Sheen,_Gloss,_Glitter,_RippleAmp,_RippleFreq,_RippleSpeed,_Blob;
 float Hash3(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float Glints(float3 p){float3 c=floor(p);float h=Hash3(c);float3 f=frac(p)-.5;
  float spot=pow(saturate(1-length(f)*2.4),5);return spot*pow(saturate(.5+.5*sin(_Time.y*1.6+h*60)),12)*step(.72,h);}
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;float3 n=UnityObjectToWorldNormal(v.normal);
  float3 c=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
  if(_Blob>.5){
   // breathe along the normal, plus a slow sideways sway; the beat comes from where the blob stands
   float beat=Hash3(floor(c*7.1))*6.283;
   float s=sin(_Time.y*1.7+beat)*.07+sin(_Time.y*3.1+beat*2+w.y*9)*.03;
   w+=n*s*length(w-c);w.xz+=float2(sin(_Time.y*.9+beat),cos(_Time.y*1.1+beat))*.015;
  }else{
   // rings travelling outward from the centre; tilt the normal with the ring slope so the sheen ripples too
   float2 d=w.xz-c.xz;float r=length(d);float ph=r*_RippleFreq*6.283-_Time.y*_RippleSpeed;
   float k=saturate(v.color.r)*_RippleAmp;
   w.y+=sin(ph)*k*(.6+.4*sin(_Time.y*.7+r));
   float2 dir=r>1e-4?d/r:float2(0,0);float slope=cos(ph)*k*_RippleFreq*6.283;
   n=normalize(n-float3(dir.x,0,dir.y)*slope);
  }
  o.world=w;o.normal=n;o.pos=mul(UNITY_MATRIX_VP,float4(w,1));UNITY_TRANSFER_FOG(o,o.pos);return o;}
 half4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos-i.world);float nv=saturate(dot(n,v));
  float fres=pow(1-nv,3);
  // thin-film rainbow: the hue walks with the viewing angle, height and a slow drift, like oil on water
  float hue=nv*1.7+i.world.y*.6+dot(i.world.xz,float2(.11,.07))+_Time.y*.04;
  float3 film=.5+.5*cos(6.283*(hue+float3(0,.33,.67)));
  float3 col=lerp(_Deep.rgb,_Base.rgb,fres*.8+.2)+film*(.12+.88*fres)*_Sheen*.55;
  float3 l=normalize(float3(.3,1,.2));float3 hv=normalize(l+v);
  col+=pow(saturate(dot(n,hv)),140)*_Gloss+pow(saturate(dot(n,hv)),18)*_Gloss*.12*film;
  col+=Glints(i.world*24)*_Glitter*float3(1,.95,1);
  UNITY_APPLY_FOG(i.fogCoord,col);
  return half4(col,1);}
 ENDCG }
 // depth/shadow pass, so the jellies (which hide behind the camera's depth picture) don't draw over the goop
 UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
 }
}

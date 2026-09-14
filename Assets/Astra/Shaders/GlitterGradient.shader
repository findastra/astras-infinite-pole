Shader "Astra/Glitter Gradient"
{
 Properties {
  _MainTex("Particle",2D)="white"{}
  _ColorA("Gradient start",Color)=(0.35,0.12,1,1)
  _ColorB("Gradient end",Color)=(0.1,0.85,1,1)
  _Brightness("Brightness",Range(0,3))=1
  _Twinkle("Twinkle",Range(0,1))=0.5
  _GradientScale("Gradient scale",Float)=0.12
  _Shape("0 texture, 1 diamond, 2 confetti",Float)=0
  _DriftSpeed("Color cycles per second",Float)=0.004
  _MorphSpeed("Shape cycles per second",Float)=0.009
  _Phase("Evolution phase",Float)=0
  _Flow("Magic flow",Range(0,1))=0.6
  _FlowSeed("Flow seed",Float)=0
  _React("Player wake",Float)=0
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Blend SrcAlpha One ZWrite Off Cull Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float3 world:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
 sampler2D _MainTex; fixed4 _ColorA,_ColorB;float _Brightness,_Twinkle,_GradientScale,_Shape,_DriftSpeed,_MorphSpeed,_Phase;
 float _Flow,_FlowSeed,_React; float4 _Body,_HandL,_HandR,_Wake;
 float3 pushAway(float3 p,float3 center,float radius) { float3 d=p-center; float len=length(d); return d/max(len,0.08)*pow(saturate(1-len/radius),2)*0.75; }
 float3 rotateHue(float3 color,float angle) {
  float3 axis=normalize(float3(1,1,1)); float s=sin(angle),c=cos(angle);
  return max(0,color*c+cross(axis,color)*s+axis*dot(axis,color)*(1-c));
 }
 v2f vert(appdata v) {v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.uv=v.uv;o.color=v.color;float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;
  float t=_Time.y*0.22;float seed=_FlowSeed;
  float3 relative=w-_Body.xyz;
  float orbit=sin(t*.37+seed)*.45+t*.35;
  float sn=sin(orbit*_Flow),cs=cos(orbit*_Flow);
  w.xz=_Body.xz+float2(relative.x*cs-relative.z*sn,relative.x*sn+relative.z*cs);
  float3 flow=float3(sin(w.y*.6+t+seed)+cos(w.z*.4-t*.7),sin(w.x*.5-t+seed*.7),cos(w.y*.5+t*.8+seed)+sin(w.x*.4+t*.6));
  w+=flow*_Flow*.7;
  float3 closest=float3(_Body.x,clamp(w.y,_Body.y-_Body.w,_Body.y+_Body.w),_Body.z);
  float influence=saturate(1-distance(w,closest)/1.8);
  w+=_React*(pushAway(w,closest,1.65)+pushAway(w,_HandL.xyz,.8)+pushAway(w,_HandR.xyz,.8)+_Wake.xyz*influence*.18);
  o.world=w;o.pos=mul(UNITY_MATRIX_VP,float4(w,1));return o;}
 half4 frag(v2f i):SV_Target {
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  float2 p=i.uv*2-1;float mask=tex2D(_MainTex,i.uv).a;
  if(_Shape>0.5 && _Shape<1.5)mask=pow(saturate(1-abs(p.x)-abs(p.y)),0.65);
  if(_Shape>1.5)mask=step(abs(p.x),0.7)*step(abs(p.y),0.9);
  float morph=0.5-0.5*cos((_Time.y*_MorphSpeed+_Phase)*6.2831853);
  float radius=length(p);float angle=atan2(p.y,p.x);
  float starRadius=lerp(0.38,0.92,pow(0.5+0.5*cos(angle*5),3));
  float star=1-smoothstep(starRadius-0.12,starRadius,radius);
  float diamond=pow(saturate(1-abs(p.x)-abs(p.y)),0.7);
  float alternate=lerp(star,diamond,0.5+0.5*sin((_Time.y*_MorphSpeed*0.47+_Phase)*6.2831853));
  mask=lerp(mask,alternate,morph*step(0.00001,_MorphSpeed));
  float blend=0.5+0.5*sin(i.world.y*_GradientScale+i.world.x*0.17+i.world.z*0.11);
  float phase=dot(i.world,float3(7.1,9.7,13.3))+_FlowSeed;
  float flash=pow(saturate(.5+.5*sin(_Time.y*2.1+phase)),18);
  float pulse=lerp(1,.32+flash*3.5,_Twinkle);
  float core=exp(-dot(p,p)*38);
  float rays=pow(saturate(1-abs(p.x)),28)*pow(saturate(1-abs(p.y)),2)+pow(saturate(1-abs(p.y)),28)*pow(saturate(1-abs(p.x)),2);
  mask=mask*.18+core+ rays*(.2+flash*.7);
  float nearFade=smoothstep(0.15,0.65,distance(_WorldSpaceCameraPos,i.world));
  float3 color=rotateHue(lerp(_ColorA.rgb,_ColorB.rgb,blend),_Time.y*_DriftSpeed*6.2831853);
  color=lerp(color,float3(1,1,1),flash*.65);
  return half4(color*_Brightness*i.color.rgb*pulse, saturate(mask)*i.color.a*nearFade);
 }
 ENDCG
 }
 }
}



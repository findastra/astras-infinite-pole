Shader "Astra/Rainbow Flow" {
 // Claude (2026-10-01): liquid-rainbow streams flowing around the pole, like liquid-light art.
 // Drawn on the inside of a huge open cylinder that follows the player up and down. The pattern is pinned to the
 // world (cylinder angle + world height), so it flows endlessly while you climb. Black gaps stay see-through.
 // Each stream has stacked bands: cyan edge > green > yellow > orange > magenta core. HDR, so it blooms.
 Properties {
  _Scale("Pattern size (smaller = bigger streams)",Range(.01,.4))=.04
  _Speed("Flow speed",Range(0,2))=.6
  _Band("Stream width",Range(.3,2))=1
  _Glow("Glow",Range(0,4))=.9
  _Glitter("Glitter",Range(0,3))=1
  _Gap("Space between streams",Range(.4,.85))=.66
  _Theme("Theme (0 rainbow, 1 angel, 2 devil)",Range(0,2))=0
  _Style("Pattern (0 rivers, 1 aurora, 2 oil swirl, 3 lava blobs, 4 rain streaks, 5 tiger marble)",Range(0,5))=0
  _FadeStart("Fade start above/below you (m)",Float)=35
  _FadeEnd("Fade end (m)",Float)=60
 }
 SubShader { Tags {"Queue"="Transparent-20" "RenderType"="Transparent" "IgnoreProjector"="True"} Blend One One ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 #include "AstraStorm.cginc"
 struct appdata {float4 vertex:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 float _Scale,_Speed,_Band,_Glow,_Glitter,_Gap,_Theme,_Style,_FadeStart,_FadeEnd;
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 float h3(float3 p){p=frac(p*.3183099+.1);p*=17.;return frac(p.x*p.y*p.z*(p.x+p.y+p.z));}
 float n3(float3 x){float3 i=floor(x),f=frac(x);f=f*f*(3.-2.*f);
  return lerp(lerp(lerp(h3(i),h3(i+float3(1,0,0)),f.x),lerp(h3(i+float3(0,1,0)),h3(i+float3(1,1,0)),f.x),f.y),
              lerp(lerp(h3(i+float3(0,0,1)),h3(i+float3(1,0,1)),f.x),lerp(h3(i+float3(0,1,1)),h3(i+float3(1,1,1)),f.x),f.y),f.z);}
 float fbm(float3 p){float v=0.,a=.5;for(int k=0;k<4;k++){v+=a*n3(p);p=p*2.03+float3(1.7,9.2,4.1);a*=.5;}return v;}
 float3 hsv(float hh){return saturate(abs(frac(hh+float3(0.,2./3.,1./3.))*6.-3.)-1.);}
 float3 pal(float u,float t){
  if(_Theme<.5) return hsv(lerp(.52,-.17,u)+.03*sin(t));
  if(_Theme<1.5) return lerp(float3(1.,.72,.28),float3(1.,1.,.96),u)*1.1;
  return lerp(float3(1.,.28,.02),float3(.45,0.,.02),u)*1.1;
 }
 half4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  float t=_Time.y*_Speed;
  float gapAdj=_Gap;
  float a=atan2(i.world.z,i.world.x); float r=length(i.world.xz);
  // seamless around the cylinder: sample the noise on a circle (no seam at the back)
  float3 q=float3(cos(a)*r*_Scale, sin(a)*r*_Scale, i.world.y*_Scale);
  q.z+=.35*sin(a*3.+t*.4);
  float3 w=float3(fbm(q*1.3+float3(0,0,-t*.35)), fbm(q*1.3+float3(5.2,1.3,-t*.3)), 0);
  float3 w2=float3(fbm(q+w*1.8+float3(1.7-t*.12,9.2,0)), fbm(q+w*1.8+float3(8.3,2.8+t*.15,0)), 0);
  float f=fbm(q+w2*1.6);
  int st=(int)(_Style+.5);
  // every style ends up with one number, `stream`: 1 inside a ribbon, 0 in the gaps between them
  float stream;
  if(st==0)      stream=.5+.5*sin((f*22.+w2.x*5.)/_Band);                                  // rivers: marbled liquid-light bands
  else if(st==1) stream=.5+.5*sin((i.world.y*_Scale*7.+w2.x*9.+f*5.)/_Band);               // aurora: tall silky curtains
  else if(st==2) stream=.5+.5*sin((length(q.xy)*9.+a*2.+f*14.+t*.6)/_Band);                // oil swirl: tight rings around the pole
  else if(st==3) stream=smoothstep(.34,.66,f*1.35+w2.y*.5);                                 // lava lamp: big slow round blobs
  else if(st==4) stream=.5+.5*sin((a*22.+w.x*4.+i.world.y*_Scale*2.)/_Band);                // rain: vertical streaks falling round you
  else           stream=frac((f*9.+w2.x*3.)/_Band)<.5?1.:0.;                                 // tiger marble: hard-edged stripes
  float soft=st==5?.012:.04;
  float body=smoothstep(_Gap,_Gap+soft,stream);
  float uu=saturate((stream-_Gap-.02)/(1.-_Gap-.02));
  float u=lerp(uu,floor(uu*6.)/6.+.08,st==3?.25:.55);
  float seam=smoothstep(0.,.06,frac(uu*6.))*.25+.75;
  float3 col=pal(u,t+f*3.)*body*seam*_Glow;
  float rim=smoothstep(_Gap,_Gap+.03,stream)*smoothstep(_Gap+.07,_Gap+.03,stream);
  if(st==5)rim=0;
  col+=float3(1.,.97,1.)*rim*.5*_Glow;
  // glitter speckles riding the streams, plus faint stars in the gaps
  float3 gc=floor(float3(a*r*6.,i.world.y*6.,0));float s=h3(gc+3.);
  float tw=.5+.5*sin(_Time.y*5.+s*60.);
  col+=float3(1,.92,1)*(step(.985,s)*body*_Glitter*1.4+step(.997,h3(gc+11.))*.6)*tw;
  float fade=1.-smoothstep(_FadeStart,_FadeEnd,abs(i.world.y-_WorldSpaceCameraPos.y));
  return half4(lerp(col,col*.3,_UdonStorm)*fade,1);
 }
 ENDCG } } }

Shader "Astra/Glitter Cloud" {
 // 2026-10-07 (Claude, round 37): "Fuzzy texture" (cotton fibres in the colour + a soft velvet sheen at the edges)
 // replaces the bump relief, which the owner said looked like dented tin foil. Relief is now its own slider, default 0.
 // 2026-10-06 (Claude, cloud redo): optional soft cloud texture. "Cloud texture" adds gentle billow relief at three
 // scales (a bump from 3D noise in the cloud's own space, so it rides along as the cloud turns), soft shading in the
 // folds and a faint per-billow tint. "Use vertex colours" reads the fused meshes' baked folds and billow tints.
 // "Cloud texture on" (keyword _CLOUDTEX) compiles the texture in. All default off, which gives exactly the old look, so the DJ deck, phone booths and chairs that share this shader don't change.
 // 2026-10-01 (Claude): Stormscape turns every cloud into a dark rainbow gradient of greys and blacks (AstraStorm.cginc).
 // Claude round 3a/3f: soft, rounded cloud shading with twinkling glitter.
 // Every cloud gets its own pastel tint (close to white) that slowly shifts as it drifts and over time.
 // Instanced, no textures. The DJ deck uses this shader too, with Color variety set to 0.
 Properties {
  _Top("Top color",Color)=(1,.97,1,1)
  _Bottom("Underside color",Color)=(.93,.84,.95,1)
  _Rim("Rim glow",Color)=(1.5,1.2,1.6,1)
  _Glitter("Glitter",Range(0,4))=2.2
  _Pastel("Color variety (0 = none)",Range(0,1))=.2
  _ColorSpeed("Color change speed",Range(0,.2))=.025
  [Toggle(_CLOUDTEX)] _CloudTex("Cloud texture on",Float)=0
  _Texture("Cloud texture (0 = off)",Range(0,1))=0
  _TextureScale("Cloud texture size",Range(.25,4))=1
  _Fold("Fold shade color",Color)=(.8,.7,.9,1)
  _VColor("Use vertex colours (fused meshes)",Range(0,1))=0
  _Fuzz("Fuzzy texture (cotton fibres + soft sheen)",Range(0,1))=0
  _Relief("Bump relief (0 = none; high looks like dented foil)",Range(0,1))=0
 }
 SubShader {Tags {"Queue"="Geometry" "RenderType"="Opaque"}
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma shader_feature_local _CLOUDTEX
 #pragma target 3.0
 #include "UnityCG.cginc"
 #include "AstraStorm.cginc"
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float3 center:TEXCOORD2;float3 opos:TEXCOORD3;float4 color:TEXCOORD4;UNITY_VERTEX_OUTPUT_STEREO};
 float4 _Top,_Bottom,_Rim,_Fold;float _Glitter,_Pastel,_ColorSpeed,_Texture,_TextureScale,_VColor,_Fuzz,_Relief;
 float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float Hash3(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 // 2026-09-26 (Claude): glints live in 3D cells, so every sparkle is a round point on any surface angle (no stretching).
 float Glints(float3 p,float speed,float density){
  float3 cell=floor(p);float h=Hash3(cell);float3 f=frac(p)-.5;
  float3 off=float3(Hash3(cell+3.1),Hash3(cell+7.7),Hash3(cell+5.3))-.5;
  float spot=pow(saturate(1-length(f-off*.6)*2.6),4);
  float flash=pow(saturate(.5+.5*sin(_Time.y*speed+h*63)),10);
  return spot*flash*step(1-density,h)*saturate(1-length(fwidth(p))*.35);
 }
 // smooth 3D value noise, 0..1
 float Noise(float3 p){
  float3 i=floor(p);float3 f=frac(p);f=f*f*(3-2*f);
  float a=lerp(lerp(Hash3(i),Hash3(i+float3(1,0,0)),f.x),lerp(Hash3(i+float3(0,1,0)),Hash3(i+float3(1,1,0)),f.x),f.y);
  float b=lerp(lerp(Hash3(i+float3(0,0,1)),Hash3(i+float3(1,0,1)),f.x),lerp(Hash3(i+float3(0,1,1)),Hash3(i+float3(1,1,1)),f.x),f.y);
  return lerp(a,b,f.z);
 }
 // billow height at three scales; each octave fades out before it gets small enough on screen to shimmer
 float Billows(float3 p){
  float fade1=saturate(1.5-length(fwidth(p*3.1))*1.6),fade2=saturate(1.5-length(fwidth(p*7.3))*1.6);
  return Noise(p*1.15)*.55+(1-abs(Noise(p*3.1+7.3)*2-1))*.3*fade1+Noise(p*7.3+2.1)*.15*fade2;
 }
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);
  o.center=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
  // the cloud's own space in metres: the texture turns with the cloud and doesn't stretch on scaled clouds
  float3 sc=float3(length(unity_ObjectToWorld._m00_m10_m20),length(unity_ObjectToWorld._m01_m11_m21),length(unity_ObjectToWorld._m02_m12_m22));
  o.opos=v.vertex.xyz*sc+o.center*.173;
  o.color=lerp(float4(.5,0,.5,1),v.color,_VColor);
  return o;}
 half4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  float3 n=normalize(i.normal);float3 eye=normalize(_WorldSpaceCameraPos-i.world);
  float bil=.5,fold=0,fuzz=0,fz=0;
  #ifdef _CLOUDTEX
  // only materials with "Cloud texture on" compile this in; the DJ deck, booths and chairs that share the shader skip it
  bil=Billows(i.opos/_TextureScale);
  if(_Relief>0){
   // optional bump from the billow height. 2026-10-07: off by default, the owner said it looked like dented tin foil
   float3 dpx=ddx(i.world),dpy=ddy(i.world);float hx=ddx(bil),hy=ddy(bil);
   float3 r1=cross(dpy,n),r2=cross(n,dpx);float det=dot(dpx,r1);
   float3 grad=sign(det)*(hx*r1+hy*r2)*.15*_Relief*_TextureScale;
   n=normalize(max(abs(det),1e-12)*n-grad);
  }
  fold=smoothstep(.5,.18,bil)*_Texture;
  // 2026-10-07 (Claude, round 37): fuzzy cotton texture. Fine fibres at two scales tint the colour only (no bumps, so
  // no foil glints); each scale fades out before it is small enough on screen to shimmer.
  {
   float3 fp=i.opos/_TextureScale;
   float a1=saturate(1.5-length(fwidth(fp*19.))*1.4),a2=saturate(1.5-length(fwidth(fp*47.))*1.4);
   fuzz=((Noise(fp*19.)-.5)*a1*.65+(Noise(fp*47.+3.7)-.5)*a2*.45)*_Fuzz;
   fz=_Fuzz;
  }
  #endif
  fold=saturate(fold+i.color.g*.6*_VColor);
  // 2026-10-07 relook: folds read as dark blotches from afar, so they fade out with distance (gone by ~45 m)
  fold*=1-saturate(length(_WorldSpaceCameraPos-i.world)/45)*.75;
  // pastel tint: differs per cloud (from where it is), drifts as the cloud moves and over time, plus a soft gradient across the cloud
  float hue=dot(i.center,float3(.047,.031,.061))+_Time.y*_ColorSpeed+dot(i.world-i.center,float3(.06,.09,.05))+(i.color.b-.5)*.18*_VColor;
  float3 rainbow=.5+.5*cos(6.2832*(hue+float3(0,.33,.67)));
  float3 tint=lerp(float3(1,1,1),rainbow,_Pastel);
  // soft, rounded shading: bright tops, gently shaded sides and underside
  float up=saturate(n.y*lerp(.55,.42,fz)+lerp(.5,.56,fz));   // fuzzy clouds: softer, wrapped light
  float3 col=tint*lerp(_Bottom.rgb,_Top.rgb,up)*(.86+.14*up);
  // folds between billows sink into a soft lilac shade; billow crowns catch a little extra light
  col=lerp(col,col*lerp(float3(1,1,1),_Fold.rgb,.7),fold*.6)*(1+(bil-.5)*.22*_Texture);
  col*=1+fuzz*.32;
  float facing=saturate(dot(n,eye));
  float rim=pow(1-facing,2.2)*lerp(1,.55,fz);
  // velvet sheen: the fibres catch light along the edges, so the outline looks soft instead of hard
  col=lerp(col,tint*_Top.rgb*1.04,pow(1-facing,1.3)*.38*fz);
  float3 p=i.world;
  float g=Glints(p*90,1.7,.5)+Glints(p*34+11.3,1.1,.35)*1.4;
  float3 calm=col+_Rim.rgb*tint*rim*.45+float3(1,.95,1)*g*_Glitter*2;
  if(_UdonStorm<=0)return half4(calm,1);
  float3 storm=StormCloud(i.world,i.center,up,rim)*(1-fold*.35)+float3(.75,.8,1)*g*_Glitter*.18;   // glitter becomes faint wet glints
  return half4(lerp(calm,storm,_UdonStorm),1);
 }
 ENDCG }
 // 2026-10-27 (Claude, jelly beds): a depth/shadow pass, so the jelly beds (which hide behind whatever is in the camera's
 // depth picture) stop drawing over clouds in front of them. Only the jellies' shadow-only sun casts shadows, and it lights nothing.
 UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
 }
}

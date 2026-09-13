Shader "Astra/Sparkle Clouds" {
Properties { _ColorA("Cloud gradient A",Color)=(1,.32,.16,1) _ColorB("Cloud gradient B",Color)=(1,.65,.38,1) }
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha One ZWrite Off Cull Off
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"
struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float3 world:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
float4 _ColorA,_ColorB;
v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
float4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
float2 p=i.uv*2-1;float n=noise(p*3+_Time.y*.018);n+=noise(p*7-_Time.y*.027)*.35;
float soft=pow(saturate(1-dot(p,p)),2)*smoothstep(.12,.85,n);
float2 cells=i.uv*22;float2 local=frac(cells)-.5;float h=hash(floor(cells));
float glint=pow(saturate(1-length(local)*7),5)*step(.91,h)*pow(.5+.5*sin(_Time.y*1.5+h*70),8);
float3 color=lerp(_ColorA.rgb,_ColorB.rgb,.5+.5*sin(i.world.y*.3+i.world.x*.15+_Time.y*.04));
float fade=smoothstep(.7,2.5,distance(i.world,_WorldSpaceCameraPos));
return float4(color*(.65+glint*8),soft*i.color.a*fade); }
ENDCG } } }

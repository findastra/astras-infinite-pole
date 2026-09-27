Shader "Astra/Light Beam" {
 // Claude round 3h: soft additive stage-light beam. Brightest at the lamp, fades along its length and at the edges,
 // hue cycles slowly (each beam offset by its own position). HDR so it blooms.
 Properties { _Color("Beam color",Color)=(2.4,.9,2.2,1) _Intensity("Intensity",Range(0,4))=1.2 _HueSpeed("Color cycle speed",Range(0,1))=.08 }
 SubShader { Tags {"Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True"} Blend One One ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 normal:TEXCOORD2;float3 center:TEXCOORD3;UNITY_VERTEX_OUTPUT_STEREO};
 float4 _Color;float _Intensity,_HueSpeed;
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;
  o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.center=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;return o;}
 half4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  float3 eye=normalize(_WorldSpaceCameraPos-i.world);
  float edge=pow(saturate(abs(dot(normalize(i.normal),eye))),1.6);      // soft sides
  float along=pow(1-i.uv.y,1.8);                                          // fades toward the far end
  float hue=_Time.y*_HueSpeed+dot(i.center,float3(.13,.07,.11));
  float3 cycle=.55+.45*cos(6.2832*(hue+float3(0,.33,.67)));
  float3 col=_Color.rgb*lerp(float3(1,1,1),cycle,.6)*_Intensity*edge*along*.35;
  return half4(col,1);
 }
 ENDCG } } }

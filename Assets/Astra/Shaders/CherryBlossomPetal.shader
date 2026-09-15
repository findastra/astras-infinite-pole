Shader "Astra/Cherry Blossom Petal" {
 Properties {_Color("Petal pink",Color)=(1,.48,.7,1)}
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
 float4 _Color;
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 half4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 float2 p=i.uv*2-1;float width=.60+.22*p.y;
 float edge=1-length(float2(p.x/width,p.y));
 float alpha=smoothstep(0,.065,edge);
 float notch=1-smoothstep(.02,.12,length(float2(p.x*1.8,(p.y-.98)*1.5)));alpha*=1-notch;
 float fold=.85+.15*cos(p.x*5+p.y*2);
 float3 col=lerp(_Color.rgb,float3(1,.91,.96),saturate(p.y*.3+.6))*fold;
 return half4(col*i.color.rgb,alpha*i.color.a);}
 ENDCG }
 }
}

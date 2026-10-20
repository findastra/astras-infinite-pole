// Unity Stable shaders (Unity Unicorn), written by vine-spawn-with-trumpet-flowers, Claude Opus 5.5, 2026-10-19.
// Own work for Astra; free to use in her worlds. Vertex colours, Standard lighting, two-sided, petals glow, tips sway.
Shader "Unity Stable/Vine Bloom"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Gloss ("Stem and leaf gloss", Range(0,1)) = 0.32
        _PetalGloss ("Petal gloss", Range(0,1)) = 0.5
        _Glow ("Flower glow", Range(0,3)) = 0.2
        _Sway ("Sway (m)", Range(0,0.05)) = 0.006
        _SwaySpeed ("Sway speed", Range(0,4)) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
        #pragma target 3.0
        #pragma multi_compile_instancing
        fixed4 _Tint; half _Gloss, _PetalGloss, _Glow, _Sway, _SwaySpeed;
        struct Input { float4 color : COLOR; float facing : VFACE; };
        void vert(inout appdata_full v)
        {
            float free = v.texcoord2.x;
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            float t = _Time.y * _SwaySpeed + dot(wp, float3(1.7, 0.9, 1.3));
            float3 off = float3(sin(t), 0.5 * sin(t * 0.73 + 1.1), cos(t * 0.91)) * (_Sway * free);
            v.vertex.xyz += mul((float3x3)unity_WorldToObject, off);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            half petal = IN.color.a;
            o.Albedo = IN.color.rgb * _Tint.rgb;
            o.Smoothness = lerp(_Gloss, _PetalGloss, petal);
            o.Metallic = 0;
            o.Emission = IN.color.rgb * _Glow * petal;
            o.Normal = float3(0, 0, IN.facing > 0 ? 1 : -1);
        }
        ENDCG
    }
    FallBack "Diffuse"
}

// The flame layer (art bible: only light sources are saturated). The sprite is drawn in grey
// levels; this shader maps dark -> edge colour, mid -> body colour, bright -> core colour of the
// active flame, and pushes it above 1 (HDR) so bloom makes it glow. It ignores 2D lights: a
// flame is its own light.
Shader "Wickfall/FlameSprite"
{
    Properties
    {
        _MainTex("Flame (grey levels)", 2D) = "white" {}
        _Core("Core", Color) = (1, 0.914, 0.659, 1)
        _Body("Body", Color) = (1, 0.757, 0.302, 1)
        _Edge("Edge", Color) = (0.949, 0.541, 0.114, 1)
        _Emission("Emission", Range(0, 6)) = 1.8
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes { float3 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Core;
                half4 _Body;
                half4 _Edge;
                half _Emission;
            CBUFFER_END

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.color = v.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (tex.a <= 0.01) discard;
                half g = max(tex.r, max(tex.g, tex.b));
                half3 ramp = g < 0.45 ? _Edge.rgb : (g < 0.8 ? _Body.rgb : _Core.rgb);
                return half4(ramp * _Emission * i.color.rgb, tex.a * i.color.a);
            }
            ENDHLSL
        }
    }
}

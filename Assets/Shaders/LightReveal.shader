// PDF §4 "Shader LightReveal : Dans le Creux Profond, n'affiche le sol que là où la lumière du joueur arrive."
// The ground is drawn only where 2D light reaches it; outside the light it is fully transparent (the
// void shows through). A faint bone-white rim marks the edge of what the light reveals.
Shader "Wickfall/LightReveal"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _RevealStart("Reveal starts at light", Range(0, 1)) = 0.04
        _RevealFull("Fully revealed at light", Range(0, 1)) = 0.35
        _RimColor("Rim colour", Color) = (0.81, 0.77, 0.70, 1)
        _RimStrength("Rim strength", Range(0, 2)) = 0.55
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
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                half2 lightingUV : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _RevealStart;
                half _RevealFull;
                half4 _RimColor;
                half _RimStrength;
            CBUFFER_END

            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                o.color = v.color * _Color * unity_SpriteColor;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            half4 Frag(Varyings i) : SV_Target
            {
                half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                if (main.a <= 0.001) discard;

                InputData2D inputData;
                InitializeInputData(i.uv, i.lightingUV, inputData);

                SurfaceData2D surface;
                InitializeSurfaceData(main.rgb, main.a, mask, surface);
                half4 lit = CombinedShapeLightShared(surface, inputData);

                // The same lighting on a white surface = how much light reaches this pixel.
                SurfaceData2D probe;
                InitializeSurfaceData(half3(1, 1, 1), 1, mask, probe);
                half4 lightHere = CombinedShapeLightShared(probe, inputData);
                half amount = saturate(max(lightHere.r, max(lightHere.g, lightHere.b)));

                half reveal = smoothstep(_RevealStart, _RevealFull, amount);
                half rimBand = saturate(1 - abs(amount - _RevealStart * 1.6) / max(_RevealStart * 1.2, 0.001));
                half3 color = lit.rgb + _RimColor.rgb * rimBand * _RimStrength * 0.25;
                half alpha = main.a * saturate(max(reveal, rimBand * 0.6));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}

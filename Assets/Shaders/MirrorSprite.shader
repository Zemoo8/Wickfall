// The Reflections (PDF §2.2 Reflets): "pure mirror-black copy of the player, pale edge highlights,
// glitchy". Draws any sprite (the player's own frames) as polished black glass: a charcoal body,
// a pale rim where the silhouette ends (brighter where light reaches it), and horizontal glitch slices.
Shader "Wickfall/MirrorSprite"
{
    Properties
    {
        _MainTex("Sprite", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _BodyColor("Body", Color) = (0.055, 0.051, 0.047, 1)
        _RimColor("Rim", Color) = (0.93, 0.90, 0.83, 1)
        _RimMinimum("Rim in darkness", Range(0, 1)) = 0.28
        _Glitch("Glitch amount", Range(0, 1)) = 0
        _GlitchSeed("Glitch seed", Float) = 0
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

            struct Attributes { float3 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; half2 lightingUV : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _BodyColor;
                half4 _RimColor;
                half _RimMinimum;
                half _Glitch;
                float _GlitchSeed;
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

            float Hash(float n) { return frac(sin(n) * 43758.5453); }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                // Horizontal slices slide sideways by whole texels (a cracked mirror).
                float row = floor(uv.y / _MainTex_TexelSize.y / 3.0);
                float slide = (Hash(row + _GlitchSeed) - 0.5) * 2.0;
                uv.x += step(0.55, Hash(row * 1.7 + _GlitchSeed)) * round(slide * 2.0) * _MainTex_TexelSize.x * _Glitch;

                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                if (a <= 0.01) discard;
                float2 t = _MainTex_TexelSize.xy;
                half n = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, t.y)).a;
                half s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, t.y)).a;
                half e = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(t.x, 0)).a;
                half w = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(t.x, 0)).a;
                half edge = saturate(4 - (n + s + e + w));

                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv);
                InputData2D inputData;
                InitializeInputData(uv, i.lightingUV, inputData);
                SurfaceData2D probe;
                InitializeSurfaceData(half3(1, 1, 1), 1, mask, probe);
                half4 lightHere = CombinedShapeLightShared(probe, inputData);
                half amount = saturate(max(lightHere.r, max(lightHere.g, lightHere.b)));

                half3 body = _BodyColor.rgb * (0.6 + 0.4 * amount);
                half rim = edge * max(_RimMinimum, amount);
                half3 color = lerp(body, _RimColor.rgb, rim);
                return half4(color, a * i.color.a);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}

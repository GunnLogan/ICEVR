Shader "Custom/FootstepOverlay_URP_VR_Emission_Clean"
{
    Properties
    {
        _StepMaskLeft     ("Left Footprint Mask",    2D) = "white" {}
        _StepMaskRight    ("Right Footprint Mask",   2D) = "white" {}
        _StepLength       ("Step Spacing",           Float) = 1.0
        _FootSize         ("Foot Size",              Float) = 0.3
        _StepCount        ("Steps to Reveal",        Float) = 5
        _EmissionColor    ("Emission Color",         Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength",      Float) = 2.0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Globals set from C#
            float4 _TeleportStart;
            float4 _TeleportEnd;

            TEXTURE2D(_StepMaskLeft);  SAMPLER(sampler_StepMaskLeft);
            TEXTURE2D(_StepMaskRight); SAMPLER(sampler_StepMaskRight);

            half4 _EmissionColor;
            float _EmissionStrength;

            CBUFFER_START(UnityPerMaterial)
                float _StepLength;
                float _FootSize;
                float _StepCount;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                ZERO_INITIALIZE(Varyings, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 worldPos = TransformObjectToWorld(IN.positionOS);
                OUT.positionHCS = TransformObjectToHClip(worldPos);
                OUT.worldPos    = worldPos;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 camPos  = _TeleportStart.xyz;
                float3 targPos = _TeleportEnd.xyz;
                float3 dir     = normalize(targPos - camPos);
                float total    = distance(targPos, camPos);
                if (_StepCount <= 0 || total < _StepLength * 0.5) clip(-1);

                float proj = dot(IN.worldPos - camPos, dir);
                if (proj < 0 || proj > total) clip(-1);

                float idx = floor(proj / _StepLength + 0.5);
                if (idx > _StepCount) clip(-1);

                float3 stepPos = camPos + dir * idx * _StepLength;
                float dist = distance(IN.worldPos, stepPos);
                float maskUV = saturate(dist / _FootSize);

                float leftA  = SAMPLE_TEXTURE2D(_StepMaskLeft,  sampler_StepMaskLeft, float2(maskUV,0.5)).r;
                float rightA = SAMPLE_TEXTURE2D(_StepMaskRight, sampler_StepMaskRight, float2(maskUV,0.5)).r;
                float footA  = (fmod(idx, 2.0) < 1.0) ? leftA : rightA;
                if (footA <= 0) clip(-1);

                half3 glow = _EmissionColor.rgb * (_EmissionColor.a * _EmissionStrength) * footA;
                return half4(glow, footA);
            }
        ENDHLSL

        Pass
        {
            Name "TransparentFootsteps"
            Tags { "LightMode"="UniversalForwardTransparent" }
            Cull Off              // draw both sides
            ZTest Always          // render over all geometry
            Blend One One         // additive blending
            ZWrite Off            // no depth writes

            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing
                #pragma multi_compile_fog
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
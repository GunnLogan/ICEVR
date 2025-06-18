Shader "Custom/PulseShader/TransparentAlpha_Stereo_URP"
{
    Properties
    {
        _BaseColor      ("Base Color",       Color) = (1,1,1,1)
        _EmissionColor  ("Emission Color",   Color) = (1,1,1,1)
        _PulseSpeed     ("Pulse Speed",      Float) = 2.0
        _PulseIntensity("Emission Intensity", Float) = 0.2
        _PulseAmount    ("Max Scale Add",    Float) = 0.2
        _Alpha          ("Base Alpha",       Range(0,1)) = 0.5
        _UseGlobalTime  ("Auto Pulse?",      Float) = 1.0
        _TriggerTime    ("Trigger Time",     Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 200

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" "StereoInstancing"="True" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Uniforms
            float4 _BaseColor;
            float4 _EmissionColor;
            float  _PulseSpeed;
            float  _PulseIntensity;
            float  _PulseAmount;
            float  _Alpha;
            float  _UseGlobalTime;
            float  _TriggerTime;

            // Compute normalized pulse [0..1]
            float ComputePulse(float time)
            {
                // 2*PI = 6.28318530718
                float raw = sin(time * _PulseSpeed * 6.28318530718) * 0.5 + 0.5;
                return pow(raw, 1.2);
            }

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  pulseNorm  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Choose time source
                float t = (_UseGlobalTime > 0.5)
                          ? _Time.y
                          : (_Time.y - _TriggerTime);

                // Compute pulse
                float pulse = ComputePulse(t);
                o.pulseNorm = pulse;

                // Scale object
                float scale = 1.0 + _PulseAmount * pulse;
                float3 posWS = TransformObjectToWorld(v.positionOS * scale);
                o.positionCS = TransformWorldToHClip(posWS);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Color = base + emission * pulse
                float3 col = _BaseColor.rgb + _EmissionColor.rgb * i.pulseNorm * _PulseIntensity;
                float  a   = _Alpha * i.pulseNorm;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}

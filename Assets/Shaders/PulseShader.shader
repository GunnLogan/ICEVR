Shader "Custom/PulseShader/TransparentAlpha_Stereo"
{
    Properties
    {
        _BaseColor      ("Base Color",       Color) = (1,1,1,1)
        _EmissionColor  ("Emission Color",   Color) = (1,1,1,1)
        _PulseSpeed     ("Pulse Speed",      Float) = 2.0
        _PulseIntensity ("Emission Intensity", Float) = 0.2
        _PulseAmount    ("Max Scale Add",    Float) = 0.2
        _Alpha          ("Base Alpha",       Range(0,1)) = 0.5
        _UseGlobalTime  ("Auto Pulse?",      Float) = 1.0
        _TriggerTime    ("Trigger Time",     Float) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "StereoTargetEye"="Both" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 200

        Pass
        {
            // tell Unity to use instanced stereo (single-pass)
            Tags { "StereoInstancing"="True" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex    : POSITION;
                float3 normal    : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos           : SV_POSITION;
                float3 worldNormal   : NORMAL;
                float  pulseNorm     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _BaseColor;
            fixed4 _EmissionColor;
            float _PulseSpeed;
            float _PulseIntensity;
            float _PulseAmount;
            float _Alpha;
            float _UseGlobalTime;
            float _TriggerTime;

            float ComputePulse(float time)
            {
                float raw = sin(time * _PulseSpeed * UNITY_PI * 2.0) * 0.5 + 0.5;
                return pow(raw, 1.2);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float t = (_UseGlobalTime > 0.5) 
                          ? _Time.y 
                          : (_Time.y - _TriggerTime);

                float pulse = ComputePulse(t);
                o.pulseNorm = pulse;

                float scale = 1.0 + _PulseAmount * pulse;
                float4 worldPos = float4(v.vertex.xyz * scale, 1.0);
                o.pos = UnityObjectToClipPos(worldPos);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(i.worldNormal, L));
                fixed3 baseCol = _BaseColor.rgb * NdotL;
                fixed3 emi = _EmissionColor.rgb * i.pulseNorm * _PulseIntensity;
                float a = _Alpha * i.pulseNorm;

                return fixed4(baseCol + emi, a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
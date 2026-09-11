Shader "Academic/Gradient Background"
{
    Properties
    {
        _Top ("Top", Color) = (.24,.26,.29,1)
        _Bottom ("Bottom", Color) = (.035,.04,.05,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 position : SV_POSITION; float4 screen : TEXCOORD0; };
            float4 _Top, _Bottom;
            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                o.position = UnityObjectToClipPos(vertex);
                o.screen = ComputeScreenPos(o.position);
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                return lerp(_Bottom, _Top, saturate(i.screen.y / i.screen.w));
            }
            ENDHLSL
        }
    }
}

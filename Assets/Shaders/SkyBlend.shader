// v75 sky: three HDR cubemaps (day, golden hour dusk, night with the city glow on the horizon) mixed by DayCycle,
// so 14:00, 18:08, 19:00 and 22:00 each get their own sky instead of one tinted golden hour sky.
Shader "StreetEmpire/SkyBlend"
{
    Properties
    {
        [NoScaleOffset] _DayTex ("Day", Cube) = "grey" {}
        [NoScaleOffset] _DuskTex ("Dusk", Cube) = "grey" {}
        [NoScaleOffset] _NightTex ("Night", Cube) = "black" {}
        _Weights ("Weights (day, dusk, night)", Vector) = (0, 1, 0, 0)
        _Exposure ("Exposure", Range(0, 8)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            samplerCUBE _DayTex, _DuskTex, _NightTex;
            half4 _DayTex_HDR, _DuskTex_HDR, _NightTex_HDR;
            float4 _Weights; half _Exposure;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.pos = UnityObjectToClipPos(v); o.dir = v.xyz; return o; }
            half4 frag(v2f i) : SV_Target
            {
                half3 d = DecodeHDR(texCUBE(_DayTex, i.dir), _DayTex_HDR);
                half3 k = DecodeHDR(texCUBE(_DuskTex, i.dir), _DuskTex_HDR);
                half3 n = DecodeHDR(texCUBE(_NightTex, i.dir), _NightTex_HDR);
                half3 c = d * _Weights.x + k * _Weights.y + n * _Weights.z;
                return half4(c * _Exposure, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}

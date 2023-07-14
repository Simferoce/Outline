Shader "Custom/Wireframe"
{
    Properties
    {
        _WireframeColor("Color", Color) = (0,0,0,1)
        _WireframeThickness("Thickness", Range(0,10)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
        //ZWrite Off
        //Cull off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 positionOS : POSITION;
            };

            struct v2g
            {
                float4 positionCS : SV_POSITION;
            };

            struct g2f 
            {
                float4 positionCS : SV_POSITION;
                float3 barys : TEXCOORD9;
            };

            v2g vert (appdata v)
            {
                v2g o;
                o.positionCS = UnityObjectToClipPos(v.positionOS);
                return o;
            }

            float4 _WireframeColor;
            float _WireframeThickness;

            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> stream)
            {
                g2f v0;
                g2f v1;
                g2f v2;

                v0.positionCS = input[0].positionCS;
                v0.barys = float3(1, 0, 0);
                v1.positionCS = input[1].positionCS;
                v1.barys = float3(0, 1, 0);
                v2.positionCS = input[2].positionCS;
                v2.barys = float3(0, 0, 1);

                stream.Append(v0);
                stream.Append(v1);
                stream.Append(v2);
                stream.RestartStrip();
            }

            fixed4 frag (g2f i) : SV_Target
            {
                float3 deltas = fwidth(i.barys);
                i.barys = step(deltas * _WireframeThickness, i.barys);
                float minBary = min(i.barys.x, min(i.barys.y, i.barys.z));
                _WireframeColor.a = 1 - minBary;
                return _WireframeColor;
            }
            ENDCG
        }
    }
}

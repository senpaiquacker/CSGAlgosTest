Shader "Test/Wireframe"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_WireColor("Wire Color", Color) = (1,0,0,1)
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
				#include "UnityCG.cginc"
				#pragma target 5.0
				#pragma vertex vert
				#pragma geometry geom
				#pragma fragment frag
				
				
				half4 _Color, _WireColor;
				
				struct v2g
				{
					float4 position : POSITION;
					float2 uv : TEXCOORD0;
				};
				struct v2f
				{
					float4 position : SV_POSITION;
					float3 dist: TEXCOORD1;
					float2 uv : TEXCOORD0;
				};
				
				v2g vert(appdata_base IN)
				{
					v2g OUT;
					OUT.position = UnityObjectToClipPos(IN.vertex);
					OUT.uv = IN.texcoord;
					return OUT;
				}
				
				[maxvertexcount(3)] void geom(triangle v2g input[3], inout LineStream<v2f> output)
				{
					//float2 RESOLUTION = float2(_SreenParams.x/2.0, _SreenParams.y/2.0);
					
					float2 p0 = input[0].position.xy / input[0].position.w;
					float2 p1 = input[1].position.xy / input[1].position.w;
					float2 p2 = input[2].position.xy / input[2].position.w;
					
					float2 v0 = p2-p1;
					float2 v1 = p2-p0;
					float2 v2 = p1-p0;
					float area = abs(v1.x * v2.y - v1.y * v2.x);
					
					v2f OUT;
					
					OUT.position = input[0].position;
					OUT.uv = input[0].uv;
					OUT.dist = float3(area/length(v0),0,0);
					output.Append(OUT);

					OUT.position = input[1].position;
					OUT.uv = input[1].uv;
					output.Append(OUT);

					OUT.position = input[2].position;
					OUT.uv = input[2].uv;
					OUT.dist = float3(0,0,area/length(v2));
					output.Append(OUT);
				}
				
				half4 frag(v2f IN) : COLOR
				{
					float d = min(IN.dist.x, min(IN.dist.y, IN.dist.z));
					float I = exp2(-4.0*d*d);
					return lerp(_Color, _WireColor, I);
				}

			ENDCG
		}
	}
}

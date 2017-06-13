Shader "Unlit/ParticleVisualize"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Blend One One
		ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#pragma target 5.0
			
			#include "UnityCG.cginc"
			#include "ParticleCommon.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 col : TEXCOORD1;
				float size : TEXCOORD2;
				float4 pos : SV_POSITION;
			};
			
			StructuredBuffer<ParticleData> _Particles;
			StructuredBuffer<uint> _Active;

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (uint id : SV_VertexID)
			{
				uint idx = _Active[id];
				v2f o;
				o.pos = float4(_Particles[idx].position, 1);
				o.uv = float2(0,0);
				o.col = _Particles[idx].color;
				o.size = _Particles[idx].isActive ? _Particles[idx].size : 0; // 有効出ないときはサイズを0にする

				return o;
			}
			
			[maxvertexcount(4)]
			void geom(point v2f input[1], inout TriangleStream<v2f> outStream)
			{
				v2f o;

				// 全ての頂点で共通の値を計算しておく
				float4 pos = input[0].pos;
				float4 col = input[0].col;
				o.size = input[0].size;

				for (int x = 0; x < 2; x++)
				{
					for (int y = 0; y < 2; y++)
					{
						// ビルボード用の行列
						float4x4 billboardMatrix = UNITY_MATRIX_V;
						billboardMatrix._m03 = billboardMatrix._m13 = billboardMatrix._m23 = billboardMatrix._m33 = 0;

						float2 uv = float2(x, y);
						o.uv = uv;

						o.pos = pos + mul(float4((uv * 2 - float2(1, 1)) * o.size, 0, 1), billboardMatrix);
						o.pos = mul(UNITY_MATRIX_VP, o.pos);

						o.col = col;

						outStream.Append(o);
					}
				}
				outStream.RestartStrip();
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv) * i.col;
				return col;
			}
			ENDCG
		}
	}
}

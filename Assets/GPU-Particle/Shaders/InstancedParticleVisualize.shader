Shader "Unlit/InstancedParticleVisualize"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma target 5.0
			
			#include "UnityCG.cginc"
			#include "ParticleCommon.cginc"
			#include "Assets/CGINC/Quaternion.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float3 normal : TEXCOORD0;
				float4 col : TEXCOORD1;
				float4 pos : SV_POSITION;
			};
			
			StructuredBuffer<ParticleData> _Particles;
			StructuredBuffer<uint> _Active;
			
			v2f vert (appdata v, uint instanceID : SV_InstanceID)
			{
				uint idx = _Active[instanceID];
				ParticleData p = _Particles[idx];
				
				float4 r = fromToRotation(float3(0,0,1), normalize(p.velocity));
				v.vertex.xyz = rotateWithQuaternion(v.vertex.xyz, r);
				v.vertex.xyz *= p.size * sqrt(p.duration);
				v.vertex.xyz += p.position;

				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.normal = rotateWithQuaternion(v.normal, r);
				o.col = p.color;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 n = i.normal;
				float l = dot(n, float3(1,1,0));
				
				return saturate(l)*i.col + float4(0,0,1,0) * pow(max(0, 0.1/n.z),2);
			}
			ENDCG
		}
	}
}

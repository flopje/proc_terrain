Shader "Test/FlatShading2"
{
	Properties
	{
		_Color("Color", Color) = (1,0,0,1)
		_SpecColor("Specular Material Color", Color) = (1,1,1,1)
		_MainTex("Color (RGB) Alpha (A)", 2D) = "white"
		_Shininess("Shininess", Float) = 1.0
		_OpacityWater("Water opacity", Float) = 1.0
		_WaveLength("Wave length", Float) = 0.5
		_WaveHeight("Wave height", Float) = 0.5
		_WaveSpeed("Wave speed", Float) = 1.0
		_RandomHeight("Random height", Float) = 0.5
		_RandomSpeed("Random Speed", Float) = 0.5
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "LightMode" = "ForwardBase" }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma Lambert alpha

			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surf Standard fullforwardshadows

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			float rand(float3 co)
			{
				return frac(sin(dot(co.xyz ,float3(12.9898,78.233,45.5432))) * 43758.5453);
			}

			float rand2(float3 co)
			{
				return frac(sin(dot(co.xyz ,float3(19.9128,75.2,34.5122))) * 12765.5213);
			}

			float _WaveLength;
			float _WaveHeight;
			float _WaveSpeed;
			float _RandomHeight;
			float _RandomSpeed;
			float _OpacityWater;

			uniform float4 _LightColor0;

			sampler2D _MainTex;
			uniform float4 _Color;
			uniform float4 _SpecColor;
			uniform float _Shininess;

			struct v2f
			{
				float3  wPos : POSITION1;
				float4  pos : SV_POSITION;
				float3	norm : NORMAL;
				float2  uv : TEXCOORD0;
				LIGHTING_COORDS(1,2)
			};

			float4 _MainTex_ST;

			v2f vert(appdata_full v)
			{
				float3 v0 = mul(_Object2World, v.vertex).xyz;
				
				float phase0 = (_WaveHeight)* sin((_Time[1] * _WaveSpeed) + (v0.x * _WaveLength) + (v0.z * _WaveLength) + rand2(v0.xzz));
				float phase0_1 = (_RandomHeight)*sin(cos(rand(v0.xzz) * _RandomHeight * cos(_Time[1] * _RandomSpeed * sin(rand(v0.xxz)))));

				v0.y += phase0 + phase0_1;

				v.vertex.xyz = mul((float3x3)_World2Object, v0);

				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.norm = v.normal;
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.wPos = mul(_Object2World, v.vertex);
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				//o.Alpha = _MainText.a;
				return o;
			}

			fixed4 frag(v2f IN) : COLOR
			{
				float3 x = ddx(IN.wPos);
				float3 y = ddy(IN.wPos);
				float3 vn = -normalize(cross(x, y));

				#if UNITY_UV_STARTS_AT_TOP
					vn = normalize(cross(x, y));
				#endif

				float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				float attenuation = LIGHT_ATTENUATION(IN);

				float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;

				float3 diffuseReflection =
				attenuation * _LightColor0.rgb * _Color.rgb
				* max(0.0, dot(vn, lightDirection)); //For some reason this line will always be 0 thus making diffuseReflection 0

				fixed4 texcol = tex2D(_MainTex, IN.uv).a;
				return fixed4(texcol * (ambientLighting + diffuseReflection), _OpacityWater);
			}

			ENDCG
		}
	}
	FallBack "Diffuse"
}
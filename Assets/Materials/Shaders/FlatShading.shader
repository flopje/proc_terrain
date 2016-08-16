Shader "Custom/FlatShading" {
		Properties
		{
			_Color("Color", Color) = (1,1,1,1)
			_MainTex("Albedo (RGB)", 2D) = "white" {}
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

				#include "UnityCG.cginc"
				#include "AutoLight.cginc"

				// Physically based Standard lighting model, and enable shadows on all light types
				//#pragma  surf Standard fullforwardshadows

				// Use shader model 3.0 target, to get nicer looking lighting
				#pragma target 3.0

				uniform float4 _LightColor0;

				sampler2D _MainTex;
				uniform float4 _Color;
				
				#if UNITY_UV_STARTS_AT_TOP
				/*if (_MainTex_TexelSize.y < 0) 
				{
					uv.y = 1 - uv.y;
				}*/
						
				#endif

				struct v2f
				{
					float3  wPos : POSITION1;
					float4  pos : SV_POSITION;
					float3	norm : NORMAL;
					float2  uv : TEXCOORD0;
					LIGHTING_COORDS(1,2)
				};

				float4 _MainTex_ST;

				float4 vert1(float2 uv : TEXCOORD0) : SV_POSITION
				{
					float4 pos;
					pos.xy = uv;
					// we're rendering with upside-down flipped projection,
					// so flip the vertical UV coordinate too
					if (_ProjectionParams.x < 0)
						pos.y = 1 - pos.y;
					pos.z = 0;
					pos.w = 1;
					return pos;
				}

				v2f vert(appdata_full v)
				{
					v2f o;
					
					o.norm = v.normal;
					o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
					o.wPos = mul(_Object2World, v.vertex);
					o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
					TRANSFER_VERTEX_TO_FRAGMENT(o);
					return o;
				}

				fixed4 frag(v2f IN) : COLOR
				{
					float3 x = ddx(IN.wPos);
					float3 y = ddy(IN.wPos);
					float3 vn = -normalize(cross(x, y));;

					#if UNITY_UV_STARTS_AT_TOP
						vn = normalize(cross(x, y));
					#endif

					float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
					float attenuation = LIGHT_ATTENUATION(IN);

					float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;

					float3 diffuseReflection =
						attenuation * _LightColor0.rgb * _Color.rgb
						* max(0.0, dot(vn, lightDirection)); //For some reason this line will always be 0 thus making diffuseReflection 0

					fixed4 texcol = tex2D(_MainTex, IN.uv);
					return fixed4(texcol * (ambientLighting + diffuseReflection), 1.0);
				}

			ENDCG
			}
		}
		FallBack "Diffuse"
	}
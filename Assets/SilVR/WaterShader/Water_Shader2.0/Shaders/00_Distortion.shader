Shader "Custom/Distortion" {
	Properties
	{
		_MainTex("Render Texture", 2D) = "black" {}
		_Refraction ("Refraction", Range (0.00, 30.0)) = 1.0
		_Power ("Power", Range (1.00, 100.0)) = 1.0
		_AlphaPower ("Vertex Alpha Power", Range (1.00, 100.0)) = 1.0
		//_BumpMap( "Normal Map", 2D ) = "bump" {}
		_imgRes("Image resolution", float) = 270

		_maxRefraction("Max refraction", Range(0,1)) = 1

		_Cull ( "Face Culling", Int ) = 2
	}

	SubShader
	{
		Tags { "Queue" = "Transparent+1000" "RenderType" = "Transparent" }

		GrabPass
		{
			"_GrabTextureWorld"
		}
		
		Pass
		{
			Cull [_Cull]

			CGPROGRAM
				#pragma target 3.0
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc"
				#include "UnityStandardUtils.cginc"
				#include "UnityStandardInput.cginc"


				  bool isOrthographic()
					{
						return UNITY_MATRIX_P[3][3] == 1;
					}

				// From Valve's Lab Renderer, Copyright (c) Valve Corporation, All rights reserved. 
				float3 Vec3TsToWs( float3 vVectorTs, float3 vNormalWs, float3 vTangentUWs, float3 vTangentVWs )
				{
					float3 vVectorWs;
					vVectorWs.xyz = vVectorTs.x * vTangentUWs.xyz;
					vVectorWs.xyz += vVectorTs.y * vTangentVWs.xyz;
					vVectorWs.xyz += vVectorTs.z * vNormalWs.xyz;
					return vVectorWs.xyz; // Return without normalizing
				}

				// From Valve's Lab Renderer, Copyright (c) Valve Corporation, All rights reserved. 
				float3 Vec3TsToWsNormalized( float3 vVectorTs, float3 vNormalWs, float3 vTangentUWs, float3 vTangentVWs )
				{
					return normalize( Vec3TsToWs( vVectorTs.xyz, vNormalWs.xyz, vTangentUWs.xyz, vTangentVWs.xyz ) );
				}

				struct VS_INPUT
				{
					float4 vPosition : POSITION;
					float3 vNormal : NORMAL;
					float2 vTexcoord0 : TEXCOORD0;
					float4 vTangentUOs_flTangentVSign : TANGENT;
					float4 vColor : COLOR;
				};

				struct PS_INPUT
				{
					float4 vGrabPos : TEXCOORD0;
					float4 vPos : SV_POSITION;
					float4 vColor : COLOR;
					float2 vTexCoord0 : TEXCOORD1;
					float3 vNormalWs : TEXCOORD2;
					float3 vTangentUWs : TEXCOORD3;
					float3 vTangentVWs : TEXCOORD4;
					float3 xAxis : TEXCOORD5;
					float2 uv : TEXCOORD6;

				};

				PS_INPUT vert(VS_INPUT i)
				{


					PS_INPUT o;
					
					o.uv = float2(i.vTexcoord0.x*.5+.5,i.vTexcoord0.y);

					o.xAxis = mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0));

					// Clip space position
					o.vPos = UnityObjectToClipPos(i.vPosition);
					
					// Grab position
					o.vGrabPos = ComputeGrabScreenPos(o.vPos);
					
					// World space normal
					o.vNormalWs = UnityObjectToWorldNormal(i.vNormal);

					// Tangent
					o.vTangentUWs.xyz = UnityObjectToWorldDir( i.vTangentUOs_flTangentVSign.xyz ); // World space tangentU
					o.vTangentVWs.xyz = cross( o.vNormalWs.xyz, o.vTangentUWs.xyz ) * i.vTangentUOs_flTangentVSign.w;

					// Texture coordinates
					o.vTexCoord0.xy = i.vTexcoord0.xy;

					// Color
					o.vColor = i.vColor;

					return o;
				}

				sampler2D _GrabTextureWorld;
				float _Refraction;
				float _Power;
				float _AlphaPower;
				float _maxRefraction;
				//sampler2D _MainTex;
				float _imgRes;

				float4 frag(PS_INPUT i) : SV_Target
				{
					// Tangent space normals

					if (isOrthographic())
					{
						clip(-1);
					}

					fixed4 col = tex2D(_MainTex, i.uv);

					//float2 uv = i.uv;
					float q = 1 / _imgRes;

					float2 cauv = i.uv + float2(0, q);
					float2 cbuv = i.uv + float2(0, -q);
					float2 ccuv = i.uv + float2(q, 0);
					float2 cduv = i.uv + float2(-q, 0);
	
					float2 caauv = i.uv + float2(q, q);
					float2 cbbuv = i.uv + float2(q, -q);
					float2 cccuv = i.uv + float2(q, q);
					float2 cdduv = i.uv + float2(-q, -q);
	
					fixed4 d = tex2D(_MainTex, i.uv).x;
	
					fixed4 ca = tex2D(_MainTex, cauv).x;
					fixed4 cb = tex2D(_MainTex, cbuv).x;
					fixed4 cc = tex2D(_MainTex, ccuv).x;
					fixed4 cd = tex2D(_MainTex, cduv).x;
	
					fixed4 caa = tex2D(_MainTex, cauv).x;
					fixed4 cbb = tex2D(_MainTex, cbuv).x;
					fixed4 ccc = tex2D(_MainTex, ccuv).x;
					fixed4 cdd = tex2D(_MainTex, cduv).x;

					float diffXY1 = caa - cbb;
					float diffXY2 = ccc - cdd;

					float diffY = ca - cb;
					float diffX = cc - cd;

					float4 c = float4(.5 + -diffX * .5, .5 + -diffY * .5, 1, .5);

					//float3 vNormalTs = (-diffX, -diffY, 1);

					//float3 vNormalTs = UnpackScaleNormal( tex2D( _BumpMap, i.vTexCoord0.xy ), 1 );

					float3 vNormalTs = UnpackScaleNormal(c, 1);
					
					//float3 vNormalTs = tex2D(_BumpMap, i.vTexCoord0.xy);

					// Tangent space -> World space
					float3 vNormalWs = Vec3TsToWsNormalized( vNormalTs.xyz, i.vNormalWs.xyz, i.vTangentUWs.xyz, i.vTangentVWs.xyz );

					// World space -> View space
					//float3 vNormalVs = normalize(mul((float3x3)UNITY_MATRIX_V, vNormalWs));


					// Calculate offset
					float2 offset = (vNormalWs.xz - float2(0, 0)) * _Refraction;
					
					//offset *= pow(length(vNormalVs.xy), _Power);

					// Scale to pixel size
					offset /= float2(_ScreenParams.x, _ScreenParams.y);

					// Scale with vertex alpha
					offset *= pow(i.vColor.a, _AlphaPower);

					// Sample grab texture
					float4 vDistortColor = tex2Dproj(_GrabTextureWorld, i.vGrabPos + float4(offset.y, offset.y, 0.0, 0.0));

					// Debug normals
					//return float4(offset.y*255, offset.x*255, 0, 1);

					return vDistortColor;
					//return c;
				}
			ENDCG
		}
	}
}
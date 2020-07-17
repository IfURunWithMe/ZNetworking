﻿//Written by ifurkend of Moonflower Carnivore in 2017. All rights reserved.
//In case you really want to support Shader Model 1 which is completely antique, comment lines 7, 49 and 80 which involves _Bias, then uncomment line 79.
Shader "Particles/Alpha Blended Cubemap Stretch" {
	Properties {
		_TintColor ("Tint Color", Color) = (0.16, 0.16, 0.16, 1)
		_MainTex ("Base (RGB) Mask (A)", 2D) = "white" {}
		[NoScaleOffset] _Cube ("Cubemap (6 frames layout)", Cube) = "grey" {}
		//_AmbientPow ("Ambient Power", Range(0,1)) = 0.5
		_Bias ("Cubemap Bias (blur)", Range(0,8)) = 0//The greater bias, the more blur is the cubemap. This requires the cubemap has mip maps generated in texture settings.
		_CubemapPow ("Cubemap Intensity", Range(0, 2)) = 0.7//Opacity of cubemap.
		_Stretch ("Circular Stretch", Range(-10,10)) = -0.3//How much the cubemap is stretched in (positive) or out (negative) in roughly the circular shape.
		_CubemapOffset ("Cubemap Offset", Range(-2, 2)) = -0.15//How much the cubemap is offset corresponding to the view position of each vertex.
		_DLightPow ("Dir Light Power", Range(0,100)) = 0.5//How much the main directional light affects the material.
		_Glow ("Intensity", Range(0, 100)) = 0//Only increase the _Glow/Intensity when HDR mode is enabled in your rendering camera.
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "PreviewType" = "Plane"}
		Cull Back
		ZTest On
		ZWrite Off
		Lighting On
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				
				#pragma multi_compile_particles
				
				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc"

				struct appdata {
					float4 vertex : POSITION;
					half2 uv : TEXCOORD0;
					half4 color : COLOR;
					float3 normal : NORMAL;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					half2 uv : TEXCOORD0;
					//half3 worldRefl : TEXCOORD1;
					half3 normal : TEXCOORD1;
					half2 viewVertex : TEXCOORD2;
					half4 color : COLOR;
				};
				
				half4 _TintColor;
				sampler2D _MainTex;
				half4 _MainTex_ST;
				samplerCUBE _Cube;
				half _CubemapPow;
				half _Bias;
				//half _AmbientPow;
				half _DLightPow;
				half _Glow;
				half _Stretch;
				half _CubemapOffset;
				
				v2f vert (appdata v) {
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.viewVertex = mul(o.vertex, UNITY_MATRIX_P).xy * _CubemapOffset;
					o.uv = TRANSFORM_TEX(v.uv,_MainTex);
					o.normal = v.normal;
					o.color = v.color;
					//o.color.rgb += ShadeSH9(half4(UnityObjectToWorldNormal(v.normal),1)) * _AmbientPow;
					o.color.rgb += _LightColor0.rgb * _DLightPow;
					o.color.rgb += _Glow;
					/*
					float3 worldNormal = UnityObjectToWorldNormal(float3(
					v.normal.xy,
					//v.normal.z
					v.normal.z*2-1
					));
					o.worldRefl = reflect(float3(
					v.uv.x-0.5,
					v.uv.y-0.5,
					0.5
					), worldNormal);
					*/
					return o;
				}
				
				fixed4 frag (v2f i) : SV_Target {
					half2 uvRepos = i.uv * 2 - 1;
					half xSq = uvRepos.x * uvRepos.x;
					half ySq = uvRepos.y * uvRepos.y;
					half zSq = xSq + ySq;
					float3 worldNormal = UnityObjectToWorldNormal(float3(
					i.normal.xy,
					//v.normal.z
					i.normal.z*2-1
					));
					float3 worldRefl = reflect(float3(
					uvRepos.x * pow(abs(uvRepos.x / xSq/zSq), _Stretch) * 0.5 + i.viewVertex.x,
					uvRepos.y * pow(abs(uvRepos.y / ySq/zSq), _Stretch) * 0.5 + i.viewVertex.y,
					0.5
					), worldNormal);
					//fixed4 cubemap = texCUBE (_Cube, i.worldRefl) * i.color * _TintColor;
					fixed4 cubemap = texCUBElod (_Cube, half4(worldRefl,_Bias));
					fixed4 maintex2d = tex2D (_MainTex, i.uv);
					fixed4 col;
					//col.rgb = (cubemap.rgb * _CubemapPow * maintex2d + maintex2d.rgb * _TintColor.rgb) * i.color.rgb;
					col.rgb = lerp(maintex2d.rgb * _TintColor.rgb, cubemap.rgb * maintex2d, _CubemapPow) * i.color.rgb;
					col.a = cubemap.a * maintex2d.a * i.color.a * _TintColor.a;// * (cubemap.r * 0.33 + cubemap.g * 0.33 + cubemap.b * 0.33);
					return col;
				}
			ENDCG
		}
	}
}
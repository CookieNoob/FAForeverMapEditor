﻿// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/PropShader" {
	Properties {
		//_Color ("Color", Color) = (1,1,1,1)
		[MaterialEnum(Off,0,On,1)] 
		_GlowAlpha ("Glow (Alpha)", Int) = 0
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap ("Normal ", 2D) = "gray" {}
		_Clip ("Alpha cull", Range (0, 1)) = 0.5
		//_Glossiness ("Smoothness", Range(0,1)) = 0.5
		//_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="TransparentCutout" "Queue" = "Transparent+1" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf BlinnPhong noforwardadd addshadow novertexlights noshadowmask halfasview interpolateview

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex, _BumpMap;
		half _Clip;
		struct Input {
			float2 uv_MainTex;
		};

		//fixed4 _SunColor;
		//fixed4 _SunAmbience;
		//fixed4 _ShadowColor;

		//half _Glossiness;
		//half _Metallic;
		//fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		fixed _GlowAlpha;

		inline fixed3 UnpackNormalDXT5nmScaled (fixed4 packednormal, fixed scale)
		{
			fixed3 normal = 0;
			normal.xz = packednormal.wx * 2 - 1;
			normal.y = sqrt(1 - saturate(dot(normal.xz, normal.xz)));
			normal.xz *= scale;

			return normal.xzy;
		}

		void surf (Input IN, inout SurfaceOutput o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * 0.65;
			o.Albedo = c.rgb;
			fixed4 n = tex2D(_BumpMap, IN.uv_MainTex);
			o.Normal = UnpackNormalDXT5nmScaled(n, 1 );

			if(_GlowAlpha > 0){
				o.Emission = c.rgb * c.a;
			}
			else{
				clip(c.a * 2 - _Clip);
				o.Alpha = c.a * 2;
			}
		}
		ENDCG
	}

	FallBack "Standard"
}

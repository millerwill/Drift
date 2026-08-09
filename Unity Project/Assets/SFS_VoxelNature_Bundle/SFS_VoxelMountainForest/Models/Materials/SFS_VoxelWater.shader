// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SFS/VoxelWater"
{
	Properties
	{
		_Smoothness("Smoothness", Range( 0 , 1)) = 0.8
		_LightColor("Light Color", Color) = (1,1,1,0)
		_DeepColor("Deep Color", Color) = (0,0.3608235,0.3882353,0)
		_WaterDepth("Water Depth", Range( 0 , 5)) = 1
		_Refraction("Refraction", Range( 0 , 0.2)) = 0.1
		_WaveScale("Wave Scale", Range( 0 , 1)) = 0.1
		_WaveSpeed("Wave Speed", Range( 0 , 0.3)) = 0.1
		_WaveHeight("Wave Height", Range( 0 , 10)) = 1
		_WaveDirection("Wave Direction", Vector) = (1,1,0,0)
		_WaterFoam("Water Foam", 2D) = "white" {}
		_WaveFoamTiling("Wave Foam Tiling", Range( 0.1 , 10)) = 2
		_WaveFoamSpeed("Wave Foam Speed", Range( 0 , 0.2)) = 0.01
		_WaveFoamOpacity("Wave Foam Opacity", Range( 0 , 1)) = 0.5
		_NormalMap("Normal Map", 2D) = "white" {}
		_NormalScale("Normal Scale", Range( 0 , 3)) = 0.24
		_NormalSpeed("Normal Speed", Range( 0 , 1)) = 0.01
		_NormalStrength("Normal Strength", Range( 0 , 1)) = 0.3
		_NormalMainDirection("Normal Main Direction", Vector) = (1,0,0,0)
		_NormalSecondDirection("Normal Second Direction", Vector) = (-1,0,0,0)
		_EdgeSize("Edge Size", Range( 0 , 1.5)) = 0
		_EdgePower("Edge Power", Range( 0 , 10)) = 1
		_EdgeColor("Edge Color", Color) = (1,1,1,0)
		_EdgeFoamSize("Edge Foam Size", Range( 0 , 2)) = 1.5
		_EdgeFoamOpacity("Edge Foam Opacity", Range( 0 , 1)) = 0.75
		_EdgeFoamFade("Edge Foam Fade", Range( 0 , 1)) = 1
		_Metallic("Metallic", Range( 0 , 1)) = 0
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		GrabPass{ }
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityCG.cginc"
		#include "Tessellation.cginc"
		#pragma target 4.6
		#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
		#else
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
		#endif
		#pragma exclude_renderers xboxseries playstation switch nomrt 
		#pragma surface surf Standard keepalpha noshadow vertex:vertexDataFunc tessellate:tessFunction 
		struct Input
		{
			float3 worldPos;
			float4 screenPos;
		};

		uniform float _WaveHeight;
		uniform float _WaveSpeed;
		uniform float2 _WaveDirection;
		uniform float _WaveScale;
		uniform sampler2D _NormalMap;
		uniform float2 _NormalMainDirection;
		uniform float _NormalSpeed;
		uniform float _NormalScale;
		uniform float _NormalStrength;
		uniform float2 _NormalSecondDirection;
		uniform float4 _DeepColor;
		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		uniform float _Refraction;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _WaterDepth;
		uniform float4 _LightColor;
		uniform sampler2D _WaterFoam;
		uniform float _WaveFoamTiling;
		uniform float _WaveFoamSpeed;
		uniform float _WaveFoamOpacity;
		uniform float _EdgePower;
		uniform float4 _EdgeColor;
		uniform float _EdgeSize;
		uniform float _EdgeFoamOpacity;
		uniform float _EdgeFoamSize;
		uniform float _EdgeFoamFade;
		uniform float _Metallic;
		uniform float _Smoothness;


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		{
			#if UNITY_UV_STARTS_AT_TOP
			float scale = -1.0;
			#else
			float scale = 1.0;
			#endif
			float4 o = pos;
			o.y = pos.w * 0.5f;
			o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
			return o;
		}


		float4 tessFunction( appdata_full v0, appdata_full v1, appdata_full v2 )
		{
			float4 Tesselation131 = UnityDistanceBasedTess( v0.vertex, v1.vertex, v2.vertex, 0.0,80.0,( _WaveHeight * 8.0 ));
			return Tesselation131;
		}

		void vertexDataFunc( inout appdata_full v )
		{
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float4 appendResult13 = (float4(ase_worldPos.x , ase_worldPos.z , 0.0 , 0.0));
			float4 WorldSpaceUV14 = appendResult13;
			float4 WaveTiling25 = ( ( WorldSpaceUV14 * float4( float2( 0.2,0.2 ), 0.0 , 0.0 ) ) * _WaveScale );
			float2 panner3 = ( ( _Time.y * _WaveSpeed ) * _WaveDirection + WaveTiling25.xy);
			float simplePerlin2D1 = snoise( panner3 );
			float3 WaveHeight37 = ( ( float3(0,0.1,0) * _WaveHeight ) * simplePerlin2D1 );
			v.vertex.xyz += WaveHeight37;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float3 ase_worldPos = i.worldPos;
			float4 appendResult13 = (float4(ase_worldPos.x , ase_worldPos.z , 0.0 , 0.0));
			float4 WorldSpaceUV14 = appendResult13;
			float4 temp_output_64_0 = ( WorldSpaceUV14 * _NormalScale );
			float2 panner68 = ( 1.0 * _Time.y * ( _NormalMainDirection * _NormalSpeed ) + temp_output_64_0.xy);
			float2 panner69 = ( 1.0 * _Time.y * ( _NormalSecondDirection * ( _NormalSpeed * 3.0 ) ) + ( temp_output_64_0 * ( _NormalScale * 5.0 ) ).xy);
			float3 NormalMap78 = BlendNormals( UnpackScaleNormal( tex2D( _NormalMap, panner68 ), _NormalStrength ) , UnpackScaleNormal( tex2D( _NormalMap, panner69 ), _NormalStrength ) );
			o.Normal = NormalMap78;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
			float4 screenColor117 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( float3( (ase_grabScreenPosNorm).xy ,  0.0 ) + ( _Refraction * NormalMap78 ) ).xy);
			float4 clampResult118 = clamp( screenColor117 , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
			float4 Refraction120 = clampResult118;
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float eyeDepth314 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float4 ase_vertex4Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float3 ase_viewPos = UnityObjectToViewPos( ase_vertex4Pos );
			float ase_screenDepth = -ase_viewPos.z;
			float temp_output_316_0 = ( eyeDepth314 - ase_screenDepth );
			float WaterDepth470 = _WaterDepth;
			float PerspectiveDepthMask477 = ( 1.0 - saturate( ( ( temp_output_316_0 * 0.2 ) + (0.0 + (( 1.0 - WaterDepth470 ) - 0.0) * (1.0 - 0.0) / (1.0 - 0.0)) ) ) );
			float screenDepth122 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth122 = abs( ( screenDepth122 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( ( _WaterDepth / 500.0 ) ) );
			float clampResult124 = clamp( ( 1.0 - distanceDepth122 ) , 0.0 , 1.0 );
			float OrthographicDepth125 = clampResult124;
			float temp_output_603_0 = ( unity_OrthoParams.w == 0.0 ? PerspectiveDepthMask477 : OrthographicDepth125 );
			float4 lerpResult606 = lerp( _DeepColor , Refraction120 , temp_output_603_0);
			float4 color624 = IsGammaSpace() ? float4(0.764151,0.764151,0.764151,0) : float4(0.5448383,0.5448383,0.5448383,0);
			float4 lerpResult611 = lerp( _LightColor , ( _DeepColor * color624 ) , ( 1.0 - temp_output_603_0 ));
			float2 panner142 = ( 1.0 * _Time.y * float2( 0,0 ) + ( ( WorldSpaceUV14 / 10.0 ) * _WaveFoamTiling ).xy);
			float2 temp_cast_5 = (_WaveFoamSpeed).xx;
			float2 panner104 = ( 1.0 * _Time.y * temp_cast_5 + ( WorldSpaceUV14 * 0.05 ).xy);
			float simplePerlin2D103 = snoise( panner104*0.9 );
			float4 WaveTiling25 = ( ( WorldSpaceUV14 * float4( float2( 0.2,0.2 ), 0.0 , 0.0 ) ) * _WaveScale );
			float2 panner3 = ( ( _Time.y * _WaveSpeed ) * _WaveDirection + WaveTiling25.xy);
			float simplePerlin2D1 = snoise( panner3 );
			float WavePatern519 = simplePerlin2D1;
			float clampResult109 = clamp( ( tex2D( _WaterFoam, panner142 ).r * ( simplePerlin2D103 + WavePatern519 + ( _WaveFoamSpeed * -1.0 ) ) ) , 0.0 , 1.0 );
			float4 temp_cast_9 = (clampResult109).xxxx;
			float4 color238 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
			float4 lerpResult239 = lerp( temp_cast_9 , color238 , ( 1.0 - _WaveFoamOpacity ));
			float4 WaveFoam100 = lerpResult239;
			float4 Albedo520 = ( ( lerpResult606 * lerpResult611 ) + ( WaveFoam100 * ( 1.0 - WavePatern519 ) ) );
			o.Albedo = Albedo520.rgb;
			float4 color463 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
			float EdgeSize333 = _EdgeSize;
			float PerspectiveEdgeMask327 = ( 1.0 - saturate( ( temp_output_316_0 + (0.0 + (( 1.0 - EdgeSize333 ) - 0.0) * (1.0 - 0.0) / (1.0 - 0.0)) ) ) );
			float4 lerpResult464 = lerp( _EdgeColor , color463 , ( 1.0 - PerspectiveEdgeMask327 ));
			float2 temp_cast_11 = (_WaveFoamSpeed).xx;
			float2 panner138 = ( 1.0 * _Time.y * temp_cast_11 + ( ( WorldSpaceUV14 / 10.0 ) * _WaveFoamTiling ).xy);
			float4 tex2DNode82 = tex2D( _WaterFoam, panner138 );
			float4 color173 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
			float EdgeFoamSize422 = _EdgeFoamSize;
			float PerspectiveFoamMask328 = ( 1.0 - saturate( ( temp_output_316_0 + (0.0 + (( 1.0 - EdgeFoamSize422 ) - 0.0) * (1.0 - 0.0) / (1.0 - 0.0)) ) ) );
			float4 lerpResult421 = lerp( ( _EdgeFoamOpacity * tex2DNode82 ) , color173 , ( 1.0 - PerspectiveFoamMask328 ));
			float4 color430 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
			float EdgeFoamFade415 = _EdgeFoamFade;
			float PerspectiveFoamFade433 = ( 1.0 - saturate( ( temp_output_316_0 + (0.0 + (( 1.0 - EdgeFoamFade415 ) - 0.0) * (1.0 - 0.0) / (1.0 - 0.0)) ) ) );
			float clampResult429 = clamp( ( ( 1.0 - PerspectiveFoamFade433 ) * ( 1.0 - 0.0 ) ) , 0.0 , 1.0 );
			float4 lerpResult431 = lerp( lerpResult421 , color430 , ( 1.0 - clampResult429 ));
			float4 PerspectiveWaterCollision376 = ( ( _EdgePower * lerpResult464 ) + lerpResult431 );
			float screenDepth181 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth181 = abs( ( screenDepth181 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( ( _EdgeSize / 5000.0 ) ) );
			float clampResult185 = clamp( ( ( 1.0 - distanceDepth181 ) * _EdgePower ) , 0.0 , 1.0 );
			float screenDepth51 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth51 = abs( ( screenDepth51 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( ( _EdgeFoamSize / 3000.0 ) ) );
			float4 lerpResult168 = lerp( ( _EdgeFoamOpacity * tex2DNode82 ) , color173 , distanceDepth51);
			float4 clampResult58 = clamp( lerpResult168 , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
			float4 color234 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
			float screenDepth229 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth229 = abs( ( screenDepth229 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( ( _EdgeFoamFade / 5000.0 ) ) );
			float clampResult233 = clamp( ( ( 1.0 - distanceDepth229 ) * ( 1.0 - 0.0 ) ) , 0.0 , 1.0 );
			float4 lerpResult204 = lerp( clampResult58 , color234 , clampResult233);
			float4 OrthographicWaterCollision56 = ( ( clampResult185 * _EdgeColor ) + lerpResult204 );
			o.Emission = ( unity_OrthoParams.w == 0.0 ? PerspectiveWaterCollision376 : OrthographicWaterCollision56 ).rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
91;49;1822;921;351.158;1453.722;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;15;-2910.736,-3955.443;Inherit;False;902.2126;303.0005;;3;12;13;14;World Space UV;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;12;-2860.736,-3905.443;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;13;-2555.646,-3905.442;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;27;-1659.096,-502.9941;Inherit;False;1002.44;563.8085;;6;25;19;17;20;16;18;Wave Tiling;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;62;-1799.08,-4540.053;Inherit;False;4151.04;1794.099;;62;376;448;466;463;431;465;236;237;209;56;204;185;234;430;58;440;421;233;464;429;232;361;168;423;183;174;206;381;169;425;173;235;230;51;462;55;82;442;427;181;229;157;231;180;441;138;426;158;228;227;179;85;81;87;84;88;422;333;178;52;415;226;Water Collision;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;14;-2280.523,-3904.08;Float;False;WorldSpaceUV;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;79;-4400.329,-2614.742;Inherit;False;2463.89;1144.414;;19;63;65;64;66;67;72;70;73;74;71;75;69;60;61;68;77;59;76;78;Normal Map;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;226;-1681.339,-3988.73;Float;False;Property;_EdgeFoamFade;Edge Foam Fade;24;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;16;-1609.096,-452.994;Inherit;False;14;WorldSpaceUV;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.Vector2Node;18;-1595.439,-223.6432;Float;False;Constant;_WaveStretch;Wave Stretch;2;0;Create;True;0;0;0;False;0;False;0.2,0.2;0.23,0.01;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;65;-4293.266,-1933.829;Float;False;Property;_NormalScale;Normal Scale;14;0;Create;True;0;0;0;False;0;False;0.24;0.24;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;73;-3808.151,-2223.366;Float;False;Property;_NormalSpeed;Normal Speed;15;0;Create;True;0;0;0;False;0;False;0.01;0.01;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;63;-4350.328,-2159.073;Inherit;False;14;WorldSpaceUV;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;329;232.7623,-2531.543;Inherit;False;1834.365;1248.042;;32;328;327;324;322;323;326;433;461;325;438;319;437;460;459;391;436;435;332;316;331;314;315;434;439;471;472;473;474;475;476;477;625;Perspective Depth Masks;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;20;-1378.496,-202.316;Float;False;Property;_WaveScale;Wave Scale;5;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;127;-1661.274,-1219.929;Inherit;False;1488.181;511.0056;;8;189;122;126;124;125;123;190;470;Orthographic Depth Mask;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-1335.433,-449.2491;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;415;-1192.955,-4213.735;Inherit;False;EdgeFoamFade;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;71;-3605.127,-1777.767;Float;False;Property;_NormalSecondDirection;Normal Second Direction;18;0;Create;True;0;0;0;False;0;False;-1,0;-1,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;64;-4091.749,-2064.882;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.Vector2Node;70;-3827.838,-2513.358;Float;False;Property;_NormalMainDirection;Normal Main Direction;17;0;Create;True;0;0;0;False;0;False;1,0;1,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;-3590.178,-2115.16;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;66;-4080.097,-1798.795;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;178;-1725.691,-4466.191;Float;False;Property;_EdgeSize;Edge Size;19;0;Create;True;0;0;0;False;0;False;0;0;0;1.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-1747.085,-3529.534;Float;False;Property;_EdgeFoamSize;Edge Foam Size;22;0;Create;True;0;0;0;False;0;False;1.5;1.5;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-1097.646,-448.0161;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;-3481.098,-2329.126;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;67;-3830.152,-1937.957;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;439;279.2027,-1774.557;Inherit;False;415;EdgeFoamFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;39;-4399.185,-1300.644;Inherit;False;2618.978;758.4284;;19;131;128;129;130;22;132;37;23;35;24;10;3;1;9;36;6;8;26;519;Wave Offset;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;123;-1637.274,-1176.318;Float;False;Property;_WaterDepth;Water Depth;3;0;Create;True;0;0;0;False;0;False;1;1.5;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;75;-3338.222,-1841.103;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;8;-4332.254,-1138.827;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;59;-3127.923,-2329.544;Float;True;Property;_NormalMap;Normal Map;13;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RegisterLocalVarNode;25;-871.5711,-450.0451;Float;False;WaveTiling;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.OneMinusNode;434;506.8984,-1772.792;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;77;-3141.419,-2130.896;Float;False;Property;_NormalStrength;Normal Strength;16;0;Create;True;0;0;0;False;0;False;0.3;0.3;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;69;-3119.139,-1896.114;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;422;-1403.559,-3676.421;Inherit;False;EdgeFoamSize;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;333;-1429.21,-4499.128;Inherit;False;EdgeSize;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;470;-1303.94,-937.0135;Inherit;False;WaterDepth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SurfaceDepthNode;315;279.5177,-2365.521;Inherit;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;68;-3243.265,-2560.793;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-4365.952,-795.0511;Float;False;Property;_WaveSpeed;Wave Speed;6;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0.3;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenDepthNode;314;280.6059,-2464.632;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;60;-2794.098,-2244.365;Inherit;True;Property;_TextureSample1;Texture Sample 1;11;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;476;265.0959,-1541.284;Inherit;False;470;WaterDepth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;331;280.7451,-2018.222;Inherit;False;422;EdgeFoamSize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;435;691.0125,-1775.331;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;332;274.7579,-2240.247;Inherit;False;333;EdgeSize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-3885.086,-801.6122;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;316;739.937,-2479.777;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;61;-2797.409,-2040.325;Inherit;True;Property;_TextureSample0;Texture Sample 0;11;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;6;-4108.345,-1226.95;Float;False;Property;_WaveDirection;Wave Direction;8;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;26;-3879.81,-1248.83;Inherit;False;25;WaveTiling;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;110;-1881.573,-2562.71;Inherit;False;1978.426;1143.533;;23;542;100;239;238;109;241;240;108;91;142;103;140;545;104;98;549;105;97;99;95;96;106;550;Wave Foam;1,1,1,1;0;0
Node;AmplifyShaderEditor.OneMinusNode;471;504.2866,-1547.564;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;96;-1842.66,-2315.2;Float;False;Constant;_Float1;Float 1;17;0;Create;True;0;0;0;False;0;False;10;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;391;515.5342,-2020.096;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;436;1147.957,-1781.835;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;190;-1600.642,-942.5927;Inherit;False;Constant;_Float8;Float 8;25;0;Create;True;0;0;0;False;0;False;500;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;76;-2448.905,-2142.429;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;459;505.7139,-2250.827;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;3;-3556.228,-805.0349;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-1854.92,-1982.26;Float;False;Constant;_FoamMask;Foam Mask;18;0;Create;True;0;0;0;False;0;False;0.05;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;95;-1831.573,-2512.154;Inherit;False;14;WorldSpaceUV;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;97;-1570.991,-2514.621;Inherit;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;140;-1317.483,-2256.582;Float;False;Property;_WaveFoamSpeed;Wave Foam Speed;11;0;Create;True;0;0;0;False;0;False;0.01;0.01;0;0.2;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;472;694.7378,-1547.289;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;121;-3447.197,-3455.289;Inherit;False;1549.791;720.6443;;9;111;112;113;115;114;116;117;118;120;Refraction;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-1655.887,-1985.286;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;1;-3180.616,-798.8697;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;99;-1600.169,-2287.494;Float;False;Property;_WaveFoamTiling;Wave Foam Tiling;10;0;Create;True;0;0;0;False;0;False;2;2;0.1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;319;699.6483,-2022.634;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;437;1381.663,-1782.637;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;625;942.2935,-1758.312;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;189;-1298.065,-1166.338;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;78;-2143.377,-2138.392;Float;False;NormalMap;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TFHCRemapNode;460;699.6156,-2250.826;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;84;-1518.79,-3168.661;Inherit;False;14;WorldSpaceUV;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;88;-1759.131,-2992.441;Float;False;Constant;_Float0;Float 0;17;0;Create;True;0;0;0;False;0;False;10;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;115;-3385.667,-3002.87;Inherit;False;78;NormalMap;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;545;-1047.167,-1949.714;Inherit;False;Constant;_Float7;Float 7;22;0;Create;True;0;0;0;False;0;False;0.9;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;122;-1058.094,-1167.268;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;438;1588.244,-1786.823;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;104;-1342.856,-1973.824;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;325;1156.593,-2029.138;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;519;-2905.461,-793.5659;Inherit;False;WavePatern;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;113;-3411.763,-3110.534;Float;False;Property;_Refraction;Refraction;4;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0.2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;461;1153.363,-2280.863;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;473;1149.845,-1530.706;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GrabScreenPosition;111;-3392.786,-3306.791;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;98;-1227.454,-2502.136;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;87;-1290.7,-3017.587;Inherit;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;179;-1677.296,-4242.222;Inherit;False;Constant;_Float4;Float 4;22;0;Create;True;0;0;0;False;0;False;5000;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;323;1383.369,-2032.249;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;142;-880.6089,-2513.099;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;542;-856.6183,-1834.25;Inherit;True;519;WavePatern;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;474;1383.551,-1531.507;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;112;-3131.557,-3306.669;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;81;-1018.284,-3242.887;Float;True;Property;_WaterFoam;Water Foam;9;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SaturateNode;326;1381.969,-2277.233;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;227;-1632.943,-3764.761;Inherit;False;Constant;_Float11;Float 11;22;0;Create;True;0;0;0;False;0;False;5000;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;126;-826.0995,-1160.548;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;549;-860.4141,-1639.481;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;114;-3108.687,-3024.503;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;103;-862.3895,-2059.035;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;85;-1067.499,-3017.649;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;433;1784.68,-1784.552;Inherit;False;PerspectiveFoamFade;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;116;-2858.529,-3114.009;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;228;-1348.145,-3953.752;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;426;473.7306,-3192.61;Inherit;False;433;PerspectiveFoamFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;322;1580.293,-2275.785;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;91;-529.5405,-2514.004;Inherit;True;Property;_TextureSample3;Texture Sample 3;17;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;158;-1722.572,-3291.406;Inherit;False;Constant;_Float6;Float 6;22;0;Create;True;0;0;0;False;0;False;3000;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;180;-1423.816,-4255.458;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;550;-601.8177,-1846.407;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;475;1590.132,-1535.694;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;124;-633.2087,-1161.403;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;441;281.2346,-2972.031;Inherit;False;Constant;_Float5;Float 5;22;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;324;1586.718,-2034.126;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;138;-840.661,-3019.19;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;427;734.6018,-2997.04;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;157;-1391.952,-3436.722;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;82;-585.5923,-3255.065;Inherit;True;Property;_TextureSample2;Texture Sample 2;16;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DepthFade;181;-1146.11,-4444.088;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;229;-1101.757,-3966.627;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;328;1786.911,-2030.334;Inherit;False;PerspectiveFoamMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;125;-383.4038,-1164.604;Float;False;OrthographicDepth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;108;-138.9878,-2155.322;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenColorNode;117;-2610.668,-3088.763;Float;False;Global;_GrabScreen0;Grab Screen 0;19;0;Create;True;0;0;0;False;0;False;Object;-1;False;False;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;240;-891.5579,-2291.123;Inherit;False;Property;_WaveFoamOpacity;Wave Foam Opacity;12;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;477;1790.335,-1533.184;Inherit;False;PerspectiveDepthMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;442;474.0984,-2974.496;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;531;-4185.276,-372.5292;Inherit;False;2176.512;1304.714;;19;530;524;525;591;617;624;619;261;529;520;527;620;606;611;623;603;600;601;602;Albedo;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-564.6547,-3703.375;Float;False;Property;_EdgeFoamOpacity;Edge Foam Opacity;23;0;Create;True;0;0;0;False;0;False;0.75;0.75;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;327;1776.966,-2277.282;Inherit;False;PerspectiveEdgeMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;231;-1085.136,-3759.053;Inherit;False;Constant;_Float12;Float 12;22;0;Create;True;0;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;241;-547.1778,-2092.711;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;174;-967.3392,-4211.478;Inherit;False;Property;_EdgePower;Edge Power;20;0;Create;True;0;0;0;False;0;False;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;169;-278.6446,-3707.833;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;235;-809.1355,-3970.271;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;601;-4148.78,-16.80595;Inherit;False;477;PerspectiveDepthMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;173;-499.7017,-3048.335;Inherit;False;Constant;_Color0;Color 0;22;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DepthFade;51;-1131.204,-3461.63;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;425;927.2208,-2994.815;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;118;-2391.521,-3086.936;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;109;-145.0114,-1911.262;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;381;-245.669,-2959.421;Inherit;False;328;PerspectiveFoamMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;462;554.8392,-3697.885;Inherit;False;327;PerspectiveEdgeMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;238;-518.4976,-2304.296;Inherit;False;Constant;_Color1;Color 1;23;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;602;-4134.699,258.0933;Inherit;False;125;OrthographicDepth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;206;-857.4547,-4449.974;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OrthoParams;600;-3917.557,-14.3545;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;230;-765.6396,-3750.839;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;261;-3372.598,-317.1659;Inherit;False;Property;_DeepColor;Deep Color;2;0;Create;True;0;0;0;False;0;False;0,0.3608235,0.3882353,0;0,0.3608235,0.3882353,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Compare;603;-3853.231,172.0208;Inherit;False;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;463;792.1531,-3481.326;Inherit;False;Constant;_Color6;Color 6;22;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;465;829.7936,-3712.98;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;120;-2135.995,-3089.941;Float;False;Refraction;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;239;-361.4415,-1662.825;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;232;-510.4382,-3956.754;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;624;-3411.408,100.0696;Inherit;False;Constant;_Color3;Color 3;24;0;Create;True;0;0;0;False;0;False;0.764151,0.764151,0.764151,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;236;321.4102,-4241.636;Inherit;False;Property;_EdgeColor;Edge Color;21;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;168;-19.24202,-3601.467;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;423;7.240859,-3062.839;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;183;-667.0554,-4456.668;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;429;1149.365,-2991.308;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;361;-85.53477,-3378.523;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;58;258.5672,-3604.006;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;234;267.1721,-3827.543;Inherit;False;Constant;_Color2;Color 2;23;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;430;1153.565,-3180.518;Inherit;False;Constant;_Color4;Color 4;22;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;617;-3577.032,234.2173;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;421;215.7896,-3366.316;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;591;-3384.498,377.7988;Inherit;False;Property;_LightColor;Light Color;1;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;185;-418.9063,-4458.562;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;524;-3047.892,817.1061;Inherit;False;519;WavePatern;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;623;-3159.163,71.85415;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;619;-3558.083,-126.3503;Inherit;False;120;Refraction;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;100;-110.7767,-1661.502;Float;False;WaveFoam;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;440;1386.695,-3127.351;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;233;-287.9488,-3968.27;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;464;1058.773,-3737.946;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;431;1582.641,-3264.377;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;35;-3191.214,-1261.738;Float;False;Property;_WaveHeight;Wave Height;7;0;Create;True;0;0;0;False;0;False;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;204;530.6669,-3937.326;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;530;-2820.854,817.8502;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-2657.305,-1177.38;Float;False;Constant;_Tesselation;Tesselation;1;0;Create;True;0;0;0;False;0;False;8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;606;-3137.125,-150.7504;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;611;-2940.009,254.2709;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;466;1367.106,-3834.938;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;237;571.3902,-4443.419;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector3Node;23;-3496.22,-1151.316;Float;False;Constant;_WaveDir;Wave Dir;2;0;Create;True;0;0;0;False;0;False;0,0.1,0;0,0.1,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;525;-2836.912,724.7085;Inherit;False;100;WaveFoam;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;129;-2442.031,-1017.05;Float;False;Constant;_Float2;Float 2;20;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;-3145.412,-1033.09;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;529;-2629.819,720.7446;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;620;-2671.161,-56.41319;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;448;1843.058,-3620.205;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;132;-2463.757,-1254.514;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;209;832.5389,-4130.726;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;130;-2437.031,-787.0513;Float;False;Constant;_Float3;Float 3;20;0;Create;True;0;0;0;False;0;False;80;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;-2910.257,-1026.026;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;527;-2446.22,-61.8286;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DistanceBasedTessNode;128;-2229.798,-1049.403;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;56;1146.278,-4299.559;Float;False;OrthographicWaterCollision;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;376;2082.545,-3610.186;Float;False;PerspectiveWaterCollision;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OrthoParams;336;-356.7745,-520.7637;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;380;-298.2066,-339.7899;Inherit;False;376;PerspectiveWaterCollision;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;57;-306.0987,-146.9373;Inherit;False;56;OrthographicWaterCollision;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;37;-2673.495,-804.9501;Float;False;WaveHeight;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;131;-1985.742,-1046.809;Float;False;Tesselation;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;520;-2217.479,-64.67489;Inherit;False;Albedo;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.Compare;337;99.59393,-482.4578;Inherit;False;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;40;623.7838,-950.2544;Float;False;Property;_Smoothness;Smoothness;0;0;Create;True;0;0;0;False;0;False;0.8;0.8;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;80;94.57294,-955.6893;Inherit;False;78;NormalMap;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;38;93.12567,-760.5506;Inherit;False;37;WaveHeight;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;134;622.5612,-714.7772;Inherit;False;131;Tesselation;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;526;90.69232,-1167.427;Inherit;False;520;Albedo;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;626;629.842,-1040.722;Inherit;False;Property;_Metallic;Metallic;25;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;388.0441,-1044.111;Float;False;True;-1;6;ASEMaterialInspector;0;0;Standard;SFS/VoxelWater;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;2;Translucent;0.5;True;False;0;False;Opaque;;Transparent;All;14;d3d9;d3d11_9x;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;ps4;psp2;n3ds;wiiu;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;True;2;15;10;25;False;0.5;False;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;13;0;12;1
WireConnection;13;1;12;3
WireConnection;14;0;13;0
WireConnection;17;0;16;0
WireConnection;17;1;18;0
WireConnection;415;0;226;0
WireConnection;64;0;63;0
WireConnection;64;1;65;0
WireConnection;74;0;73;0
WireConnection;66;0;65;0
WireConnection;19;0;17;0
WireConnection;19;1;20;0
WireConnection;72;0;70;0
WireConnection;72;1;73;0
WireConnection;67;0;64;0
WireConnection;67;1;66;0
WireConnection;75;0;71;0
WireConnection;75;1;74;0
WireConnection;25;0;19;0
WireConnection;434;0;439;0
WireConnection;69;0;67;0
WireConnection;69;2;75;0
WireConnection;422;0;52;0
WireConnection;333;0;178;0
WireConnection;470;0;123;0
WireConnection;68;0;64;0
WireConnection;68;2;72;0
WireConnection;60;0;59;0
WireConnection;60;1;68;0
WireConnection;60;5;77;0
WireConnection;435;0;434;0
WireConnection;10;0;8;0
WireConnection;10;1;9;0
WireConnection;316;0;314;0
WireConnection;316;1;315;0
WireConnection;61;0;59;0
WireConnection;61;1;69;0
WireConnection;61;5;77;0
WireConnection;471;0;476;0
WireConnection;391;0;331;0
WireConnection;436;0;316;0
WireConnection;436;1;435;0
WireConnection;76;0;60;0
WireConnection;76;1;61;0
WireConnection;459;0;332;0
WireConnection;3;0;26;0
WireConnection;3;2;6;0
WireConnection;3;1;10;0
WireConnection;97;0;95;0
WireConnection;97;1;96;0
WireConnection;472;0;471;0
WireConnection;105;0;95;0
WireConnection;105;1;106;0
WireConnection;1;0;3;0
WireConnection;319;0;391;0
WireConnection;437;0;436;0
WireConnection;625;0;316;0
WireConnection;189;0;123;0
WireConnection;189;1;190;0
WireConnection;78;0;76;0
WireConnection;460;0;459;0
WireConnection;122;0;189;0
WireConnection;438;0;437;0
WireConnection;104;0;105;0
WireConnection;104;2;140;0
WireConnection;325;0;316;0
WireConnection;325;1;319;0
WireConnection;519;0;1;0
WireConnection;461;0;316;0
WireConnection;461;1;460;0
WireConnection;473;0;625;0
WireConnection;473;1;472;0
WireConnection;98;0;97;0
WireConnection;98;1;99;0
WireConnection;87;0;84;0
WireConnection;87;1;88;0
WireConnection;323;0;325;0
WireConnection;142;0;98;0
WireConnection;474;0;473;0
WireConnection;112;0;111;0
WireConnection;326;0;461;0
WireConnection;126;0;122;0
WireConnection;549;0;140;0
WireConnection;114;0;113;0
WireConnection;114;1;115;0
WireConnection;103;0;104;0
WireConnection;103;1;545;0
WireConnection;85;0;87;0
WireConnection;85;1;99;0
WireConnection;433;0;438;0
WireConnection;116;0;112;0
WireConnection;116;1;114;0
WireConnection;228;0;226;0
WireConnection;228;1;227;0
WireConnection;322;0;326;0
WireConnection;91;0;81;0
WireConnection;91;1;142;0
WireConnection;180;0;178;0
WireConnection;180;1;179;0
WireConnection;550;0;103;0
WireConnection;550;1;542;0
WireConnection;550;2;549;0
WireConnection;475;0;474;0
WireConnection;124;0;126;0
WireConnection;324;0;323;0
WireConnection;138;0;85;0
WireConnection;138;2;140;0
WireConnection;427;0;426;0
WireConnection;157;0;52;0
WireConnection;157;1;158;0
WireConnection;82;0;81;0
WireConnection;82;1;138;0
WireConnection;181;0;180;0
WireConnection;229;0;228;0
WireConnection;328;0;324;0
WireConnection;125;0;124;0
WireConnection;108;0;91;1
WireConnection;108;1;550;0
WireConnection;117;0;116;0
WireConnection;477;0;475;0
WireConnection;442;0;441;0
WireConnection;327;0;322;0
WireConnection;241;0;240;0
WireConnection;169;0;55;0
WireConnection;169;1;82;0
WireConnection;235;0;229;0
WireConnection;51;0;157;0
WireConnection;425;0;427;0
WireConnection;425;1;442;0
WireConnection;118;0;117;0
WireConnection;109;0;108;0
WireConnection;206;0;181;0
WireConnection;230;0;231;0
WireConnection;603;0;600;4
WireConnection;603;2;601;0
WireConnection;603;3;602;0
WireConnection;465;0;462;0
WireConnection;120;0;118;0
WireConnection;239;0;109;0
WireConnection;239;1;238;0
WireConnection;239;2;241;0
WireConnection;232;0;235;0
WireConnection;232;1;230;0
WireConnection;168;0;169;0
WireConnection;168;1;173;0
WireConnection;168;2;51;0
WireConnection;423;0;381;0
WireConnection;183;0;206;0
WireConnection;183;1;174;0
WireConnection;429;0;425;0
WireConnection;361;0;55;0
WireConnection;361;1;82;0
WireConnection;58;0;168;0
WireConnection;617;0;603;0
WireConnection;421;0;361;0
WireConnection;421;1;173;0
WireConnection;421;2;423;0
WireConnection;185;0;183;0
WireConnection;623;0;261;0
WireConnection;623;1;624;0
WireConnection;100;0;239;0
WireConnection;440;0;429;0
WireConnection;233;0;232;0
WireConnection;464;0;236;0
WireConnection;464;1;463;0
WireConnection;464;2;465;0
WireConnection;431;0;421;0
WireConnection;431;1;430;0
WireConnection;431;2;440;0
WireConnection;204;0;58;0
WireConnection;204;1;234;0
WireConnection;204;2;233;0
WireConnection;530;0;524;0
WireConnection;606;0;261;0
WireConnection;606;1;619;0
WireConnection;606;2;603;0
WireConnection;611;0;591;0
WireConnection;611;1;623;0
WireConnection;611;2;617;0
WireConnection;466;0;174;0
WireConnection;466;1;464;0
WireConnection;237;0;185;0
WireConnection;237;1;236;0
WireConnection;24;0;23;0
WireConnection;24;1;35;0
WireConnection;529;0;525;0
WireConnection;529;1;530;0
WireConnection;620;0;606;0
WireConnection;620;1;611;0
WireConnection;448;0;466;0
WireConnection;448;1;431;0
WireConnection;132;0;35;0
WireConnection;132;1;22;0
WireConnection;209;0;237;0
WireConnection;209;1;204;0
WireConnection;36;0;24;0
WireConnection;36;1;1;0
WireConnection;527;0;620;0
WireConnection;527;1;529;0
WireConnection;128;0;132;0
WireConnection;128;1;129;0
WireConnection;128;2;130;0
WireConnection;56;0;209;0
WireConnection;376;0;448;0
WireConnection;37;0;36;0
WireConnection;131;0;128;0
WireConnection;520;0;527;0
WireConnection;337;0;336;4
WireConnection;337;2;380;0
WireConnection;337;3;57;0
WireConnection;0;0;526;0
WireConnection;0;1;80;0
WireConnection;0;2;337;0
WireConnection;0;3;626;0
WireConnection;0;4;40;0
WireConnection;0;11;38;0
WireConnection;0;14;134;0
ASEEND*/
//CHKSM=C75FE3A48EE1DA8C9E1EB0DA0B60562B6E0BBC6F
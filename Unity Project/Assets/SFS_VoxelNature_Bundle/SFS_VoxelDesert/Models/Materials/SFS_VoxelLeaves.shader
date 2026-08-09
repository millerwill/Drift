// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SFS/VoxelLeaves"
{
	Properties
	{
		_AlbedoTexture("Albedo Texture", 2D) = "white" {}
		_TextureTint("Texture Tint", Color) = (1,1,1,1)
		_TextureContrast("Texture Contrast", Range( 0 , 2)) = 1.3
		_TreeBendStrength("Tree Bend Strength", Range( 0 , 0.02)) = 0.01
		_WindSpeed("Wind Speed", Range( 0 , 5)) = 2.5
		_WindScale("Wind Scale", Range( 0 , 0.5)) = 0.1
		_LightWindScale("Light Wind Scale", Range( 0 , 2)) = 0.5
		_LightWindStrength("Light Wind Strength", Range( 0 , 0.5)) = 0.2
		[Toggle(_SUBSURFACESCATTERING_ON)] _SubSurfaceScattering("SubSurface Scattering", Float) = 0
		_SSSStrength("SSS Strength", Range( 0 , 50)) = 25
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Off
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _SUBSURFACESCATTERING_ON
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
			float2 uv_texcoord;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform float _TreeBendStrength;
		uniform float _WindSpeed;
		uniform float _LightWindScale;
		uniform float _LightWindStrength;
		uniform float _WindScale;
		uniform float _TextureContrast;
		uniform sampler2D _AlbedoTexture;
		uniform float4 _AlbedoTexture_ST;
		uniform float4 _TextureTint;
		uniform float _SSSStrength;


		float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }

		float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }

		float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }

		float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }

		float snoise( float3 v )
		{
			const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
			float3 i = floor( v + dot( v, C.yyy ) );
			float3 x0 = v - i + dot( i, C.xxx );
			float3 g = step( x0.yzx, x0.xyz );
			float3 l = 1.0 - g;
			float3 i1 = min( g.xyz, l.zxy );
			float3 i2 = max( g.xyz, l.zxy );
			float3 x1 = x0 - i1 + C.xxx;
			float3 x2 = x0 - i2 + C.yyy;
			float3 x3 = x0 - 0.5;
			i = mod3D289( i);
			float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
			float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
			float4 x_ = floor( j / 7.0 );
			float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
			float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 h = 1.0 - abs( x ) - abs( y );
			float4 b0 = float4( x.xy, y.xy );
			float4 b1 = float4( x.zw, y.zw );
			float4 s0 = floor( b0 ) * 2.0 + 1.0;
			float4 s1 = floor( b1 ) * 2.0 + 1.0;
			float4 sh = -step( h, 0.0 );
			float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
			float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
			float3 g0 = float3( a0.xy, h.x );
			float3 g1 = float3( a0.zw, h.y );
			float3 g2 = float3( a1.xy, h.z );
			float3 g3 = float3( a1.zw, h.w );
			float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
			g0 *= norm.x;
			g1 *= norm.y;
			g2 *= norm.z;
			g3 *= norm.w;
			float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
			m = m* m;
			m = m* m;
			float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
			return 42.0 * dot( m, px);
		}


		float4 CalculateContrast( float contrastValue, float4 colorTarget )
		{
			float t = 0.5 * ( 1.0 - contrastValue );
			return mul( float4x4( contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1 ), colorTarget );
		}

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float temp_output_55_0 = ( ( cos( ( ( ( ase_worldPos.x + ase_worldPos.z ) * 0.06 ) + _Time.y ) ) * ase_vertex3Pos.y ) * _TreeBendStrength );
			float4 appendResult61 = (float4(temp_output_55_0 , 0.0 , temp_output_55_0 , 0.0));
			float4 break74 = mul( appendResult61, unity_ObjectToWorld );
			float4 appendResult90 = (float4(break74.x , 0 , break74.z , 0.0));
			float mulTime54 = _Time.y * _WindSpeed;
			float simplePerlin3D473 = snoise( ( ase_worldPos + ( mulTime54 * 0.5 ) )*_LightWindScale );
			float simplePerlin3D470 = snoise( ( ase_worldPos + mulTime54 )*_WindScale );
			float4 temp_cast_0 = (( ( ( simplePerlin3D473 * _LightWindStrength ) + simplePerlin3D470 ) * 0.1 )).xxxx;
			v.vertex.xyz += ( appendResult90 + mul( temp_cast_0, unity_ObjectToWorld ).x ).xyz;
			v.vertex.w = 1;
		}

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			#ifdef UNITY_PASS_FORWARDBASE
			float ase_lightAtten = data.atten;
			if( _LightColor0.a == 0)
			ase_lightAtten = 0;
			#else
			float3 ase_lightAttenRGB = gi.light.color / ( ( _LightColor0.rgb ) + 0.000001 );
			float ase_lightAtten = max( max( ase_lightAttenRGB.r, ase_lightAttenRGB.g ), ase_lightAttenRGB.b );
			#endif
			#if defined(HANDLE_SHADOWS_BLENDING_IN_GI)
			half bakedAtten = UnitySampleBakedOcclusion(data.lightmapUV.xy, data.worldPos);
			float zDist = dot(_WorldSpaceCameraPos - data.worldPos, UNITY_MATRIX_V[2].xyz);
			float fadeDist = UnityComputeShadowFadeDistance(data.worldPos, zDist);
			ase_lightAtten = UnityMixRealtimeAndBakedShadows(data.atten, bakedAtten, UnityComputeShadowFade(fadeDist));
			#endif
			float3 ase_worldPos = i.worldPos;
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld
			float3 ase_worldlightDir = 0;
			#else //aseld
			float3 ase_worldlightDir = Unity_SafeNormalize( UnityWorldSpaceLightDir( ase_worldPos ) );
			#endif //aseld
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_normWorldNormal = normalize( ase_worldNormal );
			float dotResult373 = dot( ase_worldlightDir , ase_normWorldNormal );
			#if defined(LIGHTMAP_ON) && ( UNITY_VERSION < 560 || ( defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) ) )//aselc
			float4 ase_lightColor = 0;
			#else //aselc
			float4 ase_lightColor = _LightColor0;
			#endif //aselc
			float2 uv_AlbedoTexture = i.uv_texcoord * _AlbedoTexture_ST.xy + _AlbedoTexture_ST.zw;
			float4 Albedo339 = ( tex2D( _AlbedoTexture, uv_AlbedoTexture ) * _TextureTint );
			float4 DirectLight383 = ( ( saturate( dotResult373 ) * ase_lightAtten ) * ase_lightColor * Albedo339 );
			UnityGI gi385 = gi;
			float3 diffNorm385 = ase_worldNormal;
			gi385 = UnityGI_Base( data, 1, diffNorm385 );
			float3 indirectDiffuse385 = gi385.indirect.diffuse + diffNorm385 * 0.0001;
			float4 IndirectLight388 = ( float4( indirectDiffuse385 , 0.0 ) * Albedo339 );
			float3 ase_worldViewDir = Unity_SafeNormalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float dotResult353 = dot( ase_worldlightDir , ase_worldViewDir );
			float dotResult352 = dot( ase_worldlightDir , ase_normWorldNormal );
			float4 color464 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
			float4 lerpResult466 = lerp( ( ase_lightColor * Albedo339 ) , color464 , 0.7);
			float4 SSS369 = saturate( ( ( (-dotResult353*1.0 + -0.85) * ( ( (dotResult352*1.0 + 1.0) * ase_lightAtten ) * lerpResult466 ) ) * _SSSStrength ) );
			#ifdef _SUBSURFACESCATTERING_ON
				float4 staticSwitch408 = SSS369;
			#else
				float4 staticSwitch408 = float4( 0,0,0,0 );
			#endif
			c.rgb = CalculateContrast(_TextureContrast,( DirectLight383 + IndirectLight388 + staticSwitch408 )).rgb;
			c.a = 1;
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			o.Normal = float3(0,0,1);
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
-1871;63;1822;949;5473.085;906.8549;4.243862;True;False
Node;AmplifyShaderEditor.CommentaryNode;102;-1334.026,-675.5239;Inherit;False;904.0563;636.3807;;4;339;104;105;103;Albedo;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;103;-1310.675,-617.5695;Inherit;True;Property;_AlbedoTexture;Albedo Texture;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;104;-1219.997,-416.2609;Inherit;False;Property;_TextureTint;Texture Tint;1;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-978.7307,-613.2847;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;409;-2933.82,1849.182;Inherit;False;1998.883;802.8091;;24;369;368;367;365;366;362;361;466;359;357;356;465;353;354;464;355;454;453;455;352;351;348;347;350;SubSurface Scattering;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;350;-2908.316,1915.061;Inherit;False;True;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldNormalVector;347;-2897.531,2212.948;Inherit;False;True;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;339;-624.0823,-613.8099;Inherit;False;Albedo;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;38;-2936.895,18.46617;Inherit;False;2510.547;980.6156;;17;90;73;74;66;61;62;55;49;48;46;45;44;101;203;41;100;39;Tree Sway;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;351;-2619.64,2316.091;Inherit;False;Constant;_SSSOffset;SSS Offset;18;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;455;-2752.311,2536.845;Inherit;False;339;Albedo;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.DotProductOpNode;352;-2612.408,2186.598;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;39;-2899.031,68.46621;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LightColorNode;453;-2735.186,2409.78;Inherit;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;348;-2643.896,1988.542;Inherit;False;World;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;454;-2490.649,2409.143;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;47;-2946.285,1129.501;Inherit;False;2354.225;554.777;;16;469;468;478;474;479;58;470;471;477;476;475;473;54;52;51;480;Leaves Wind;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;341;-870.4576,1852.703;Inherit;False;1287.315;901.9287;;2;346;344;Custom Light;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;100;-2852.204,438.9487;Inherit;False;Constant;_Float0;Float 0;6;0;Create;True;0;0;0;False;0;False;0.06;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;41;-2659.716,173.0062;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;354;-2417.633,2151.808;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;464;-2282.956,2442.859;Inherit;False;Constant;_Transparent;Transparent;21;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LightAttenuation;355;-2414.859,2285.311;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;353;-2444.473,1912.206;Inherit;True;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;465;-1892.702,2332.981;Inherit;False;Constant;_LightMultiplier;LightMultiplier;21;0;Create;True;0;0;0;False;0;False;0.7;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;101;-2439.649,369.5132;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-2917.527,1442.299;Inherit;False;Property;_WindSpeed;Wind Speed;4;0;Create;True;0;0;0;False;0;False;2.5;2.5;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;356;-2124.966,1920.688;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;203;-2530.084,669.0228;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;344;-851.5306,1904.644;Inherit;False;1244.692;428.0206;Direct Light;10;383;382;379;381;377;380;376;373;371;372;;1,1,1,1;0;0
Node;AmplifyShaderEditor.LerpOp;466;-2070.504,2293.51;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;357;-2231.143,2018.877;Inherit;False;Constant;_SSSShiftAngle;SSS Shift Angle;16;0;Create;True;0;0;0;False;0;False;-0.85;-0.5;-1;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;359;-2095.961,2112.417;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;44;-2210.662,370.2249;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;361;-1942.82,1920.861;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;372;-821.4677,1955.759;Inherit;False;True;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldNormalVector;371;-789.3725,2111.313;Inherit;False;True;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleTimeNode;54;-2629.819,1447.061;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;362;-1890.602,2109.787;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CosOpNode;45;-1983.302,371.9178;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;51;-2915.163,1193.47;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PosVertexDataNode;46;-2178.539,607.274;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;477;-2365.971,1326.492;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;373;-530.2604,1953.82;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;365;-1646.058,1911.959;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;366;-1711.78,2120.81;Inherit;False;Property;_SSSStrength;SSS Strength;9;0;Create;True;0;0;0;False;0;False;25;25;0;50;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;-1781.405,544.1213;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;475;-2154.976,1281.116;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;346;-850.2186,2364.65;Inherit;False;759.3661;369.7148;Indirect Light;4;388;387;385;386;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;49;-1844.597,818.6393;Inherit;False;Property;_TreeBendStrength;Tree Bend Strength;3;0;Create;True;0;0;0;False;0;False;0.01;0.02;0;0.02;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;376;-378.6014,1953.202;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;367;-1425.883,2001.474;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LightAttenuation;377;-479.1888,2091.348;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;476;-2301.356,1188.59;Inherit;False;Property;_LightWindScale;Light Wind Scale;6;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;480;-1628.75,1347.443;Inherit;False;Property;_LightWindStrength;Light Wind Strength;7;0;Create;True;0;0;0;False;0;False;0.2;0.2;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;386;-817.7946,2523.764;Inherit;False;339;Albedo;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;380;-185.6487,1953.506;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LightColorNode;381;-210.4308,2073.749;Inherit;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;379;-222.166,2194.009;Inherit;False;339;Albedo;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;-1543.801,542.0212;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;58;-2302.055,1582.358;Inherit;False;Property;_WindScale;Wind Scale;5;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.IndirectDiffuseLighting;385;-820.3595,2414.925;Inherit;False;Tangent;1;0;FLOAT3;0,0,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;368;-1277.114,1918.171;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;473;-1862.52,1202.439;Inherit;True;Simplex3D;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;471;-2159.646,1434.273;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;387;-532.4109,2415.853;Inherit;True;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ObjectToWorldMatrixNode;62;-1275.894,854.7983;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;382;18.63747,1953.007;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;61;-1301.942,537.6384;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;470;-1861.089,1450.738;Inherit;True;Simplex3D;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;369;-1133.118,1912.938;Inherit;False;SSS;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;479;-1341.751,1228.953;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;400;-333.3245,558.5048;Inherit;False;369;SSS;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;474;-1152.744,1431.464;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;388;-283.4577,2413.327;Inherit;False;IndirectLight;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;66;-1052.238,535.0244;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;383;195.5322,1950.584;Inherit;False;DirectLight;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;478;-899.019,1428.404;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;73;-785.7487,219.7389;Inherit;False;Constant;_YAxisOffset;Y Axis Offset;3;0;Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.BreakToComponentsNode;74;-809.5957,536.8811;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;403;-319.3245,326.5043;Inherit;False;383;DirectLight;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ObjectToWorldMatrixNode;468;-940.307,1584.054;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.GetLocalVarNode;401;-322.3245,413.5043;Inherit;False;388;IndirectLight;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;408;-139.1015,534.3377;Inherit;False;Property;_SubSurfaceScattering;SubSurface Scattering;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;90;-573.4728,537.0459;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-11.6907,684.8025;Inherit;False;Property;_TextureContrast;Texture Contrast;2;0;Create;True;0;0;0;False;0;False;1.3;1.3;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;469;-731.0156,1427.691;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;402;142.6765,423.5046;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleContrastOpNode;459;288.0555,564.0139;Inherit;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;96;-332.7861,947.0486;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;514.7962,491.6205;Float;False;True;-1;2;ASEMaterialInspector;0;0;CustomLighting;SFS/VoxelLeaves;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;105;0;103;0
WireConnection;105;1;104;0
WireConnection;339;0;105;0
WireConnection;352;0;350;0
WireConnection;352;1;347;0
WireConnection;454;0;453;0
WireConnection;454;1;455;0
WireConnection;41;0;39;1
WireConnection;41;1;39;3
WireConnection;354;0;352;0
WireConnection;354;2;351;0
WireConnection;353;0;350;0
WireConnection;353;1;348;0
WireConnection;101;0;41;0
WireConnection;101;1;100;0
WireConnection;356;0;353;0
WireConnection;466;0;454;0
WireConnection;466;1;464;0
WireConnection;466;2;465;0
WireConnection;359;0;354;0
WireConnection;359;1;355;0
WireConnection;44;0;101;0
WireConnection;44;1;203;0
WireConnection;361;0;356;0
WireConnection;361;2;357;0
WireConnection;54;0;52;0
WireConnection;362;0;359;0
WireConnection;362;1;466;0
WireConnection;45;0;44;0
WireConnection;477;0;54;0
WireConnection;373;0;372;0
WireConnection;373;1;371;0
WireConnection;365;0;361;0
WireConnection;365;1;362;0
WireConnection;48;0;45;0
WireConnection;48;1;46;2
WireConnection;475;0;51;0
WireConnection;475;1;477;0
WireConnection;376;0;373;0
WireConnection;367;0;365;0
WireConnection;367;1;366;0
WireConnection;380;0;376;0
WireConnection;380;1;377;0
WireConnection;55;0;48;0
WireConnection;55;1;49;0
WireConnection;368;0;367;0
WireConnection;473;0;475;0
WireConnection;473;1;476;0
WireConnection;471;0;51;0
WireConnection;471;1;54;0
WireConnection;387;0;385;0
WireConnection;387;1;386;0
WireConnection;382;0;380;0
WireConnection;382;1;381;0
WireConnection;382;2;379;0
WireConnection;61;0;55;0
WireConnection;61;2;55;0
WireConnection;470;0;471;0
WireConnection;470;1;58;0
WireConnection;369;0;368;0
WireConnection;479;0;473;0
WireConnection;479;1;480;0
WireConnection;474;0;479;0
WireConnection;474;1;470;0
WireConnection;388;0;387;0
WireConnection;66;0;61;0
WireConnection;66;1;62;0
WireConnection;383;0;382;0
WireConnection;478;0;474;0
WireConnection;74;0;66;0
WireConnection;408;0;400;0
WireConnection;90;0;74;0
WireConnection;90;1;73;2
WireConnection;90;2;74;2
WireConnection;469;0;478;0
WireConnection;469;1;468;0
WireConnection;402;0;403;0
WireConnection;402;1;401;0
WireConnection;402;2;408;0
WireConnection;459;1;402;0
WireConnection;459;0;106;0
WireConnection;96;0;90;0
WireConnection;96;1;469;0
WireConnection;0;13;459;0
WireConnection;0;11;96;0
ASEEND*/
//CHKSM=47D05A741588789272D2810342A8581C4596C860
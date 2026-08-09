// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SFS/VoxelGrass"
{
	Properties
	{
		_AlbedoTexture("Albedo Texture", 2D) = "white" {}
		_TextureTint("Texture Tint", Color) = (1,1,1,1)
		_TextureContrast("Texture Contrast", Range( 0 , 2)) = 1.3
		_WindSpeed("Wind Speed", Range( 0 , 5)) = 2.5
		_WindScale("Wind Scale", Range( 0 , 0.5)) = 0.1
		_LightWindScale("Light Wind Scale", Range( 0 , 2)) = 0.5
		_LightWindStrength("Light Wind Strength", Range( 0 , 0.5)) = 0.2
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
		};

		uniform float _WindSpeed;
		uniform float _LightWindScale;
		uniform float _LightWindStrength;
		uniform float _WindScale;
		uniform float _TextureContrast;
		uniform sampler2D _AlbedoTexture;
		uniform float4 _AlbedoTexture_ST;
		uniform float4 _TextureTint;


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
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float mulTime80 = _Time.y * _WindSpeed;
			float simplePerlin3D85 = snoise( ( ase_worldPos + ( mulTime80 * 0.5 ) )*_LightWindScale );
			float simplePerlin3D89 = snoise( ( ase_worldPos + mulTime80 )*_WindScale );
			float4 appendResult98 = (float4(( ase_vertex3Pos.y * ( ( ( simplePerlin3D85 * _LightWindStrength ) + simplePerlin3D89 ) * 0.1 ) ) , 0.0 , ( ase_vertex3Pos.y * ( ( ( simplePerlin3D85 * _LightWindStrength ) + simplePerlin3D89 ) * 0.1 ) ) , 0.0));
			v.vertex.xyz += mul( appendResult98, unity_ObjectToWorld ).xyz;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_AlbedoTexture = i.uv_texcoord * _AlbedoTexture_ST.xy + _AlbedoTexture_ST.zw;
			o.Albedo = CalculateContrast(_TextureContrast,( tex2D( _AlbedoTexture, uv_AlbedoTexture ) * _TextureTint )).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
-1871;63;1822;949;5302.255;2090.144;4.483808;True;False
Node;AmplifyShaderEditor.CommentaryNode;78;-2857.436,273.4778;Inherit;False;2682.296;550.7268;;21;98;100;96;93;95;91;90;88;89;85;86;87;84;83;82;92;81;80;79;101;94;Grass Wind;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;79;-2828.678,586.2762;Inherit;False;Property;_WindSpeed;Wind Speed;3;0;Create;True;0;0;0;False;0;False;2.5;2.5;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;80;-2540.97,591.0381;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;81;-2826.314,337.4468;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;82;-2319.65,468.4442;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;84;-2301.614,320.416;Inherit;False;Property;_LightWindScale;Light Wind Scale;5;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;83;-2151.184,408.8923;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;88;-1679.639,485.345;Inherit;False;Property;_LightWindStrength;Light Wind Strength;6;0;Create;True;0;0;0;False;0;False;0.2;0.2;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;85;-1933.661,336.29;Inherit;True;Simplex3D;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;86;-2296.238,716.2094;Inherit;False;Property;_WindScale;Wind Scale;4;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;87;-2147.753,580.2753;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;90;-1400.741,352.6783;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;89;-1932.23,584.5895;Inherit;True;Simplex3D;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;91;-1231.984,577.4664;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;93;-1014.712,576.4315;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;95;-1228.119,315.2578;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;96;-859.0459,421.9942;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;44;-1190.714,-437.5432;Inherit;False;1000.072;658.6371;Comment;5;49;48;47;46;45;Albedo;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;46;-1154.223,-370.8489;Inherit;True;Property;_AlbedoTexture;Albedo Texture;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;45;-1111.753,-77.06602;Inherit;False;Property;_TextureTint;Texture Tint;1;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;101;-710.324,569.5021;Inherit;False;Constant;_Float1;Float 1;10;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;100;-715.5415,669.6783;Inherit;False;FLOAT;1;0;FLOAT;0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;-670.4398,-99.00592;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-758.8409,105.094;Inherit;False;Property;_TextureContrast;Texture Contrast;2;0;Create;True;0;0;0;False;0;False;1.3;1.3;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectToWorldMatrixNode;92;-421.3321,699.4818;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.DynamicAppendNode;98;-571.9938,647.2441;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleContrastOpNode;49;-402.6401,56.9939;Inherit;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;94;-311.6859,322.7905;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-73.24595,27.12818;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;SFS/VoxelGrass;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;80;0;79;0
WireConnection;82;0;80;0
WireConnection;83;0;81;0
WireConnection;83;1;82;0
WireConnection;85;0;83;0
WireConnection;85;1;84;0
WireConnection;87;0;81;0
WireConnection;87;1;80;0
WireConnection;90;0;85;0
WireConnection;90;1;88;0
WireConnection;89;0;87;0
WireConnection;89;1;86;0
WireConnection;91;0;90;0
WireConnection;91;1;89;0
WireConnection;93;0;91;0
WireConnection;96;0;95;2
WireConnection;96;1;93;0
WireConnection;100;0;96;0
WireConnection;47;0;46;0
WireConnection;47;1;45;0
WireConnection;98;0;100;0
WireConnection;98;1;101;0
WireConnection;98;2;100;0
WireConnection;49;1;47;0
WireConnection;49;0;48;0
WireConnection;94;0;98;0
WireConnection;94;1;92;0
WireConnection;0;0;49;0
WireConnection;0;11;94;0
ASEEND*/
//CHKSM=5902B1995F58818B758A14A48D2C527EE9DD8B66
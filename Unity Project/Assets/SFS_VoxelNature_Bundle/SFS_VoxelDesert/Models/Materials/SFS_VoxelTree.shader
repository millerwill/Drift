// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SFS/VoxelTree"
{
	Properties
	{
		_AlbedoTexture("Albedo Texture", 2D) = "white" {}
		_TextureTint("Texture Tint", Color) = (1,1,1,1)
		_TextureContrast("Texture Contrast", Range( 0 , 2)) = 1.3
		_BendAmount("Bend Amount", Range( 0 , 0.02)) = 0.01
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

		uniform float _BendAmount;
		uniform float _TextureContrast;
		uniform sampler2D _AlbedoTexture;
		uniform float4 _AlbedoTexture_ST;
		uniform float4 _TextureTint;


		float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
		{
			original -= center;
			float C = cos( angle );
			float S = sin( angle );
			float t = 1 - C;
			float m00 = t * u.x * u.x + C;
			float m01 = t * u.x * u.y - S * u.z;
			float m02 = t * u.x * u.z + S * u.y;
			float m10 = t * u.x * u.y + S * u.z;
			float m11 = t * u.y * u.y + C;
			float m12 = t * u.y * u.z - S * u.x;
			float m20 = t * u.x * u.z - S * u.y;
			float m21 = t * u.y * u.z + S * u.x;
			float m22 = t * u.z * u.z + C;
			float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
			return mul( finalMatrix, original ) + center;
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
			float temp_output_92_0 = ( ( cos( ( ( ( ase_worldPos.x + ase_worldPos.z ) * 0.06 ) + _Time.y ) ) * ase_vertex3Pos.y ) * _BendAmount );
			float4 appendResult93 = (float4(temp_output_92_0 , 0.0 , temp_output_92_0 , 0.0));
			float4 break96 = mul( appendResult93, unity_ObjectToWorld );
			float4 appendResult98 = (float4(break96.x , 0 , break96.z , 0.0));
			float3 rotatedValue100 = RotateAroundAxis( float3( 0,0,0 ), appendResult98.xyz, float3( 0,0,0 ), 0.0 );
			v.vertex.xyz += rotatedValue100;
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
-1871;63;1822;949;1178.81;416.9676;1.154245;True;False
Node;AmplifyShaderEditor.CommentaryNode;81;-3183.573,71.86874;Inherit;False;2993.77;1032.762;Comment;19;100;99;98;97;96;95;94;93;92;91;90;89;88;87;86;85;84;83;82;TreeSway;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;82;-3145.709,121.8688;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;83;-3098.882,492.3514;Inherit;False;Constant;_Float0;Float 0;6;0;Create;True;0;0;0;False;0;False;0.06;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;84;-2906.394,226.4088;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;85;-2776.762,722.425;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;86;-2686.327,422.916;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;87;-2457.34,423.6277;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;88;-2229.98,425.3206;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;89;-2425.217,660.6763;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;90;-2091.274,872.0416;Inherit;False;Property;_BendAmount;Bend Amount;3;0;Create;True;0;0;0;False;0;False;0.01;0.01;0;0.02;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;91;-2028.078,597.5236;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;92;-1790.475,595.4235;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectToWorldMatrixNode;94;-1522.568,908.2006;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.DynamicAppendNode;93;-1548.616,591.0407;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;95;-1298.912,588.4267;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;78;-1353.664,-733.7945;Inherit;False;1000.072;658.6371;Comment;4;76;3;24;74;Albedo;1,1,1,1;0;0
Node;AmplifyShaderEditor.ColorNode;24;-1277.665,-376.7945;Inherit;False;Property;_TextureTint;Texture Tint;1;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-1311.102,-662.0837;Inherit;True;Property;_AlbedoTexture;Albedo Texture;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;96;-1056.271,590.2834;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.Vector3Node;97;-1032.424,273.1415;Inherit;False;Constant;_YAxisOffset;Y Axis Offset;3;0;Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;-833.3916,-395.2573;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;98;-820.1485,590.4482;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;99;-761.6747,844.2751;Inherit;False;Constant;_Float1;Float 1;7;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;77;-1101.322,-283.085;Inherit;False;Property;_TextureContrast;Texture Contrast;2;0;Create;True;0;0;0;False;0;False;1.3;1.3;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleContrastOpNode;76;-565.5916,-239.2576;Inherit;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RotateAboutAxisNode;100;-502.0893,597.2489;Inherit;False;False;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;SFS/VoxelTree;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;84;0;82;1
WireConnection;84;1;82;3
WireConnection;86;0;84;0
WireConnection;86;1;83;0
WireConnection;87;0;86;0
WireConnection;87;1;85;0
WireConnection;88;0;87;0
WireConnection;91;0;88;0
WireConnection;91;1;89;2
WireConnection;92;0;91;0
WireConnection;92;1;90;0
WireConnection;93;0;92;0
WireConnection;93;2;92;0
WireConnection;95;0;93;0
WireConnection;95;1;94;0
WireConnection;96;0;95;0
WireConnection;74;0;3;0
WireConnection;74;1;24;0
WireConnection;98;0;96;0
WireConnection;98;1;97;2
WireConnection;98;2;96;2
WireConnection;76;1;74;0
WireConnection;76;0;77;0
WireConnection;100;1;99;0
WireConnection;100;3;98;0
WireConnection;0;0;76;0
WireConnection;0;11;100;0
ASEEND*/
//CHKSM=DC4D3922FA929E769A8221B1F2074582A9A61E5A
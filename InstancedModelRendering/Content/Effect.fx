#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 ViewProjection;
float3 LightDir;

texture2D ColorTex;

sampler2D ColorSampler = sampler_state
{
	Texture = <ColorTex>;
};

struct VertexInput
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
	float2 TexCoord : TEXCOORD0;
};

struct PixelInput
{
	float4 Position : SV_POSITION0;
	float3 Normal : TEXCOORD0;
	float2 TexCoord : TEXCOORD1;
};


PixelInput VS(VertexInput input)
{
	PixelInput output;

	output.Position = mul(input.Position, World);
	output.Position = mul(output.Position, ViewProjection);

	output.Normal = normalize(mul(input.Normal, World));

	output.TexCoord = input.TexCoord;

	return output;
}

PixelInput VSInstanced(VertexInput input, in float4x4 instanceTransform : TEXCOORD1)
{
	PixelInput output;

	output.Position = mul(input.Position, World);
	output.Position = mul(output.Position, instanceTransform);
	output.Position = mul(output.Position, ViewProjection);

	output.Normal = normalize(mul(input.Normal, World));
	output.Normal = normalize(mul(output.Normal, instanceTransform));

	output.TexCoord = input.TexCoord;

	return output;
}

float4 PS(PixelInput input) : COLOR
{
	float4 color = tex2D(ColorSampler, input.TexCoord);

	// calculate diffuse
	float diffuse = saturate(dot(input.Normal, LightDir));

	float3 diffuseColor = float3(1, 1, 1);
	color.rgb *= diffuse * diffuseColor;

	return color;
}


technique Render
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VS();
		PixelShader = compile PS_SHADERMODEL PS();
	}
};

technique RenderInstanced
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VSInstanced();
		PixelShader = compile PS_SHADERMODEL PS();
	}
};

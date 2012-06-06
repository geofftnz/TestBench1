float4x4 World;
float4x4 View;
float4x4 Projection;

// TODO: add effect parameters here.

Texture HeightTex;
sampler HeightTexSampler = sampler_state { texture = <HeightTex>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = Wrap; AddressV = Wrap;};



struct VertexShaderInput
{
    float3 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;

    // TODO: add input channels such as texture
    // coordinates and vertex colors here.
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;

    // TODO: add vertex shader outputs such as colors and texture
    // coordinates here. These values will automatically be interpolated
    // over the triangle, and provided as input to your pixel shader.
};

float2 halfPixel;
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    //input.Position.x =  input.Position.x - 2*halfPixel.x;
    //input.Position.y =  input.Position.y + 2*halfPixel.y;
    output.Position = float4(input.Position,1);
    output.TexCoord = input.TexCoord ;
    return output;
}

/*
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

	//output.Position = input.Position;

	output.Texture = input.Texture;
    // TODO: add your vertex shader code here.

    return output;
}*/

float texel = 0.0009765625f;

float SampleHeight(float2 p)
{
	float4 cell = tex2D(HeightTexSampler, p);
	return (cell.r + cell.g) * texel; // scale to texel space.
}

float4 SampleTerrainNoScale(float2 p)
{
	return tex2D(HeightTexSampler, p);
}

float AOSample(float h, float s, float d)
{
	if (h>=s) return 0.0f;

	return atan((s-h)/d) / 1.570796;
}

float contour(float h0, float h1,float h2, float h3, float h4, float contourscale)
{
	float b0 = floor(h0*contourscale);
	float b1 = floor(h1*contourscale);
	float b2 = floor(h2*contourscale);
	float b3 = floor(h3*contourscale);
	float b4 = floor(h4*contourscale);

	return (b0>b1 || b0>b2 || b0>b3 || b0>b4) ? 1.0:0.0;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 col;// = {0.6f,0.6f,0.6f,1.0f};

	//float4 col1 = {0.85,0.85,0.9,1.0};
	//float4 col2 = col1; //{0.9,0.8,0.7,1.0};
	//float4 col3 = {0.2,0.6,0.1,1.0};

	float4 colH1 = {0.7,0.7,0.8,1.0};
	float4 colH2 = {0.9,0.9,0.95,1.0};

	float4 colL1 = {0.8,0.6,0.5,1.0};
	float4 colL2 = {1.0,0.8,0.1,1.0};

	//float4 col3 = {0.25,0.4,0.2,1.0};
	//float4 col4 = {0.7,0.72,0.4,1.0};

	float4 p = SampleTerrainNoScale(input.TexCoord);
	float h = SampleHeight(input.TexCoord);

	//col = (col2 * h) + (col1 * (1.0-h));
	//col = lerp(col1,col2,h);
	
	float looseblend = clamp(p.g * 0.2,0,1);


	col = lerp(lerp(colH1,colH2,h),lerp(colL1,colL2,h),looseblend);

	//col = lerp(col,col3,looseblend);


	float h1 = SampleHeight(input.TexCoord + float2(0,-texel));
	float h2 = SampleHeight(input.TexCoord + float2(0,texel));
	float h3 = SampleHeight(input.TexCoord + float2(-texel,0));
	float h4 = SampleHeight(input.TexCoord + float2(texel,0));

	//float h5 = SampleHeight(input.TexCoord + float2(texel*2,texel*-3));
	//float h6 = SampleHeight(input.TexCoord + float2(texel*-2,texel*-3));
	//float h7 = SampleHeight(input.TexCoord + float2(texel*-3,texel*2));
	//float h8 = SampleHeight(input.TexCoord + float2(texel*3,texel*-2));
//
	float3 n = normalize(float3(h2-h1,h4-h3,2.0*texel));
	float3 l = normalize(float3(0.5,0.2,0.2));
	
	float diffuse = clamp(dot(n,l)*0.5+0.5,0,1);
	//float ao = 0.0f;
	//ao+=AOSample(h,h1,texel);
	//ao+=AOSample(h,h2,texel);
	//ao+=AOSample(h,h3,texel);
	//ao+=AOSample(h,h4,texel);
//
	//ao+=AOSample(h,h5,3.6*texel);
	//ao+=AOSample(h,h6,3.6*texel);
	//ao+=AOSample(h,h7,3.6*texel);
	//ao+=AOSample(h,h8,3.6*texel);
//
	//ao=clamp(1.0-(ao*0.25),0,1);

	
	

	col *= (0.3 + 0.7 * diffuse);
	//col = col * (diffuse * 0.8 + 0.2 *ao);

	col.r += contour(h,h1,h2,h3,h4,50) * 0.1;
	col.r += contour(h,h1,h2,h3,h4,10) * 0.1;

	//col = float4(0,0,0,1);
	//col.r += contour(h1,h2,h3,h4,100) * (0.3 + h*0.3);
	//col.r += contour(h1,h2,h3,h4,20) * (0.3 + h*0.3);
	

	col.a = 1.0;

	//col.b = s*10;
	
	//col.r = input.Texture.x;
	//col.g = input.Texture.y;


    return col;
}

technique Relief
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}

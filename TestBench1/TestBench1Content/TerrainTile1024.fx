//	defines
#define TEXDIM 1024
#define TEXLOG2 10




float4x4 World;
float4x4 TexToView;
float4x4 View;
float4x4 Projection;
float3 Eye;
float3 LightDir;


//------- Texture Samplers --------

Texture HeightTex;
sampler HeightTexSampler = sampler_state { texture = <HeightTex>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = mirror; AddressV = mirror;};
sampler HeightTexSampler2 = sampler_state { texture = <HeightTex>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = Wrap; AddressV = Wrap;};

Texture ShadeTex;
sampler ShadeTexSampler = sampler_state { texture = <ShadeTex>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = POINT; AddressU = Wrap; AddressV = Wrap;};

// TODO: add effect parameters here.

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 BoxCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float4 BoxCoord : TEXCOORD0;
};


struct PixelToFrame
{
    float4 Colour : COLOR0;
	float Depth: DEPTH0;
};



// stolen from dxsdk "RayCastTerrain" sample.
//--------------------------------------------------------------------------------------
// Intersect the ray with the texture bounding box so we know where to start.
// This is for the case where our eyepoint is outside of the box.
//--------------------------------------------------------------------------------------
float3 GetFirstSceneIntersection( float3 vRayO, float3 vRayDir )
{
    // Intersect the ray with the bounding box
    // ( y - vRayO.y ) / vRayDir.y = t

    float fMaxT = -1;
    float t;
    float3 vRayIntersection;

    // -X plane
    if( vRayDir.x > 0 )
    {
        t = ( 0 - vRayO.x ) / vRayDir.x;
        fMaxT = max( t, fMaxT );
    }

    // +X plane
    if( vRayDir.x < 0 )
    {
        t = ( 1 - vRayO.x ) / vRayDir.x;
        fMaxT = max( t, fMaxT );
    }

    // -Y plane
    if( vRayDir.y > 0 )
    {
        t = ( 0 - vRayO.y ) / vRayDir.y;
        fMaxT = max( t, fMaxT );
    }

    // +Y plane
    if( vRayDir.y < 0 )
    {
        t = ( 1 - vRayO.y ) / vRayDir.y;
        fMaxT = max( t, fMaxT );
    }

    // -Z plane
    if( vRayDir.z > 0 )
    {
        t = ( 0 - vRayO.z ) / vRayDir.z;
        fMaxT = max( t, fMaxT );
    }

    // +Z plane
    if( vRayDir.z < 0 )
    {
        t = ( 1 - vRayO.z ) / vRayDir.z;
        fMaxT = max( t, fMaxT );
    }

    vRayIntersection = vRayO + vRayDir * fMaxT;

    return vRayIntersection;
}




VertexShaderOutput VSTileBox(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

	output.BoxCoord = input.BoxCoord;

    return output;
}


float4 IntersectRayHeightMap(float3 rayPos, float3 rayDir)
{

	float4 p = {0.0f,0.0f,0.0f,0.0f};

	float3 boxEnter = rayPos;
	float3 posRayDir = rayDir;

	float3 texEntry;
	float3 texExit;
	float3 texHit;
	float height= 0.0f;
	float t,tx,tz,qx,qz,qf;

	float umul=1.0f, uofs=0.0f, vmul=1.0f, vofs=0.0f;	// texture coordinate flipping

	int level = TEXLOG2-1;  // replace with log2(texdim)-1
	qf = pow(2.0f,TEXLOG2-level); // quantization factor

	if (rayDir.x < 0.0f) // dx negative, invert x on texture sample
	{
		posRayDir.x = -posRayDir.x;
		boxEnter.x = 1.0f - boxEnter.x;
		umul=-1.0f;
		uofs=1.0f;
	}
	if (rayDir.z < 0.0f) // dz negative, invert z on texture sample
	{
		posRayDir.z = -posRayDir.z;
		boxEnter.z = 1.0f - boxEnter.z;
		vmul=-1.0f;
		vofs=1.0f;
	}

	texEntry = boxEnter;

	float n = 0.0f;

	while ( texEntry.x < 1.0f && texEntry.z < 1.0f && p.w < 0.5f ) 
	{
		n = n + 0.01;

		height = tex2Dlod(HeightTexSampler, float4(texEntry.x+uofs, texEntry.z+vofs, 0, level)); // grab height at point for mip level
			
		qx = (floor(texEntry.x * qf) + 1.0f) / qf;		
		qz = (floor(texEntry.z * qf) + 1.0f) / qf;  // quantize texcoords for level
			
		tx = (qx - texEntry.x) / posRayDir.x; 
		tz = (qz - texEntry.z) / posRayDir.z; // compute intersections
			
		t = min(tx,tz); // closest intersection

		texExit = texEntry + posRayDir * t; // exit point
		texExit = float3((t == tx)?((floor(texEntry.x*qf)+1.0f)/qf):texExit.x, texExit.y, (t == tz)?((floor(texEntry.z*qf)+1.0f)/qf):texExit.z);  // correct for rounding errors
			
		if (  ( (posRayDir.y < 0.0f) ? texExit.y : texEntry.y)    <= height) // intersection, hit point = texEntry
		{
			// actual hit location
			p.xyz = (posRayDir.y < 0.0f) ? texEntry + posRayDir * max((height - texEntry.y) / posRayDir.y,0.0f) : texEntry;

			if (level < 1)  // at actual intersection
			{
				p.w = 0.61f + n;

				// if we've hit well below the point, discard. This effectively sets a max slope, so use with caution.
				//if (height - p.y > 0.02f)
				//{
				//	p.w = 0.55f;
				//}

			}
			else // still walking through the mipmaps
			{
				texEntry = p.xyz;  // advance ray to hit point
				level--;  // drop level
				qf = pow(2.0f,TEXLOG2-level);  // update quantization factor
			}
		}
		else // no intersection
		{
			texEntry = texExit;  // move ray to exit point
			level = (t == tx) ?  min(level+1-fmod(floor(texExit.x*qf),2.0f) ,TEXLOG2-1) : min(level+1-fmod(floor(texExit.z*qf),2.0f) ,TEXLOG2-1); // go up a level if we reach the edge of our current 2x2 block
			qf = pow(2.0f,TEXLOG2-level); // update quantization factor
		}
	}  // end of while loop

	p.x = umul * p.x + uofs;
	p.z = vmul * p.z + vofs;

    return p;

}

/*
float3 CalculateNormal(float3 p)
{
	// Grab the samples on either side of the current one.
	// If we're at an edge, then use the current location in place of the off-edge samples.

	float2 q = float2(floor(p.x*TEXDIM)/TEXDIM,floor(p.z*TEXDIM)/TEXDIM);
	float qf = 1.0f/TEXDIM;
	// north/south/west/east points
	float h;

	h = tex2Dlod(HeightTexSampler, float4(q.x,q.y-qf,0,0));
	float3 n = float3(q.x, h, q.y-qf);

	h = tex2Dlod(HeightTexSampler, float4(q.x,q.y+qf,0,0));
	float3 s = float3(q.x, h, q.y+qf);

	h=tex2Dlod(HeightTexSampler, float4(q.x-qf,q.y,0,0));
	float3 w = float3(q.x-qf,h , q.y);

	h = tex2Dlod(HeightTexSampler, float4(q.x+qf,q.y,0,0));
	float3 e = float3(q.x+qf, h, q.y);
	
	return normalize(cross(e-w,s-n));
}*/

float texel = 0.0009765625f;
float SampleHeight(float2 p)
{
	//float4 cell = tex2D(HeightTexSampler, p);
	//return (cell.r + cell.g) * texel; // scale to texel space.
	float h = tex2Dlod(HeightTexSampler2, float4(p.x,p.y,0,0));
	return h.r*4;
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

float4 GenerateCol(float3 p)
{
	float4 colH1 = {0.3,0.247,0.223,1.0};
	float4 colH2 = {0.3,0.247,0.223,1.0};

	float4 colL1 = {0.41,0.39,0.16,1.0};
	float4 colL2 = {0.41,0.39,0.16,1.0};

	float4 colW = {0.7,0.8,1.0,1.0};
	float4 colA = {1.0,0.0,0.0,1.0};

	float4 s = tex2D(ShadeTexSampler,p.xy);
	float h = SampleHeight(p.xy);

	float looseblend = s.g * 2.0f;
	float4 col = lerp(lerp(colH1,colH2,h),lerp(colL1,colL2,h),looseblend);

	col = lerp(col,colW,clamp(s.b*s.b*3.0,0,0.3)); // water

	//col = lerp(col,colA,s.a);


	float h1 = SampleHeight(float2(p.x,p.y-texel));
	float h2 = SampleHeight(float2(p.x,p.y+texel));
	float h3 = SampleHeight(float2(p.x-texel,p.y));
	float h4 = SampleHeight(float2(p.x+texel,p.y));

	float3 n = normalize(float3(h4-h3,h2-h1,2.0*texel));
	//float3 l = normalize(float3(0.5,0.2,0.2));
	
	float diffuse = clamp(dot(n,LightDir)*0.5+0.5,0,1);
	col *= (0.3 + 0.7 * diffuse);

	//float cscale = 10.0f;
	//col.r += contour(h,h1,h2,h3,h4,cscale*5) * 0.1;
	//col.r += contour(h,h1,h2,h3,h4,cscale) * 0.1;

	col.a = 1.0f;

	return col;
}

// raycast with function call to intersect
PixelToFrame PSRaycastTile(VertexShaderOutput input) 
{
	PixelToFrame output;

	//output.Colour = float4(0.0f,1.0f,0.6f,1.0f);
	output.Depth = 1.0f;

	//float4 colH1 = {0.7,0.7,0.8,1.0};
	//float4 colH2 = {0.9,0.9,0.95,1.0};

	float4 col={0.0f,0.0f,1.0f,1.0f};
	float3 rayDir = normalize(input.BoxCoord.xyz - Eye);
	float3 boxEnter = GetFirstSceneIntersection(Eye, rayDir);

	float4 p = IntersectRayHeightMap( boxEnter, rayDir);

	if (p.w > 0.6)
	{
		//float3 n = CalculateNormal(p.xyz);
		//float l = dot(n,LightDir)*0.2f+0.6f;
		//col.rgb = lerp(colH1,colH2,p.y * 4.0f) * l;
		//col.a = 1.0f;
		col = GenerateCol(p.xzy);
		
	}
	else
	{
		discard;
	}

	// depth calculation
	float4 pp2 = mul(float4(p.xyz,1),World);
	float4 pp = mul(mul(pp2.xyzw,View),Projection);  
	float d = pp.z/pp.w;

	float dd = abs(length(p.xyz - Eye));

	float4 fogcol = {0.8f, 0.88f, 0.92f,1.0f};
	float fogamount = 1.0f / (exp(dd * dd * 0.1));

	//col = lerp(fogcol,col,fogamount);

	output.Depth = d;
	output.Colour = lerp(fogcol,col,fogamount);

	return output;
}


technique ShowParams
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_3_0 VSTileBox();
        PixelShader = compile ps_3_0 PSRaycastTile();
    }
}

technique RaycastTile1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_3_0 VSTileBox();
        PixelShader = compile ps_3_0 PSRaycastTile();
    }
}




struct BoundingBoxVSIn
{
    float4 Position : POSITION0;
    float4 Colour : COLOR0;
};

struct BoundingBoxPSIn
{
    float4 Position : POSITION0;
	float4 Colour : COLOR0;
};

BoundingBoxPSIn BoundingBoxVS(BoundingBoxVSIn input)
{
    BoundingBoxPSIn output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
	output.Colour = input.Colour;

    return output;
}

float4 BoundingBoxPS(BoundingBoxPSIn input) : COLOR0
{

    return input.Colour;
}



technique BBox
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 BoundingBoxVS();
        PixelShader = compile ps_3_0 BoundingBoxPS();
    }
}




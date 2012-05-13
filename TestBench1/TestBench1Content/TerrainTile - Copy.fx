//	defines
#define TEXLOG2 8




float4x4 World;
float4x4 View;
float4x4 Projection;
float3 Eye;


//------- Texture Samplers --------

Texture HeightTex;
sampler HeightTexSampler = sampler_state { texture = <HeightTex>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = mirror; AddressV = mirror;};



// TODO: add effect parameters here.

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 BoxCoord : TEXCOORD0;

    // TODO: add input channels such as texture
    // coordinates and vertex colors here.
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float4 BoxCoord : TEXCOORD0;

    // TODO: add vertex shader outputs such as colors and texture
    // coordinates here. These values will automatically be interpolated
    // over the triangle, and provided as input to your pixel shader.
};

struct HeightmapIntersection
{
	float3 Position;
	float3 Normal;
	bool hit;
};




struct PixelToFrame
{
    float4 Color : COLOR0;
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

float4 PSBruteForce(VertexShaderOutput input) : COLOR0
{

	float height;
	float4 col = {0.0f,0.0f,0.0f,1.0f};

	float3 boxEnter = GetFirstSceneIntersection(Eye, normalize(input.BoxCoord.xyz - Eye));

	float t = 0.0f;
	float dt = t / 20.0f;

	float3 testPos = boxEnter;
	float3 dpos = (input.BoxCoord.xyz - boxEnter) / 250.0f;
	bool fragDiscard = true;
	float count = 0;

	for(int i = 0; i < 250; i++){
		height = tex2D(HeightTexSampler, testPos.xz);

		fragDiscard = false;
		if (testPos.y >= height){
			testPos += dpos;
			fragDiscard = true;
		}
		else{
			count += 1.0f;
		}
	}

	if (fragDiscard){
		discard;
	}

	//float4 col1 = {0.0f,0.2f,0.0f,1.0f};
	//float4 col2 = {0.8f,1.0f,0.3f,1.0f};
	//col = lerp(col1,col2,count * 0.01f);
	//col = lerp(col1,col2,testPos.y*4.0f);

	col.rb = testPos.xz;
	col.g = testPos.y*4.0f;
	col.a = 1.0f;
	

/*
	float3 texSamplePos = boxEnter;
	height = tex2D(HeightTexSampler, texSamplePos.xz);

	float4 col1 = {0.0f,0.2f,0.0f,1.0f};
	float4 col2 = {0.8f,1.0f,0.3f,1.0f};
	col = lerp(col1,col2,height*4.0f);
	height = tex2D(HeightTexSampler, input.BoxCoord.xz);
	col = (col + lerp(col1,col2,height*4.0f)) * 0.5f;;
*/
    return col;
}


float4 PSRaycastTile1(VertexShaderOutput input) : COLOR0
{

	float height;
	float4 col = {0.0f,0.0f,0.0f,1.0f};

	float3 rayDir = normalize(input.BoxCoord.xyz - Eye);
	float3 boxEnter = GetFirstSceneIntersection(Eye, rayDir);
	float3 posRayDir = rayDir;

	float3 texEntry;
	float3 texExit;
	float3 texHit;
	float t,tx,tz;
	float n = 0;

	int level = 7;  // replace with log2(texdim)-1

	if (posRayDir.x < 0.0f)
	{
		// dx negative, invert x on texture sample
		posRayDir.x = -posRayDir.x;
		boxEnter.x = 1.0f - boxEnter.x;

		if (posRayDir.z < 0.0f)
		{
			// dz negative, invert z on texture sample
			posRayDir.z = -posRayDir.z;
			boxEnter.z = 1.0f - boxEnter.z;

			col = float4(1.0f,0.0f,0.0f,1.0f);
		}
		else
		{
			// dz positive, leave z as-is on texture sample
			col = float4(1.0f,1.0f,0.0f,1.0f);
		}
	}
	else
	{
		// dx positive, leave x as-is on texture sample
		if (posRayDir.z < 0.0f)
		{
			// dz negative, invert z on texture sample
			posRayDir.z = -posRayDir.z;
			boxEnter.z = 1.0f - boxEnter.z;

			col = float4(0.0f,1.0f,0.0f,1.0f);
		}
		else
		{
			col = float4(0.0f,0.0f,1.0f,0.5f);
			
			// dz positive, leave z as-is on texture sample
			texEntry = boxEnter;
			height = -10.0f;//tex2Dlod(HeightTexSampler, float4(texEntry.x, texEntry.z, 0, 0));

			while (texEntry.x < 1.0f && texEntry.z < 1.0f && texEntry.y > height)
			{
				n+=1.0f/255.0f;
				height = tex2Dlod(HeightTexSampler, float4(texEntry.x, texEntry.z, 0, 0));

				// compute x-direction intersection
				tx = (((floor(texEntry.x*256.0f)+1)/256.0) - texEntry.x) / posRayDir.x;

				// compute y-direction intersection
				tz = (((floor(texEntry.z*256.0f)+1)/256.0) - texEntry.z) / posRayDir.z;

				// closest intersection
				t = min(tx,tz);

				// exit point
				texExit = texEntry + posRayDir * t;

				// explicitly set exit point to avoid roundoff-error looping.
				if (t == tx)
				{
					texExit.x = ((floor(texEntry.x*256.0f)+1)/256.0);
				}
				else
				{
					texExit.z = ((floor(texEntry.z*256.0f)+1)/256.0);
				}

				if (posRayDir.y >= 0.0f)  // ray travelling up (move this test out)
				{
					if (texEntry.y <= height){ // intersection, hit point = texEntry
						texHit = texEntry;
						col = float4(texHit.x,texHit.y,texHit.z,1.0f);
					}
				}
				else  // ray travelling down
				{
					if (texExit.y <= height){ // intersection, hit point = texEntry
						
						texHit = texEntry + posRayDir * max((height - texEntry.y) / posRayDir.y,0.0f);

						//col = float4(texHit.x,texHit.y,texHit.z,1.0f);
						col = float4(0.2+n,1.0-n*3.0f,0.0f,1.0f);
					}
				}

				texEntry = texExit;
			}

			//col = float4(0.0f,0.0f,1.0f,1.0f);
		}
	}

	if (col.a < 1.0f) discard;

    return col;
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

	//if (posRayDir.y < 0.0f) // dy negative, ray travelling down
	//{
		
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
					p.w = 0.5f + n;
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
/*				
	}
	else   // dy positive, ray travelling up
	{
		while ( texEntry.x < 1.0f && texEntry.z < 1.0f && p.w < 0.5f ) 
		{
			height = tex2Dlod(HeightTexSampler, float4(texEntry.x*umul+uofs, texEntry.z*vmul+vofs, 0, level)); // grab height at point for mip level
			qx = (floor(texEntry.x * qf) + 1.0f) / qf;		
			qz = (floor(texEntry.z * qf) + 1.0f) / qf;  // quantize texcoords for level
			tx = (qx - texEntry.x) / posRayDir.x; 
			tz = (qz - texEntry.z) / posRayDir.z; // compute intersections
			t = min(tx,tz); // closest intersection
			texExit = texEntry + posRayDir * t; // exit point
			texExit = float3((t == tx)?((floor(texEntry.x*qf)+1.0f)/qf):texExit.x, texExit.y, (t == tz)?((floor(texEntry.z*qf)+1.0f)/qf):texExit.z);  // correct for rounding errors
			if (texEntry.y <= height) // intersection, hit point = texEntry
			{
				p.xyz = texEntry;
				if (level < 1)  // at actual intersection
				{
					p.w = 1.0f;
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
	}*/

	p.x = umul * p.x + uofs;
	p.z = vmul * p.z + vofs;

    return p;

}


// raycast with function call to intersect
float4 PSRaycastTile4(VertexShaderOutput input) : COLOR0
{

	float4 col={0.0f,0.0f,1.0f,1.0f};
	float3 rayDir = normalize(input.BoxCoord.xyz - Eye);
	float3 boxEnter = GetFirstSceneIntersection(Eye, rayDir);

	float4 p = IntersectRayHeightMap( boxEnter, rayDir);

	if (p.w > 0.5)
	{
		//col.r = p.y*10.0f;
		//col.g = 0.8f - (p.w - 0.5f)*2.0f;  //p.xz;// * 0.5 + float3(0.5f,0.5f,0.5f);
		////col.b = (p.w - 0.5f) * 1.5f;
		//col.a = 1.0f;
//
		//col.b = 0.0f;
		//col.b += (rayDir.x < 0.0f)?0.6f:0.0f;
		//col.b += (rayDir.z < 0.0f)?0.4f:0.0f;

		col.rg = p.xz;
		col.b = (p.w-0.5f);
		col.a = 1.0f;
	}
	else
	{
		discard;
	}

	return col;
}

// raycast with mipmap accelleration
float4 PSRaycastTile2(VertexShaderOutput input) : COLOR0
{

	float height;
	float4 col = {0.0f,0.0f,0.0f,1.0f};

	float3 rayDir = normalize(input.BoxCoord.xyz - Eye);
	float3 boxEnter = GetFirstSceneIntersection(Eye, rayDir);
	float3 posRayDir = rayDir;

	float3 texEntry;
	float3 texExit;
	float3 texHit;
	float t,tx,tz,qx,qz,qf;
	float n = 0;

	int level = 7;  // replace with log2(texdim)-1
	// quantization factor
	qf = pow(2.0f,8-level);


	if (posRayDir.x < 0.0f)
	{
		// dx negative, invert x on texture sample
		posRayDir.x = -posRayDir.x;
		boxEnter.x = 1.0f - boxEnter.x;

		if (posRayDir.z < 0.0f)
		{
			// dz negative, invert z on texture sample
			posRayDir.z = -posRayDir.z;
			boxEnter.z = 1.0f - boxEnter.z;

			col = float4(1.0f,0.0f,0.0f,1.0f);
		}
		else
		{
			// dz positive, leave z as-is on texture sample
			col = float4(1.0f,1.0f,0.0f,1.0f);
		}
	}
	else
	{
		// dx positive, leave x as-is on texture sample
		if (posRayDir.z < 0.0f)
		{
			// dz negative, invert z on texture sample
			posRayDir.z = -posRayDir.z;
			boxEnter.z = 1.0f - boxEnter.z;

			col = float4(0.0f,1.0f,0.0f,1.0f);
		}
		else
		{
			col = float4(0.0f,0.0f,1.0f,0.1f);
			
			// dz positive, leave z as-is on texture sample
			texEntry = boxEnter;
			height = -10.0f;//tex2Dlod(HeightTexSampler, float4(texEntry.x, texEntry.z, 0, 0));

			while ( texEntry.x < 1.0f && texEntry.z < 1.0f && col.a < 0.5f ) // && col.a < 0.5f  && texEntry.y > height
			{
				n+=1.0f/255.0f;  // sample counter

				//col.r = texEntry.x;
				//col.g = texEntry.z;
				//col.b = n;

				// grab height at point for mip level
				height = tex2Dlod(HeightTexSampler, float4(texEntry.x, texEntry.z, 0, level));

				// quantize texcoords for level
				qx = (floor(texEntry.x * qf) + 1.0f) / qf;
				qz = (floor(texEntry.z * qf) + 1.0f) / qf;

				// compute x-direction intersection
				tx = (qx - texEntry.x) / posRayDir.x;

				// compute y-direction intersection
				tz = (qz - texEntry.z) / posRayDir.z;

				// closest intersection
				t = min(tx,tz);

				// exit point
				texExit = texEntry + posRayDir * t;

				// explicitly set exit point to avoid roundoff-error looping.
				//if (t == tx)
				//{
					//texExit.x = ((floor(texEntry.x*qf)+1.0f)/qf);
				//}
				//else
				//{
					//texExit.z = ((floor(texEntry.z*qf)+1.0f)/qf);
				//}

				//texExit.x = (t == tx)?((floor(texEntry.x*qf)+1.0f)/qf):texExit.x;
				//texExit.z = (t == tz)?((floor(texEntry.z*qf)+1.0f)/qf):texExit.z;

				texExit = float3(
					(t == tx)?((floor(texEntry.x*qf)+1.0f)/qf):texExit.x, 
					texExit.y, 
					(t == tz)?((floor(texEntry.z*qf)+1.0f)/qf):texExit.z);
				

				/*
				if (posRayDir.y >= 0.0f)  // ray travelling up (move this test out)
				{
					if (texEntry.y <= height){ // intersection, hit point = texEntry
						texHit = texEntry;

						if (level < 1)  // at actual intersection
						{
							col = float4(0.2+n,1.0-n*3.0f,0.0f,1.0f);
						}
						else
						{
							// advance ray
							texEntry = texExit;

							// drop level
							level--;

							// quantization factor
							qf = pow(2.0f,8-level);
						}
					}

					// bail out
					n = 1.0f;
				}
				else  // ray travelling down
				{

				*/
					if (texExit.y <= height){ // intersection, hit point = texEntry
						
						texHit = texEntry + posRayDir * max((height - texEntry.y) / posRayDir.y,0.0f);

						if (level < 1)  // at actual intersection
						{
							col = float4(0.2+n,1.0-n*3.0f,0.0f,1.0f);
						}
						else // still walking through the mipmaps
						{
							// advance ray
							texEntry = texHit;

							// drop level
							level--;

							// quantization factor
							qf = pow(2.0f,8-level);
						}
					}
					else // no intersection
					{
						// move ray to exit point
						texEntry = texExit;

						// go up a level if we reach the edge of our current 2x2 block
						if (t == tx)
						{
							level = min(level+1-fmod(floor(texExit.x*qf),2.0f) ,7);
						}
						else
						{
							level = min(level+1-fmod(floor(texExit.z*qf),2.0f) ,7);
						}
						// quantization factor
						qf = pow(2.0f,8-level);

					}
				//}
			}
			/*
			if (col.a < 1.0)
			{
				discard;
			}*/

		}
	}
    return col;
}


// raycast with mipmap accelleration and interpolation
float4 PSRaycastTile3(VertexShaderOutput input) : COLOR0
{

	float height;
	float4 col = {0.0f,0.0f,0.0f,1.0f};

	float3 rayDir = normalize(input.BoxCoord.xyz - Eye);
	float3 boxEnter = GetFirstSceneIntersection(Eye, rayDir);
	float3 posRayDir = rayDir;

	float3 texEntry;
	float3 texExit;
	float3 texHit;
	float t,tx,tz,qx,qz,qf;
	float n = 0;

	int level = 7;  // replace with log2(texdim)-1
	// quantization factor
	qf = pow(2.0f,8-level);


	if (posRayDir.x < 0.0f)
	{
		// dx negative, invert x on texture sample
		posRayDir.x = -posRayDir.x;
		boxEnter.x = 1.0f - boxEnter.x;

		if (posRayDir.z < 0.0f)
		{
			// dz negative, invert z on texture sample
			posRayDir.z = -posRayDir.z;
			boxEnter.z = 1.0f - boxEnter.z;

			col = float4(1.0f,0.0f,0.0f,1.0f);
		}
		else
		{
			// dz positive, leave z as-is on texture sample
			col = float4(1.0f,1.0f,0.0f,1.0f);
		}
	}
	else
	{
		// dx positive, leave x as-is on texture sample
		if (posRayDir.z < 0.0f)
		{
			// dz negative, invert z on texture sample
			posRayDir.z = -posRayDir.z;
			boxEnter.z = 1.0f - boxEnter.z;

			col = float4(0.0f,1.0f,0.0f,1.0f);
		}
		else
		{
			col = float4(0.0f,0.0f,1.0f,0.1f);
			
			// dz positive, leave z as-is on texture sample
			texEntry = boxEnter;
			height = -10.0f;//tex2Dlod(HeightTexSampler, float4(texEntry.x, texEntry.z, 0, 0));

			while ( texEntry.x < 1.0f && texEntry.z < 1.0f && col.a < 0.5f ) // && col.a < 0.5f  && texEntry.y > height
			{
				n+=1.0f/255.0f;  // sample counter

				//col.r = texEntry.x;
				//col.g = texEntry.z;
				//col.b = n;

				// grab height at point for mip level
				height = tex2Dlod(HeightTexSampler, float4(texEntry.x, texEntry.z, 0, level));

				// quantize texcoords for level
				qx = (floor(texEntry.x * qf) + 1.0f) / qf;
				qz = (floor(texEntry.z * qf) + 1.0f) / qf;

				// compute x-direction intersection
				tx = (qx - texEntry.x) / posRayDir.x;

				// compute y-direction intersection
				tz = (qz - texEntry.z) / posRayDir.z;

				// closest intersection
				t = min(tx,tz);

				// exit point
				texExit = texEntry + posRayDir * t;

				// explicitly set exit point to avoid roundoff-error looping.
				//if (t == tx)
				//{
					//texExit.x = ((floor(texEntry.x*qf)+1.0f)/qf);
				//}
				//else
				//{
					//texExit.z = ((floor(texEntry.z*qf)+1.0f)/qf);
				//}

				//texExit.x = (t == tx)?((floor(texEntry.x*qf)+1.0f)/qf):texExit.x;
				//texExit.z = (t == tz)?((floor(texEntry.z*qf)+1.0f)/qf):texExit.z;

				texExit = float3(
					(t == tx)?((floor(texEntry.x*qf)+1.0f)/qf):texExit.x, 
					texExit.y, 
					(t == tz)?((floor(texEntry.z*qf)+1.0f)/qf):texExit.z);
				

				/*
				if (posRayDir.y >= 0.0f)  // ray travelling up (move this test out)
				{
					if (texEntry.y <= height){ // intersection, hit point = texEntry
						texHit = texEntry;

						if (level < 1)  // at actual intersection
						{
							col = float4(0.2+n,1.0-n*3.0f,0.0f,1.0f);
						}
						else
						{
							// advance ray
							texEntry = texExit;

							// drop level
							level--;

							// quantization factor
							qf = pow(2.0f,8-level);
						}
					}

					// bail out
					n = 1.0f;
				}
				else  // ray travelling down
				{

				*/
					if (texExit.y <= height){ // intersection, hit point = texEntry
						
						//texHit = texEntry + posRayDir * max((height - texEntry.y) / posRayDir.y,0.0f);

						if (level < 1)  // at actual intersection
						{
							//col = float4(0.2+n,1.0-n*3.0f,0.0f,1.0f);


							// we've gotten to the bottom of the quadtree, so calculate the interpolated entry and exit heights.
							//h00 = tex2Dlod(HeightTexSampler, float4((floor(texEntry.x*qf)+1.0)/qf, (floor(texEntry.z*qf)+1.0)/qf, 0, level));
							float h10 = tex2Dlod(HeightTexSampler, float4((floor(texEntry.x*qf)+1.0)/qf, (floor(texEntry.z*qf))/qf, 0, level));
							float h11 = tex2Dlod(HeightTexSampler, float4((floor(texEntry.x*qf)+1.0)/qf, (floor(texEntry.z*qf)+1.0)/qf, 0, level));
							float h01 = tex2Dlod(HeightTexSampler, float4((floor(texEntry.x*qf))/qf, (floor(texEntry.z*qf)+1.0)/qf, 0, level));

							float xx = (texEntry.x - (floor(texEntry.x*qf))/qf) * qf;
							float zz = (texEntry.z - (floor(texEntry.z*qf))/qf) * qf;

							float hentry=0.0f;
							float hexit=0.0f;

							if ( xx > zz)
							{
								hentry = lerp(height,h10,xx);
							}
							else
							{
								hentry = lerp(height,h01,zz);
							}

							xx = (texExit.x - (floor(texExit.x*qf))/qf)*qf;
							zz = (texExit.z - (floor(texExit.z*qf))/qf)*qf;

							if ( xx < zz)
							{
								hexit = lerp(h01,h11,xx);
							}
							else
							{
								hexit = lerp(h10,h11,zz);
							}

							float q = texEntry.y - texExit.y - hentry + hexit;

							if (texEntry.y > hentry && texExit.y <= hexit && q != 0.0f)  // real intersection
							{
								float tt = (texEntry.y - hentry) / q;
								texHit = texEntry + posRayDir * tt / qf;	
								col = float4(0.2+n,1.0-n*3.0f,0.0f,1.0f);															
							}
							else
							{
								// carry on

								// move ray to exit point
								texEntry = texExit;

								// go up a level if we reach the edge of our current 2x2 block
								if (t == tx)
								{
									level = min(level+1-fmod(floor(texExit.x*qf),2.0f) ,7);
								}
								else
								{
									level = min(level+1-fmod(floor(texExit.z*qf),2.0f) ,7);
								}
								// quantization factor
								qf = pow(2.0f,8-level);
							}
						}
						else // still walking through the mipmaps
						{
							// advance ray
							texEntry = texHit;

							// drop level
							level--;

							// quantization factor
							qf = pow(2.0f,8-level);
						}
					}
					else // no intersection
					{
						// move ray to exit point
						texEntry = texExit;

						// go up a level if we reach the edge of our current 2x2 block
						if (t == tx)
						{
							level = min(level+1-fmod(floor(texExit.x*qf),2.0f) ,7);
						}
						else
						{
							level = min(level+1-fmod(floor(texExit.z*qf),2.0f) ,7);
						}
						// quantization factor
						qf = pow(2.0f,8-level);

					}
				//}
			}
			/*
			if (col.a < 1.0)
			{
				discard;
			}*/

		}
	}
    return col;
}


technique ShowParams
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_3_0 VSTileBox();
        PixelShader = compile ps_3_0 PSBruteForce();
    }
}

technique RaycastTile1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_3_0 VSTileBox();
        PixelShader = compile ps_3_0 PSRaycastTile4();
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
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 BoundingBoxVS();
        PixelShader = compile ps_2_0 BoundingBoxPS();
    }
}




using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Threading.Tasks;
using Utils;

namespace TerrainEngine
{
    public class TerrainTile
    {

        /// <summary>
        /// Width of tile data
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Height of tile data
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// The heightmap used to generate the tile's textures
        /// 
        /// Consider removing this once generation is done to save memory.
        /// 
        /// This needs to be (width+1) * (height+1) to allow for a 1 row overlap with next tile vertices on right/bottom edges.
        /// </summary>
        public float[] Data { get; private set; }

        public float MinHeight { get; private set; }
        public float MaxHeight { get; private set; }


        private Vector3 BoundingBoxLowerCorner;
        private Vector3 BoundingBoxUpperCorner;

        /// <summary>
        /// vertices of the bounding box for full render
        /// </summary>
        private RaycastBoxVertex[] BoundingBoxVertex = new RaycastBoxVertex[8];

        /// <summary>
        /// vertices of bounding box for wireframe render
        /// </summary>
        private VertexPositionColor[] BoundingBoxRenderVertex = new VertexPositionColor[8];

        /// <summary>
        /// indices of the bounding box
        /// </summary>
        private short[] BoundingBoxIndex = new short[36];

        private short[] BoundingBoxRenderIndex = new short[24];

        /// <summary>
        /// float texture of heightmap. This is the texture that will be raymarched.
        /// </summary>
        private Texture2D HeightTex;

        /// <summary>
        /// normal map for this tile.
        /// 
        /// Later we will have to sort out how normals are calculated across tile boundaries.
        /// </summary>
        private Texture2D NormalTex;

        /// <summary>
        /// texture map containing data for the shader, such as AO and shadow maps.
        /// </summary>
        private Texture2D ShadeTex;

        // rendering-specifics

        /// <summary>
        /// Offset of this tile in the world.
        /// </summary>
        public Vector3 Offset { get; set; }

        public float Scale { get; set; }

        /// <summary>
        /// Tile->World transform
        /// </summary>
        public Matrix TileMatrix { get; private set; }

        /// <summary>
        /// Inverse tile matrix, for transforming the eye position into tile-space.
        /// </summary>
        public Matrix InverseTileMatrix { get; private set; }
        


        public TerrainTile(int width, int height, Vector3 offset, float scale)
        {
            this.Width = width;
            this.Height = height;
            this.Data = new float[this.Width * this.Height];

            this.Offset = offset;// new Vector3(-0.5f, 0.0f, -0.5f);
            this.Scale = scale;
            this.SetTransformMatrix();
        }



        /// <summary>
        /// Upload textures
        /// </summary>
        protected void LoadContent()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        protected void UnloadContent()
        {
        }

        public void Draw(GameTime gameTime, GraphicsDevice device, Effect effect, Vector3 eyePos, Matrix viewMatrix, Matrix worldMatrix, Matrix projectionMatrix, Vector3 lightDirection)
        {

            // transform the eye pos into tile space
            Vector3 eyePosTile = Vector3.Transform(eyePos, this.InverseTileMatrix);

            //Matrix texToViewMatrix = Matrix.Multiply(Matrix.Multiply(this.TileMatrix, viewMatrix),projectionMatrix);
            Matrix texToViewMatrix = Matrix.Multiply(this.TileMatrix, Matrix.Multiply(viewMatrix, projectionMatrix));

            effect.Parameters["HeightTex"].SetValue(this.HeightTex);
            effect.Parameters["Eye"].SetValue(eyePosTile);
            effect.Parameters["World"].SetValue(this.TileMatrix);
            effect.Parameters["View"].SetValue(viewMatrix);
            effect.Parameters["TexToView"].SetValue(texToViewMatrix);
            effect.Parameters["Projection"].SetValue(projectionMatrix);
            effect.Parameters["LightDir"].SetValue(lightDirection);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                this.RenderTile(device);
            }
        }

        public void DrawBox(GameTime gameTime, GraphicsDevice device, Effect effect, Vector3 eyePos, Matrix viewMatrix, Matrix worldMatrix, Matrix projectionMatrix, Vector3 lightDirection)
        {

            // transform the eye pos into tile space
            Vector3 eyePosTile = Vector3.Transform(eyePos, this.InverseTileMatrix);

            //effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["HeightTex"].SetValue(this.HeightTex);
            effect.Parameters["Eye"].SetValue(eyePosTile);
            effect.Parameters["World"].SetValue(this.TileMatrix);
            effect.Parameters["View"].SetValue(viewMatrix);
            //effect.Parameters["TexToView"].SetValue(texToViewMatrix);
            effect.Parameters["Projection"].SetValue(projectionMatrix);
            effect.Parameters["LightDir"].SetValue(lightDirection);
            //var light = new Vector3(1.2f, 3.0f, 0.5f);
            //light.Normalize();
            //effect.Parameters["xLightDirection"].SetValue(light);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();


                this.RenderBoundingBox(device);
                //this.RenderTile(device);

            }
        }

        private void RenderTile(GraphicsDevice device)
        {
            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, this.BoundingBoxVertex, 0, 8, this.BoundingBoxIndex, 0, 12);
            
        }
        private void RenderBoundingBox(GraphicsDevice device)
        {
            device.DrawUserIndexedPrimitives(PrimitiveType.LineList, this.BoundingBoxRenderVertex, 0, 8, this.BoundingBoxRenderIndex, 0, 12);
        }

        /// <summary>
        /// Creates the transform and inv-transform matrices.
        /// </summary>
        private void SetTransformMatrix()
        {

            var scaleMatrix = Matrix.CreateScale(this.Scale,1.0f,this.Scale);
            var offsetMatrix = Matrix.CreateTranslation(this.Offset);

            this.TileMatrix = Matrix.Multiply(scaleMatrix, offsetMatrix);//Matrix.CreateTranslation(this.Offset);
            this.InverseTileMatrix = Matrix.Invert(this.TileMatrix);
        }

        private void CreateHeightTexture(GraphicsDevice device)
        {
            int maxlevel = 0;
            int x = this.Width;

            x >>= 1;
            while (x > 0)
            {
                maxlevel++;
                x >>= 1;
            }
            
            //TODO: specify mipmaps and load max mipmap into each level.
            this.HeightTex = new Texture2D(device, this.Width, this.Height, true, SurfaceFormat.Single);
            //this.HeightTex.SetData(this.Data);

            float[] mipleveldata = new float[this.Data.Length];
            this.Data.CopyTo(mipleveldata,0);

            for (int level = 0; level <= maxlevel; level++)
            {
                //this.HeightTex.SetData(this.Data);
                //this.HeightTex.SetData(level,new Rectangle(0,0,1<<level,1<<level),mipleveldata,0,(1<<level)*(1<<level));
                this.HeightTex.SetData(level, new Rectangle(0, 0, this.Width >> level, this.Width >> level), mipleveldata, 0, mipleveldata.Length);

                //if (level == 0)
                //{
                    // first level is 3x3 sampled
                  //  mipleveldata = mipleveldata.GenerateMaximumMipMapLevel3(this.Width >> level, this.Width >> level);
                //}
                //else
                //{
                    mipleveldata = mipleveldata.GenerateMaximumMipMapLevel(this.Width >> level, this.Width >> level);
                //}
            }
        }

        private void CreateShadeTexture(GraphicsDevice device)
        {
            // TODO: generate AO and shadow data - this will have to be done on a whole-terrain scale and supplied to tile.
            var dummydata = new Color[this.Width * this.Height];

            this.ShadeTex = new Texture2D(device, this.Width, this.Height, false, SurfaceFormat.Color);
        }

        /// <summary>
        /// Creates the normal texture for this tile.
        /// </summary>
        /// <param name="device"></param>
        private void CreateNormalTexture(GraphicsDevice device)
        {
            var tex = new Color[this.Width * this.Height];

            float dx = 1.0f / (float)(this.Width - 1);
            float dz = 1.0f / (float)(this.Height - 1);
            float dx2 = 1.0f / (float)(this.Width - 1);
            float dz2 = 1.0f / (float)(this.Height - 1);

            //            for (int y = 0; y < this.Height; y++)
            Parallel.For(0, this.Height, (y) =>
            {
                int i = y * this.Width;
                for (int x = 0; x < this.Width; x++)
                {
                    Vector3 v = new Vector3((float)x / (float)(this.Width - 1), this.Data[i], (float)y / (float)(this.Height - 1));

                    // calculate normal from heightmap
                    int n, s, e, w;
                    int ne, nw, se, sw;

                    n = ((x).ClampInclusive(0, this.Width - 1)) + (((y - 1).ClampInclusive(0, this.Height - 1)) * this.Width);
                    ne = ((x + 1).ClampInclusive(0, this.Width - 1)) + (((y - 1).ClampInclusive(0, this.Height - 1)) * this.Width);
                    e = ((x + 1).ClampInclusive(0, this.Width - 1)) + (((y).ClampInclusive(0, this.Height - 1)) * this.Width);
                    se = ((x + 1).ClampInclusive(0, this.Width - 1)) + (((y + 1).ClampInclusive(0, this.Height - 1)) * this.Width);
                    s = ((x).ClampInclusive(0, this.Width - 1)) + (((y + 1).ClampInclusive(0, this.Height - 1)) * this.Width);
                    sw = ((x - 1).ClampInclusive(0, this.Width - 1)) + (((y + 1).ClampInclusive(0, this.Height - 1)) * this.Width);
                    w = ((x - 1).ClampInclusive(0, this.Width - 1)) + (((y).ClampInclusive(0, this.Height - 1)) * this.Width);
                    nw = ((x - 1).ClampInclusive(0, this.Width - 1)) + (((y - 1).ClampInclusive(0, this.Height - 1)) * this.Width);

                    Vector3 vn, vs, ve, vw;
                    Vector3 vne, vnw, vse, vsw;

                    vn = new Vector3(v.X, this.Data[n], v.Z - dz) - v;
                    vs = new Vector3(v.X, this.Data[s], v.Z + dz) - v;
                    vw = new Vector3(v.X - dx, this.Data[w], v.Z) - v;
                    ve = new Vector3(v.X + dx, this.Data[e], v.Z) - v;

                    vne = new Vector3(v.X + dx2, this.Data[ne], v.Z - dz2) - v;
                    vse = new Vector3(v.X + dx2, this.Data[se], v.Z + dz2) - v;
                    vnw = new Vector3(v.X - dx2, this.Data[nw], v.Z - dz2) - v;
                    vsw = new Vector3(v.X - dx2, this.Data[sw], v.Z + dz2) - v;

                    Vector3 normal = new Vector3(0.0f);

                    normal += Vector3.Cross(vn, vne);
                    normal += Vector3.Cross(vne, ve);
                    normal += Vector3.Cross(ve, vse);
                    normal += Vector3.Cross(vse, vs);
                    normal += Vector3.Cross(vs, vsw);
                    normal += Vector3.Cross(vsw, vw);
                    normal += Vector3.Cross(vw, vnw);
                    normal += Vector3.Cross(vnw, vn);
                    normal.Normalize();

                    tex[i] = normal.ToColor();

                    i++;
                }
            });

            this.NormalTex = new Texture2D(device, this.Width, this.Height, false, SurfaceFormat.Color);
            this.NormalTex.SetData(tex);
        }


        private void SetupTestData()
        {
            var r = new Random();

            double rx = r.NextDouble();
            double ry = r.NextDouble();

            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = y * this.Width;
                    for (int x = 0; x < this.Width; x++)
                    {
                        this.Data[i] = 0f;

                        for (int j = 1; j < 5; j++)
                        {
                            this.Data[i] += SimplexNoise.noise((float)rx + x * 0.003f * (1 << j), (float)ry+y * 0.003f * (1 << j), j * 3.3f) * (1.0f / ((1 << j) + 1));
                        }
                        i++;
                    }
                }
            );
        }


        public void Setup(GraphicsDevice device)
        {
            //this.SetupTestData();
            LoadContent(device);
        }

        public void LoadContent(GraphicsDevice device)
        {
            this.SetupBoundingBox();
            this.CreateHeightTexture(device);
            this.CreateNormalTexture(device);
            this.CreateShadeTexture(device);
        }

        public void SetDataFromCells(Terrain.Cell[] cells, int xofs, int yofs, int stride)
        {
            int si=0, di=0;

            for (int y = 0; y < this.Height; y++)
            {
                si = (yofs + y) * stride + xofs;
                for (int x = 0; x < this.Width; x++)
                {
                    this.Data[di++] = cells[si++].h;
                }
            }
        }


        public bool IsInBox(Vector3 p)
        {
            p -= this.Offset;

            return
                p.X >= BoundingBoxLowerCorner.X && p.X <= BoundingBoxUpperCorner.X &&
                p.Y >= BoundingBoxLowerCorner.Y && p.Y <= BoundingBoxUpperCorner.Y &&
                p.Z >= BoundingBoxLowerCorner.Z && p.Z <= BoundingBoxUpperCorner.Z;
        }


        private void SetupBoundingBox()
        {
            float minx = 0f;
            float maxx = 1f;

            float minz = 0f;
            float maxz = 1f;

            this.MinHeight = this.Data.Min()-0.01f;
            this.MaxHeight = this.Data.Max();

            BoundingBoxLowerCorner = new Vector3(minx, MinHeight, minz);
            BoundingBoxUpperCorner = new Vector3(maxx, MaxHeight, maxz);
            
            for (int i = 0; i < 8; i++)
            {
                this.BoundingBoxRenderVertex[i].Position.X = (i & 0x02) == 0 ? ((i & 0x01) == 0 ? minx : maxx) : ((i & 0x01) == 0 ? maxx : minx);
                this.BoundingBoxRenderVertex[i].Position.Y = ((i & 0x04) == 0 ? MinHeight : MaxHeight);
                this.BoundingBoxRenderVertex[i].Position.Z = (i & 0x02) == 0 ? minz : maxz;

                this.BoundingBoxRenderVertex[i].Color = new Color(this.BoundingBoxRenderVertex[i].Position.X, this.BoundingBoxRenderVertex[i].Position.Y, this.BoundingBoxRenderVertex[i].Position.Z, 255);

                this.BoundingBoxVertex[i].Position = this.BoundingBoxRenderVertex[i].Position;
                
                this.BoundingBoxVertex[i].BoxCoord.X = this.BoundingBoxVertex[i].Position.X;
                this.BoundingBoxVertex[i].BoxCoord.Y = this.BoundingBoxVertex[i].Position.Y;
                this.BoundingBoxVertex[i].BoxCoord.Z = this.BoundingBoxVertex[i].Position.Z;
            }

            int j = 0;

            for (int i = 0; i < 4; i++)
            {
                this.BoundingBoxRenderIndex[j++] = (short)i;
                this.BoundingBoxRenderIndex[j++] = (short)((i + 1) % 4);
                this.BoundingBoxRenderIndex[j++] = (short)(i + 4);
                this.BoundingBoxRenderIndex[j++] = (short)((i + 1) % 4 + 4);
                this.BoundingBoxRenderIndex[j++] = (short)(i);
                this.BoundingBoxRenderIndex[j++] = (short)(i + 4);
            }

            j = 0;

            int[] cubeindex = {
                                  7,3,2,
                                  7,2,6,
                                  6,2,1,
                                  6,1,5,
                                  5,1,0,
                                  5,0,4,
                                  4,3,7,
                                  4,0,3,
                                  3,1,2,
                                  3,0,1,
                                  5,7,6,
                                  5,4,7
                              };

            for (int i = 0; i < 36; i++)
            {
                this.BoundingBoxIndex[i] = (short)(cubeindex[i]);
            }


        }

    }
}





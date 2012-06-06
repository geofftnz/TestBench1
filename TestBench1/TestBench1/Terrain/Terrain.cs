using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using Utils;

namespace TestBench1.Terrain
{
    public class SimpleTerrain
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public float[] Data { get; private set; }
        public VertexPositionNormalTexture[] Vertices { get; private set; }
        public short[] Index { get; private set; }

        private VertexPositionColor[] BoundingBoxVertex = new VertexPositionColor[8];
        private int[] BoundingBoxIndex = new int[24];

        public Texture2D TexCol;


        public SimpleTerrain()
        {
            this.Width = 64;
            this.Height = 64;
            this.Data = new float[this.Width * this.Height];
            this.Vertices = new VertexPositionNormalTexture[this.Width * this.Height];
            this.Index = new short[this.Width * 2 * (this.Height - 1)];   // triangle strips
        }

        public SimpleTerrain(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Data = new float[this.Width * this.Height];
            this.Vertices = new VertexPositionNormalTexture[this.Width * this.Height];
            this.Index = new short[this.Width * 2 * (this.Height - 1)];   // triangle strips
        }

        private void SetupTestData()
        {
            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = y * this.Width;
                    for (int x = 0; x < this.Width; x++)
                    {
                        this.Data[i] = 0f;

                        for (int j = 1; j < 6; j++)
                        {
                            this.Data[i] += SimplexNoise.noise(x * 0.003f * (1 << j), y * 0.003f * (1 << j), j * 3.3f) * (1.0f / ((1 << j) + 1));
                        }
                        i++;
                    }
                  
                });
        }


        private void SetupTexture(GraphicsDevice d)
        {

            // generate texture colours
            var tex = new Color[this.Width * this.Height];

            for (int y = 0; y < this.Height; y++)
            {
                int i = y * this.Width;
                for (int x = 0; x < this.Width; x++)
                {

                    //float a = (this.Data[i]) * 5f + 0.5f;
                    //tex[i] = Color.Lerp(Color.DarkGreen, Color.Beige, a);

                    tex[i] = this.Vertices[i].Normal.ToColor();


                    i++;
                }
            }

            this.TexCol = new Texture2D(d, this.Width, this.Height, false, SurfaceFormat.Color);
            this.TexCol.SetData(tex);
        }

        private void SetupVertices()
        {

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
                    this.Vertices[i].Position = v;

                    //this.Vertices[i].Normal = new Vector3(0f, 1f, 0f);
                    //this.Vertices[i].Color = new Color((float)((float)x / (float)(this.Width - 1)), (float)((float)y / (float)(this.Height - 1)), this.Data[i] * 5f + .5f, 1.0f);

                    this.Vertices[i].TextureCoordinate.X = (float)x / (float)(this.Width - 1);
                    this.Vertices[i].TextureCoordinate.Y = (float)y / (float)(this.Height - 1);

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

                    this.Vertices[i].Normal = normal;

                    i++;
                }
            });
        }

        private void SetupIndices()
        {
            int i = 0;

            for (int y = 0; y < this.Height - 1; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    this.Index[i++] = (short)(x + (y + 1) * this.Width);
                    this.Index[i++] = (short)(x + y * this.Width);

                }
            }
        }

        private void SetupBB()
        {
            // find min/max xyz
            float minx = Vertices.Select(v => v.Position.X).Min();
            float miny = Vertices.Select(v => v.Position.Y).Min();
            float minz = Vertices.Select(v => v.Position.Z).Min();
            float maxx = Vertices.Select(v => v.Position.X).Max();
            float maxy = Vertices.Select(v => v.Position.Y).Max();
            float maxz = Vertices.Select(v => v.Position.Z).Max();

            int i;
            for (i = 0; i < 8; i++)
            {
                this.BoundingBoxVertex[i].Position.X = (i & 0x02) == 0 ? ((i & 0x01) == 0 ? minx : maxx) : ((i & 0x01) == 0 ? maxx : minx);
                this.BoundingBoxVertex[i].Position.Z = (i & 0x02) == 0 ? minz : maxz;
                this.BoundingBoxVertex[i].Position.Y = (i & 0x04) == 0 ? miny : maxy;

                this.BoundingBoxVertex[i].Color = new Color(255, 255, 255, 255);
            }

            int j = 0;

            for (i = 0; i < 4; i++)
            {
                this.BoundingBoxIndex[j++] = i;
                this.BoundingBoxIndex[j++] = (i + 1) % 4;
                this.BoundingBoxIndex[j++] = i + 4;
                this.BoundingBoxIndex[j++] = (i + 1) % 4 + 4;
                this.BoundingBoxIndex[j++] = i;
                this.BoundingBoxIndex[j++] = i + 4;
            }
        }


        public void Setup()
        {
            this.SetupTestData();
            this.SetupVertices();
            this.SetupIndices();
            this.SetupBB();

        }

        public void LoadContent(GraphicsDevice d)
        {
            this.SetupTexture(d);
        }

        public void Render(GraphicsDevice d)
        {
            for (int y = 0; y < this.Height - 1; y++)
            {
                d.DrawUserIndexedPrimitives(PrimitiveType.TriangleStrip, this.Vertices, 0, this.Width * this.Height, this.Index, y * 2 * this.Width, (this.Width - 1) * 2);
            }

            //d.DrawUserIndexedPrimitives(PrimitiveType.LineList, this.BoundingBoxVertex, 0, 8, this.BoundingBoxIndex, 0, 12);
        }

    }
}

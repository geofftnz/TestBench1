using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TerrainEngine
{
    public class Terrain
    {

        public struct Cell
        {
            public float h;

        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Cell[] Data { get; private set; }

        public List<TerrainTile> Tiles { get; private set; }

        private int TileSize = 256;
        private int NXTiles;
        private int NYTiles;
        //private float scaleFactor = 1.0f / 256f;

        public float HeightAt(float x, float y)
        {
            int xx = (int)(x * this.TileSize);
            int yy = (int)(y * this.TileSize);

            float xfrac = (x * (float)TileSize) - (float)xx;
            float yfrac = (y * (float)TileSize) - (float)yy;

            if (xx < 0) return 0;
            if (yy < 0) return 0;
            if (xx >= this.Width-1) { return 0; }
            if (yy >= this.Height-1) { return 0; }

            float h00 = this.Data[xx + yy * this.Width].h;
            float h10 = this.Data[xx + 1 + yy * this.Width].h;
            float h01 = this.Data[xx + (yy+1) * this.Width].h;
            float h11 = this.Data[xx + 1 + (yy+1) * this.Width].h;

            return MathHelper.Lerp(MathHelper.Lerp(h00, h10, xfrac), MathHelper.Lerp(h01, h11, xfrac), yfrac);
        }

        public Vector3 ClampToGround(Vector3 pos)
        {
            pos.Y = this.HeightAt(pos.X, pos.Z);
            return pos;               
        }


        public Terrain(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            this.NXTiles = Width / TileSize;
            this.NYTiles = Height / TileSize;

            this.Data = new Cell[Width * Height];
            this.Tiles = new List<TerrainTile>();
        }

        public void LoadContent(GraphicsDevice device)
        {
            foreach (var tile in this.Tiles)
            {
                tile.Setup(device);
            }
        }


        #region Terrain operations
        public void Clear(float height)
        {
            for (int i = 0; i < Width * Height; i++)
            {
                Data[i].h = height;
            }
        }

        public void AddSimplexNoise(int octaves, float scale, float amplitude)
        {
            var r = new Random(1);

            double rx = r.NextDouble();
            double ry = r.NextDouble();

            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = y * this.Width;
                    for (int x = 0; x < this.Width; x++)
                    {
                        for (int j = 1; j < octaves; j++)
                        {
                            this.Data[i].h += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));
                        }
                        i++;
                    }
                }
            );
        }

        public void AddRamp()
        {
            Parallel.For(0, this.Height,
                y=>{
                    int i = y * this.Width;
                    for (int x = 0; x < this.Width; x++)
                    {
                        this.Data[i++].h = (float)x * 0.0005f + (float)y * 0.0005f;// +(float)Math.Sin((float)(x + y) * 0.1) * 0.005f + (float)Math.Sin((float)(y - x) * 0.1) * 0.005f;// +y * 0.0005f;
                    }
                }
            );
        }

        #endregion


        public void SetupTestTerrain()
        {
            this.Clear(0.0f);
            this.AddSimplexNoise(4, 0.0002f, 5.0f);
            this.AddSimplexNoise(4, 0.005f, 0.2f);
            //this.AddRamp();
        }

        // TODO: turn terrain into tiles
        public void LoadTiles()
        {
            // discard old tiles
            // TODO: do we need to unload resources here?
            this.Tiles.Clear();

            for (int y = 0; y < NYTiles; y++)
            {
                for (int x = 0; x < NXTiles; x++)
                {
                    //var tile = new TerrainTile(TileSize, TileSize, new Vector3((float)(x * TileSize), 0.0f, (float)(y * TileSize)),(float)TileSize);
                    var tile = new TerrainTile(TileSize, TileSize, new Vector3((float)(x), 0.0f, (float)(y)), 1.0f);
                    tile.SetDataFromCells(this.Data, x * TileSize, y * TileSize, this.Width);
                    this.Tiles.Add(tile);
                }
            }
        }


        public void Draw(GameTime gameTime, GraphicsDevice device, Effect effect, Vector3 eyePos, Matrix viewMatrix, Matrix worldMatrix, Matrix projectionMatrix, Vector3 lightDirection)
        {
            device.DepthStencilState = DepthStencilState.Default;

            foreach (var tile in Tiles)
            {
                if (!tile.IsInBox(eyePos))
                {
                    tile.Draw(gameTime, device, effect, eyePos, viewMatrix, worldMatrix, projectionMatrix, lightDirection);
                }
            }
        }


        public void DrawBox(GameTime gameTime, GraphicsDevice device, Effect effect, Vector3 eyePos, Matrix viewMatrix, Matrix worldMatrix, Matrix projectionMatrix, Vector3 lightDirection)
        {
            device.DepthStencilState = DepthStencilState.Default;

            foreach (var tile in Tiles)
            {
                tile.DrawBox(gameTime, device, effect, eyePos, viewMatrix, worldMatrix, projectionMatrix, lightDirection);
            }
        }


    }
}

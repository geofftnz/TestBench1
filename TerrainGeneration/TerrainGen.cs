using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Utils;
using Microsoft.Xna.Framework;
using System.Threading;

namespace TerrainGeneration
{
    public class TerrainGen
    {
        const int NUMTHREADS = 3;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Cell
        {
            /// <summary>
            /// Hard rock - eroded by wind and water
            /// </summary>
            public float Hard;
            /// <summary>
            /// Loose material/soil/dust - moved by wind and water
            /// </summary>
            public float Loose;
            /// <summary>
            /// Average saturation of tile - used for river mapping
            /// </summary>
            public float Water;
            /// <summary>
            /// Aux padding out to 16 bytes.
            /// </summary>
            public float Aux;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Cell[] Map { get; private set; }
        private float[] TempDiffMap;

        public TerrainGen(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Map = new Cell[this.Width * this.Height];
            this.TempDiffMap = new float[this.Width * this.Height];
        }

        /// <summary>
        /// Gets the cell index of x,y
        /// 
        /// Deals with wraparound
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int C(int x, int y)
        {
            return x.Wrap(this.Width) + y.Wrap(this.Height) * this.Width;
        }

        private void ClearTempDiffMap()
        {
            Array.Clear(this.TempDiffMap, 0, this.Width * this.Height);
        }

        public void InitTerrain1()
        {
            this.Clear(0.0f);
            this.AddSimplexNoise(6, 0.1f / (float)this.Width, 2000.0f);
            //this.AddSimplexNoise(4, 0.1f / (float)this.Width, 4000.0f, x => Math.Abs(x));
            this.AddSimplexPowNoise(7, 0.333f / (float)this.Width, 8000.0f, 3.0f, x => Math.Abs(x));
            this.AddSimplexPowNoise(4, 1.7f / (float)this.Width, 500.0f, 4.0f, x => Math.Abs(x));
            this.AddSimplexNoise(7, 3.0f / (float)this.Width, 100.0f);

            this.AddLooseMaterial(5.0f);

            //this.AddSimplexNoise(2, 0.1f, 1.0f);

            //this.AddDiscontinuousNoise(3,0.003f, 200.0f, 0.1f);

            //this.AddSimplexNoise(4, 0.002f, 5.0f);
            //this.AddSimplexNoise(4, 0.01f, 0.2f);
        }

        public void ModifyTerrain()
        {
            this.Slump(1.0f, 0.05f, 100000);
        }
        public void Clear(float height)
        {
            for (int i = 0; i < Width * Height; i++)
            {
                this.Map[i] = new Cell();
            }
        }

        public void AddLooseMaterial(float amount)
        {
            for (int i = 0; i < Width * Height; i++)
            {
                this.Map[i].Loose += amount;
            }
        }

        #region noise
        public void AddSimplexNoise(int octaves, float scale, float amplitude)
        {
            var r = new Random();

            float rx = (float)r.NextDouble();
            float ry = (float)r.NextDouble();


            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = (int)y * this.Width;
                    float s = (float)y / (float)this.Height;
                    for (int x = 0; x < this.Width; x++)
                    {
                        float h = 0.0f;
                        float t = (float)x / (float)this.Width;
                        for (int j = 1; j <= octaves; j++)
                        {
                            //h += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));
                            h += SimplexNoise.wrapnoise(s, t, (float)this.Width, (float)this.Height, rx, ry, (float)(scale * (1 << j))) * (float)(1.0 / ((1 << j) + 1));
                        }
                        this.Map[i].Hard += h * amplitude;
                        i++;
                    }
                }
            );
        }

        public void AddSimplexNoise(int octaves, float scale, float amplitude, Func<float,float> transform)
        {
            var r = new Random();

            float rx = (float)r.NextDouble();
            float ry = (float)r.NextDouble();


            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = (int)y * this.Width;
                    float s = (float)y / (float)this.Height;
                    for (int x = 0; x < this.Width; x++)
                    {
                        float h = 0.0f;
                        float t = (float)x / (float)this.Width;
                        for (int j = 1; j <= octaves; j++)
                        {
                            //h += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));
                            h += transform(SimplexNoise.wrapnoise(s, t, (float)this.Width, (float)this.Height, rx, ry, (float)(scale * (1 << j))) * (float)(1.0 / ((1 << j) + 1)));
                        }
                        this.Map[i].Hard += h * amplitude;
                        i++;
                    }
                }
            );
        }

        public void AddSimplexPowNoise(int octaves, float scale, float amplitude, float power, Func<float,float> postTransform)
        {
            var r = new Random();

            float rx = (float)r.NextDouble();
            float ry = (float)r.NextDouble();


            Parallel.For(0, this.Height,
                (y) =>
                {
                    int i = (int)y * this.Width;
                    float s = (float)y / (float)this.Height;
                    for (int x = 0; x < this.Width; x++)
                    {
                        float h = 0.0f;
                        float t = (float)x / (float)this.Width;
                        for (int j = 1; j <= octaves; j++)
                        {
                            //h += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));
                            h += SimplexNoise.wrapnoise(s, t, (float)this.Width, (float)this.Height, rx, ry, (float)(scale * (1 << j))) * (float)(1.0 / ((1 << j) + 1));
                        }
                        this.Map[i].Hard += postTransform((float)Math.Pow(h, power) * amplitude);
                        i++;
                    }
                }
            );
        }
        public void AddDiscontinuousNoise(int octaves, float scale, float amplitude, float threshold)
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
                        //this.Map[i].Hard += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));

                        float a = 0.0f;
                        for (int j = 1; j < octaves; j++)
                        {
                            a += SimplexNoise.noise((float)rx + x * scale * (1 << j), (float)ry + y * scale * (1 << j), j * 3.3f) * (amplitude / ((1 << j) + 1));
                        }

                        if (a > threshold)
                        {
                            this.Map[i].Hard += amplitude;
                        }

                        i++;
                    }
                }
            );
        }
        #endregion


        /// <summary>
        /// Slumps the terrain by looking at difference between cell and adjacent cells
        /// </summary>
        /// <param name="threshold">height difference that will trigger a redistribution of material</param>
        /// <param name="amount">amount of material to move (proportional to difference)</param>
        /// 
        public void Slump(float threshold, float amount, int numIterations)
        {
            float amount2 = amount * 0.707f;
            this.ClearTempDiffMap();

            Func<int, int, float, float, float[], float> SlumpF = (pFrom, pTo, h, a, diffmap) =>
            {
                float loose = this.Map[pFrom].Loose; // can only slump loose material.
                if (loose > 0.0f)
                {
                    float diff = (this.Map[pFrom].Hard + loose) - h;
                    if (diff > threshold)
                    {
                        diff -= threshold;
                        if (diff > loose)
                        {
                            diff = loose;
                        }
                        diffmap[pFrom] -= diff * a;
                        diffmap[pTo] += diff * a;
                        return diff * a;
                    }
                }
                return 0f;
            };

            Action<int, int, float[]> SlumpTo = (x, y, diffmap) =>
            {
                int p = C(x, y);
                int n = C(x, y - 1);
                int s = C(x, y + 1);
                int w = C(x - 1, y);
                int e = C(x + 1, y);

                int nw = C(x - 1, y - 1);
                int ne = C(x + 1, y - 1);
                int sw = C(x - 1, y + 1);
                int se = C(x + 1, y + 1);

                float h = this.Map[p].Hard + this.Map[p].Loose; 

                h += SlumpF(n, p, h, amount, diffmap);
                h += SlumpF(s, p, h, amount, diffmap);
                h += SlumpF(w, p, h, amount, diffmap);
                h += SlumpF(e, p, h, amount, diffmap);

                h += SlumpF(nw, p, h, amount2, diffmap);
                h += SlumpF(ne, p, h, amount2, diffmap);
                h += SlumpF(sw, p, h, amount2, diffmap);
                h += SlumpF(se, p, h, amount2, diffmap);
            };

            //var threadlocal = new { diffmap = new float[this.Width * this.Height], r = new Random() };
            var threadlocal = new { diffmap = this.TempDiffMap, r = new Random() };
            for (int i = 0; i < numIterations; i++)
            {
                int x = threadlocal.r.Next(this.Width);
                int y = threadlocal.r.Next(this.Height);

                SlumpTo(x, y, threadlocal.diffmap);
            }
            for (int i = 0; i < this.Width * this.Height; i++)
            {
                this.Map[i].Loose += threadlocal.diffmap[i];
            }


            /*
            Parallel.For(0, numIterations, 
                () => new {diffmap = new float[this.Width * this.Height], r = new Random()},
                (j, loop, threadlocal) =>
                {
                    //for (int i = 0; i < numIterations; i++)
                    //{
                        int x = threadlocal.r.Next(this.Width);
                        int y = threadlocal.r.Next(this.Height);

                        SlumpTo(x, y, threadlocal.diffmap);
                    //}

                    return threadlocal;
                },
                (threadlocal) =>
                {
                    lock (this)
                    {
                        for (int i = 0; i < this.Width * this.Height; i++)
                        {
                            this.Map[i].Hard += threadlocal.diffmap[i];
                        }
                    }
                }
             );*/

        }

    }
}

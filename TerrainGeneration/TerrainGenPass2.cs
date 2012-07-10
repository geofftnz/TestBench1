using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Utils;
using Microsoft.Xna.Framework;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace TerrainGeneration
{
    public class TerrainGenPass2 : ITerrainGen
    {
        const int FILEMAGIC = 0x54455231;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Cell
        {
            /// <summary>
            /// Underlying rock
            /// 
            /// Wont be affected by snow mechanics.
            /// </summary>
            public float Rock;

            /// <summary>
            /// Ice, produced slowly by compacting snow of sufficient depth.
            /// Does not slump.
            /// </summary>
            public float Ice;

            /// <summary>
            /// Packed snow. Eroded by wind and transformed into powder.
            /// Formed by compaction of powder over time.
            /// Slowly slumps if max slope exceeded.
            /// </summary>
            public float Snow;

            /// <summary>
            /// Powder. Readily transported by wind. Deposited on lee slopes.
            /// Readily slopes if max slope exceeded.
            /// </summary>
            public float Powder;

            public float Height
            {
                get
                {
                    return Rock + Ice + Snow + Powder;
                }
            }
        }

        public struct Parameters
        {
            public int WindNumParticles;
            //public float MinSnowFallAmount;
            //public float MaxSnowFallAmount;
            //public float MinWindSpeed;
            //public float MaxWindSpeed;

            /// <summary>
            /// Proportion of snowfall that is affected by the wind direction and the surface normal.
            /// The remainder gets added regardless.
            /// </summary>
            public float SnowFallWindDirectionComponent;

            public float InitialSnowFallRate;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Cell[] Map { get; private set; }
        private float[] TempDiffMap;
        private float[] TempDiffMap2;
        private Vector3[] MapNormals;
        private long Iterations = 0;

        private List<WindErosionParticle> WindParticles = new List<WindErosionParticle>();

        public Parameters parameters;

        private float CurrentWindAngle = 1.7f;
        private float CurrentWindSpeed = 0.2f;
        private Vector3 CurrentWindVector = Vector3.Normalize(new Vector3(1f, 1f, -0.5f));
        private float _CurrentSnowFallRate = 0f;
        public float CurrentSnowFallRate
        {
            get
            {
                return this._CurrentSnowFallRate;
            }
            set
            {
                if (value < 0f)
                {
                    this._CurrentSnowFallRate = 0f;
                    return;
                }
                this._CurrentSnowFallRate = value;
            }
        }



        private Random rand = new Random();

        public TerrainGenPass2(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Map = new Cell[this.Width * this.Height];
            this.MapNormals = new Vector3[this.Width * this.Height];
            this.TempDiffMap = new float[this.Width * this.Height];
            this.TempDiffMap2 = new float[this.Width * this.Height];

            // init parameters
            this.parameters.WindNumParticles = 4000;

            this.parameters.SnowFallWindDirectionComponent = 0.8f;
            this.parameters.InitialSnowFallRate = 0.0f;
            this.CurrentSnowFallRate = this.parameters.InitialSnowFallRate;

            //this.parameters.MinSnowFallAmount = 0f;
            //this.parameters.MaxSnowFallAmount = 0.1f;
            //this.parameters.MinWindSpeed = 0f;
            //this.parameters.MaxWindSpeed = 1.5f;


            for (int i = 0; i < this.parameters.WindNumParticles; i++)
            {
                this.WindParticles.Add(new WindErosionParticle(rand.Next(this.Width), rand.Next(this.Height)));
            }
        }

        private Func<int, int, int> C = TileMath.C1024;
        private Func<int, int> CX = TileMath.CX1024;
        private Func<int, int> CY = TileMath.CY1024;

        private void ClearTempDiffMap()
        {
            Array.Clear(this.TempDiffMap, 0, this.Width * this.Height);
        }

        public void InitFromPass1(TerrainGen pass1)
        {
            if (pass1 == null)
            {
                throw new ArgumentNullException("pass1");
            }
            if (pass1.Width != this.Width || pass1.Height != this.Height)
            {
                throw new InvalidOperationException("Pass1 terrain is a different size.");
            }

            for (int i = 0; i < this.Width * this.Height; i++)
            {
                this.Map[i].Rock = pass1.Map[i].Height;
                this.Map[i].Ice = 0f;
                this.Map[i].Snow = 0f;
                this.Map[i].Powder = 0f;
            }
            this.CalculateNormals();

            // 3cm should be visible
            //this.AddPowder(0.05f, new Vector3(0f,0f,-1f));
        }

        public void ModifyTerrain()
        {
            // don't need to recalculate normals that often.
            if (this.Iterations % 100 == 0)
            {
                this.CalculateNormals();
            }

            // calculate current wind vector
            this.CurrentWindVector.X = (float)(Math.Cos(this.CurrentWindAngle) * this.CurrentWindSpeed);
            this.CurrentWindVector.Y = (float)(-Math.Sin(this.CurrentWindAngle) * this.CurrentWindSpeed);
            this.CurrentWindVector.Z = -1f;
            this.CurrentWindVector.Normalize();

            
            this.AddPowder(this.CurrentSnowFallRate, this.CurrentWindVector);

            switch (this.Iterations % 2)
            {
                case 0:
                    this.SlumpPowder(0.78f, 0.025f, 0.05f);// repose angle means 0.78 slope, 2.5cm min depth, slump 5%
                    break;
                case 1:
                    this.CompactPowder(1.0f, 0.01f, 0.5f);
                    break;
            }


            //this.AddPowder(50000,0.2f);
            //this.SlumpPowder(25000, 0.78f, 0.01f, 0.1f);
            //this.CompactPowder(25000, 0.5f, 0.05f, 0.5f);


            Iterations++;
        }






        public void CalculateNormals()
        {
            ParallelHelper.For2DParallel(this.Width, this.Height, (cx, cy, i) =>
            {
                float h1 = this.Map[C(cx, cy - 1)].Height;
                float h2 = this.Map[C(cx, cy + 1)].Height;
                float h3 = this.Map[C(cx - 1, cy)].Height;
                float h4 = this.Map[C(cx + 1, cy)].Height;

                this.MapNormals[i].X = h3 - h4;
                this.MapNormals[i].Y = h1 - h2;
                this.MapNormals[i].Z = 2f;
                this.MapNormals[i].Normalize();
            });
        }

        public void AddPowderRandom(int numSamples, float amount)
        {
            for (int i = 0; i < numSamples; i++)
            {
                int index = this.rand.Next(this.Width * this.Height);
                this.Map[index].Powder += amount;
            }
        }

        public void AddPowder(float amount, Vector3 direction)
        {
            float a1 = this.parameters.SnowFallWindDirectionComponent;
            float a0 = (1f - a1);

            a0 *= amount;
            a1 *= amount;

            direction = -direction;
            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                this.Map[i].Powder += a0 + a1 * Vector3.Dot(direction, this.MapNormals[i]).ClampInclusive(0f, 1f);
            });

        }

        private void SlumpPowder(float slopeThreshold, float depthThreshold, float amount)
        {
            this.ClearDiffMap2();

            this.SlumpPowderOneDirection(slopeThreshold, depthThreshold, amount, -1, 0);
            this.SlumpPowderOneDirection(slopeThreshold, depthThreshold, amount, 1, 0);
            this.SlumpPowderOneDirection(slopeThreshold, depthThreshold, amount, 0, -1);
            this.SlumpPowderOneDirection(slopeThreshold, depthThreshold, amount, 0, 1);

            slopeThreshold *= 1.414f;
            this.SlumpPowderOneDirection(slopeThreshold, depthThreshold, amount, -1, -1);
            this.SlumpPowderOneDirection(slopeThreshold, depthThreshold, amount, 1, -1);
            this.SlumpPowderOneDirection(slopeThreshold, depthThreshold, amount, -1, 1);
            this.SlumpPowderOneDirection(slopeThreshold, depthThreshold, amount, 1, 1);

            this.AddDiffMapToPowder(this.TempDiffMap2);

        }

        /// <summary>
        /// Calculates how much material would slump down to the current location from the point at the given offset.
        /// This is written into the first temporary buffer.
        /// </summary>
        /// <param name="slopeThreshold"></param>
        /// <param name="depthThreshold"></param>
        /// <param name="amount"></param>
        private void SlumpPowderOneDirection(float slopeThreshold, float depthThreshold, float amount, int xofs, int yofs)
        {
            ParallelHelper.For2DParallelOffset(this.Width, this.Height, xofs, yofs, (pTo, pFrom) =>
            {
                //int pFrom = C(x + xofs, y + yofs);  // cell we're taking material from
                float h = this.Map[pTo].Height;
                this.TempDiffMap[pTo] = 0f;

                float powder = this.Map[pFrom].Powder - depthThreshold; // can only slump powder over our depth threshold
                if (powder > 0f)
                {
                    float diff = (((this.Map[pFrom].Height) - h) - slopeThreshold);
                    if (diff > 0f)
                    {
                        if (diff > powder)
                        {
                            diff = powder;
                        }

                        this.TempDiffMap[pTo] = diff * amount;
                    }
                }
            });

            this.CombineDiffMapsForDirection(xofs, yofs);
        }

        private void CombineDiffMapsForDirection(int xofs, int yofs)
        {
            ParallelHelper.For2D(this.Width, this.Height, (x, y, pTo) =>
            {
                this.TempDiffMap2[C(x + xofs, y + yofs)] -= this.TempDiffMap[pTo];
                this.TempDiffMap2[pTo] += this.TempDiffMap[pTo];
            });
        }

        private void AddDiffMapToPowder(float[] diffmap)
        {
            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                this.Map[i].Powder += diffmap[i];
            });
        }

        private void ClearDiffMap1()
        {
            Array.Clear(this.TempDiffMap, 0, this.Width * this.Height);
        }
        private void ClearDiffMap2()
        {
            Array.Clear(this.TempDiffMap2, 0, this.Width * this.Height);
        }


        public void CompactPowder(float minDepth, float amount, float invDensityRatio)
        {

            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                float powder = this.Map[i].Powder;
                if (powder > minDepth)
                {
                    powder = (powder - minDepth) * amount;
                    this.Map[i].Powder -= powder;
                    this.Map[i].Snow += powder * invDensityRatio;
                }
            });
        }


        public void SlumpPowderRandom(int numIterations, float slopeThreshold, float depthThreshold, float amount)
        {
            float _threshold2 = (float)(slopeThreshold * Math.Sqrt(2.0));
            this.ClearTempDiffMap();

            Func<int, int, float, float, float, float[], float> SlumpF = (pFrom, pTo, h, a, threshold, diffmap) =>
            {
                float powder = this.Map[pFrom].Powder; // can only slump powder.
                if (powder > depthThreshold)
                {
                    float diff = (this.Map[pFrom].Height) - h;
                    if (diff > threshold)
                    {
                        diff -= threshold;
                        if (diff > powder)
                        {
                            diff = powder;
                        }

                        diff *= a;

                        diffmap[pFrom] -= diff;
                        diffmap[pTo] += diff;

                        return diff;
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

                float h = this.Map[p].Height;
                float a = amount;

                float th = slopeThreshold;
                float th2 = th * 1.414f;

                h += SlumpF(n, p, h, a, th, diffmap);
                h += SlumpF(s, p, h, a, th, diffmap);
                h += SlumpF(w, p, h, a, th, diffmap);
                h += SlumpF(e, p, h, a, th, diffmap);

                h += SlumpF(nw, p, h, a, th2, diffmap);
                h += SlumpF(ne, p, h, a, th2, diffmap);
                h += SlumpF(sw, p, h, a, th2, diffmap);
                h += SlumpF(se, p, h, a, th2, diffmap);
            };

            var threadlocal = new { diffmap = this.TempDiffMap, r = new Random() };
            for (int i = 0; i < numIterations; i++)
            {
                int x = threadlocal.r.Next(this.Width);
                int y = threadlocal.r.Next(this.Height);

                SlumpTo(x, y, threadlocal.diffmap);
            }

            Parallel.For(0, 8, j =>
            {
                int ii = j * ((this.Width * this.Height) >> 3);
                for (int i = 0; i < (this.Width * this.Height) >> 3; i++)
                {
                    this.Map[ii].Powder += threadlocal.diffmap[ii];
                    ii++;
                }
            });


        }
        public void CompactPowderRandom(int numSamples, float minDepth, float amount, float invDensityRatio)
        {
            for (int i = 0; i < numSamples; i++)
            {
                int index = this.rand.Next(this.Width * this.Height);

                float powder = this.Map[index].Powder;
                float diff = powder - minDepth;
                if (diff > 0f)
                {
                    diff *= amount;
                    this.Map[index].Powder -= diff;
                    this.Map[index].Snow += diff * invDensityRatio;
                }

            }
        }



        #region File IO

        public void Save(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 256 * 1024))
            {
                using (var sw = new BinaryWriter(fs))
                {
                    sw.Write(FILEMAGIC);
                    sw.Write(this.Width);
                    sw.Write(this.Height);

                    for (int i = 0; i < this.Width * this.Height; i++)
                    {
                        sw.Write(this.Map[i].Rock);
                        sw.Write(this.Map[i].Ice);
                        sw.Write(this.Map[i].Snow);
                        sw.Write(this.Map[i].Powder);
                    }

                    sw.Close();
                }
                fs.Close();
            }
        }

        public void Load(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None, 256 * 1024))
            {
                using (var sr = new BinaryReader(fs))
                {
                    int magic = sr.ReadInt32();
                    if (magic != FILEMAGIC)
                    {
                        throw new Exception("Not a terrain file");
                    }

                    int w, h;

                    w = sr.ReadInt32();
                    h = sr.ReadInt32();

                    if (w != this.Width || h != this.Height)
                    {
                        // TODO: handle size changes
                        throw new Exception(string.Format("Terrain size {0}x{1} did not match generator size {2}x{3}", w, h, this.Width, this.Height));
                    }



                    for (int i = 0; i < this.Width * this.Height; i++)
                    {
                        this.Map[i].Rock = sr.ReadSingle();
                        this.Map[i].Ice = sr.ReadSingle();
                        this.Map[i].Snow = sr.ReadSingle();
                        this.Map[i].Powder = sr.ReadSingle();
                    }

                    sr.Close();
                }
                fs.Close();
            }
        }

        #endregion

    }
}




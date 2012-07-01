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

        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Cell[] Map { get; private set; }
        private float[] TempDiffMap;

        private List<ErosionParticle2> WindParticles = new List<ErosionParticle2>();

        public Parameters parameters;
        private Random rand = new Random();

        public TerrainGenPass2(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Map = new Cell[this.Width * this.Height];
            this.TempDiffMap = new float[this.Width * this.Height];

            // init parameters
            this.parameters.WindNumParticles = 4000;


            for (int i = 0; i < this.parameters.WindNumParticles; i++)
            {
                this.WindParticles.Add(new ErosionParticle2(rand.Next(this.Width), rand.Next(this.Height)));
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
        }

        public void ModifyTerrain()
        {
            this.AddPowder(50000,0.1f);
            this.SlumpPowder(25000, 0.4f, 0.05f, 0.1f);
            this.CompactPowder(25000, 0.5f, 0.05f, 0.5f);
        }



        public void AddPowder(int numSamples, float amount)
        {
            for (int i = 0; i < numSamples; i++)
            {
                int index = this.rand.Next(this.Width * this.Height);
                this.Map[index].Powder += amount;
            }
        }

        public void SlumpPowder(int numIterations,float slopeThreshold, float depthThreshold, float amount)
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

        public void CompactPowder(int numSamples, float minDepth, float amount, float invDensityRatio)
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




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerrainGeneration;
using System.IO;
using System.Diagnostics;
using U = Utils;
using Utils;

namespace PerfBenchmark
{

    public class ParallelForBenchmark
    {
        public int Width { get; set; }
        public int Height { get; set; }

        TerrainGenPass2.Cell[] Map;
        TerrainGenPass2.Cell[] Map0;
        float[] TempDiffMap;
        float[] TempDiffMap2;
        float[] TempDiffMap3;


        public ParallelForBenchmark(int w, int h)
        {
            this.Width = w;
            this.Height = h;
            this.Map = new TerrainGenPass2.Cell[this.Width * this.Height];
            this.Map0 = new TerrainGenPass2.Cell[this.Width * this.Height];
            this.TempDiffMap = new float[this.Width * this.Height];
            this.TempDiffMap2 = new float[this.Width * this.Height];
            this.TempDiffMap3 = new float[this.Width * this.Height];

            var rand = new Random();

            for (int i = 0; i < this.Width * this.Height; i++)
            {
                this.Map0[i].Powder = (float)rand.NextDouble();
                this.Map0[i].Snow = (float)rand.NextDouble();
                this.Map0[i].Ice = (float)rand.NextDouble();
                this.Map0[i].Rock = (float)rand.NextDouble();
            }

        }

        private void InitData()
        {
            Array.Copy(this.Map0, this.Map, this.Width * this.Height);
        }

        public void Verify()
        {

        }

        public void Run(int runs)
        {
            var m = this.Map;
            float minDepth = 0.8f;
            float amount = 0.05f;
            float invDensityRatio = 0.5f;
            float depthThreshold = 0.1f;
            float slopeThreshold = 0.02f;
            Func<int, int, int> C = TileMath.C1024;
            int xofs = -1, yofs = -1;


            Compare("Branching", runs, (x, y, pTo) =>
            {
                int pFrom = C(x + xofs, y + yofs);  // cell we're taking material from
                float h = this.Map[pTo].Height;
                float transferAmount = 0f;

                float powder = this.Map[pFrom].Powder; // can only slump powder.
                if (powder > depthThreshold)
                {
                    float diff = (((this.Map[pFrom].Height) - h) - slopeThreshold);
                    if (diff > 0f)
                    {
                        if (diff > powder)
                        {
                            diff = powder;
                        }

                        transferAmount = diff * amount;
                    }
                }

                this.TempDiffMap[pTo] = transferAmount;
            });

            Compare("Branching2", runs, (x, y, pTo) =>
            {
                int pFrom = C(x + xofs, y + yofs);  // cell we're taking material from
                this.TempDiffMap[pTo] = 0f;

                float powder = this.Map[pFrom].Powder; // can only slump powder.
                if (powder > depthThreshold)
                {
                    float diff = (((this.Map[pFrom].Height) - this.Map[pTo].Height) - slopeThreshold);
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


            Console.WriteLine(string.Format("Parallel with offset: {0}ms", U.Utils.AverageTime(() =>
            {
                InitData(); return U.Utils.TimeFor(() => ParallelHelper.For2DParallelOffset(Width, Height, 1, 1, (pTo, pFrom) =>
                {
                    this.TempDiffMap[pTo] = 0f;

                    float powder = this.Map[pFrom].Powder; // can only slump powder.
                    if (powder > depthThreshold)
                    {
                        float diff = (((this.Map[pFrom].Height) - this.Map[pTo].Height) - slopeThreshold);
                        if (diff > 0f)
                        {
                            if (diff > powder)
                            {
                                diff = powder;
                            }

                            this.TempDiffMap[pTo] = diff * amount;
                        }
                    }
                }));
            }, runs)));


            /*
            Compare("Branching", runs, (x, y, i) =>
            {
                float powder = this.Map[i].Powder;
                float diff = powder - minDepth;
                if (diff > 0f)
                {
                    diff *= amount;
                    this.Map[i].Powder -= diff;
                    this.Map[i].Snow += diff * invDensityRatio;
                }
            });

            Compare("Branching2", runs, (x, y, i) =>
            {
                float powder = this.Map[i].Powder;
                if (powder > minDepth)
                {
                    powder = (powder - minDepth) * amount;
                    this.Map[i].Powder -= powder;
                    this.Map[i].Snow += powder * invDensityRatio;
                }
            });

            Compare("Branching3", runs, (x, y, i) =>
            {
                float powder = this.Map[i].Powder;
                float diff = powder - minDepth;
                diff = diff < 0f ? 0f : diff;
                
                diff *= amount;
                this.Map[i].Powder -= diff;
                this.Map[i].Snow += diff * invDensityRatio;
            });*/


            /*
            Compare("Single float set",runs,(x, y, i) =>
            {
                m[i].Powder = 1f;
            });

            Compare("4 float set", runs, (x, y, i) =>
            {
                m[i].Powder = 1f;
                m[i].Snow = 2f;
                m[i].Ice = 3f;
                m[i].Rock = 4f;
            });

            Compare("3 float add", runs, (x, y, i) =>
            {
                m[i].Powder = 
                m[i].Snow +
                m[i].Ice +
                m[i].Rock;
            });

            Compare("3 float mul", runs, (x, y, i) =>
            {
                m[i].Powder =
                m[i].Snow *
                m[i].Ice *
                m[i].Rock;
            });

            float d = 0.01f;
            Compare("3 float mul const", runs, (x, y, i) =>
            {
                m[i].Powder *= d;
                m[i].Snow *= d;
                m[i].Ice *= d;
                m[i].Rock *= d;
            });
             */

            Console.WriteLine("Done");
        }

        public void Compare(string name, int runs, Action<int, int, int> a)
        {
            Console.WriteLine(name);
            Console.WriteLine(string.Format("Single thread: {0}ms", U.Utils.AverageTime(() => { InitData(); return U.Utils.TimeFor(() => ParallelHelper.For2DSingle(Width, Height, a)); }, runs)));
            Console.WriteLine(string.Format("Parallel thread: {0}ms", U.Utils.AverageTime(() => { InitData(); return U.Utils.TimeFor(() => ParallelHelper.For2DParallel(Width, Height, a)); }, runs)));
            Console.WriteLine(string.Format("Parallel thread, unrolled: {0}ms", U.Utils.AverageTime(() => { InitData(); return U.Utils.TimeFor(() => ParallelHelper.For2DParallelUnrolled(Width, Height, a)); }, runs)));
            Console.WriteLine(string.Format("Batched thread: {0}ms", U.Utils.AverageTime(() => { InitData(); return U.Utils.TimeFor(() => ParallelHelper.For2DParallelBatched(Width, Height, a)); }, runs)));
        }

    }
}

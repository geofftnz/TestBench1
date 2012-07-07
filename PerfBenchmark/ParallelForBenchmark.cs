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


        public ParallelForBenchmark(int w, int h)
        {
            this.Width = w;
            this.Height = h;
            this.Map = new TerrainGenPass2.Cell[this.Width * this.Height];

        }

        public void Verify()
        {

        }

        public void Run(int runs)
        {
            var m = this.Map;

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

            Console.WriteLine("Done");
        }

        public void Compare(string name, int runs, Action<int, int, int> a)
        {
            Console.WriteLine(name);
            Console.WriteLine(string.Format("Single thread: {0}ms", U.Utils.AverageTime(() => U.Utils.TimeFor(() => ParallelHelper.For2DSingle(Width, Height, a)), runs)));
            Console.WriteLine(string.Format("Parallel thread: {0}ms", U.Utils.AverageTime(() => U.Utils.TimeFor(() => ParallelHelper.For2DParallel(Width, Height, a)), runs)));
            Console.WriteLine(string.Format("Parallel thread, unrolled: {0}ms", U.Utils.AverageTime(() => U.Utils.TimeFor(() => ParallelHelper.For2DParallelUnrolled(Width, Height, a)), runs)));
            Console.WriteLine(string.Format("Batched thread: {0}ms", U.Utils.AverageTime(() => U.Utils.TimeFor(() => ParallelHelper.For2DParallelBatched(Width, Height, a)), runs)));
        }

    }
}

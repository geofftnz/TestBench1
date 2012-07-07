using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            int runs = 100;

            ParallelForBenchmark benchmark = new ParallelForBenchmark(1024,1024);

            benchmark.Run(runs);

            Console.ReadKey();
        }
    }
}

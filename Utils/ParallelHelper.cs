using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class ParallelHelper
    {
        const int BATCHSIZE = 4;

        public static void For2D(int Width, int Height, Action<int, int, int> op)
        {
            For2DParallelUnrolled(Width, Height, op);
        }
        public static void For2D(int Width, int Height, Action< int> op)
        {
            For2DParallelUnrolled(Width, Height, op);
        }

        public static void For2DSingle(int Width, int Height, Action<int, int, int> op)
        {
            int i = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    op(x, y, i);
                    i++;
                }
            }
        }

        public static void For2DParallel(int Width, int Height, Action<int, int, int> op)
        {

            Parallel.For(0, Height, (y) =>
            {
                int i = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    op(x, y, i);
                    i++;
                }
            });
        }

        public static void For2DParallel(int Width, int Height, Action<int> op)
        {

            Parallel.For(0, Height, (y) =>
            {
                int i = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    op(i++);
                }
            });
        }
        public static void For2DParallelUnrolled(int Width, int Height, Action<int, int, int> op)
        {
            if ((Width & 0x07) == 0)
            {
                Parallel.For(0, Height, (y) =>
                {
                    int i = y * Width;
                    for (int x = 0; x < Width; )
                    {
                        op(x++, y, i++);
                        op(x++, y, i++);
                        op(x++, y, i++);
                        op(x++, y, i++);
                        op(x++, y, i++);
                        op(x++, y, i++);
                        op(x++, y, i++);
                        op(x++, y, i++);
                    }

                });
            }
            else
            {
                For2DParallel(Width, Height, op);
            }
        }
        public static void For2DParallelUnrolled(int Width, int Height, Action<int> op)
        {
            if ((Width & 0x07) == 0)
            {
                Parallel.For(0, Height, (y) =>
                {
                    int i = y * Width;
                    for (int x = 0; x < Width; x+=8)
                    {
                        op(i++);
                        op(i++);
                        op(i++);
                        op(i++);
                        op(i++);
                        op(i++);
                        op(i++);
                        op(i++);
                    }

                });
            }
            else
            {
                For2DParallel(Width, Height, op);
            }
        }


        public static void For2DParallelBatched(int Width, int Height, Action<int, int, int> op)
        {
            int hbatch = Height / BATCHSIZE;

            // run in BATCHSIZE row blocks
            Parallel.For(0, hbatch, (yy) =>
            {
                int i = yy * Width * BATCHSIZE;
                for (int y = yy; y < yy + BATCHSIZE; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        op( x, y, i);
                        i++;
                    }
                }
            });

            // run for the rest
            int ii = hbatch * Width * BATCHSIZE;
            for (int y = hbatch * BATCHSIZE; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    op(x, y, ii);
                    ii++;
                }
            }

        }

    }
}

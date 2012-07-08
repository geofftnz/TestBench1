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
            For2DParallel(Width, Height, op);
        }
        public static void For2D(int Width, int Height, Action< int> op)
        {
            For2DParallel(Width, Height, op);
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


        public static void For2DParallelOffset(int Width, int Height, int xofs, int yofs, Action<int, int>  op)
        {

            Parallel.For(0, Height, (y) =>
            {
                int i = y * Width;
                int y2 = ((y + yofs + Height) & (Height - 1));
                int x2 = (xofs + Width) & (Width - 1);
                int i2 = x2 + y2 * Width;
                op(i, i2);
                i++;

                i2 = 1 + xofs + y2 * Width;
                for (int x = 1; x < Width-1; x++)
                {
                    op(i,i2);
                    i++; i2++;
                }

                i2 = ((Width - 1 + xofs) & (Width-1)) + y2 * Width;
                op(i, i2);

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

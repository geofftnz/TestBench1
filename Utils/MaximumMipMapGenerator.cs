using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    public static class MaximumMipMapGenerator
    {

        public static T max<T>(T a, T b) where T : IComparable
        {
            return a.CompareTo(b) > 0 ? a : b;
        }

        public static T min<T>(T a, T b) where T : IComparable
        {
            return a.CompareTo(b) < 0 ? a : b;
        }

        public static float[] GenerateMaximumMipMapLevel(this float[] s, int w, int h)
        {
            // end condition
            if (w < 1 || h < 1)
            {
                return null;
            }

            // allocate new buffer
            float[] d = new float[(w >> 1) * (h >> 1)];

            int i = 0,x,y,s0,s1;

            for (y = 0; y < h >> 1; y++)
            {
                s0 = (y<<1) * w;
                s1 = ((y<<1)+1) * w;

                for (x = 0; x < w >> 1; x++)
                {
                    d[i++] = max(max(s[s0], s[s1]), max(s[s0+1], s[s1+1]));

                    s0 += 2;
                    s1 += 2;
                }
            }

            return d;
        }

        /// <summary>
        /// Generates a max mip-map level, but extends the sampling rect to 3x3 instead of 2x2
        /// 
        /// Still results in a 1/4-sized map
        /// </summary>
        /// <param name="s"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static float[] GenerateMaximumMipMapLevel3(this float[] s, int w, int h)
        {
            // end condition
            if (w < 1 || h < 1)
            {
                return null;
            }

            // allocate new buffer
            float[] d = new float[(w >> 1) * (h >> 1)];

            int i = 0, x, y, s0, s1, s2;

            for (y = 0; y < h >> 1; y++)
            {
                s0 = (y << 1) * w;
                s1 = ((y << 1) + 1) * w;
                s2 = (y == (h>>1)-1) ? s1: ((y << 1) + 2) * w;  // set to s1 on last row

                for (x = 0; x < w >> 1; x++)
                {
                    d[i++] =
                        max(
                            max(  // top left 2x2 block
                                max(s[s0], s[s1]),
                                max(s[s0 + 1], s[s1 + 1])
                            ),
                            max(
                                max(s[s0 + ((x == (w >> 1) - 1) ? 1 : 2)], s[s1 + ((x == (w >> 1) - 1) ? 1 : 2)]),  // 2 extra to the right
                                max(s[s2], s[s2 + 1])  // 2 extra below
                            )
                        );


                    s0 += 2;
                    s1 += 2;
                    s2 += 2;
                    s2 = min(s2, (w >> 1) - 1);// increment and clamp - otherwise would overflow on last column

                }
            }

            return d;
        }


    }
}

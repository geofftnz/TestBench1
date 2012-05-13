using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Utils
{
    public class FrameCounter
    {
        const int BUFLEN = 5;
        private Stopwatch sw = new Stopwatch();
        private long[] tickBuffer = new long[BUFLEN];
        private int bufferPos = 0;

        public double FPS
        {
            get
            {
                var ts = new TimeSpan(tickBuffer[bufferPos] - tickBuffer[(bufferPos + 1) % BUFLEN]).TotalSeconds;

                if (ts>0.0){
                    return (double)BUFLEN / ts;
                }
                return 0.0;
            }
        }


        public FrameCounter()
        {
        }

        public void Start()
        {
            sw.Start();

            long ticks = sw.ElapsedTicks;

            for (int i = 0; i < BUFLEN; i++)
            {
                tickBuffer[i] = ticks;
            }
        }

        public void Stop()
        {
            sw.Stop();
            sw.Reset();
        }

        public void Frame()
        {
            bufferPos++;
            bufferPos %= BUFLEN;
            tickBuffer[bufferPos] = sw.ElapsedTicks;
        }

    }
}

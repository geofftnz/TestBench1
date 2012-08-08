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
    public class TerrainGenWater2 : ITerrainGen
    {
        const int FILEMAGIC = 0x54455231;

        public class TerrainParameters
        {
            public float TotalWaterBudget = 1024f * 1024f * 2.0f;
            public float MaxRainPerFrame = 1024f * 1024f * 0.05f;
        }

        public TerrainParameters Parameters = new TerrainParameters();
        public float AtmosphericWater = 0f;
        public int Iterations { get; set; }



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
            /// Water
            /// </summary>
            public float Water;

            /// <summary>
            /// Change in height over time. Used for visualisation
            /// </summary>
            public float DeltaHeight;

            /// <summary>
            /// Amount of material slumped recently.
            /// </summary>
            //public float Slumping;


            public float Height
            {
                get
                {
                    return Hard + Loose + Water;
                }
            }

            public float GroundLevel
            {
                get
                {
                    return Hard + Loose;
                }
            }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Cell[] Map { get; private set; }
        public Cell[] ErosionMap { get; private set; }
        private float[] TempDiffMap;
        private float[] TotalCellDrop;

        public TerrainGenWater2(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Map = new Cell[this.Width * this.Height];
            this.ErosionMap = new Cell[this.Width * this.Height];
            this.TempDiffMap = new float[this.Width * this.Height];
            this.TotalCellDrop = new float[this.Width * this.Height];

            this.Iterations = 0;
            this.AtmosphericWater = Parameters.TotalWaterBudget;

            Random r = new Random();
        }

        /// <summary>
        /// Gets the cell index of x,y
        /// 
        /// Deals with wraparound
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>

        private Func<int, int, int> C = (x, y) => ((x + 1024) & 1023) + (((y + 1024) & 1023) << 10);
        private Func<int, int> CX = (i) => i & 1023;
        private Func<int, int> CY = (i) => (i >> 10) & 1023;

        private void ClearTempDiffMap()
        {
            Array.Clear(this.TempDiffMap, 0, this.Width * this.Height);
        }

        public void InitTerrain1()
        {
            this.Clear(0.0f);

            this.AddSimplexNoise(10, 0.9f / (float)this.Width, 1000.0f);
            this.AddSimplexNoise(10, 1.43f / (float)this.Width, 800.0f, h => Math.Abs(h), h => h * h);
            this.AddSimplexNoise(6, 3.7f / (float)this.Width, 400.0f, h => h * h, h => h * h);

            this.AddSimplexNoise(10, 7.7f / (float)this.Width, 30.0f, h => Math.Abs(h), h => (h * h * 2f).ClampInclusive(0.1f, 10.0f) - 0.1f);
            this.AddSimplexNoise(5, 37.7f / (float)this.Width, 10.0f, h => Math.Abs(h), h => (h * h * 2f).ClampInclusive(0.1f, 10.0f) - 0.1f);

            this.AddLooseMaterial(15.0f);
            this.AddSimplexNoiseToLoose(5, 17.7f / (float)this.Width, 10.0f);

            this.SetBaseLevel();
        }


        public void ModifyTerrain()
        {
            if (this.AtmosphericWater > 0f)
            {
                float rainAmount = this.AtmosphericWater.ClampInclusive(0f, this.Parameters.MaxRainPerFrame);
                this.AtmosphericWater -= rainAmount;
                this.AddRain(rainAmount);
            }

            this.RunWater();

            //this.RunWater2(this.WaterIterationsPerFrame);
            //this.Slump(this.TerrainSlumpMaxHeightDifference, this.TerrainSlumpMovementAmount, this.TerrainSlumpSamplesPerFrame);
            //this.Slump(this.TerrainSlump2MaxHeightDifference, this.TerrainSlump2MovementAmount, this.TerrainSlump2SamplesPerFrame);
            //this.Collapse(this.TerrainCollapseMaxHeightDifference, this.TerrainCollapseMovementAmount, 1f, this.TerrainCollapseSamplesPerFrame);

            this.Iterations++;
        }


        public void Clear(float height)
        {
            for (int i = 0; i < Width * Height; i++)
            {
                this.Map[i].Hard = 0f;
                this.Map[i].Loose = 0f;
                this.Map[i].Water = 0f;
                this.Map[i].DeltaHeight = 0f;
            }
        }

        public void SetBaseLevel()
        {
            float min = this.Map.Select(c => c.Hard).Min();

            for (int i = 0; i < Width * Height; i++)
            {
                this.Map[i].Hard -= min;
            }
        }

        public void AddLooseMaterial(float amount)
        {
            for (int i = 0; i < Width * Height; i++)
            {
                this.Map[i].Loose += amount;
            }
        }

        #region Water


        public void AddRain(float totalAmount)
        {
            // water per cell
            float amount = totalAmount / (this.Width * this.Height);

            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                this.Map[i].Water += amount;
            });
        }



        public void RunWater()
        {
            // water moves downhill to all cells in proportion to their relative slopes.

            // need to calculate total drop amounts for each cell
            //Array.Clear(this.TempDiffMap, 0, this.Width * this.Height);

            // clear erosion map
            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                this.ErosionMap[i].Hard = 0f;
                this.ErosionMap[i].Loose = 0f;
                this.ErosionMap[i].Water = 0f;
                this.ErosionMap[i].DeltaHeight = 0f;
            });

            ParallelHelper.For2D(this.Width, this.Height, (x, y, i) =>
            {
                float drop = 0f;
                float h = this.Map[i].Height;
                drop += (h - this.Map[C(x - 1, y)].Height).ClampLower(0f);
                drop += (h - this.Map[C(x + 1, y)].Height).ClampLower(0f);
                drop += (h - this.Map[C(x, y - 1)].Height).ClampLower(0f);
                drop += (h - this.Map[C(x, y + 1)].Height).ClampLower(0f);

                drop += (h - this.Map[C(x - 1, y - 1)].Height).ClampLower(0f) * 0.707f;
                drop += (h - this.Map[C(x - 1, y + 1)].Height).ClampLower(0f) * 0.707f;
                drop += (h - this.Map[C(x + 1, y - 1)].Height).ClampLower(0f) * 0.707f;
                drop += (h - this.Map[C(x + 1, y + 1)].Height).ClampLower(0f) * 0.707f;

                this.TotalCellDrop[i] = drop;
            });

            Action<int, int, float, float, float> MoveWater = (pFrom, pTo, height, waterAmount, totalDrop) =>
            {
                float destHeight = this.Map[pTo].Height;
                if (destHeight < height)
                {
                    float dropAmount = (height - destHeight).ClampLower(0f) / totalDrop;
                    if (dropAmount > waterAmount)
                    {
                        dropAmount = waterAmount;
                    }
                    float moveAmount = dropAmount * 0.1f;

                    this.ErosionMap[pFrom].Water -= moveAmount;
                    this.ErosionMap[pTo].Water += moveAmount;

                    float groundFrom = this.Map[pFrom].GroundLevel;
                    float groundTo = this.Map[pTo].GroundLevel;

                    // if the underlying ground is downhill, move some loose material.
                    if (groundFrom > groundTo)
                    {
                        moveAmount *= 0.1f; // erode slower than we move water.

                        float diff = (groundFrom - groundTo)*0.1f;
                        if (diff > moveAmount)
                        {
                            diff = moveAmount;
                        }

                        float looseAvailable = this.Map[pFrom].Loose;

                        if (looseAvailable > 0.005f)
                        {
                            if (diff > looseAvailable)
                            {
                                diff = looseAvailable;
                            }
                            this.ErosionMap[pFrom].Loose -= diff;
                            this.ErosionMap[pTo].Loose += diff;
                        }
                        else
                        {
                            // erode hard material
                            this.ErosionMap[pFrom].Hard -= diff;
                            this.ErosionMap[pTo].Loose += diff;
                        }

                    }
                }
            };

            // now calculate how much water to move around. Single threaded for now.
            ParallelHelper.For2DSingle(this.Width, this.Height, (x, y, i) =>
            {
                float totalDrop = this.TotalCellDrop[i];
                if (totalDrop > 0.0f)
                {
                    float h = this.Map[i].Height;
                    float waterAmount = this.Map[i].Water;

                    MoveWater(i, C(x - 1, y), h, waterAmount, totalDrop);
                    MoveWater(i, C(x + 1, y), h, waterAmount, totalDrop);
                    MoveWater(i, C(x, y - 1), h, waterAmount, totalDrop);
                    MoveWater(i, C(x, y + 1), h, waterAmount, totalDrop);

                    waterAmount *= 0.707f;
                    MoveWater(i, C(x - 1, y - 1), h, waterAmount, totalDrop);
                    MoveWater(i, C(x - 1, y + 1), h, waterAmount, totalDrop);
                    MoveWater(i, C(x + 1, y - 1), h, waterAmount, totalDrop);
                    MoveWater(i, C(x + 1, y + 1), h, waterAmount, totalDrop);

                }
            });

            // recombine diff map
            ParallelHelper.For2D(this.Width, this.Height, (i) =>
            {
                this.Map[i].Hard += this.ErosionMap[i].Hard;
                this.Map[i].Loose += this.ErosionMap[i].Loose;
                this.Map[i].Water += this.ErosionMap[i].Water;
            });


        }


        #endregion

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
        public void AddSimplexNoiseToLoose(int octaves, float scale, float amplitude)
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
                        this.Map[i].Loose += h * amplitude;
                        i++;
                    }
                }
            );
        }

        public void AddSimplexNoise(int octaves, float scale, float amplitude, Func<float, float> transform, Func<float, float> postTransform)
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

                        if (postTransform != null)
                        {
                            this.Map[i].Hard += postTransform(h) * amplitude;
                        }
                        else
                        {
                            this.Map[i].Hard += h * amplitude;
                        }
                        i++;
                    }
                }
            );
        }

        public void AddSimplexPowNoise(int octaves, float scale, float amplitude, float power, Func<float, float> postTransform)
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
        public void Slump(float _threshold, float amount, int numIterations)
        {
            //float amount2 = amount * 0.707f;
            float _threshold2 = (float)(_threshold * Math.Sqrt(2.0));
            this.ClearTempDiffMap();

            Func<int, int, float, float, float, float[], float> SlumpF = (pFrom, pTo, h, a, threshold, diffmap) =>
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

                float h = this.Map[p].Hard + this.Map[p].Loose;
                float a = amount;

                float th = _threshold;
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

            //var threadlocal = new { diffmap = new float[this.Width * this.Height], r = new Random() };
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
                    this.Map[ii].Loose += threadlocal.diffmap[ii];
                    ii++;
                }
            });


        }


        /// <summary>
        /// Similar to Slump(), but works on hard material instead of loose, and only when amount of loose coverage is below a certain threshold
        /// </summary>
        /// <param name="_threshold"></param>
        /// <param name="amount"></param>
        /// <param name="numIterations"></param>
        public void Collapse(float _threshold, float amount, float looseThreshold, int numIterations)
        {
            //float amount2 = amount * 0.707f;
            float _threshold2 = (float)(_threshold * Math.Sqrt(2.0));
            this.ClearTempDiffMap();

            Func<int, int, float, float, float, float[], float> SlumpF = (pFrom, pTo, h, a, threshold, diffmap) =>
            {

                if (this.Map[pFrom].Loose > looseThreshold)
                {
                    return 0f;
                }

                float diff = this.Map[pFrom].Hard - h;
                if (diff > threshold)
                {
                    diff -= threshold;

                    diff *= a;

                    diffmap[pFrom] -= diff;
                    diffmap[pTo] += diff;

                    return diff;
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

                h += SlumpF(n, p, h, amount, _threshold, diffmap);
                h += SlumpF(s, p, h, amount, _threshold, diffmap);
                h += SlumpF(w, p, h, amount, _threshold, diffmap);
                h += SlumpF(e, p, h, amount, _threshold, diffmap);

                h += SlumpF(nw, p, h, amount, _threshold2, diffmap);
                h += SlumpF(ne, p, h, amount, _threshold2, diffmap);
                h += SlumpF(sw, p, h, amount, _threshold2, diffmap);
                h += SlumpF(se, p, h, amount, _threshold2, diffmap);
            };

            //var threadlocal = new { diffmap = new float[this.Width * this.Height], r = new Random() };
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
                    float d = threadlocal.diffmap[ii];
                    if (d < 0)
                    {
                        this.Map[ii].Hard += d;
                    }
                    else
                    {
                        this.Map[ii].Loose += d;
                    }
                    ii++;
                }
            });


        }

        private Func<Cell[], int, int, float, float, float> CollapseCellFunc = (m, ci, i, h, a) =>
        {
            float diff = (h - m[i].Height);

            if (diff > m[ci].Loose * 0.2f)
                diff = m[ci].Loose * 0.2f;

            if (diff > 0f)
            {
                diff *= a;
                m[i].Loose += diff;
                return diff;
            }
            return 0f;
        };

        private Func<Cell[], int, int, float, float, float> CollapseToCellFunc = (m, ci, i, h, a) =>
        {
            float diff = (m[i].Height - h);

            // take at most half available loose material
            if (diff > m[i].Loose * 0.25f)
                diff = m[i].Loose * 0.25f;

            if (diff > 0f)
            {
                diff *= a;
                m[i].Loose -= diff;
                return diff;
            }
            return 0f;
        };

        /// <summary>
        /// Collapses loose material away from the specified point.
        /// 
        /// 
        /// </summary>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="amount"></param>
        public void CollapseFrom(int cx, int cy, float amount)
        {
            int ci = C(cx, cy);
            float h = this.Map[ci].Height;
            float dh = 0f;
            float amount2 = amount * 0.707f;

            dh += CollapseCellFunc(this.Map, ci, C(cx - 1, cy), h, amount);
            dh += CollapseCellFunc(this.Map, ci, C(cx + 1, cy), h, amount);
            dh += CollapseCellFunc(this.Map, ci, C(cx, cy - 1), h, amount);
            dh += CollapseCellFunc(this.Map, ci, C(cx, cy + 1), h, amount);
            dh += CollapseCellFunc(this.Map, ci, C(cx - 1, cy - 1), h, amount2);
            dh += CollapseCellFunc(this.Map, ci, C(cx - 1, cy + 1), h, amount2);
            dh += CollapseCellFunc(this.Map, ci, C(cx + 1, cy - 1), h, amount2);
            dh += CollapseCellFunc(this.Map, ci, C(cx + 1, cy + 1), h, amount2);

            if (dh < this.Map[ci].Loose)
            {
                this.Map[ci].Loose -= dh;
            }
            else
            {
                this.Map[ci].Hard -= (dh - this.Map[ci].Loose);
                this.Map[ci].Loose = 0f;
            }
        }

        public void CollapseTo(int cx, int cy, float amount)
        {
            int ci = C(cx, cy);
            float h = this.Map[ci].Height;
            float dh = 0f;
            float amount2 = amount * 0.707f;

            dh += CollapseToCellFunc(this.Map, ci, C(cx - 1, cy), h, amount);
            dh += CollapseToCellFunc(this.Map, ci, C(cx + 1, cy), h, amount);
            dh += CollapseToCellFunc(this.Map, ci, C(cx, cy - 1), h, amount);
            dh += CollapseToCellFunc(this.Map, ci, C(cx, cy + 1), h, amount);
            dh += CollapseToCellFunc(this.Map, ci, C(cx - 1, cy - 1), h, amount2);
            dh += CollapseToCellFunc(this.Map, ci, C(cx - 1, cy + 1), h, amount2);
            dh += CollapseToCellFunc(this.Map, ci, C(cx + 1, cy - 1), h, amount2);
            dh += CollapseToCellFunc(this.Map, ci, C(cx + 1, cy + 1), h, amount2);

            this.Map[ci].Loose += dh;

        }

        #region utils

        private Vector3 CellNormal(int cx, int cy)
        {
            float h1 = this.Map[C(cx, cy - 1)].Height;
            float h2 = this.Map[C(cx, cy + 1)].Height;
            float h3 = this.Map[C(cx - 1, cy)].Height;
            float h4 = this.Map[C(cx + 1, cy)].Height;

            return Vector3.Normalize(new Vector3(h3 - h4, h1 - h2, 2f));
        }

        // fall vector as the weighted sum of the vectors between cells
        private Vector3 GroundFallVector(int cx, int cy)
        {
            //float diag = 1.0f;

            float h0 = this.Map[C(cx, cy)].GroundLevel;
            float h1 = this.Map[C(cx, cy - 1)].GroundLevel;
            float h2 = this.Map[C(cx, cy + 1)].GroundLevel;
            float h3 = this.Map[C(cx - 1, cy)].GroundLevel;
            float h4 = this.Map[C(cx + 1, cy)].GroundLevel;
            Vector3 f = new Vector3(0.0f);

            f += (new Vector3(0, -1, h1 - h0) * (h0 - h1));
            f += (new Vector3(0, 1, h2 - h0) * (h0 - h2));
            f += (new Vector3(-1, 0, h3 - h0) * (h0 - h3));
            f += (new Vector3(1, 0, h4 - h0) * (h0 - h4));

            f.Normalize();
            return f;
        }

        #endregion

        public float WaterHeightAt(float x, float y)
        {
            int xx = (int)(x * this.Width);
            int yy = (int)(y * this.Height);

            float xfrac = (x * (float)Width) - (float)xx;
            float yfrac = (y * (float)Height) - (float)yy;

            float h00 = this.Map[C(xx, yy)].Height;
            float h10 = this.Map[C(xx + 1, yy)].Height;
            float h01 = this.Map[C(xx, yy + 1)].Height;
            float h11 = this.Map[C(xx + 1, yy + 1)].Height;

            return MathHelper.Lerp(MathHelper.Lerp(h00, h10, xfrac), MathHelper.Lerp(h01, h11, xfrac), yfrac);
        }

        public Vector3 ClampToGround(Vector3 pos)
        {
            pos.Y = this.WaterHeightAt(pos.X, pos.Z) / 4096.0f;
            return pos;
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
                        sw.Write(this.Map[i].Hard);
                        sw.Write(this.Map[i].Loose);
                        sw.Write(this.Map[i].Water);
                        sw.Write(this.Map[i].DeltaHeight);
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
                        this.Map[i].Hard = sr.ReadSingle();
                        this.Map[i].Loose = sr.ReadSingle();
                        this.Map[i].Water = sr.ReadSingle();
                        this.Map[i].DeltaHeight = sr.ReadSingle();
                    }

                    this.AtmosphericWater = Parameters.TotalWaterBudget;

                    sr.Close();
                }
                fs.Close();
            }
        }

        #endregion


    }
}

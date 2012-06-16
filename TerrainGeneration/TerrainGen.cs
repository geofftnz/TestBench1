using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Utils;
using Microsoft.Xna.Framework;
using System.Threading;

namespace TerrainGeneration
{
    public class TerrainGen
    {
        const int NUMTHREADS = 3;

        #region Generation Parameters
        public float TerrainSlumpMaxHeightDifference { get; set; }
        public float TerrainSlumpMovementAmount { get; set; }
        public int TerrainSlumpSamplesPerFrame { get; set; }

        public int WaterNumParticles { get; set; }
        public int WaterIterationsPerFrame { get; set; }
        #endregion

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
            /// Standing water - will flow unrestricted
            /// </summary>
            public float Water;
            /// <summary>
            /// Non-height component indicating how much flowing water is over this tile.
            /// </summary>
            public float MovingWater;

            public float Height
            {
                get
                {
                    return Hard + Loose;
                }
            }

            public float WHeight
            {
                get
                {
                    return Hard + Loose;// +MovingWater;
                }
            }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Cell[] Map { get; private set; }
        private float[] TempDiffMap;

        private List<ErosionParticle> WaterParticlesOld = new List<ErosionParticle>();
        private List<ErosionParticle2> WaterParticles = new List<ErosionParticle2>();

        public TerrainGen(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Map = new Cell[this.Width * this.Height];
            this.TempDiffMap = new float[this.Width * this.Height];


            // init parameters
            this.TerrainSlumpMaxHeightDifference = 0.8f;
            this.TerrainSlumpMovementAmount = 0.08f;
            this.TerrainSlumpSamplesPerFrame = 5000;

            this.WaterNumParticles = 5000;
            this.WaterIterationsPerFrame = 20;

            this.Iterations = 0;

            Random r = new Random();

            for (int i = 0; i < this.WaterNumParticles; i++)
            {
                this.WaterParticles.Add(new ErosionParticle2(r.Next(this.Width), r.Next(this.Height)));
            }

        }

        /// <summary>
        /// Gets the cell index of x,y
        /// 
        /// Deals with wraparound
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int C(int x, int y)
        {
            return ((x + 1024) & 1023) + (((y + 1024) & 1023) << 10);
            //return x.Wrap(this.Width) + y.Wrap(this.Height) * this.Width;
        }

        private int CX(int i)
        {
            return i & 1023;
            //return (i % this.Width).Wrap(this.Width);
        }
        private int CY(int i)
        {
            return (i >> 10) & 1023;
            //return (i / this.Width).Wrap(this.Height);
        }

        private void ClearTempDiffMap()
        {
            Array.Clear(this.TempDiffMap, 0, this.Width * this.Height);
        }

        public void InitTerrain1()
        {
            this.Clear(0.0f);
            this.AddSimplexNoise(6, 0.1f / (float)this.Width, 2000.0f);
            this.AddSimplexPowNoise(8, 0.05f / (float)this.Width, 6000.0f, 3.0f, x => Math.Abs(x));
            this.AddSimplexNoise(9, 0.7f / (float)this.Width, 500.0f);

            this.AddLooseMaterial(5.0f);
            this.AddSimplexNoiseToLoose(7, 17.7f / (float)this.Width, 3.0f);



            //this.AddSimplexPowNoise(4, 1.7f / (float)this.Width, 500.0f, 4.0f, x => Math.Abs(x));
            //this.AddSimplexNoise(9, 0.3f / (float)this.Width, 2000.0f, x => Math.Abs(x)); // really big hills


            //this.AddSimplexNoise(2, 0.1f, 1.0f);

            //this.AddDiscontinuousNoise(3,0.003f, 200.0f, 0.1f);

            //this.AddSimplexNoise(4, 0.002f, 5.0f);
            //this.AddSimplexNoise(4, 0.01f, 0.2f);

            this.SetBaseLevel();
        }


        public void ModifyTerrain()
        {
            this.RunWater2(this.WaterIterationsPerFrame);
            this.Slump(this.TerrainSlumpMaxHeightDifference, this.TerrainSlumpMovementAmount, this.TerrainSlumpSamplesPerFrame);

            // fade water amount
            if (this.Iterations % 8 == 0)
            {
                for (int i = 0; i < this.Width * this.Height; i++)
                {
                    this.Map[i].MovingWater *= 0.8f;
                }
            }

            this.Iterations++;
        }


        public void Clear(float height)
        {
            for (int i = 0; i < Width * Height; i++)
            {
                this.Map[i] = new Cell();
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

        public void RunWater2(int CellsPerRun)
        {
            // This will probably be single-thread only due to extensive modification of the heightmap.
            // Could interlock the fucker, but that'll be a performance nightmare with tight loops.

            var up = new Vector3(0f, 0f, 1f);
            var rand = new Random();
            var tileDir = new Vector2(0);
            

            Func<int, float, float> LowestNeighbour = (i, h) => this.Map[i].WHeight < h ? this.Map[i].WHeight : h;

            foreach (var wp in this.WaterParticles)
            {
                //int celli = wp.Pos.CellIndex(this.Width, this.Height);// grab current cell index
                int cellx = wp.Pos.CellX(this.Width);
                int celly = wp.Pos.CellY(this.Height);
                int celli = C(cellx, celly);
                int cellnx, cellny;
                int cellox = cellx, celloy = celly; // check for oscillation in a small area
                bool needReset = false;

                wp.CarryingDecay *= 1.2f;  //1.04
                if (wp.CarryingDecay >= 1.0f)
                {
                    this.Map[celli].Loose = wp.CarryingAmount;
                    wp.Reset(rand.Next(this.Width), rand.Next(this.Height), rand);// reset particle
                }

                // run the particle for a number of cells
                for (int i = 0; i < CellsPerRun; i++)
                {

                    // add some flowing water to the terrain so we can see it
                    this.Map[celli].MovingWater += 0.001f;

                    // get our current height
                    float h = this.Map[celli].WHeight;

                    // hole check - if the minimum height of our neighbours exceeds our own height, try to fill the hole
                    float lowestNeighbour = this.Map[C(cellx - 1, celly)].WHeight;
                    lowestNeighbour = LowestNeighbour(C(cellx + 1, celly), h);
                    lowestNeighbour = LowestNeighbour(C(cellx, celly - 1), h);
                    lowestNeighbour = LowestNeighbour(C(cellx, celly + 1), h);
                    lowestNeighbour = LowestNeighbour(C(cellx - 1, celly - 1), h);
                    lowestNeighbour = LowestNeighbour(C(cellx - 1, celly + 1), h);
                    lowestNeighbour = LowestNeighbour(C(cellx + 1, celly - 1), h);
                    lowestNeighbour = LowestNeighbour(C(cellx + 1, celly + 1), h);

                    float ndiff = lowestNeighbour - h;
                    if (ndiff > 0f)
                    {
                        if (wp.CarryingAmount > ndiff)
                        {
                            // carrying more than difference -> fill hole
                            this.Map[celli].Loose += ndiff;
                            wp.CarryingAmount -= ndiff;
                        }
                        else
                        {
                            // stuck in hole, reset
                            needReset = true;
                            break;
                        }
                    }


                    // compute fall vector of current cell
                    //var normal = CellNormal(cellx, celly);
                    var fall = FallVector(cellx, celly);  //FallVector(normal, up);

                    // if fall vector points up, bail out
                    if (fall.Z > 0.0f)
                    {
                        needReset = true;
                        break;
                    }

                    wp.Fall = wp.Fall + fall;
                    wp.Fall.Normalize();

                    // compute exit point and new cell coords
                    tileDir.X = wp.Fall.X;
                    tileDir.Y = wp.Fall.Y;

                    // sanity check: If the direction is changing such that we're going to get stuck on an edge, move point out into tile
                    if (tileDir.X < 0f)
                    {
                        if ((wp.Pos.X - (float)Math.Floor(wp.Pos.X)) < 0.05f)
                        {
                            wp.Pos.X += 0.2f;
                        }
                    }
                    else
                    {
                        if ((wp.Pos.X - (float)Math.Floor(wp.Pos.X)) > 0.95f)
                        {
                            wp.Pos.X -= 0.2f;
                        }
                    }
                    if (tileDir.Y < 0f)
                    {
                        if ((wp.Pos.Y - (float)Math.Floor(wp.Pos.Y)) < 0.05f)
                        {
                            wp.Pos.Y += 0.2f;
                        }
                    }
                    else
                    {
                        if ((wp.Pos.Y - (float)Math.Floor(wp.Pos.Y)) > 0.95f)
                        {
                            wp.Pos.Y -= 0.2f;
                        }
                    }

                    // compute exit
                    var newPos = TileMath.TileIntersect(wp.Pos, tileDir, cellx, celly, out cellnx, out cellny);

                    // if the intersection func has returned the same cell, we're stuck and need to reset
                    if (cellx == cellnx && celly == cellny)
                    {
                        needReset = true;
                        break;
                    }

                    // calculate index of next cell
                    int cellni = C(cellnx, cellny);
                    float nh = this.Map[cellni].WHeight;

                    ndiff = nh - h;
                    // check to see if we're being forced uphill. If we are we drop material to try and level with our new position. If we can't do that we reset.

                    if (ndiff > 0f)
                    {
                        if (wp.CarryingAmount > ndiff)
                        {
                            float uphillDrop = ndiff;// *0.5f;
                            // carrying more than difference -> fill hole
                            this.Map[celli].Loose += uphillDrop;
                            wp.CarryingAmount -= uphillDrop;
                            /*
                            // eat some material from the cell we're being forced into
                            float uphillEat = ndiff * 0.2f;
                            float nextcellLoose = this.Map[cellni].Loose;
                            float nextcellHard = this.Map[cellni].Hard;

                            if (uphillEat < nextcellLoose)
                            {
                                this.Map[cellni].Loose -= uphillEat;
                                wp.CarryingAmount += uphillEat;
                            }
                            else
                            {
                                this.Map[cellni].Loose -= nextcellLoose;
                                wp.CarryingAmount += nextcellLoose;
                            }*/
                        }
                        else
                        {
                            // stuck in hole, reset
                            needReset = true;
                            break;
                        }

                        // collapse any material we've just deposited.
                        CollapseFrom(cellx, celly, 0.05f);
                    }
                    else
                    {
                        float slope = (float)Math.Atan(-ndiff) / 1.570796f;

                        // modify speed of particle
                        wp.Speed = wp.Speed * 0.5f + 0.5f * slope;
                    }


                    // calculate distance that we're travelling across cell.
                    float crossdistance = Vector2.Distance(wp.Pos, newPos);

                    // work out fraction of cell we're crossing, as a proportion of the length of the diagonal (root-2)
                    crossdistance /= 1.4142136f;


                    // calculate new carrying capacity
                    wp.CarryingCapacity = (10.8f * wp.Speed) * (1.0f - wp.CarryingDecay);


                    // if we're over our carrying capacity, start dropping material
                    float cdiff = wp.CarryingAmount - wp.CarryingCapacity;
                    if (cdiff > 0.0f)
                    {
                        cdiff *= 0.8f * crossdistance; // amount to drop

                        // drop a portion of our material
                        this.Map[cellni].Loose += cdiff;  // drop at new location
                        wp.CarryingAmount -= cdiff;

                        //CollapseFrom(cellx, celly, 0.1f);
                    }
                    else  // we're under our carrying capacity, so do some erosion
                    {
                        cdiff = -cdiff;

                        float loose = this.Map[celli].Loose;
                        float hard = this.Map[celli].Hard;

                        float erosionFactor = ((0.02f + wp.Speed) * crossdistance) * (1f + this.Map[celli].MovingWater * 20.0f); // erode more where there is lots of water.

                        float looseErodeAmount = erosionFactor; // erosion coefficient for loose material
                        float hardErodeAmount = 0.3f * erosionFactor; // erosion coefficient for hard material

                        // first of all, see if we can pick up any loose material.
                        if (loose > 0.0f)
                        {
                            if (looseErodeAmount > loose)
                            {
                                looseErodeAmount = loose;
                            }

                            this.Map[celli].Loose -= looseErodeAmount;
                            wp.CarryingAmount += looseErodeAmount;

                            CollapseTo(cellx, celly, 0.02f);
                        }
                        else  // we're down to hard material
                        {
                            this.Map[celli].Hard -= hardErodeAmount;
                            wp.CarryingAmount += hardErodeAmount; // loose material is less dense than hard, so make it greater.
                        }
                        
                    }
                    //}

                    // move particle params
                    wp.Pos = newPos;
                    cellx = cellnx; // this may not work across loop runs. May need to store on particle.
                    celly = cellny;
                    celli = cellni;
                }

                
                // if we haven't moved further than a portion of the cells per run (manhattan distance), then decay carrying capacity faster
                if (Math.Abs(cellx - cellox) + Math.Abs(celly - celloy) < CellsPerRun / 2)
                {
                    wp.CarryingDecay *= 1.2f;
                }

                if (needReset)
                {
                    this.Map[celli].Loose += wp.CarryingAmount;
                    wp.Reset(rand.Next(this.Width), rand.Next(this.Height),rand);// reset particle
                }

            }

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

        public void AddSimplexNoise(int octaves, float scale, float amplitude, Func<float, float> transform)
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
                        this.Map[i].Hard += h * amplitude;
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
                        diffmap[pFrom] -= diff * a;
                        diffmap[pTo] += diff * a;
                        return diff * a;
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
            for (int i = 0; i < this.Width * this.Height; i++)
            {
                this.Map[i].Loose += threadlocal.diffmap[i];
            }


        }



        private Func<Cell[], int, int, float, float, float> CollapseCellFunc = (m, ci, i, h, a) =>
        {
            float diff = (h - m[i].Height);

            if (diff > m[ci].Loose*0.2f)
                diff = m[ci].Loose*0.2f;

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
        private Vector3 FallVector(int cx, int cy)
        {
            //float diag = 1.0f;

            float h0 = this.Map[C(cx, cy)].WHeight;
            float h1 = this.Map[C(cx, cy - 1)].WHeight;
            float h2 = this.Map[C(cx, cy + 1)].WHeight;
            float h3 = this.Map[C(cx - 1, cy)].WHeight;
            float h4 = this.Map[C(cx + 1, cy)].WHeight;
            /*
            float h5 = this.Map[C(cx - 1, cy - 1)].WHeight;
            float h6 = this.Map[C(cx - 1, cy + 1)].WHeight;
            float h7 = this.Map[C(cx + 1, cy - 1)].WHeight;
            float h8 = this.Map[C(cx + 1, cy + 1)].WHeight;
            */
            Vector3 f = new Vector3(0.0f);

            /*
            f.Y -= h0 - h1;
            f.Z -= h0 - h1;

            f.Y += h0 - h2;
            f.Z -= h0 - h2;

            f.X -= h0 - h3;
            f.Z -= h0 - h3;

            f.X += h0 - h4;
            f.Z -= h0 - h4;
            */

            f += (new Vector3(0, -1, h1 - h0) * (h0 - h1));
            f += (new Vector3(0, 1, h2 - h0) * (h0 - h2));
            f += (new Vector3(-1, 0, h3 - h0) * (h0 - h3));
            f += (new Vector3(1, 0, h4 - h0) * (h0 - h4));

            /*
            f += (new Vector3(-1, -1, h5 - h0) * ((h0 - h5))) * diag;
            f += (new Vector3(-1, 1, h6 - h0) * ((h0 - h6))) * diag;
            f += (new Vector3(1, -1, h7 - h0) * ((h0 - h7))) * diag;
            f += (new Vector3(1, 1, h8 - h0) * ((h0 - h8))) * diag;*/

            f.Normalize();
            //f *= 0.25f;
            return f;
        }

        #endregion

        public float HeightAt(float x, float y)
        {
            int xx = (int)(x * this.Width);
            int yy = (int)(y * this.Height);

            float xfrac = (x * (float)Width) - (float)xx;
            float yfrac = (y * (float)Height) - (float)yy;

            float h00 = this.Map[C(xx,yy)].Height;
            float h10 = this.Map[C(xx+1, yy)].Height;
            float h01 = this.Map[C(xx, yy+1)].Height;
            float h11 = this.Map[C(xx+1, yy+1)].Height;

            return MathHelper.Lerp(MathHelper.Lerp(h00, h10, xfrac), MathHelper.Lerp(h01, h11, xfrac), yfrac);
        }

        public Vector3 ClampToGround(Vector3 pos)
        {
            pos.Y = this.HeightAt(pos.X, pos.Z) / 4096.0f;
            return pos;
        }

    }
}

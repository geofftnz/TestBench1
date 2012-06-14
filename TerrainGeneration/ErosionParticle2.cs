using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TerrainGeneration
{
    public class ErosionParticle2
    {
        public Vector2 Pos;
        //public Vector2 Vel;

        public float CarryingCapacity;
        public float CarryingAmount;
        public float Speed;

        public ErosionParticle2()
        {
            Reset(0, 0);
        }

        public ErosionParticle2(int x, int y)
            : this()
        {
            Reset(x, y);
        }

        public void Reset(int x, int y)
        {
            this.Pos = new Vector2((float)x + 0.5f, (float)y + 0.5f);
          //  this.Vel = new Vector2(0.0f);
            this.CarryingAmount = 0.0f;
            this.CarryingCapacity = 0.0f;
            this.Speed = 0f;
        }
    }
}

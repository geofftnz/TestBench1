using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TerrainGeneration
{
    public class ErosionParticle
    {

        /// <summary>
        /// Position of the particle on the heightfield
        /// </summary>
        //public Vector2 Position { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        
        /// <summary>
        /// Velocity of material in particle, not velocity of particle across terrain.
        /// </summary>
        public float Velocity { get; set; }

        /// <summary>
        /// Amount of material this particle can currently carry.
        /// Depends on velocity.
        /// </summary>
        public float CarryingCapacity { get; set; }

        /// <summary>
        /// Amount of material this particle is carrying.
        /// </summary>
        public float CarryingAmount { get; set; }


        public ErosionParticle()
        {

        }

        public ErosionParticle(int x, int y)
        {
            this.Reset(x,y);
        }

        public virtual void Reset(int x, int y)
        {
            this.X = x;
            this.Y = y;
            this.Velocity = 0.0f;
            this.CarryingAmount = 0.0f;
            this.CarryingCapacity = 0.0f;
        }

        /// <summary>
        /// Returns the amount of loose material to deposit on the terrain.
        /// </summary>
        /// <param name="sheddingRate"></param>
        /// <returns></returns>
        public virtual float DepositAmount(float sheddingRate)
        {
            float diff = this.CarryingAmount - this.CarryingCapacity;

            if (diff > 0.0f)
            {
                diff *= sheddingRate;

                this.CarryingAmount -= diff;
                return diff;
            }
            return 0.0f;
        }

    }
}

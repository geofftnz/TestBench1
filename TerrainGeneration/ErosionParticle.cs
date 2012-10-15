using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TerrainGeneration
{
    public class WindErosionParticle
    {

        /// <summary>
        /// Position of the particle on the heightfield
        /// </summary>
        //public Vector2 Position { get; set; }
        public Vector2 Position;

        /// <summary>
        /// Height of particle. This should always be above the surface of the ground.
        /// As a particle is forced upwards, it increases in velocity.
        /// Particle height decays exponentially and velocity with it.
        /// </summary>
        public float Height;  

        /// <summary>
        /// Velocity of material in particle, not velocity of particle across terrain.
        /// </summary>
        public float Velocity;

        /// <summary>
        /// Amount of material this particle can currently carry.
        /// Depends on velocity.
        /// </summary>
        public float CarryingCapacity;

        /// <summary>
        /// Amount of material this particle is carrying.
        /// </summary>
        public float CarryingAmount;


        public WindErosionParticle()
        {
            this.Position = new Vector2(0f);
        }

        public WindErosionParticle(int x, int y)
            : this()
        {
            this.Reset(x, y);
        }

        public virtual void Reset(int x, int y)
        {
            this.Position.X = (float)x + 0.5f;
            this.Position.Y = (float)y + 0.5f;
            this.Velocity = 0.0f;
            this.CarryingAmount = 0.0f;
            this.CarryingCapacity = 0.0f;
            this.Height = 0f;
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

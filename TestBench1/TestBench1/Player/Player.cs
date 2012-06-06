using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TestBench1
{
    public class Player : GameComponent
    {
        private MouseState prevMouse;
        private MouseState currMouse;

        private KeyboardState prevKeyboard;
        private KeyboardState currKeyboard;

        float movementSpeed = 0.1f;

        /// <summary>
        /// base (ground) position 
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// look angle (up/down), in radians 
        /// </summary>
        private float angleUpDown = (float)Math.PI * 0.5f;
        private float MINANGLEUPDOWN = (float)0.01f;
        private float MAXANGLEUPDOWN = (float)Math.PI * 0.99f;
        public float AngleUpDown
        {
            get { return angleUpDown; }
            set
            {

                if (value < MINANGLEUPDOWN)
                {
                    angleUpDown = MINANGLEUPDOWN;
                }
                else
                    if (value > MAXANGLEUPDOWN)
                    {
                        angleUpDown = MAXANGLEUPDOWN;
                    }
                    else
                    {
                        angleUpDown = value;
                    }

            }
        }

        /// <summary>
        /// look angle (left/right), in radians
        /// </summary>
        private float angleLeftRight = 0.0f;
        public float AngleLeftRight
        {
            get { return angleLeftRight; }
            set
            {
                angleLeftRight = value;
            }
        }

        /// <summary>
        /// height of eye above ground
        /// </summary>
        public float EyeHeight { get; set; }


        //private Vector3 forwardVector = new Vector3(1f, 0f, 0f);

        public Vector3 EyePos
        {
            get
            {
                return this.Position + Vector3.Up * this.EyeHeight;
            }
        }

        public Vector3 LookTarget
        {
            get
            {
                return this.EyePos + new Vector3((float)(Math.Cos(this.AngleLeftRight) * Math.Sin(AngleUpDown)), (float)Math.Cos(AngleUpDown), (float)(Math.Sin(this.AngleLeftRight) * Math.Sin(AngleUpDown)));
            }
        }

        // get view matrix
        public Matrix ViewMatrix
        {
            get
            {
                // calculate target position
                return Matrix.CreateLookAt(this.EyePos, this.LookTarget, Vector3.Up);
            }
        }



        public Player(Game game)
            : base(game)
        {
            this.UpdateOrder = 1;

            this.Position = new Vector3(0f, 0f, 0f);
            this.AngleUpDown = (float)Math.PI * 0.5f;
            this.AngleLeftRight = 0f;
            this.EyeHeight = 5f / 256f;

            this.currMouse = Mouse.GetState();
            this.currKeyboard = Keyboard.GetState();
        }


        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // process input
            prevMouse = currMouse;
            currMouse = Mouse.GetState();

            prevKeyboard = currKeyboard;
            currKeyboard = Keyboard.GetState();

            Rectangle clientBounds = Game.Window.ClientBounds;

            int centerX = clientBounds.Width / 2;
            int centerY = clientBounds.Height / 2;
            int deltaX = centerX - currMouse.X;
            int deltaY = centerY - currMouse.Y;

            Mouse.SetPosition(centerX, centerY);

            this.AngleLeftRight += (float)deltaX * -0.005f;
            this.AngleUpDown += (float)deltaY * 0.005f;
            
            float speed = (float)(this.movementSpeed*gameTime.ElapsedGameTime.TotalSeconds);
            var pos = this.Position;
            if (currKeyboard.IsKeyDown(Keys.W))
            {
                pos.X += (float)(Math.Cos(this.AngleLeftRight) * speed);
                pos.Z += (float)(Math.Sin(this.AngleLeftRight) * speed);
            }
            if (currKeyboard.IsKeyDown(Keys.S))
            {
                pos.X -= (float)(Math.Cos(this.AngleLeftRight) * speed);
                pos.Z -= (float)(Math.Sin(this.AngleLeftRight) * speed);
            }
            if (currKeyboard.IsKeyDown(Keys.A))
            {
                pos.X += (float)(Math.Cos(this.AngleLeftRight+Math.PI * 1.5) * speed);
                pos.Z += (float)(Math.Sin(this.AngleLeftRight + Math.PI * 1.5) * speed);
            }
            if (currKeyboard.IsKeyDown(Keys.D))
            {
                pos.X += (float)(Math.Cos(this.AngleLeftRight + Math.PI * 2.5) * speed);
                pos.Z += (float)(Math.Sin(this.AngleLeftRight + Math.PI * 2.5) * speed);
            }

            this.Position = pos;
        }



    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
//using TestBench1.Terrain;
using TerrainEngine;
using System.Diagnostics;
using Utils;

namespace TestBench1
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        
        //Effect effect;
        Effect terrainTileEffect;
        GraphicsDevice device;
        SpriteFont statusFont;
        SpriteBatch sprites;
        Player player;


        FrameCounter fc = new FrameCounter();

        //VertexPositionColor[] vertices;
        //SimpleTerrain terrain;
        //TerrainTile terrainTile;
        //TerrainTile terrainTile2;

        TerrainEngine.Terrain terrain;
        

        Matrix viewMatrix;
        Matrix projectionMatrix;
        private float angle = (float)Math.PI;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            player = new Player(this);
            this.Components.Add(player);

            this.IsFixedTimeStep = false;
        }

        private void SetUpVertices()
        {
            //this.terrain.Setup();
        }

        private void SetUpCamera()
        {
            //viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 500), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.1f, 20.0f); // 5km view distance
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {

            this.terrain = new TerrainEngine.Terrain(2048, 2048);
            this.terrain.SetupTestTerrain();
            this.terrain.LoadTiles();

            graphics.PreferredBackBufferWidth = 1600;
            graphics.PreferredBackBufferHeight = 1000;
            graphics.IsFullScreen = false;
            //graphics.PreferredBackBufferFormat = SurfaceFormat.
            graphics.ApplyChanges();


            

            Window.Title = "Terrain Test 1";

            fc.Start();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            device = graphics.GraphicsDevice;

            this.sprites = new SpriteBatch(device);

            // TODO: use this.Content to load your game content here
            terrainTileEffect = Content.Load<Effect>("TerrainTile");
            statusFont = Content.Load<SpriteFont>("statusFont");

            SetUpVertices();
            SetUpCamera();

            this.terrain.LoadContent(device);

            var rt = new RenderTarget2D(device, device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Alpha8, device.PresentationParameters.DepthStencilFormat, 1, RenderTargetUsage.DiscardContents);

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
            angle += 0.002f;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            fc.Frame();

            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.Default;


            device.Clear(new Color(0.6f,0.7f,1.0f));

            float r = 0.7f;
            //Vector3 eyePos = new Vector3(r * (float)Math.Cos(angle)+1f, 0.3f, r * (float)Math.Sin(angle)+1f);

            //this.player.Position = this.terrain.ClampToGround(new Vector3(r * (float)Math.Cos(angle) + 1f, 0.0f, r * (float)Math.Sin(angle) + 1f));
            this.player.Position = this.terrain.ClampToGround(this.player.Position);

            //this.player.Position = eyePos;
            this.viewMatrix = this.player.ViewMatrix;

            //this.viewMatrix = Matrix.CreateLookAt(eyePos, new Vector3(0.2f, 0.0f, 0.5f), new Vector3(0, 1, 0));



            Matrix worldMatrix = Matrix.Identity;
            // light direction
            Vector3 lightDirection = new Vector3(1.0f, 1.0f, 0.2f);
            lightDirection.Normalize();


            terrainTileEffect.CurrentTechnique = terrainTileEffect.Techniques["RaycastTile1"];
            this.terrain.Draw(gameTime, device, terrainTileEffect, this.player.EyePos, viewMatrix, worldMatrix, projectionMatrix, lightDirection);

            terrainTileEffect.CurrentTechnique = terrainTileEffect.Techniques["BBox"];
            this.terrain.DrawBox(gameTime, device, terrainTileEffect, this.player.EyePos, viewMatrix, worldMatrix, projectionMatrix, lightDirection);


            sprites.Begin();
            sprites.DrawString(statusFont, string.Format("FPS: {0:###0}", fc.FPS), new Vector2(0, 0), Color.Wheat);

            sprites.DrawString(statusFont, string.Format("Eye: {0:0}", this.player.Position.ToString()), new Vector2(0, 16), Color.Wheat);
            sprites.End();


            base.Draw(gameTime);
        }
    }
}

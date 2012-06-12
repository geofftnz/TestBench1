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
using Utils;

namespace TerrainGeneration
{
    public class TerrainGenerationVisualiser : Microsoft.Xna.Framework.Game
    {
        /// <summary>
        /// terrain generation object that we will be visualising/controlling
        /// </summary>
        public TerrainGen Terrain { get; set; }

        // texture to hold the bit of the terrain we're looking at (1024x1024)
        private Texture2D VisTexture;
        private int VisTextureWidth = 1024;
        private int VisTextureHeight = 1024;

        // misc
        private FrameCounter fc = new FrameCounter();
        //private Matrix viewMatrix;
        private Matrix projectionMatrix;
        private GraphicsDeviceManager graphics;
        private GraphicsDevice device;
        private Effect terrainVisEffect;
        private SpriteFont statusFont;
        private SpriteBatch sprites;
        //private VertexPositionTexture[] terrainQuad = new VertexPositionTexture[4];
        //private short[] terrainQuadIndex = new short[4];
        private QuadRender quad;

        private TerrainEngine.TerrainTile tile;
        private Effect terrainTileEffect;

        private Color[] shadeTexData;

        private bool paused = false;



        public TerrainGenerationVisualiser()
        {
            this.Terrain = new TerrainGen(1024, 1024);
            this.Terrain.InitTerrain1();

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.IsFixedTimeStep = false;

            this.tile = new TerrainEngine.TerrainTile(1024, 1024, new Vector3(0f, 0f, 0f), 1.0f);
            this.shadeTexData = new Color[this.Terrain.Width * this.Terrain.Height];
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 1400;
            graphics.PreferredBackBufferHeight = 1024;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();

            Window.Title = "Generating Terrain";

            fc.Start();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            device = graphics.GraphicsDevice;
            this.sprites = new SpriteBatch(device);
            terrainVisEffect = Content.Load<Effect>("GenerationVisualiser");
            terrainTileEffect = Content.Load<Effect>("TerrainTile1024");
            statusFont = Content.Load<SpriteFont>("statusFont");

            quad = new QuadRender(device);

            // initial load
            this.CreateTexture(device);
            this.UpdateTexture();

            // terrain tile
            this.tile.LoadContent(device);

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {

            var ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.Space))
            {
                this.paused = !this.paused;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            fc.Frame();

            this.Terrain.ModifyTerrain();

            //Draw2DTerrain(gameTime);
            DrawTile(gameTime);

            sprites.Begin();
            sprites.DrawString(statusFont, string.Format("FPS: {0:###0}", fc.FPS), new Vector2(0, 0), Color.Wheat);
            sprites.End();

            base.Draw(gameTime);
        }

        private void Draw2DTerrain(GameTime gameTime)
        {
            this.projectionMatrix = Matrix.CreateOrthographic(1600f, 1200f, 0.0f, 1.0f);

            if (fc.Frames % 15 == 0)
            {
                device.Textures[0] = null;
                this.UpdateTexture();
            }

            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.Default;
            device.Clear(new Color(0.8f, 0.88f, 0.92f));

            Matrix worldMatrix = Matrix.Identity;

            this.terrainVisEffect.Parameters["HeightTex"].SetValue(this.VisTexture);
            this.terrainVisEffect.Parameters["World"].SetValue(worldMatrix);
            this.terrainVisEffect.Parameters["View"].SetValue(worldMatrix);
            this.terrainVisEffect.Parameters["Projection"].SetValue(this.projectionMatrix);

            this.terrainVisEffect.CurrentTechnique = this.terrainVisEffect.Techniques["Relief"];

            quad.RenderFullScreenQuad(this.terrainVisEffect);
        }

        private double angle = 0.0;
        private void DrawTile(GameTime gameTime)
        {

            if (fc.Frames % 20 == 1)
            {
                device.Textures[0] = null;
                device.Textures[1] = null;
                device.Textures[2] = null;
                //this.UpdateTexture();

                this.UpdateTileData();
               
            }


            this.projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.1f, 20.0f); // 5km view distance

            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.Default;

            device.Clear(new Color(0.8f, 0.88f, 0.92f));

            float r = 1.0f;
            angle += this.paused?0.0:gameTime.ElapsedGameTime.TotalSeconds * 0.1;
            Vector3 eyePos = new Vector3(r * (float)Math.Cos(angle) + 0.5f, 0.6f, r * (float)Math.Sin(angle) + 0.5f);

            //this.player.Position = this.terrain.ClampToGround(new Vector3(r * (float)Math.Cos(angle) + 1f, 0.0f, r * (float)Math.Sin(angle) + 1f));
            //this.player.Position = this.terrain.ClampToGround(this.player.Position);

            //this.player.Position = eyePos;
            //this.viewMatrix = this.player.ViewMatrix;

            Matrix viewMatrix = Matrix.CreateLookAt(eyePos, new Vector3(0.5f, 0.0f, 0.5f), new Vector3(0, 1, 0));



            Matrix worldMatrix = Matrix.Identity;
            // light direction
            Vector3 lightDirection = new Vector3(1.0f, 1.0f, 0.2f);
            lightDirection.Normalize();


            terrainTileEffect.CurrentTechnique = terrainTileEffect.Techniques["RaycastTile1"];
            this.tile.Draw(gameTime, device, terrainTileEffect, eyePos, viewMatrix, worldMatrix, projectionMatrix, lightDirection);

            this.tile.Draw(gameTime, device, terrainTileEffect, eyePos, viewMatrix, Matrix.CreateTranslation(1f, 0f, 0f), projectionMatrix, lightDirection);
            this.tile.Draw(gameTime, device, terrainTileEffect, eyePos, viewMatrix, Matrix.CreateTranslation(1f, 0f, 1f), projectionMatrix, lightDirection);
            this.tile.Draw(gameTime, device, terrainTileEffect, eyePos, viewMatrix, Matrix.CreateTranslation(0f, 0f, 1f), projectionMatrix, lightDirection);
            this.tile.Draw(gameTime, device, terrainTileEffect, eyePos, viewMatrix, Matrix.CreateTranslation(-1f, 0f, 0f), projectionMatrix, lightDirection);
            this.tile.Draw(gameTime, device, terrainTileEffect, eyePos, viewMatrix, Matrix.CreateTranslation(0f, 0f, -1f), projectionMatrix, lightDirection);
            this.tile.Draw(gameTime, device, terrainTileEffect, eyePos, viewMatrix, Matrix.CreateTranslation(-1f, 0f, -1f), projectionMatrix, lightDirection);
            this.tile.Draw(gameTime, device, terrainTileEffect, eyePos, viewMatrix, Matrix.CreateTranslation(-1f, 0f, 1f), projectionMatrix, lightDirection);
            this.tile.Draw(gameTime, device, terrainTileEffect, eyePos, viewMatrix, Matrix.CreateTranslation(1f, 0f, -1f), projectionMatrix, lightDirection);

            //terrainTileEffect.CurrentTechnique = terrainTileEffect.Techniques["BBox"];
            //worldMatrix = worldMatrix * Matrix.CreateTranslation(1.0f, 0.0f, 0.0f);
            //this.tile.DrawBox(gameTime, device, terrainTileEffect, eyePos, viewMatrix, Matrix.CreateTranslation(1.0f, 0.0f, 0.0f), projectionMatrix, lightDirection);
            //terrainTileEffect.CurrentTechnique = terrainTileEffect.Techniques["BBox"];
            //this.tile.DrawBox(gameTime, device, terrainTileEffect, eyePos, viewMatrix, worldMatrix, projectionMatrix, lightDirection);

        }




        private void CreateTexture(GraphicsDevice d)
        {
            this.VisTexture = new Texture2D(d, this.VisTextureWidth, this.VisTextureHeight, false, SurfaceFormat.Vector4);
            this.VisTexture.Name = "TerrainVisTex";
        }
        private void UpdateTexture()
        {
            this.VisTexture.SetData(this.Terrain.Map);
        }

        private void UpdateTileData()
        {
            int i = 0;

            // heights & col
            for (int y = 0; y < this.tile.Height; y++)
            {
                for (int x = 0; x < this.tile.Width; x++)
                {
                    var c = this.Terrain.Map[i];
                    this.tile.Data[i] = (c.Hard + c.Loose) / 4096.0f;
                    this.shadeTexData[i].R = (byte)((c.Hard / 4.0f).ClampInclusive(0.0f,255.0f));
                    this.shadeTexData[i].G = (byte)((c.Loose * 8.0f).ClampInclusive(0.0f, 255.0f));
                    this.shadeTexData[i].B = (byte)((c.MovingWater * 8.0f).ClampInclusive(0.0f, 255.0f));
                    i++;
                }
            }

            this.tile.UpdateHeights(device);
            this.tile.UpdateShadeTexture(this.shadeTexData);
        }


        //private void SetupTerrainQuad()
        //{
        //    int i = 0;
        //    float x0 = 0f, y0 = 0f;
        //    float x1 = 1024f, y1 = 1024f;
        //    this.terrainQuad[i++] = new VertexPositionTexture(new Vector3(x0, y0, 0.0f), new Vector2(0f, 0f));
        //    this.terrainQuad[i++] = new VertexPositionTexture(new Vector3(x1, y0, 0.0f), new Vector2(1f, 0f));
        //    this.terrainQuad[i++] = new VertexPositionTexture(new Vector3(x0, y1, 0.0f), new Vector2(0f, 1f));
        //    this.terrainQuad[i++] = new VertexPositionTexture(new Vector3(x1, y1, 0.0f), new Vector2(1f, 1f));
        //    i = 0;
        //    this.terrainQuadIndex[i++] = 0;
        //    this.terrainQuadIndex[i++] = 1;
        //    this.terrainQuadIndex[i++] = 2;
        //    this.terrainQuadIndex[i++] = 3;

        //}

    }
}

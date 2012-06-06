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
        private Matrix viewMatrix;
        private Matrix projectionMatrix;
        private GraphicsDeviceManager graphics;
        private GraphicsDevice device;
        private Effect terrainVisEffect;
        private SpriteFont statusFont;
        private SpriteBatch sprites;
        //private VertexPositionTexture[] terrainQuad = new VertexPositionTexture[4];
        //private short[] terrainQuadIndex = new short[4];
        private QuadRender quad;


        public TerrainGenerationVisualiser()
        {
            this.Terrain = new TerrainGen(1024,1024);
            this.Terrain.InitTerrain1();

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.IsFixedTimeStep = false;
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 1600;
            graphics.PreferredBackBufferHeight = 1200;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();

            this.projectionMatrix = Matrix.CreateOrthographic(1600f, 1200f, 0.0f, 1.0f);

            Window.Title = "Generating Terrain";

            fc.Start();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            device = graphics.GraphicsDevice;
            this.sprites = new SpriteBatch(device);
            terrainVisEffect = Content.Load<Effect>("GenerationVisualiser");
            statusFont = Content.Load<SpriteFont>("statusFont");

            quad = new QuadRender(device);

            // initial load
            this.CreateTexture(device);
            this.UpdateTexture();

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            fc.Frame();

            this.Terrain.ModifyTerrain();
            if (fc.Frames % 15 == 0)
            {
                device.Textures[0] = null;
                this.UpdateTexture();
            }

            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.Default;
            device.Clear(new Color(0.4f, 0.42f, 0.46f));

            Matrix worldMatrix = Matrix.Identity;


            this.terrainVisEffect.Parameters["HeightTex"].SetValue(this.VisTexture);
            this.terrainVisEffect.Parameters["World"].SetValue(worldMatrix);
            this.terrainVisEffect.Parameters["View"].SetValue(worldMatrix);
            this.terrainVisEffect.Parameters["Projection"].SetValue(this.projectionMatrix);

            this.terrainVisEffect.CurrentTechnique = this.terrainVisEffect.Techniques["Relief"];

            quad.RenderFullScreenQuad(this.terrainVisEffect);

            //foreach (var pass in this.terrainVisEffect.CurrentTechnique.Passes)
            //{
            //    pass.Apply();
            //    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleStrip, this.terrainQuad, 0, 4, this.terrainQuadIndex, 0, 2);
            //}

            sprites.Begin();
            sprites.DrawString(statusFont, string.Format("FPS: {0:###0}", fc.FPS), new Vector2(0, 0), Color.Wheat);
            sprites.End();

            base.Draw(gameTime);
        }




        private void CreateTexture(GraphicsDevice d)
        {
            this.VisTexture = new Texture2D(d, this.VisTextureWidth, this.VisTextureHeight, false, SurfaceFormat.Vector4);
        }
        private void UpdateTexture()
        {
            this.VisTexture.SetData(this.Terrain.Map);
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

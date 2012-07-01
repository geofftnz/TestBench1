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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TerrainGeneration
{
    public class TerrainGenerationVisualiser : Microsoft.Xna.Framework.Game
    {
        /// <summary>
        /// terrain generation object that we will be visualising/controlling
        /// </summary>
        public TerrainGen Terrain { get; set; }

        // texture to hold the bit of the terrain we're looking at (1024x1024)
        //private Texture2D VisTexture;
        //private int VisTextureWidth = 1024;
        //private int VisTextureHeight = 1024;

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
        private double angle = 0.0;
        private float eyeradius = 0.5f;
        private float eyeheight = 0.6f;
        private double lastUpdateTime = 0;
        private double tileUpdateInterval = 1.0;

        private Stopwatch stopwatch = new Stopwatch();
        private double generationSeconds = 0.3;

        private WalkCamera walkCamera;
        private KeyboardState currKeyboard;
        private KeyboardState prevKeyboard;

        private int autosaveIntervalSeconds = 60;
        private DateTime lastAutosavedDate = DateTime.Now;
        private string savePath = "../../../../../Terrains";
        private Dictionary<Keys, int> fileSaveSlots = new Dictionary<Keys, int>
            {
                {Keys.D1,1},
                {Keys.D2,2},
                {Keys.D3,3},
                {Keys.D4,4},
                {Keys.D5,5},
                {Keys.D6,6},
                {Keys.D7,7},
                {Keys.D8,8},
                {Keys.D9,9}
            };

        private string _statusMessage = "";
        private string StatusMessage
        {
            get
            {
                return ((int)(DateTime.Now - this.statusMessageTime).TotalSeconds < this.statusMessageSeconds) ? _statusMessage : string.Empty;
            }
            set
            {
                _statusMessage = value;
                statusMessageTime = DateTime.Now;
            }
        }
        private DateTime statusMessageTime = DateTime.Now;
        private int statusMessageSeconds = 10;


        public TerrainGenerationVisualiser()
        {
            this.Terrain = new TerrainGen(1024, 1024);
            //this.Terrain.InitTerrain1();
            this.Terrain.Clear(0.0f);

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.IsFixedTimeStep = false;

            this.tile = new TerrainEngine.TerrainTile(1024, 1024, new Vector3(0f, 0f, 0f), 1.0f);
            this.shadeTexData = new Color[this.Terrain.Width * this.Terrain.Height];

            this.walkCamera = new WalkCamera(this);
            this.Components.Add(this.walkCamera);

            this.walkCamera.EyeHeight = 500f/4096f;

            this.StatusMessage = "Press R to randomize terrain, or 1-9 to load a saved slot.";

            try
            {
                this.Terrain.Load(GetSaveFileName(0)); // load from autosave slot
            }
            catch (Exception) { 
                // swallow all exceptions when trying to auto-load terrain 
            }
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 1800;
            graphics.PreferredBackBufferHeight = 1100;
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
            //this.CreateTexture(device);
            //this.UpdateTexture();

            // terrain tile
            this.tile.LoadContent(device);

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        private bool WasPressed(Keys k, bool? shift)
        {

            bool shiftTest = true;
            if (shift.HasValue)
            {
                if (shift.Value)
                { // we want shift to be down
                    shiftTest = ((currKeyboard.IsKeyDown(Keys.LeftShift) || currKeyboard.IsKeyDown(Keys.RightShift)));
                }
                else
                { // shift must be up
                    shiftTest = !((currKeyboard.IsKeyDown(Keys.LeftShift) || currKeyboard.IsKeyDown(Keys.RightShift)));
                }
            }
            return currKeyboard.IsKeyDown(k) && !prevKeyboard.IsKeyDown(k) && shiftTest;
        }

        private bool WasPressed(Keys k)
        {
            return WasPressed(k, null);
        }

        protected override void Update(GameTime gameTime)
        {
            prevKeyboard = currKeyboard;
            currKeyboard = Keyboard.GetState();

            if (WasPressed(Keys.Space))
            {
                this.paused = !this.paused;
            }

            foreach (var k in this.fileSaveSlots.Keys)
            {
                string filename = GetSaveFileName(fileSaveSlots[k]);
                if (WasPressed(k, true))
                {
                    try
                    {
                        this.Terrain.Save(filename);
                        this.StatusMessage = string.Format("Saved to {0}", Path.GetFullPath(filename));
                    }
                    catch (Exception e)
                    {
                        this.StatusMessage = string.Format("Save: {0}: {1}", e.GetType().Name, e.Message);
                    }
                }
                if (WasPressed(k, false))
                {
                    try
                    {
                        this.Terrain.Load(filename);
                        this.StatusMessage = string.Format("Loaded from {0}", Path.GetFullPath(filename));
                    }
                    catch (Exception e)
                    {
                        this.StatusMessage = string.Format("Load: {0}: {1}", e.GetType().Name, e.Message);
                    }
                }
            }

            if (WasPressed(Keys.R))
            {
                this.Terrain.InitTerrain1();
                this.StatusMessage = "New random terrain generated";
            }

            if (WasPressed(Keys.OemOpenBrackets))
            {
                this.tileUpdateInterval *= 1.25;
                if (this.tileUpdateInterval > 10.0) this.tileUpdateInterval = 10.0;
                this.StatusMessage = string.Format("Tile update interval now {0:0.00}s", this.tileUpdateInterval);
            }
            if (WasPressed(Keys.OemCloseBrackets))
            {
                this.tileUpdateInterval *= 0.75;
                if (this.tileUpdateInterval < 0.1) this.tileUpdateInterval = 0.1;
                this.StatusMessage = string.Format("Tile update interval now {0:0.00}s", this.tileUpdateInterval);
            }
            if (WasPressed(Keys.M))
            {
                this.walkCamera.MouseEnabled = !this.walkCamera.MouseEnabled;

                this.StatusMessage = string.Format("Mouselook {0}", this.walkCamera.MouseEnabled ? "enabled" : "disabled");
            }









            /*
            if (currKeyboard.IsKeyDown(Keys.Left)) { angle += 0.02; }
            if (currKeyboard.IsKeyDown(Keys.Right)) { angle -= 0.02; }
            if (currKeyboard.IsKeyDown(Keys.Up)) { eyeradius -= 0.05f; }
            if (currKeyboard.IsKeyDown(Keys.Down)) { eyeradius += 0.05f; }
            if (currKeyboard.IsKeyDown(Keys.A)) { eyeheight += 0.05f; }
            if (currKeyboard.IsKeyDown(Keys.Z)) { eyeheight -= 0.05f; }
            */
            if (!paused && !this.walkCamera.IsMoving)
            {
                stopwatch.Restart();
                this.Terrain.ModifyTerrain();
                stopwatch.Stop();
                generationSeconds = generationSeconds * 0.9 + 0.1 * stopwatch.Elapsed.TotalSeconds;
            }

            if ((DateTime.Now - this.lastAutosavedDate).TotalSeconds > this.autosaveIntervalSeconds)
            {
                try
                {
                    this.Terrain.Save(GetSaveFileName(0));
                }
                catch (Exception e)
                {
                    this.StatusMessage = string.Format("Autosave: {0}: {1}", e.GetType().Name, e.Message);
                }
                this.lastAutosavedDate = DateTime.Now;
            }

            base.Update(gameTime);
        }

        private string GetSaveFileName(int slot)
        {
            return Path.Combine(this.savePath, string.Format("Terrain{0}.{1}.pass1.ter", slot,this.Terrain.Width));
        }

        protected override void Draw(GameTime gameTime)
        {
            fc.Frame();


            //Draw2DTerrain(gameTime);
            DrawTile(gameTime);

            sprites.Begin();
            sprites.DrawString(statusFont, string.Format("FPS: {0:###0}", fc.FPS), new Vector2(0, 0), Color.Orange);
            sprites.DrawString(statusFont, string.Format("Generation: {0:###0.000}ms", this.generationSeconds * 1000.0), new Vector2(0, 20), Color.Orange);


            sprites.DrawString(statusFont, string.Format("Eye: X:{0:###0.00} Y:{2:###0.00} H:{1:###0.00} E:{3:###0.00}", this.walkCamera.Position.X * this.Terrain.Width, this.walkCamera.Position.Y * this.Terrain.Width, this.walkCamera.Position.Z * this.Terrain.Width, this.walkCamera.EyeHeight * this.Terrain.Width), new Vector2(0, 40), Color.Orange);

            if (!string.IsNullOrWhiteSpace(this.StatusMessage))
            {
                sprites.DrawString(statusFont, this.StatusMessage, new Vector2(0, 60), Color.Red);
            }

            sprites.End();

            base.Draw(gameTime);
        }

        /*
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
        }*/

        private void DrawTile(GameTime gameTime)
        {
            double totalSec = fc.TotalSeconds;
            if (totalSec - this.lastUpdateTime > tileUpdateInterval && !paused && !this.walkCamera.IsMoving)
            {
                device.Textures[0] = null;
                device.Textures[1] = null;
                device.Textures[2] = null;
                //this.UpdateTexture();

                this.UpdateTileData();
                this.lastUpdateTime = totalSec;
            }

            this.projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.001f, 20.0f); // 5km view distance

            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.Default;

            device.Clear(new Color(0.8f, 0.88f, 0.92f));


            //angle += this.paused?0.0:gameTime.ElapsedGameTime.TotalSeconds * 0.1;
            Vector3 eyePos = new Vector3((float)eyeradius * (float)Math.Cos(angle) + 0.5f, eyeheight, (float)eyeradius * (float)Math.Sin(angle) + 0.5f);

            //this.player.Position = this.terrain.ClampToGround(new Vector3(r * (float)Math.Cos(angle) + 1f, 0.0f, r * (float)Math.Sin(angle) + 1f));
            //this.player.Position = this.terrain.ClampToGround(this.player.Position);

            //this.player.Position = eyePos;
            //this.viewMatrix = this.player.ViewMatrix;

            //Matrix viewMatrix = Matrix.CreateLookAt(eyePos, new Vector3(0.5f, 0.0f, 0.5f), new Vector3(0, 1, 0));

            this.walkCamera.Position = this.Terrain.ClampToGround(this.walkCamera.Position);
            var viewMatrix = this.walkCamera.ViewMatrix;
            eyePos = this.walkCamera.EyePos;



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
            //this.tile.DrawBox(gameTime, device, terrainTileEffect, eyePos, viewMatrix, Matrix.CreateTranslation(-1f, 0f, -1f), projectionMatrix, lightDirection);

        }



        /*
        private void CreateTexture(GraphicsDevice d)
        {
            this.VisTexture = new Texture2D(d, this.VisTextureWidth, this.VisTextureHeight, false, SurfaceFormat.Vector4);
            this.VisTexture.Name = "TerrainVisTex";
        }
        private void UpdateTexture()
        {
            this.VisTexture.SetData(this.Terrain.Map);
        }*/

        private void UpdateTileData()
        {

            // heights & col
            //for (int y = 0; y < this.tile.Height; y++)
            //{
            Parallel.For(0, this.tile.Height, y =>
            {
                int i = y * this.Terrain.Width;
                for (int x = 0; x < this.tile.Width; x++)
                {
                    var c = this.Terrain.Map[i];
                    this.tile.Data[i] = (c.Height) / 4096.0f;
                    //this.shadeTexData[i].R = (byte)((c.Hard / 4.0f).ClampInclusive(0.0f, 255.0f));
                    
                    this.shadeTexData[i].G = (byte)((c.Loose * 4.0f).ClampInclusive(0.0f, 255.0f));
                    //this.shadeTexData[i].G = (byte)((c.Slumping * 256.0f).ClampInclusive(0.0f, 255.0f));

                    this.shadeTexData[i].B = (byte)((c.MovingWater * 2048.0f).ClampInclusive(0.0f, 255.0f));
                    this.shadeTexData[i].A = (byte)((c.Erosion * 32f).ClampInclusive(0.0f, 255.0f));  // erosion rate
                    this.shadeTexData[i].R = (byte)((c.Carrying * 32f).ClampInclusive(0.0f, 255.0f)); // carrying capacity
                    i++;
                }
            });
            //}

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

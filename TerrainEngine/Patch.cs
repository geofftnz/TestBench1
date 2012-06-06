using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Utils;

namespace TerrainEngine
{

    /// <summary>
    /// Represents a patch of terrain that can be rendered at various LODs
    /// 
    /// Patch resolution maxes out at 129x129 (33k tris + patch joins), then drops to 65x65 (8k tris) , 33x33 (2k tris), 17x17 (578).
    /// Lower LODs are of limited use because the raycaster will take over when screen extent of tile drops below 256 pixels.
    /// 
    /// Also required are the textures used by the shader (normal, shade etc) and the texcoords of the corners of the patch.
    /// A tile consists of 4 patches, so a quarter of a 256x256 texture will be mapped across the patch.
    /// 
    /// </summary>
    public class Patch
    {
        public const float BASERESOLUTION = 1.0f / 256.0f;
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>
        /// Vertices for this patch. We only need pos and texture - everything else is done in the textures.
        /// </summary>
        public VertexPositionTexture[] Vertices { get; set; }

        /// <summary>
        /// Indices for triangles in the patch.
        /// </summary>
        public short[] Index { get; set; }

        /// <summary>
        /// Offset of this patch in the world
        /// </summary>
        public Vector3 Offset { get; set; }


        public Patch()
            : this(129, 129)
        {
        }

        public Patch(int w, int h)
        {
            this.Width = w;
            this.Height = h;
            this.Vertices = new VertexPositionTexture[this.Width * this.Height];
            this.Index = new short[this.Width * 2 * (this.Height - 1)];
        }

        public void SetData(float[] srcData, int srcWidth, int srcHeight, int srcXOffset, int srcYOffset, Vector2 patchOffset)
        {
            int i, j;

            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    i = y * this.Width + x;
                    j = (y + srcYOffset) * srcWidth + x + srcXOffset;

                    this.Vertices[i].Position.X = x * BASERESOLUTION;
                    this.Vertices[i].Position.Y = srcData[j];
                    this.Vertices[i].Position.Z = y * BASERESOLUTION;

                    this.Vertices[i].TextureCoordinate.X = x * BASERESOLUTION + patchOffset.X;
                    this.Vertices[i].TextureCoordinate.Y = y * BASERESOLUTION + patchOffset.Y;
                }
            }
        }

        public void SetupIndices()
        {
            
            int i = 0;

            for (int y = 0; y < this.Height - 1; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    this.Index[i++] = (short)(x + (y + 1) * this.Width);
                    this.Index[i++] = (short)(x + y * this.Width);

                }
            }
        }

        public void Render(GraphicsDevice d)
        {
            for (int y = 0; y < this.Height - 1; y++)
            {
                d.DrawUserIndexedPrimitives(PrimitiveType.TriangleStrip, this.Vertices, 0, this.Width * this.Height, this.Index, y * 2 * this.Width, (this.Width - 1) * 2);
            }
        }

    }
}

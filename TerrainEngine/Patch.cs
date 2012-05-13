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
        public int Width { get; set; }
        public int Height { get; set; }

        public VertexPositionTexture[] Vertices { get; set; }
        public short[] Index { get; set; }


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
    }
}

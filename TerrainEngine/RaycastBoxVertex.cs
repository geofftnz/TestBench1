using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TerrainEngine
{
    public struct RaycastBoxVertex:IVertexType
    {
        public Vector3 Position;
        public Vector3 BoxCoord;

        public static readonly VertexDeclaration _VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0));

        public VertexDeclaration VertexDeclaration
        {
            get { return _VertexDeclaration; }
        }

        public RaycastBoxVertex(Vector3 position, Vector3 boxcoord)
        {
            this.Position = position;
            this.BoxCoord = boxcoord;
        }

        public static bool operator !=(RaycastBoxVertex left, RaycastBoxVertex right)
        {
            if (left == null || right == null) return true;
            return left.Position != right.Position || left.BoxCoord != right.BoxCoord;
        }

        public static bool operator ==(RaycastBoxVertex left, RaycastBoxVertex right)
        {
            if (left == null || right == null) return false;
            return left.Position == right.Position && left.BoxCoord == right.BoxCoord;
        }

        public override bool Equals(object obj)
        {
            if (obj is RaycastBoxVertex)
            {
                return this == (RaycastBoxVertex)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.Position.GetHashCode() ^ this.BoxCoord.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Position, this.BoxCoord);
        }
    }
}

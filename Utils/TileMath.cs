using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Utils
{
    public static class TileMath
    {

        public static Vector3 FallVectorShit(Vector3 normal, Vector3 up)
        {
            if (normal == up)
            {
                return up;
            }

            return Vector3.Normalize(Vector3.Cross(normal, Vector3.Cross(normal, up)));
        }

        /// <summary>
        /// Find the next tile intersection in the specified direction.
        /// Assumes tiles are on whole number boundaries.
        /// 
        /// Does not correct for world boundaries.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Vector2 TileIntersect(Vector2 pos, Vector2 dir, int cx, int cy, out int nx, out int ny)
        {
            float tx, ty;  // distance to intersect each line.

            // init new cell coords.
            nx = cx;
            ny = cy;

            // early exit
            if (dir.X == 0f && dir.Y == 0f)
            {
                return pos;
            }


            if (dir.X < 0.0f)
            {
                // heading left
                tx = ((float)Math.Floor(pos.X) - pos.X) / dir.X;
                if (tx < 0f) throw new Exception("tx neg");
            }
            else
            {
                // heading right, X is positive
                if (dir.X > 0.0f)
                {
                    tx = (((float)Math.Floor(pos.X) + 1.0f) - pos.X) / dir.X;
                }
                else
                {
                    tx = 100000000.0f; // large number
                }
            }

            if (dir.Y < 0.0f)
            {
                // heading down
                ty = ((float)Math.Floor(pos.Y) - pos.Y) / dir.Y;
                if (ty < 0f) throw new Exception("tx neg");
            }
            else
            {
                // heading up
                if (dir.Y > 0.0f)
                {
                    ty = (((float)Math.Floor(pos.Y) + 1.0f) - pos.Y) / dir.Y;
                }
                else
                {
                    ty = 100000000.0f; // large number
                }
            }

            // pick the smaller one
            float t = (tx < ty) ? tx : ty;
            Vector2 exitpos = pos + dir * t;

            
            // clamp to intersected edge to avoid roundoff errors
            if (tx<ty) // intersected on X
            {
                if (dir.X < 0.0f)
                {
                    nx--;
                    exitpos.X = (float)Math.Floor(pos.X) - 0.1f;
                }
                else
                {
                    if (dir.X > 0.0f)
                    {
                        nx++;
                        exitpos.X = (float)Math.Floor(pos.X) + 1.1f;
                    }
                }
            }
            else
            {
                if (dir.Y < 0.0f)
                {
                    ny--;
                    exitpos.Y = (float)Math.Floor(pos.Y)-0.1f;
                }
                else
                {
                    if (dir.Y > 0.0f)
                    {
                        ny++;
                        exitpos.Y = (float)Math.Floor(pos.Y) + 1.1f;
                    }
                }
            }
            return exitpos;
        }



        /*
        public static Vector2 IntersectTile(this Vector2 pos, Vector2 dir)
        {
            return TileMath.TileIntersect(pos, dir);
        }*/

        public static int CellIndex(this Vector2 pos, int width, int height)
        {
            return ((int)Math.Floor(pos.X)).Wrap(width) + ((int)Math.Floor(pos.Y)).Wrap(height) * width;
        }

        public static int CellX(this Vector2 pos, int width)
        {
            return ((int)Math.Floor(pos.X)).Wrap(width);
        }
        public static int CellY(this Vector2 pos, int height)
        {
            return ((int)Math.Floor(pos.Y)).Wrap(height);
        }
    }
}

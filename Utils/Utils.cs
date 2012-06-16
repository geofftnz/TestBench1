using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Utils
{
    public static class Utils
    {
        public static T ClampInclusive<T>(this T x, T min, T max) where T : IComparable<T>
        {
            return (x.CompareTo(min) >= 0) ? ((x.CompareTo(max) <= 0) ? x : max) : min;
        }

        public static Color ToColor(this Vector3 v)
        {
            return new Color(v.X * 0.5f + 0.5f, v.Y * 0.5f + 0.5f, v.Z * 0.5f + 0.5f,1.0f);
        }

        public static Color NormalToSphericalColor(this Vector3 v)
        {
            //float r = (float)Math.Sqrt(v.X*v.X+ v.Y*v.Y+ v.Z*v.Z);
            float theta = (float)(Math.Acos(v.Y) / Math.PI);
            float rho = (float)((Math.Atan2(v.Z, v.X) + Math.PI) / (Math.PI));

            return new Color(theta,rho,0f);
        }

        public static float Wrap(this float x, float max) 
        {
            while (x < 0f) x += max;
            while (x >= max) x -= max;
            return x;
        }

        public static int Wrap(this int x, int max)
        {
            while (x < 0) x += max;
            while (x >= max) x -= max;
            return x;
        }
        
    }
}

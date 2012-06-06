using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerrainEngine;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;


namespace TerrainTest
{
    [TestClass]
    public class PatchTest
    {
        private float[] Get4x4Heights()
        {
            var f = new float[16];
            /*
             * 0  1  2  3 
             * 4  5  6  7
             * 8  9  10 11
             * 12 13 14 15
             * */

            // set some data in f
            for (int i = 0; i < 16; i++)
            {
                f[i] = (float)i;
            }
            return f;
        }


        [TestMethod]
        public void setdata_sets_data_from_the_top_left_of_a_larger_array()
        {

            var p = new Patch(3, 3);
            var f = Get4x4Heights();

            p.SetData(f, 4, 4, 0, 0, new Vector2(0f, 0f));

            Assert.IsTrue(Math.Abs(45 - p.Vertices.Select(v => v.Position.Y).Sum()) < 0.1, string.Format("expected sum of 45, got {0}", p.Vertices.Select(v => v.Position.Y).Sum()));
        }
        [TestMethod]
        public void setdata_sets_data_from_the_bottom_right_of_a_larger_array()
        {

            var p = new Patch(3, 3);
            var f = Get4x4Heights();

            p.SetData(f, 4, 4, 1, 1, new Vector2(0f, 0f));

            Assert.IsTrue(Math.Abs(90 - p.Vertices.Select(v => v.Position.Y).Sum()) < 0.1, string.Format("expected sum of 90, got {0}", p.Vertices.Select(v => v.Position.Y).Sum()));
        }
        [TestMethod]
        public void setdata_sets_texcoords_correctly_for_top_left_patch()
        {
            var p = new Patch(3, 3);
            var f = Get4x4Heights();

            p.SetData(f, 4, 4, 1, 1, new Vector2(0f, 0f));

            Assert.IsTrue(Math.Abs(p.Vertices[0].TextureCoordinate.X) < 0.001f);
            Assert.IsTrue(Math.Abs(p.Vertices[0].TextureCoordinate.Y) < 0.001f);

            Assert.IsTrue(Math.Abs((2f / 256f) - p.Vertices[8].TextureCoordinate.X) < 0.001f);
            Assert.IsTrue(Math.Abs((2f / 256f) - p.Vertices[8].TextureCoordinate.Y) < 0.001f);
        }

        [TestMethod]
        public void setdata_sets_texcoords_correctly_for_offset_patch()
        {
            var p = new Patch(3, 3);
            var f = Get4x4Heights();

            p.SetData(f, 4, 4, 1, 1, new Vector2(0.1f, 0.2f));

            Assert.IsTrue(Math.Abs(0.1f-p.Vertices[0].TextureCoordinate.X) < 0.001f);
            Assert.IsTrue(Math.Abs(0.2f-p.Vertices[0].TextureCoordinate.Y) < 0.001f);

            Assert.IsTrue(Math.Abs((2f / 256f + 0.1f) - p.Vertices[8].TextureCoordinate.X) < 0.001f);
            Assert.IsTrue(Math.Abs((2f / 256f + 0.2f) - p.Vertices[8].TextureCoordinate.Y) < 0.001f);
        }

        [TestMethod]
        public void setupindices_works()
        {
            var p = new Patch(3, 3);
            p.SetupIndices();

            Assert.AreEqual(3, p.Index[0]);
            Assert.AreEqual(0, p.Index[1]);
            Assert.AreEqual(4, p.Index[2]);
            Assert.AreEqual(1, p.Index[3]);

            Assert.AreEqual(7, p.Index[8]);
            Assert.AreEqual(4, p.Index[9]);
            Assert.AreEqual(8, p.Index[10]);
            Assert.AreEqual(5, p.Index[11]);
        }

    }
}

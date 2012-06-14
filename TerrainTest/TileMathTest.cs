using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Utils;

namespace TerrainTest
{
    [TestClass]
    public class TileMathTest
    {
        /*
        [TestMethod]
        public void test_fall_vector_returns_up_when_normal_is_up()
        {
            Assert.AreEqual(new Vector3(0,1,0),TileMath.FallVector(new Vector3(0,1,0), new Vector3(0,1,0)));
        }

        [TestMethod]
        public void test_fall_vector_returns_expected()
        {
            Assert.AreEqual(new Vector3(0, 0, -1), TileMath.FallVector(new Vector3(0, 1, 0), new Vector3(0, 0, 1)));
        }*/
        /*
        [TestMethod]
        public void test_tile_intersect_returns_pos_for_zero_dir()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.5f), TileMath.TileIntersect(new Vector2(0.5f, 0.5f), new Vector2(0f)));
        }

        [TestMethod]
        public void test_tile_intersect_returns_expected_on_up()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.0f), TileMath.TileIntersect(new Vector2(0.5f, 0.5f), new Vector2(0.0f,-1f)));
        }
        [TestMethod]
        public void test_tile_intersect_returns_expected_on_down()
        {
            Assert.AreEqual(new Vector2(0.5f, 1.0f), TileMath.TileIntersect(new Vector2(0.5f, 0.5f), new Vector2(0.0f, 1f)));
        }
        [TestMethod]
        public void test_tile_intersect_returns_expected_on_diagonal()
        {
            Assert.AreEqual(new Vector2(0.0f, 0.0f), TileMath.TileIntersect(new Vector2(0.5f, 0.5f), new Vector2(-0.2f, -0.2f)));
        }
        
        [TestMethod]
        public void test_cell_index_1()
        {
            Assert.AreEqual(0, new Vector2(0.5f, 0.5f).CellIndex(8, 8));
            Assert.AreEqual(9, new Vector2(1.5f, 1.5f).CellIndex(8, 8));
            Assert.AreEqual(0, new Vector2(8.5f, 0.5f).CellIndex(8, 8));
            Assert.AreEqual(0, new Vector2(0.5f, 8.5f).CellIndex(8, 8));
            Assert.AreEqual(8*8-1, new Vector2(-0.5f, -0.5f).CellIndex(8, 8));
        }*/

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerrainGeneration
{
    public interface ITerrainGen
    {
        void ModifyTerrain();
        void Load(string filename);
        void Save(string filename);
    }
}

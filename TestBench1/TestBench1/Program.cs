using System;

namespace TestBench1
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            //using (Game1 game = new Game1())
            //{
            //    game.Run();
            //}
            using (var game = new TerrainGeneration.TerrainGenerationVisualiser())
            //using (var game = new Game1())
            {
                game.Run();
            }
        }
    }
#endif
}


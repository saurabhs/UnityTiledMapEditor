using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration.Utils
{
    public class Constants
    {
        public static Vector2 TILE_SIZE = new Vector2( 25f, 25f );
        public static Vector2 MAP_EDITOR_SHOWCASE_GRID = new Vector2( 15, 8 );
        public static Vector3 DEFAULT_LOCAL_SCALE = new Vector3( 25f, 25f, 25f );
        public static Vector3 TREE_LOCAL_SCALE = new Vector3( 8.3f, 8.3f, 8.3f );

        public static float NATURE_PACK_SCALING = 32.5f;
    }

    [System.Serializable]
    public class TilesetData
    {
        public string prefix = string.Empty;
        public int firstgid = -1;
        public int tilecount = -1;
    }

    [System.Serializable]
    public class Coord
    {
        public int x;
        public int y;

        public Coord()
        {
            x = 0;
            y = 0;
        }

        public Coord( int x_, int y_ )
        {
            x = x_;
            y = y_;
        }

        public override string ToString()
        {
            return $"{x}, {y}";
        }
    }

    [System.Serializable]
    public class MapData
    {
        public int height = -1;
        public int width = -1;
        public int[][] data = null;
        public List<TilesetData> tilesetData = null;
    }
}
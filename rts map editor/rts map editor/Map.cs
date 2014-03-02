using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace rts_map_editor
{
    public class Map
    {
        //public List<MapTile> Walls = new List<MapTile>();

        MapTile[,] tiles;
        int tileSize = 20;
        int width, height;

        public List<BaseObject> Resources = new List<BaseObject>();
        public List<BaseObject> StartingPoints = new List<BaseObject>();

        public Map(string mapFilepath)
        {
            loadMap(mapFilepath);
            calculateTileNeighbors();
        }

        public Map(int width, int height)
        {
            createMap(width, height);
            calculateTileNeighbors();
        }

        void loadMap(string mapFilePath)
        {
            string[] lines = File.ReadAllLines(mapFilePath);

            string[] widthAndHeight = lines[0].Split(' ');
            width = int.Parse(widthAndHeight[0]);
            height = int.Parse(widthAndHeight[1]);
            int numberOfResources = int.Parse(widthAndHeight[2]);
            int numberOfStartingPoints = int.Parse(widthAndHeight[3]);

            tiles = new MapTile[height, width];

            for (int i = 0; i < height; i++)
            {
                string[] rowOfTiles = lines[i + 1].Split(' ');

                for (int s = 0; s < width; s++)
                {
                    int typeCode = int.Parse(rowOfTiles[s].Substring(0, 1));
                    int pathingCode = int.Parse(rowOfTiles[s].Substring(1, 1));
                    tiles[i, s] = new MapTile(s, i, tileSize, tileSize, typeCode, pathingCode);
                    //if (pathingCode == 1)
                    //    Walls.Add(tiles[i - 1, s]);
                }
            }

            for (int i = height + 1; i < height + 1 + numberOfResources; i++)
            {
                string[] resourceParams = lines[i].Split(' ');

                if (resourceParams[0] == "0")
                {
                    BaseObject resource = new BaseObject(new Rectangle(int.Parse(resourceParams[1]) * TileSize, int.Parse(resourceParams[2]) * TileSize, 3 * TileSize, 3 * TileSize));
                    Resources.Add(resource );
                }
            }

            for (int i = height + 1 + numberOfResources; i < height + 1 + numberOfResources + numberOfStartingPoints; i++)
            {
                string[] resourceParams = lines[i].Split(' ');

                BaseObject startingPoint = new BaseObject(new Rectangle(int.Parse(resourceParams[0]) * TileSize, int.Parse(resourceParams[1]) * TileSize, 5 * TileSize, 5 * TileSize));
                StartingPoints.Add(startingPoint);
            }

            //Util.SortByX(Walls);
        }

        void createMap(int width, int height)
        {
            this.width = width;
            this.height = height;

            tiles = new MapTile[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tiles[y, x] = new MapTile(x, y, tileSize, tileSize, 0, 0);
                }
            }

            for (int x = 0; x < width; x++)
            {
                tiles[0, x].Type = 1;
                tiles[0, x].Walkable = false;

                tiles[height - 1, x].Type = 1;
                tiles[height - 1, x].Walkable = false;
            }

            for (int y = 0; y < height; y++)
            {
                tiles[y, 0].Type = 1;
                tiles[y, 0].Walkable = false;

                tiles[y, width - 1].Type = 1;
                tiles[y, width - 1].Walkable = false;
            }
        }

        public void SaveMap(string mapFilePath)
        {
            List<string> lines = new List<string>();

            lines.Add(width + " " + height + " " + Resources.Count + " " + StartingPoints.Count);

            for (int y = 0; y < height; y++)
            {
                string line = "";
                for (int x = 0; x < width; x++)
                {
                    line += tiles[y, x].Type.ToString();
                    if (tiles[y, x].Walkable)
                        line += "0";
                    else
                        line += "1";

                    if (x < width - 1)
                        line += " ";
                }
                lines.Add(line);
            }

            foreach (BaseObject r in Resources)
            {
                lines.Add(r.Type + " " + r.Rectangle.X / tileSize + " " + r.Rectangle.Y / tileSize);
            }

            foreach (BaseObject s in StartingPoints)
            {
                lines.Add(s.X / tileSize + " " + s.Y / tileSize);
            }

            if (!Directory.Exists("C:\\rts maps\\"))
            {
                Directory.CreateDirectory("C:\\rts maps\\");
            }

            File.WriteAllLines(mapFilePath, lines);
        }

        public void MirrorLeftToRight()
        {
            Queue<MapTile> tileQueue = new Queue<MapTile>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width / 2; x++)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = Width - 1; x >= Width / 2; x--)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }

            List<BaseObject> resourcesToAdd = new List<BaseObject>();
            for (int i = 0; i < Resources.Count; )
            {
                BaseObject r = Resources[i];

                if (r.X <= Width * TileSize / 2 - r.Width)
                {
                    BaseObject resource = new BaseObject(new Rectangle(Width * TileSize - r.X - r.Width, r.Y, r.Width, r.Height));
                    resource.Type = r.Type;
                    resourcesToAdd.Add(resource);

                    i++;
                }
                else
                    Resources.Remove(r);
            }

            foreach (BaseObject r in resourcesToAdd)
                Resources.Add(r);

            List<BaseObject> startingPointsToAdd = new List<BaseObject>();
            for (int i = 0; i < StartingPoints.Count; )
            {
                BaseObject s = StartingPoints[i];

                if (s.X <= Width * TileSize / 2 - s.Width)
                {
                    BaseObject startingPoint = new BaseObject(new Rectangle(Width * TileSize - s.X - s.Width, s.Y, s.Width, s.Height));
                    startingPointsToAdd.Add(startingPoint);

                    i++;
                }
                else
                    StartingPoints.Remove(s);
            }

            foreach (BaseObject s in startingPointsToAdd)
                StartingPoints.Add(s);
        }

        /*public void MirrorRightToLeft()
        {
            Queue<MapTile> tileQueue = new Queue<MapTile>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = Width - 1; x >= Width / 2; x--)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width / 2; x++)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }
        }*/

        public void MirrorTopToBottom()
        {
            Queue<MapTile> tileQueue = new Queue<MapTile>();

            for (int y = 0; y < Height / 2; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = Height - 1; y >= Height / 2; y--)
            {
                for (int x = 0; x < Width; x++)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }

            List<BaseObject> resourcesToAdd = new List<BaseObject>();
            for (int i = 0; i < Resources.Count; )
            {
                BaseObject r = Resources[i];

                if (r.Y <= Height * TileSize / 2 - r.Height)
                {
                    BaseObject resource = new BaseObject(new Rectangle(r.X, Height * TileSize - r.Y - r.Height, r.Width, r.Height));
                    resource.Type = r.Type;
                    resourcesToAdd.Add(resource);

                    i++;
                }
                else
                    Resources.Remove(r);
            }

            foreach (BaseObject r in resourcesToAdd)
                Resources.Add(r);

            List<BaseObject> startingPointsToAdd = new List<BaseObject>();
            for (int i = 0; i < StartingPoints.Count; )
            {
                BaseObject s = StartingPoints[i];

                if (s.Y <= Height * TileSize / 2 - s.Height)
                {
                    BaseObject startingPoint = new BaseObject(new Rectangle(s.X, Height * TileSize - s.Y - s.Height, s.Width, s.Height));
                    startingPointsToAdd.Add(startingPoint);

                    i++;
                }
                else
                    StartingPoints.Remove(s);
            }

            foreach (BaseObject s in startingPointsToAdd)
                StartingPoints.Add(s);
        }

        /*public void MirrorBottomToTop()
        {
            Queue<MapTile> tileQueue = new Queue<MapTile>();

            for (int y = Height - 1; y >= Height / 2; y--)
            {
                for (int x = 0; x < Width; x++)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = 0; y < Height / 2; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }
        }*/

        public void RotateLeftToRight()
        {
            Queue<MapTile> tileQueue = new Queue<MapTile>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width / 2; x++)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = Height - 1; y >= 0; y--)
            {
                for (int x = Width - 1; x >= Width / 2; x--)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }

            List<BaseObject> resourcesToAdd = new List<BaseObject>();
            for (int i = 0; i < Resources.Count; )
            {
                BaseObject r = Resources[i];

                if (r.X <= Width * TileSize / 2 - r.Width)
                {
                    BaseObject resource = new BaseObject(new Rectangle(Width * TileSize - r.X - r.Width, Height * TileSize - r.Y - r.Height, r.Width, r.Height));
                    resource.Type = r.Type;
                    resourcesToAdd.Add(resource);

                    i++;
                }
                else
                    Resources.Remove(r);
            }

            foreach (BaseObject r in resourcesToAdd)
                Resources.Add(r);

            List<BaseObject> startingPointsToAdd = new List<BaseObject>();
            for (int i = 0; i < StartingPoints.Count; )
            {
                BaseObject s = StartingPoints[i];

                if (s.X <= Width * TileSize / 2 - s.Width)
                {
                    BaseObject startingPoint = new BaseObject(new Rectangle(Width * TileSize - s.X - s.Width, Height * TileSize - s.Y - s.Height, s.Width, s.Height));
                    startingPointsToAdd.Add(startingPoint);

                    i++;
                }
                else
                    StartingPoints.Remove(s);
            }

            foreach (BaseObject s in startingPointsToAdd)
                StartingPoints.Add(s);
        }

        /*public void RotateRightToLeft()
        {
            Queue<MapTile> tileQueue = new Queue<MapTile>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = Width / 2; x < Width; x++)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = Height - 1; y >= 0; y--)
            {
                for (int x = Width / 2 - 1; x >= 0; x--)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }
        }*/

        public void RotateTopToBottom()
        {
            Queue<MapTile> tileQueue = new Queue<MapTile>();

            for (int y = 0; y < Height / 2; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = Height - 1; y >= Height / 2; y--)
            {
                for (int x = Width - 1; x >= 0; x--)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }

            List<BaseObject> resourcesToAdd = new List<BaseObject>();
            for (int i = 0; i < Resources.Count; )
            {
                BaseObject r = Resources[i];

                if (r.Y <= Height * TileSize / 2 - r.Height)
                {
                    BaseObject resource = new BaseObject(new Rectangle(Width * TileSize - r.X - r.Width, Height * TileSize - r.Y - r.Height, r.Width, r.Height));
                    resource.Type = r.Type;
                    resourcesToAdd.Add(resource);

                    i++;
                }
                else
                    Resources.Remove(r);
            }

            foreach (BaseObject r in resourcesToAdd)
                Resources.Add(r);

            List<BaseObject> startingPointsToAdd = new List<BaseObject>();
            for (int i = 0; i < StartingPoints.Count; )
            {
                BaseObject s = StartingPoints[i];

                if (s.Y <= Height * TileSize / 2 - s.Height)
                {
                    BaseObject startingPoint = new BaseObject(new Rectangle(Width * TileSize - s.X - s.Width, Height * TileSize - s.Y - s.Height, s.Width, s.Height));
                    startingPointsToAdd.Add(startingPoint);

                    i++;
                }
                else
                    StartingPoints.Remove(s);
            }

            foreach (BaseObject s in startingPointsToAdd)
                StartingPoints.Add(s);
        }

        /*public void RotateBottomToTop()
        {
            Queue<MapTile> tileQueue = new Queue<MapTile>();

            for (int y = Height / 2; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = Height / 2- 1; y >= 0; y--)
            {
                for (int x = Width - 1; x >= 0; x--)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }
        }*/

        public void Rotate()
        {
            Queue<MapTile> tileQueue = new Queue<MapTile>();

            // top left to top right
            /*for (int y = 0; y < Height / 2; y++)
            {
                for (int x = 0; x < Width / 2; x++)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = 0; y < Height / 2; y++)
            {
                for (int x = Width - 1; x >= Width / 2; x--)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }

            // top left to bottom left
            for (int y = 0; y < Height / 2; y++)
            {
                for (int x = 0; x < Width / 2; x++)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = Height - 1; y >= Height / 2; y--)
            {
                for (int x = 0; x < Width / 2; x++)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }

            // top right to bottom right
            // top left to bottom left
            for (int y = 0; y < Height / 2; y++)
            {
                for (int x = Width / 2; x < Width - 1; x++)
                {
                    tileQueue.Enqueue(Tiles[y, x]);
                }
            }

            for (int y = Height - 1; y >= Height / 2; y--)
            {
                for (int x = Width / 2; x < Width - 1; x++)
                {
                    MapTile tile = tileQueue.Dequeue();

                    Tiles[y, x].Type = tile.Type;
                    Tiles[y, x].Walkable = tile.Walkable;
                }
            }*/

            MirrorLeftToRight();
            MirrorTopToBottom();
        }

        void calculateTileNeighbors()
        {
            for (int i = 0; i < height; i++)
            {
                for (int s = 0; s < width; s++)
                {
                    MapTile tile = tiles[i, s];

                    if (i - 1 >= 0)
                    {
                        MapTile neighbor = tiles[i - 1, s];
                        tile.Neighbors.Add(neighbor);
                    }
                    if (i + 1 < height)
                    {
                        MapTile neighbor = tiles[i + 1, s];
                        tile.Neighbors.Add(neighbor);
                    }
                    if (s - 1 >= 0)
                    {
                        MapTile neighbor = tiles[i, s - 1];
                        tile.Neighbors.Add(neighbor);
                    }
                    if (s + 1 < width)
                    {
                        MapTile neighbor = tiles[i, s + 1];
                        tile.Neighbors.Add(neighbor);
                    }
                    if (i - 1 >= 0 && s - 1 >= 0)
                    {
                        MapTile neighbor = tiles[i - 1, s - 1];
                        tile.Neighbors.Add(neighbor);
                    }
                    if (i - 1 >= 0 && s + 1 < width)
                    {
                        MapTile neighbor = tiles[i - 1, s + 1];
                        tile.Neighbors.Add(neighbor);
                    }
                    if (i + 1 < height && s - 1 >= 0)
                    {
                        MapTile neighbor = tiles[i + 1, s - 1];
                        tile.Neighbors.Add(neighbor);
                    }
                    if (i + 1 < height && s + 1 < width)
                    {
                        MapTile neighbor = tiles[i + 1, s + 1];
                        tile.Neighbors.Add(neighbor);
                    }
                }
            }
        }

        // find centerpoint of nearest walkable tile from given vector
        public Vector2 FindNearestWalkableTile(Vector2 point)
        {
            int y = (int)MathHelper.Clamp(point.Y / tileSize, 0, height - 1);
            int x = (int)MathHelper.Clamp(point.X / tileSize, 0, width - 1);
            MapTile tile = tiles[y, x];

            if (tile.Walkable)
                return tile.CenterPoint;

            MapTile neighbor;

            // find nextdoor neighbor closer to given vector
            float howFarLeft = tile.CenterPoint.X - point.X;
            float howFarRight = point.X - tile.CenterPoint.X;
            float howFarUp = tile.CenterPoint.Y - point.Y;
            float howFarDown = point.Y - tile.CenterPoint.Y;

            float biggest = 0;

            if (howFarLeft > biggest)
                biggest = howFarLeft;
            if (howFarRight > biggest)
                biggest = howFarRight;
            if (howFarUp > biggest)
                biggest = howFarUp;
            if (howFarDown > biggest)
                biggest = howFarDown;

            if (howFarLeft == biggest && x - 1 >= 0)
            {
                neighbor = tiles[y, x - 1];
                if (neighbor.Walkable)
                    return neighbor.CenterPoint;
            }
            else if (howFarRight == biggest && x + 1 < width)
            {
                neighbor = tiles[y, x + 1];
                if (neighbor.Walkable)
                    return neighbor.CenterPoint;
            }
            else if (howFarUp == biggest && y - 1 >= 0)
            {
                neighbor = tiles[y - 1, x];
                if (neighbor.Walkable)
                    return neighbor.CenterPoint;
            }
            else if (howFarDown == biggest && y + 1 < height)
            {
                neighbor = tiles[y + 1, x];
                if (neighbor.Walkable)
                    return neighbor.CenterPoint;
            }

            // find next closest neighbor
            for (int i = 0; ; i++)
            {
                if (y - i >= 0)
                {
                    neighbor = tiles[y - i, x];
                    if (neighbor.Walkable)
                        return neighbor.CenterPoint;
                }
                if (y + i < height)
                {
                    neighbor = tiles[y + i, x];
                    if (neighbor.Walkable)
                        return neighbor.CenterPoint;
                }
                if (x - i >= 0)
                {
                    neighbor = tiles[y, x - i];
                    if (neighbor.Walkable)
                        return neighbor.CenterPoint;
                }
                if (x + i < width)
                {
                    neighbor = tiles[y, x + i];
                    if (neighbor.Walkable)
                        return neighbor.CenterPoint;
                }
                if (y - i >= 0 && x - i >= 0)
                {
                    neighbor = tiles[y - i, x - i];
                    if (neighbor.Walkable)
                        return neighbor.CenterPoint;
                }
                if (y - i >= 0 && x + i < width)
                {
                    neighbor = tiles[y - i, x + i];
                    if (neighbor.Walkable)
                        return neighbor.CenterPoint;
                }
                if (y + i < height && x - i >= 0)
                {
                    neighbor = tiles[y + i, x - i];
                    if (neighbor.Walkable)
                        return neighbor.CenterPoint;
                }
                if (y + i < height && x + i < width)
                {
                    neighbor = tiles[y + i, x + i];
                    if (neighbor.Walkable)
                        return neighbor.CenterPoint;
                }
            }
        }

        public int Width
        {
            get
            {
                return width;
            }
        }
        public int Height
        {
            get
            {
                return height;
            }
        }
        public MapTile[,] Tiles
        {
            get
            {
                return tiles;
            }
        }
        public int TileSize
        {
            get
            {
                return tileSize;
            }
        }
    }

    public class MapTile : BaseObject
    {
        new public readonly int X, Y;
        public int Type;
        public bool Walkable;
        public readonly float CollisionRadius;
        public bool Visible = false;

        public List<MapTile> Neighbors = new List<MapTile>();

        public MapTile(int x, int y, int width, int height, int typeCode, int pathingCode)
            : base(new Rectangle(x * width, y * height, width, height))
        {
            X = x;
            Y = y;
            Type = typeCode;
            Walkable = (pathingCode == 0);
            CollisionRadius = width / 2f;
        }

        /*public bool IntersectsUnit(Unit u)
        {
            return Vector2.Distance(centerPoint, u.CenterPoint) < (CollisionRadius + u.Radius);
        }*/
    }
}

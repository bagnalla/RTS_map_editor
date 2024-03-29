﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace rts_map_editor
{
    public class ResourceType
    {
        public static readonly ResourceType
            Roks;

        static ResourceType()
        {
            Roks = new ResourceType();
            Roks.Name = "Roks";
            Roks.NormalTexture = Game1.Game.Content.Load<Texture2D>("WC2Gold");
            Roks.DepletedTexture = Game1.Game.Content.Load<Texture2D>("WC2Gold");
            Roks.CargoTexture = Game1.Game.Content.Load<Texture2D>("rock");
            Roks.AmountOfResources = 500;
            Roks.Size = 3;
        }

        public Texture2D NormalTexture, DepletedTexture, CargoTexture;

        public int AmountOfResources { get; private set; }

        public string Name { get; private set; }

        public int Size { get; private set; }
    }

    public abstract class Resource : BaseObject
    {
        public static Map Map;
        //public static PathFinder PathFinder;

        public static List<Resource> Resources { get; private set; }
        public ResourceType Type { get; private set; }

        //public List<PathNode> OccupiedPathNodes = new List<PathNode>();

        int amount;
        new public int X, Y;
        int Size;
        public float Radius { get; private set; }
        public bool Depleted { get; protected set; }

        static Resource()
        {
            Resources = new List<Resource>();
        }

        public Resource(ResourceType type, Point location, int size)
            : base(new Rectangle(location.X * Map.TileSize, location.Y * Map.TileSize, size * Map.TileSize, size * Map.TileSize))
        {
            Type = type;
            Texture = type.NormalTexture;
            Amount = type.AmountOfResources;
            Size = size;
            Radius = (size * Map.TileSize) / 2f;

            X = location.X;
            Y = location.Y;

            //setOccupiedPathNodes();

            Resources.Add(this);
        }

        /*void setOccupiedPathNodes()
        {
            for (int x = X; x < X + Size; x++)
            {
                for (int y = Y; y < Y + Size; y++)
                {
                    PathNode node = PathFinder.PathNodes[y, x];
                    if (Intersects(node.Tile))
                    {
                        OccupiedPathNodes.Add(node);
                        node.Blocked = true;
                        node.Blocker = this;
                    }
                }
            }
        }*/

        protected virtual void deplete()
        {
            Depleted = true;

            //foreach (PathNode pathNode in OccupiedPathNodes)
            //{
            //    pathNode.Blocked = false;
            //    pathNode.Blocker = null;
            //}
        }

        public static void UpdateResources(GameTime gameTime)
        {
            for (int i = 0; i < Resources.Count; )
            {
                Resource r = Resources[i];
                if (r.Depleted)
                {
                    RemoveResource(r);
                }
                else
                {
                    r.Update(gameTime);
                    i++;
                }
            }
        }

        protected virtual void Update(GameTime gameTime)
        {
        }

        public static void RemoveResource(Resource r)
        {
            Resources.Remove(r);

            Roks roks = r as Roks;
            if (roks != null)
                Roks.AllRoks.Remove(roks);
        }

        public int Amount
        {
            get
            {
                return amount;
            }
            set
            {
                amount = (int)MathHelper.Max(value, 0);
                if (!Depleted && amount == 0)
                    deplete();
            }
        }
        public string Name
        {
            get
            {
                return Type.Name;
            }
        }
    }

    public class Roks : Resource
    {
        public static List<Roks> AllRoks { get; private set; }
        static int allowEntranceDelay = 250, harvestDelay = 1000;

        public const int CARGO_PER_TRIP = 1;

        static Roks()
        {
            AllRoks = new List<Roks>();
        }

        //List<WorkerNublet> workersInside = new List<WorkerNublet>();
        List<int> workerTimes = new List<int>();

        public Roks(Point location)
            : base(ResourceType.Roks, location, ResourceType.Roks.Size)
        {
            AllRoks.Add(this);
        }

        int timeSinceLastEntrance = allowEntranceDelay;
        protected override void Update(GameTime gameTime)
        {
            timeSinceLastEntrance += (int)gameTime.ElapsedGameTime.TotalMilliseconds;

            /*for (int i = 0; i < workersInside.Count; )
            {
                workerTimes[i] += (int)gameTime.ElapsedGameTime.TotalMilliseconds;

                if (workerTimes[i] >= harvestDelay)
                {
                    releaseWorker(workersInside[i]);
                    workerTimes.Remove(workerTimes[i]);
                }
                else
                    i++;
            }*/
        }

        /*public bool CheckForEntrance(WorkerNublet worker)
        {
            if (worker.CargoAmount == CARGO_PER_TRIP && worker.CargoType == Type)
            {
                TownHall townHall = findNearestTownHall(worker);
                if (townHall != null)
                    worker.InsertCommand(new ReturnCargoCommand(townHall, this, 1));
                else
                    worker.NextCommand();
                return false;
            }

            if (Amount - (workersInside.Count * CARGO_PER_TRIP) <= 0)
                return false;

            if (timeSinceLastEntrance >= allowEntranceDelay)
            {
                timeSinceLastEntrance = 0;

                letWorkerEnter(worker);

                return true;
            }

            return false;
        }

        void letWorkerEnter(WorkerNublet worker)
        {
            workersInside.Add(worker);
            workerTimes.Add(0);
            worker.CenterPoint = centerPoint;
        }

        void releaseWorker(WorkerNublet worker)
        {
            worker.CargoType = Type;

            int oldAmount = Amount;
            Amount -= CARGO_PER_TRIP;
            worker.CargoAmount = oldAmount - Amount;

            //int newAmount = (int)(MathHelper.Max(Amount - CARGO_PER_TRIP, 0));
            //worker.CargoAmount = Amount - newAmount;
            //Amount = newAmount;

            workersInside.Remove(worker);

            TownHall townHall = findNearestTownHall(worker);

            float angle;
            if (townHall == null)
                angle = 0;
            else
                angle = (float)Math.Atan2(townHall.Rectangle.Y - CenterPoint.Y, townHall.Rectangle.X - CenterPoint.X);

            worker.CenterPoint = centerPoint + new Vector2((Radius + worker.Radius) * (float)Math.Cos(angle), (Radius + worker.Radius) * (float)Math.Sin(angle));
            worker.Rotation = angle;

            worker.FinishHarvesting();

            if (townHall != null && worker.Commands.Count <= 0)
            {
                //worker.GiveCommand(new ReturnCargoCommand(townHall, 1));
                //worker.QueueCommand(new HarvestCommand(this, 1));
                worker.ReturnCargoToNearestTownHall(this);
            }
        }

        protected override void deplete()
        {
            base.deplete();

            //foreach (WorkerNublet worker in workersInside)
            //{
            //
            //}
        }

        TownHall findNearestTownHall(Unit unit)
        {
            TownHall nearestTownHall = null;
            float nearest = int.MaxValue;

            foreach (TownHall townHall in TownHall.TownHalls)
            {
                if (townHall.Team != unit.Team || townHall.UnderConstruction)
                    continue;

                float distance = Vector2.DistanceSquared(CenterPoint, townHall.CenterPoint);
                if (distance < nearest)
                {
                    nearestTownHall = townHall;
                    nearest = distance;
                }
            }

            return nearestTownHall;
        }*/

        public override Texture2D Texture
        {
            get
            {
                if (!Depleted)
                    return Type.NormalTexture;
                else
                    return Type.DepletedTexture;
            }
            set
            {
                base.Texture = value;
            }
        }
    }
}

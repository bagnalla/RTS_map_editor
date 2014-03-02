using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;
using System.Windows.Forms;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace rts_map_editor
{
    class Editor : GameState
    {
        static bool contentLoaded = false;

        Form winForm;
        static Cursor normalCursor;

        MouseState mouseState;
        KeyboardState keyboardState, oldKeyboardState;

        State state = State.PlacingTile;

        Camera camera;
        float cameraScrollSpeed = 1000, cameraZoomSpeed = 1, cameraRotationSpeed = 4.5f, cameraRotationTarget, cameraRotationIncrement = MathHelper.PiOver2;//MathHelper.PiOver4 / 2;

        static SpriteFont tileSelectionFont, buttonFont, smallFont;

        Map map;
        int actualMapWidth, actualMapHeight;
        static Texture2D boulder1Texture, tree1Texture, roksTexture, startingPointTexture, eraserTexture;
        static Texture2D whiteBoxTexture;
        static Texture2D mirrorLeftToRightTexture, mirrorTopToBottomTexture;
        static Texture2D rotateLeftToRightTexture, rotateTopToBottomTexture;
        static Texture2D rotateTexture;

        Rectangle minimap;
        int minimapSize = 125, minimapBorderSize = 5;
        int minimapPosX, minimapPosY;
        float minimapToMapRatioX, minimapToMapRatioY;
        float minimapToScreenRatioX, minimapToScreenRatioY;
        BaseObject minimapScreenIndicatorBox;
        PrimitiveLine minimapScreenIndicatorBoxLine;
        PrimitiveLine line;

        Viewport worldViewport, uiViewport;

        int selectedTileLeft = 1, selectedTileRight, selectedResource;
        int brushSize = 0;

        SimpleButton saveButton, exitButton;
        SimpleButton mirrorLeftToRightButton, mirrorTopToBottomButton;
        SimpleButton rotateLeftToRightButton, rotateTopToBottomButton;
        SimpleButton rotateButton;
        SimpleButton brushSize0Button, brushSize1Button, brushSize2Button;
        SimpleButton startingPointButton, eraserButton;

        Rectangle saveMenuRectangle;
        SimpleButton saveMenuSaveButton, saveMenuExitButton;

        public Editor(EventHandler callback, int mapWidth, int mapHeight)
            : this(callback, null, mapWidth, mapHeight)
        {
            //map = new Map(@"Content/map1.txt");
            //map = new Map(mapWidth, mapHeight);
        }

        public Editor(EventHandler callback, string mapFilePath)
            : this(callback, mapFilePath, 0, 0)
        {
            //map = new Map(mapFilePath);
        }

        public Editor(EventHandler callback, string mapFilePath, int mapWidth, int mapHeight)
            : base(callback)
        {
            if (mapFilePath != null)
                map = new Map(mapFilePath);
            else
                map = new Map(mapWidth, mapHeight);

            actualMapWidth = map.Width * map.TileSize;
            actualMapHeight = map.Height * map.TileSize;

            uiViewport = GraphicsDevice.Viewport;
            worldViewport = GraphicsDevice.Viewport;
            worldViewport.Height -= (minimapSize + minimapBorderSize * 2);
            GraphicsDevice.Viewport = worldViewport;

            initializeMinimap();

            camera = new Camera();
            camera.Pos = new Vector2(worldViewport.Width / 2, worldViewport.Height / 2);

            if (!contentLoaded)
            {
                normalCursor = Util.LoadCustomCursor(@"Content/SC2-cursor.cur");

                tileSelectionFont = Content.Load<SpriteFont>("tileSelectionFont");
                buttonFont = Content.Load<SpriteFont>("buttonFont");
                smallFont = Content.Load<SpriteFont>("smallFont");

                whiteBoxTexture = Content.Load<Texture2D>("whitebox");
                boulder1Texture = Content.Load<Texture2D>("boulder1");
                tree1Texture = Content.Load<Texture2D>("tree2");
                roksTexture = Content.Load<Texture2D>("WC2Gold");
                startingPointTexture = Content.Load<Texture2D>("HumanTownhall");
                eraserTexture = Content.Load<Texture2D>("eraser");

                mirrorLeftToRightTexture = Content.Load<Texture2D>("rotate left to right");
                mirrorTopToBottomTexture = Content.Load<Texture2D>("rotate top to bottom");
                rotateTexture = Content.Load<Texture2D>("rotate");

                contentLoaded = true;
            }

            initializeTileSelectionAreas();

            winForm = (Form)Form.FromHandle(Game1.Game.Window.Handle);
            //Cursor.Clip = new System.Drawing.Rectangle(winForm.Location, winForm.Size);
            //winForm.Cursor = normalCursor;

            line = new PrimitiveLine(GraphicsDevice, 1);
            line.Colour = Color.White * .5f;
            //line.Alpha = .5f;

            int buttonWidth = 50, buttonHeight = 20;
            int buttonPosY = minimap.Y + minimap.Height - buttonHeight;
            int saveButtonPosX = (int)(minimapPosX + minimap.Width + (resourceSelectionArea.X - minimapPosX - minimap.Width) * .3f - buttonWidth / 2);
            saveButton = new SimpleButton(new Rectangle(saveButtonPosX, buttonPosY, buttonWidth, buttonHeight));
            SimpleButton.AddButton(saveButton);

            int exitButtonPosX = (int)(minimapPosX + minimap.Width + (resourceSelectionArea.X - minimapPosX - minimap.Width) * .6f - buttonWidth / 2);
            exitButton = new SimpleButton(new Rectangle(exitButtonPosX, buttonPosY, buttonWidth, buttonHeight));
            SimpleButton.AddButton(exitButton);

            buttonWidth = buttonWidth / 2 - 1;
            buttonHeight -= 1;

            mirrorLeftToRightButton = new SimpleButton(new Rectangle(saveButtonPosX, minimapPosY + buttonHeight + 2, buttonWidth, buttonHeight), mirrorLeftToRightTexture, null, null);
            SimpleButton.AddButton(mirrorLeftToRightButton);

            mirrorTopToBottomButton = new SimpleButton(new Rectangle(saveButtonPosX + buttonWidth + 2, minimapPosY + buttonHeight + 2, buttonWidth, buttonHeight), mirrorTopToBottomTexture, null, null);
            SimpleButton.AddButton(mirrorTopToBottomButton);

            rotateLeftToRightButton = new SimpleButton(new Rectangle(saveButtonPosX, mirrorLeftToRightButton.Y + buttonHeight + 1, buttonWidth, buttonHeight), mirrorLeftToRightTexture, null, null);
            SimpleButton.AddButton(rotateLeftToRightButton);

            rotateTopToBottomButton = new SimpleButton(new Rectangle(saveButtonPosX + buttonWidth + 2, mirrorLeftToRightButton.Y + buttonHeight + 1, buttonWidth, buttonHeight), mirrorTopToBottomTexture, null, null);
            SimpleButton.AddButton(rotateTopToBottomButton);

            rotateButton = new SimpleButton(new Rectangle(saveButtonPosX, rotateLeftToRightButton.Y + buttonHeight + 1, buttonWidth, buttonHeight), rotateTexture, null, null);
            SimpleButton.AddButton(rotateButton);

            int brushSizePosX = exitButtonPosX + exitButton.Width / 2 - buttonWidth / 2;

            brushSize0Button = new SimpleButton(new Rectangle(brushSizePosX, minimapPosY + 2 + buttonHeight + 1, buttonWidth, buttonHeight), ColorTexture.Black, null, null);
            SimpleButton.AddButton(brushSize0Button);

            brushSize1Button = new SimpleButton(new Rectangle(brushSizePosX, brushSize0Button.Y + buttonHeight + 1, buttonWidth, buttonHeight), ColorTexture.Black, null, null);
            SimpleButton.AddButton(brushSize1Button);

            brushSize2Button = new SimpleButton(new Rectangle(brushSizePosX, brushSize1Button.Y + buttonHeight + 1, buttonWidth, buttonHeight), ColorTexture.Black, null, null);
            SimpleButton.AddButton(brushSize2Button);

            int startingPointButtonPosX = (int)(minimapPosX + minimap.Width + (resourceSelectionArea.X - minimapPosX - minimap.Width) * .825f - buttonWidth / 2);
            startingPointButton = new SimpleButton(new Rectangle(startingPointButtonPosX, resourceSelectionArea.Y + resourceSelectionArea.Height / 3 - buttonHeight / 2, buttonWidth, buttonHeight));
            startingPointButton.Texture = startingPointTexture;
            SimpleButton.AddButton(startingPointButton);

            eraserButton = new SimpleButton(new Rectangle(startingPointButtonPosX, (int)(resourceSelectionArea.Y + resourceSelectionArea.Height * .7f - buttonHeight / 2), buttonWidth, buttonHeight));
            eraserButton.Texture = eraserTexture;
            SimpleButton.AddButton(eraserButton);

            int saveMenuWidth = uiViewport.Width / 3, saveMenuHeight = uiViewport.Height / 3;
            saveMenuRectangle = new Rectangle(uiViewport.Width / 2 - saveMenuWidth / 2, uiViewport.Height / 2 - saveMenuHeight / 2, saveMenuWidth, saveMenuHeight);

            buttonWidth = saveMenuRectangle.Width / 5;
            buttonHeight = saveMenuRectangle.Height / 8;
            int spacing = 5;

            saveMenuSaveButton = new SimpleButton(new Rectangle(saveMenuRectangle.X + spacing, saveMenuRectangle.Y + saveMenuRectangle.Height - buttonHeight - spacing, buttonWidth, buttonHeight));
            SimpleButton.AddButton(saveMenuSaveButton);

            saveMenuExitButton = new SimpleButton(new Rectangle(saveMenuRectangle.X + saveMenuRectangle.Width - buttonWidth - spacing, saveMenuRectangle.Y + saveMenuRectangle.Height - buttonHeight - spacing, buttonWidth, buttonHeight));
            SimpleButton.AddButton(saveMenuExitButton);
        }

        void initializeMinimap()
        {
            minimapPosX = minimapBorderSize;
            minimapPosY = uiViewport.Height - minimapSize - minimapBorderSize;
            minimapToMapRatioX = (float)minimapSize / (map.Width * map.TileSize);
            minimapToMapRatioY = (float)minimapSize / (map.Height * map.TileSize);
            minimapToScreenRatioX = (float)minimapSize / worldViewport.Width;
            minimapToScreenRatioY = (float)minimapSize / worldViewport.Height;
            minimap = new Rectangle(minimapPosX, minimapPosY, minimapSize, minimapSize);
            minimapScreenIndicatorBox = new BaseObject(new Rectangle(0, 0, (int)(worldViewport.Width * minimapToMapRatioX), (int)(worldViewport.Height * minimapToMapRatioY)));

            minimapScreenIndicatorBoxLine = new PrimitiveLine(GraphicsDevice, 1);
            minimapScreenIndicatorBoxLine.Colour = Color.White;
        }

        Rectangle tileSelectionArea1, tileSelectionArea2, resourceSelectionArea;
        List<SimpleButton> tileButtons = new List<SimpleButton>();
        List<SimpleButton> resourceButtons = new List<SimpleButton>();
        int tileButtonSize = 25;
        void initializeTileSelectionAreas()
        {
            // passable tiles
            tileSelectionArea1 = new Rectangle((int)(minimapPosX + minimapSize * 5.75f), minimapPosY, (int)(uiViewport.Width - minimapSize * 5.75f) / 2, minimapSize);

            SimpleButton normalTileButton = new SimpleButton(new Rectangle(tileSelectionArea1.X + 25, tileSelectionArea1.Y + tileSelectionArea1.Height / 2 - tileButtonSize / 2, tileButtonSize, tileButtonSize), ColorTexture.Gray, null, null);
            tileButtons.Add(normalTileButton);
            SimpleButton.AddButton(normalTileButton);

            // impassable tiles
            tileSelectionArea2 = new Rectangle(tileSelectionArea1.X + tileSelectionArea1.Width, minimapPosY, tileSelectionArea1.Width, minimapSize);

            SimpleButton boulderTileButton = new SimpleButton(new Rectangle(tileSelectionArea2.X + 25, normalTileButton.Y, tileButtonSize, tileButtonSize), boulder1Texture, null, null);
            tileButtons.Add(boulderTileButton);
            SimpleButton.AddButton(boulderTileButton);

            SimpleButton treeTileButton = new SimpleButton(new Rectangle(boulderTileButton.X + tileButtonSize + 1, normalTileButton.Y, tileButtonSize, tileButtonSize), tree1Texture, null, null);
            tileButtons.Add(treeTileButton);
            SimpleButton.AddButton(treeTileButton);

            // resources
            resourceSelectionArea = new Rectangle(tileSelectionArea1.X - tileSelectionArea1.Width, tileSelectionArea1.Y, tileSelectionArea1.Width, tileSelectionArea1.Height);

            SimpleButton roksButton = new SimpleButton(new Rectangle(resourceSelectionArea.X + 25, resourceSelectionArea.Y + resourceSelectionArea.Height / 2 - tileButtonSize / 2, tileButtonSize, tileButtonSize), roksTexture, null, null);
            resourceButtons.Add(roksButton);
            SimpleButton.AddButton(roksButton);
        }

        public override void Update(GameTime gameTime)
        {
            // update mouse and keyboard state
            mouseState = Mouse.GetState();
            oldKeyboardState = keyboardState;
            keyboardState = Keyboard.GetState();

            SimpleButton.UpdateAll(mouseState, keyboardState);

            if (state == State.Saving)
            {
                checkSaveMenuButtons();
                checkForSaveMenuTextInput();
            }
            else
            {
                checkButtons();

                checkForLeftClick(gameTime);

                checkForMouseCameraScroll(gameTime);
                checkForCameraZoom(gameTime);
                checkForCameraRotate(gameTime);
                //clampCameraToMap();
            }
        }

        void checkForLeftClick(GameTime gameTime)
        {
            //if (mouseState.LeftButton == ButtonState.Pressed)
            //    allowMiniMapClick = false;
            //else if (mouseState.LeftButton == ButtonState.Released)
            //    allowMiniMapClick = true;

            // clicked on bottom ui
            if (mouseState.Y > worldViewport.Height)
            {
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (minimap.Contains(mouseState.X, mouseState.Y))
                    {
                        Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
                        Vector2 minimapCenterPoint = new Vector2(minimap.X + minimap.Width / 2f, minimap.Y + minimap.Height / 2f);

                        float distance = Vector2.Distance(mousePosition, minimapCenterPoint);
                        float angle = (float)Math.Atan2(mousePosition.Y - minimapCenterPoint.Y, mousePosition.X - minimapCenterPoint.X);

                        mousePosition = new Vector2(minimapCenterPoint.X + distance * (float)Math.Cos(angle - camera.Rotation), minimapCenterPoint.Y + distance * (float)Math.Sin(angle - camera.Rotation));

                        camera.Pos = new Vector2((mousePosition.X - minimapPosX) / minimapToMapRatioX, (mousePosition.Y - minimapPosY) / minimapToMapRatioY);

                        //camera.Pos = new Vector2((mouseState.X - minimapPosX) / minimapToMapRatioX, (mouseState.Y - minimapPosY) / minimapToMapRatioY);
                    }
                }
            }
            // clicked somewhere above bottom ui
            else
            {
                Vector2 mousePosition = Vector2.Transform(new Vector2(mouseState.X, mouseState.Y), Matrix.Invert(camera.get_transformation(worldViewport)));

                if (state == State.PlacingTile)
                    checkForTileClick();
                else if (state == State.PlacingResource)
                {
                    checkForResourcePlacement(mousePosition);

                    if (!blocked)
                    {
                        int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize - 1, 0, map.Width - 3);
                        int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize - 1, 0, map.Height - 3);

                        if (mouseState.LeftButton == ButtonState.Pressed)
                        {
                            BaseObject resource = new BaseObject(new Rectangle(X * map.TileSize, Y * map.TileSize, 3 * map.TileSize, 3 * map.TileSize));
                            resource.Texture = roksTexture;
                            resource.Type = 0;
                            map.Resources.Add(resource);
                            blocked = true;
                        }
                    }
                }
                else if (state == State.PlacingStartingPoint)
                {
                    checkForStartingPointPlacement(mousePosition);

                    if (!blocked)
                    {
                        int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize - 2, 0, map.Width - 2);
                        int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize - 2, 0, map.Height - 2);

                        if (mouseState.LeftButton == ButtonState.Pressed)
                        {
                            BaseObject startingPoint = new BaseObject(new Rectangle(X * map.TileSize, Y * map.TileSize, 5 * map.TileSize, 5 * map.TileSize));
                            startingPoint.Texture = startingPointTexture;
                            map.StartingPoints.Add(startingPoint);
                            blocked = true;
                        }
                    }
                }
                else if (state == State.Erasing)
                {
                    checkForErase();
                }
            }
        }

        void checkForTileClick()
        {
            //Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
            Vector2 mousePosition = Vector2.Transform(new Vector2(mouseState.X, mouseState.Y), Matrix.Invert(camera.get_transformation(worldViewport)));

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                /*foreach (MapTile tile in map.Tiles)
                {
                    if (tile.Touches(mousePosition))
                    {
                        tile.Type = selectedTileLeft;
                        switch (selectedTileLeft)
                        {
                            case 0:
                                tile.Walkable = true;
                                break;
                            case 1:
                            case 2:
                                tile.Walkable = false;
                                break;
                            default:
                                tile.Walkable = true;
                                break;
                        };
                    }
                }*/
                int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize - brushSize, 0, map.Height - 1);
                int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize - brushSize, 0, map.Width - 1);

                int maxX = (int)MathHelper.Clamp(X + brushSize * 2 + 1, 0, map.Width - 1);
                int maxY = (int)MathHelper.Clamp(Y + brushSize * 2 + 1, 0, map.Height - 1);

                for (int x = X; x < maxX; x++)
                {
                    for (int y = Y; y < maxY; y++)
                    {
                        MapTile tile = map.Tiles[y, x];

                        bool allowPlace = true;

                        foreach (BaseObject r in map.Resources)
                        {
                            if (tile.Rectangle.Intersects(r.Rectangle))
                            {
                                allowPlace = false;
                                break;
                            }
                        }

                        foreach (BaseObject s in map.StartingPoints)
                        {
                            if (tile.Rectangle.Intersects(s.Rectangle))
                            {
                                allowPlace = false;
                                break;
                            }
                        }

                        if (!allowPlace)
                            continue;

                        tile.Type = selectedTileLeft;
                        switch (selectedTileLeft)
                        {
                            case 0:
                                tile.Walkable = true;
                                break;
                            case 1:
                            case 2:
                                tile.Walkable = false;
                                break;
                            default:
                                tile.Walkable = true;
                                break;
                        };
                    }
                }
            }
        }

        bool allowResourcePlacement;
        void checkForResourcePlacement(Vector2 mousePosition)
        {
            //if (mouseState.LeftButton == ButtonState.Released)
            //    allowResourcePlacement = true;
            //else if (allowResourcePlacement && mouseState.LeftButton == ButtonState.Pressed)
            //{
            //    allowResourcePlacement = false;

                if (selectedResource == 0)
                {
                    int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize - 1, 0, map.Width - 3);
                    int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize - 1, 0, map.Height - 3);

                    BaseObject resource = new BaseObject(new Rectangle(X * map.TileSize, Y * map.TileSize, 3 * map.TileSize, 3 * map.TileSize));
                    //resource.Texture = roksTexture;
                    //resource.Type = 0;

                    blocked = false;

                    foreach (BaseObject r in map.Resources)
                    {
                        if (resource.Rectangle.Intersects(r.Rectangle))
                        {
                            blocked = true;
                            break;
                        }
                    }

                    foreach (BaseObject s in map.StartingPoints)
                    {
                        if (resource.Rectangle.Intersects(s.Rectangle) || Vector2.Distance(resource.CenterPoint, s.CenterPoint) < map.TileSize * 9)
                        {
                            blocked = true;
                            break;
                        }
                    }

                    int maxX = (int)MathHelper.Clamp(X + 3, 0, map.Width);
                    int maxY = (int)MathHelper.Clamp(Y + 3, 0, map.Height);

                    for (int x = X; x < maxX; x++)
                    {
                        for (int y = Y; y < maxY; y++)
                        {
                            if (!map.Tiles[y, x].Walkable)
                            {
                                blocked = true;
                                break;
                            }
                        }
                    }

                    //if (!blocked)
                    //    map.Resources.Add(resource);
               // }
            }
        }

        bool allowStartingPointPlacement, blocked;
        void checkForStartingPointPlacement(Vector2 mousePosition)
        {
            //if (mouseState.LeftButton == ButtonState.Released)
            //    allowStartingPointPlacement = true;
            //else if (allowStartingPointPlacement && mouseState.LeftButton == ButtonState.Pressed)
            //{
            //    allowStartingPointPlacement = false;

                int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize - 2, 0, map.Width - 2);
                int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize - 2, 0, map.Height - 2);

                BaseObject startingPoint = new BaseObject(new Rectangle(X * map.TileSize, Y * map.TileSize, 5 * map.TileSize, 5 * map.TileSize));
                //startingPoint.Texture = startingPointTexture;

                blocked = false;

                foreach (BaseObject r in map.Resources)
                {
                    if (startingPoint.Rectangle.Intersects(r.Rectangle) || Vector2.Distance(startingPoint.CenterPoint, r.CenterPoint) < map.TileSize * 9)
                    {
                        blocked = true;
                        break;
                    }
                }

                foreach (BaseObject s in map.StartingPoints)
                {
                    if (startingPoint.Rectangle.Intersects(s.Rectangle))
                    {
                        blocked = true;
                        break;
                    }
                }

                int maxX = (int)MathHelper.Clamp(X + 5, 0, map.Width);
                int maxY = (int)MathHelper.Clamp(Y + 5, 0, map.Height);

                for (int x = X; x < maxX; x++)
                {
                    for (int y = Y; y < maxY; y++)
                    {
                        if (!map.Tiles[y, x].Walkable)
                        {
                            blocked = true;
                            break;
                        }
                    }
                }

                //if (!blocked)
                //    map.StartingPoints.Add(startingPoint);
            //}
        }

        void checkForErase()
        {
            Vector2 mousePosition = Vector2.Transform(new Vector2(mouseState.X, mouseState.Y), Matrix.Invert(camera.get_transformation(worldViewport)));

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                /*int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize, 0, map.Height - 1);
                int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize, 0, map.Width - 1);

                MapTile tile = map.Tiles[Y, X];
                tile.Type = 0;
                tile.Walkable = true;*/

                int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize - brushSize, 0, map.Height - 1);
                int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize - brushSize, 0, map.Width - 1);

                Rectangle eraser = new Rectangle(X * map.TileSize, Y * map.TileSize, (brushSize * 2 + 1) * map.TileSize, (brushSize * 2 + 1) * map.TileSize);

                foreach (MapTile tile in map.Tiles)
                {
                    if (eraser.Intersects(tile.Rectangle))
                    {
                        tile.Type = 0;
                        tile.Walkable = true;
                    }
                }

                for (int i = 0; i < map.Resources.Count; )
                {
                    BaseObject r = map.Resources[i];

                    if (r.Touches(mousePosition))
                        map.Resources.Remove(r);
                    else
                        i++;
                }

                for (int i = 0; i < map.StartingPoints.Count; )
                {
                    BaseObject s = map.StartingPoints[i];

                    if (s.Touches(mousePosition))
                        map.StartingPoints.Remove(s);
                    else
                        i++;
                }
            }
        }

        void checkButtons()
        {
            for (int i = 0; i < tileButtons.Count; i++)
            {
                if (tileButtons[i].Triggered)
                {
                    selectedTileLeft = i;
                    state = State.PlacingTile;
                }
            }

            for (int i = 0; i < resourceButtons.Count; i++)
            {
                if (resourceButtons[i].Triggered)
                {
                    selectedResource = i;
                    state = State.PlacingResource;
                }
            }

            //if (startingPointButton.Triggered)
            //    state = State.PlacingStartingPoint;

            if (saveButton.Triggered)
            {
                //map.SaveMap("C:\\rts maps\\map2.muh");
                state = State.Saving;
            }
            else if (exitButton.Triggered)
            {
                Game1.Game.Exit();
            }
            else if (mirrorLeftToRightButton.Triggered)
            {
                map.MirrorLeftToRight();
            }
            else if (mirrorTopToBottomButton.Triggered)
            {
                map.MirrorTopToBottom();
            }
            else if (rotateLeftToRightButton.Triggered)
            {
                map.RotateLeftToRight();
            }
            else if (rotateTopToBottomButton.Triggered)
            {
                map.RotateTopToBottom();
            }
            else if (rotateButton.Triggered)
            {
                map.Rotate();
            }
            else if (brushSize0Button.Triggered)
            {
                brushSize = 0;
            }
            else if (brushSize1Button.Triggered)
            {
                brushSize = 1;
            }
            else if (brushSize2Button.Triggered)
            {
                brushSize = 2;
            }
            else if (startingPointButton.Triggered)
            {
                state = State.PlacingStartingPoint;
            }
            else if (eraserButton.Triggered)
            {
                state = State.Erasing;
            }
        }

        void checkSaveMenuButtons()
        {
            if (saveMenuSaveButton.Triggered)
            {
            }
            else if (saveMenuExitButton.Triggered)
            {
                state = State.PlacingTile;
            }
        }

        void checkForSaveMenuTextInput()
        {
            Keys[] keys = keyboardState.GetPressedKeys();

            foreach (Keys key in keys)
            {
                if (key == Keys.Back && savePathString.Length > 0)
                {
                    savePathString = savePathString.Remove(savePathString.Length - 1);
                }

                if (!oldKeyboardState.IsKeyDown(key))
                {
                    if (key.ToString().Length == 1)
                        savePathString += key.ToString();
                    else if (key == Keys.Space)
                        savePathString += ' ';
                    //else if (key == Keys.Back && savePathString.Length > 0)
                    //    savePathString = savePathString.Remove(savePathString.Length - 1);
                    else if (key.ToString().Length == 2 && key.ToString()[0] == 'D')
                        savePathString += key.ToString()[1];
                }
            }
        }

        void checkForMouseCameraScroll(GameTime gameTime)
        {
            Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);

            Vector2 movement = Vector2.Zero;

            /*if (mousePosition.X <= 0)
                movement += new Vector2(-cameraScrollSpeed / camera.Zoom, 0);
            else if (mousePosition.X >= GraphicsDevice.Viewport.Width - 1)
                movement += new Vector2(cameraScrollSpeed / camera.Zoom, 0);

            if (mousePosition.Y <= 0)
                movement += new Vector2(0, -cameraScrollSpeed / camera.Zoom);
            else if (mousePosition.Y >= GraphicsDevice.Viewport.Height - 1)
                movement += new Vector2(0, cameraScrollSpeed / camera.Zoom);*/

            float adjustedScrollSpeed = cameraScrollSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds / camera.Zoom;

            if (mousePosition.X <= 0 || keyboardState.IsKeyDown(Keys.Left))
            {
                float angle = MathHelper.WrapAngle(-camera.Rotation + (float)Math.PI);
                movement += new Vector2(adjustedScrollSpeed * (float)Math.Cos(angle), adjustedScrollSpeed * (float)Math.Sin(angle));
            }
            else if (mousePosition.X >= uiViewport.Width - 1 || keyboardState.IsKeyDown(Keys.Right))
            {
                float angle = MathHelper.WrapAngle(-camera.Rotation);
                movement += new Vector2(adjustedScrollSpeed * (float)Math.Cos(angle), adjustedScrollSpeed * (float)Math.Sin(angle));
            }

            if (mousePosition.Y <= 0 || keyboardState.IsKeyDown(Keys.Up))
            {
                float angle = MathHelper.WrapAngle(-camera.Rotation - (float)Math.PI / 2);
                movement += new Vector2(adjustedScrollSpeed * (float)Math.Cos(angle), adjustedScrollSpeed * (float)Math.Sin(angle));
            }
            else if (mousePosition.Y >= uiViewport.Height - 1 || keyboardState.IsKeyDown(Keys.Down))
            {
                float angle = MathHelper.WrapAngle(-camera.Rotation + (float)Math.PI / 2);
                movement += new Vector2(adjustedScrollSpeed * (float)Math.Cos(angle), adjustedScrollSpeed * (float)Math.Sin(angle));
            }

            if (movement != Vector2.Zero)
                camera.Move(movement);
        }

        void checkForCameraZoom(GameTime gameTime)
        {
            if (keyboardState.IsKeyDown(Keys.OemMinus))
                //camera.Zoom -= cameraZoomSpeed;
                //camera.Zoom = MathHelper.Max(camera.Zoom - camera.Zoom * Util.ScaleWithGameTime(cameraZoomSpeed, gameTime), .5f);
                camera.Zoom -= camera.Zoom * Util.ScaleWithGameTime(cameraZoomSpeed, gameTime);

            if (keyboardState.IsKeyDown(Keys.OemPlus))
                //camera.Zoom += cameraZoomSpeed;
                //camera.Zoom = MathHelper.Min(camera.Zoom + camera.Zoom * Util.ScaleWithGameTime(cameraZoomSpeed, gameTime), 2f);
                camera.Zoom += camera.Zoom * Util.ScaleWithGameTime(cameraZoomSpeed, gameTime);
        }

        bool allowCameraRotate;
        void checkForCameraRotate(GameTime gameTime)
        {
            // check for changes to rotation target
            if (keyboardState.IsKeyUp(Keys.PageDown) && keyboardState.IsKeyUp(Keys.PageUp))
                allowCameraRotate = true;
            else if (allowCameraRotate)
            {
                if (keyboardState.IsKeyDown(Keys.PageDown))
                {
                    cameraRotationTarget += cameraRotationIncrement;
                    allowCameraRotate = false;
                }

                if (keyboardState.IsKeyDown(Keys.PageUp))
                {
                    cameraRotationTarget -= cameraRotationIncrement;
                    allowCameraRotate = false;
                }
            }

            // rotate camera to target rotation
            float actualRotationSpeed = Util.ScaleWithGameTime(cameraRotationSpeed, gameTime);
            if (Util.AngleDifference(camera.Rotation, cameraRotationTarget) < actualRotationSpeed)
                camera.Rotation = cameraRotationTarget;
            else if (camera.Rotation < cameraRotationTarget)
                camera.Rotation += actualRotationSpeed;
            else
                camera.Rotation -= actualRotationSpeed;
        }

        BaseObject cameraView = new BaseObject(new Rectangle());
        void clampCameraToMap()
        {
            cameraView.Width = (int)(worldViewport.Width / camera.Zoom);
            cameraView.Height = (int)(worldViewport.Height / camera.Zoom);
            cameraView.CenterPoint = camera.Pos;
            cameraView.Rotation = -camera.Rotation;
            cameraView.CalculateCorners();

            // upper left corner
            if (cameraView.UpperLeftCorner.X < 0)
            {
                cameraView.X += (int)Math.Round(-cameraView.UpperLeftCorner.X * (float)Math.Cos(0));
                cameraView.CalculateCorners();
            }
            if (cameraView.UpperLeftCorner.Y < 0)
            {
                cameraView.Y += (int)Math.Round(-cameraView.UpperLeftCorner.Y * (float)Math.Sin(MathHelper.PiOver2));
                cameraView.CalculateCorners();
            }
            if (cameraView.UpperLeftCorner.X > actualMapWidth)
            {
                cameraView.X += (int)Math.Round((cameraView.UpperLeftCorner.X - actualMapWidth) * (float)Math.Cos(Math.PI));
                cameraView.CalculateCorners();
            }
            if (cameraView.UpperLeftCorner.Y > actualMapHeight)
            {
                cameraView.Y += (int)Math.Round((cameraView.UpperLeftCorner.Y - actualMapHeight) * (float)Math.Sin(-MathHelper.PiOver2));
                cameraView.CalculateCorners();
            }

            // lower left corner
            if (cameraView.LowerLeftCorner.X < 0)
            {
                cameraView.X += (int)Math.Round(-cameraView.LowerLeftCorner.X * (float)Math.Cos(0));
                cameraView.CalculateCorners();
            }
            if (cameraView.LowerLeftCorner.Y < 0)
            {
                cameraView.Y += (int)Math.Round(-cameraView.LowerLeftCorner.Y * (float)Math.Sin(MathHelper.PiOver2));
                cameraView.CalculateCorners();
            }
            if (cameraView.LowerLeftCorner.X > actualMapWidth)
            {
                cameraView.X += (int)Math.Round((cameraView.LowerLeftCorner.X - actualMapWidth) * (float)Math.Cos(Math.PI));
                cameraView.CalculateCorners();
            }
            if (cameraView.LowerLeftCorner.Y > actualMapHeight)
            {
                cameraView.Y += (int)Math.Round((cameraView.LowerLeftCorner.Y - actualMapHeight) * (float)Math.Sin(-MathHelper.PiOver2));
                cameraView.CalculateCorners();
            }

            // upper right corner
            if (cameraView.UpperRightCorner.X < 0)
            {
                cameraView.X += (int)Math.Round(-cameraView.UpperRightCorner.X * (float)Math.Cos(0));
                cameraView.CalculateCorners();
            }
            if (cameraView.UpperRightCorner.Y < 0)
            {
                cameraView.Y += (int)Math.Round(-cameraView.UpperRightCorner.Y * (float)Math.Sin(MathHelper.PiOver2));
                cameraView.CalculateCorners();
            }
            if (cameraView.UpperRightCorner.X > actualMapWidth)
            {
                cameraView.X += (int)Math.Round((cameraView.UpperRightCorner.X - actualMapWidth) * (float)Math.Cos(Math.PI));
                cameraView.CalculateCorners();
            }
            if (cameraView.UpperRightCorner.Y > actualMapHeight)
            {
                cameraView.Y += (int)Math.Round((cameraView.UpperRightCorner.Y - actualMapHeight) * (float)Math.Sin(-MathHelper.PiOver2));
                cameraView.CalculateCorners();
            }

            // lower right corner
            if (cameraView.LowerRightCorner.X < 0)
            {
                cameraView.X += (int)Math.Round(-cameraView.LowerRightCorner.X * (float)Math.Cos(0));
                cameraView.CalculateCorners();
            }
            if (cameraView.LowerRightCorner.Y < 0)
            {
                cameraView.Y += (int)Math.Round(-cameraView.LowerRightCorner.Y * (float)Math.Sin(MathHelper.PiOver2));
                cameraView.CalculateCorners();
            }
            if (cameraView.LowerRightCorner.X > actualMapWidth)
            {
                cameraView.X += (int)Math.Round((cameraView.LowerRightCorner.X - actualMapWidth) * (float)Math.Cos(Math.PI));
                cameraView.CalculateCorners();
            }
            if (cameraView.LowerRightCorner.Y > actualMapHeight)
            {
                cameraView.Y += (int)Math.Round((cameraView.LowerRightCorner.Y - actualMapHeight) * (float)Math.Sin(-MathHelper.PiOver2));
                cameraView.CalculateCorners();
            }

            camera.Pos = cameraView.CenterPoint;

            /*float cameraLeftBound = camera.Pos.X + (GraphicsDevice.Viewport.Width / 2 * (float)Math.Cos((float)Math.PI + camera.Rotation));
            float cameraRightBound = camera.Pos.X + (GraphicsDevice.Viewport.Width / 2 * (float)Math.Cos(camera.Rotation));
            float cameraTopBound = camera.Pos.Y + (GraphicsDevice.Viewport.Height / 2 * (float)Math.Sin(-MathHelper.PiOver2 + camera.Rotation));
            float cameraBottomBound = camera.Pos.Y + (GraphicsDevice.Viewport.Height / 2 * (float)Math.Sin(MathHelper.PiOver2 + camera.Rotation));*/

            /*if (camera.Pos.X < GraphicsDevice.Viewport.Width / camera.Zoom / 2)
                camera.Pos = new Vector2(GraphicsDevice.Viewport.Width / camera.Zoom / 2, camera.Pos.Y);
            if (camera.Pos.X > map.Width * Map.TILESIZE - GraphicsDevice.Viewport.Width / camera.Zoom / 2)
                camera.Pos = new Vector2(map.Width * Map.TILESIZE - GraphicsDevice.Viewport.Width / camera.Zoom / 2, camera.Pos.Y);
            if (camera.Pos.Y < GraphicsDevice.Viewport.Height / camera.Zoom / 2)
                camera.Pos = new Vector2(camera.Pos.X, GraphicsDevice.Viewport.Height / camera.Zoom / 2);
            if (camera.Pos.Y > map.Height * Map.TILESIZE - GraphicsDevice.Viewport.Height / camera.Zoom / 2)
                camera.Pos = new Vector2(camera.Pos.X, map.Height * Map.TILESIZE - GraphicsDevice.Viewport.Height / camera.Zoom / 2);*/
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            GraphicsDevice.Clear(Color.Black);

            GraphicsDevice.Viewport = worldViewport;
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, camera.get_transformation(worldViewport));

            drawMap(spriteBatch);

            drawResources(spriteBatch);

            drawStartingPoints(spriteBatch);

            drawMidLinesOnMap(spriteBatch);

            drawBoxAtCursor(spriteBatch);

            spriteBatch.End();
            spriteBatch.Begin();

            GraphicsDevice.Viewport = uiViewport;

            drawMinimap(spriteBatch);
            drawMidLinesOnMinimap(spriteBatch);

            drawUI(spriteBatch);

            drawSaveMenu(spriteBatch);

            spriteBatch.End();
        }

        public void drawMap(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(ColorTexture.Gray, new Rectangle(0, 0, map.Width * map.TileSize, map.Height * map.TileSize), Color.White);
            /*foreach (MapTile tile in map.Tiles)
            {
                if (tile.Type == 0)
                    spriteBatch.Draw(ColorTexture.Gray, tile.Rectangle, Color.White);
                else if (tile.Type == 1)
                    spriteBatch.Draw(boulder1Texture, tile.Rectangle, Color.White);
                else if (tile.Type == 2)
                    spriteBatch.Draw(tree1Texture, tile.Rectangle, Color.White);
            }*/

            // finds indices to start and stop drawing at based on the camera transform, viewport size, and tile size
            Vector2 minIndices = Vector2.Transform(Vector2.Zero, Matrix.Invert(camera.get_transformation(worldViewport))) / map.TileSize;
            Vector2 maxIndices = Vector2.Transform(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Matrix.Invert(camera.get_transformation(worldViewport))) / map.TileSize;

            // keeps min indices >= 0
            int minIndicesY = (int)MathHelper.Clamp(minIndices.Y, 0, map.Height - 1);
            int minIndicesX = (int)MathHelper.Clamp(minIndices.X, 0, map.Width - 1);

            // keeps max indices within map size
            int maxIndicesY = (int)Math.Ceiling(MathHelper.Clamp(maxIndices.Y, 0, map.Height - 1));
            int maxIndicesX = (int)Math.Ceiling(MathHelper.Clamp(maxIndices.X, 0, map.Width - 1));

            int incrementX = 1;
            int incrementY = 1;

            if (minIndicesX > maxIndicesX)
                incrementX = -1;
            if (minIndicesY > maxIndicesY)
                incrementY = -1;

            for (int y = minIndicesY; (incrementY == 1 && y <= maxIndicesY) || (incrementY == -1 && y >= maxIndicesY); y += incrementY)
            {
                for (int x = minIndicesX; (incrementX == 1 && x <= maxIndicesX) || (incrementX == -1 && x >= maxIndicesX); x += incrementX)
                {
                    MapTile tile = map.Tiles[y, x];

                    if (tile.Type == 0)
                        spriteBatch.Draw(ColorTexture.Gray, tile.Rectangle, Color.White);
                    else if (tile.Type == 1)
                        spriteBatch.Draw(boulder1Texture, tile.Rectangle, Color.White);
                    else if (tile.Type == 2)
                        spriteBatch.Draw(tree1Texture, tile.Rectangle, Color.White);
                }
            }
        }

        public void drawResources(SpriteBatch spriteBatch)
        {
            foreach (BaseObject r in map.Resources)
            {
                if (r.Type == 0)
                    //spriteBatch.Draw(roksTexture, r.Rectangle, Color.White);
                    spriteBatch.Draw(roksTexture, new Rectangle((int)r.CenterPoint.X, (int)r.CenterPoint.Y, r.Width, r.Height), null, Color.White, -camera.Rotation, new Vector2(roksTexture.Width / 2, roksTexture.Height / 2), SpriteEffects.None, 0f);

            }
        }

        public void drawStartingPoints(SpriteBatch spriteBatch)
        {
            foreach (BaseObject s in map.StartingPoints)
            {
                //spriteBatch.Draw(startingPointTexture, s.Rectangle, Color.White);
                spriteBatch.Draw(startingPointTexture, new Rectangle((int)s.CenterPoint.X, (int)s.CenterPoint.Y, s.Width, s.Height), null, Color.White, -camera.Rotation, new Vector2(startingPointTexture.Width / 2, startingPointTexture.Height / 2), SpriteEffects.None, 0f);

            }
        }

        void drawMidLinesOnMap(SpriteBatch spriteBatch)
        {
            float oldAlpha = line.Alpha;
            line.Alpha = .75f;
            line.Size = (int)MathHelper.Max(1, 2 / camera.Zoom);

            line.ClearVectors();
            line.AddVector(new Vector2(map.Width * map.TileSize / 2, 0));
            line.AddVector(new Vector2(map.Width * map.TileSize / 2, map.Height * map.TileSize));
            line.Render(spriteBatch);

            line.ClearVectors();
            line.AddVector(new Vector2(0, map.Height * map.TileSize / 2));
            line.AddVector(new Vector2(map.Width * map.TileSize, map.Height * map.TileSize / 2));
            line.Render(spriteBatch);

            line.Alpha = oldAlpha;
            line.Size = 1;
        }

        void drawBoxAtCursor(SpriteBatch spriteBatch)
        {
            Vector2 mousePosition = Vector2.Transform(new Vector2(mouseState.X, mouseState.Y), Matrix.Invert(camera.get_transformation(worldViewport)));

            //int y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize, 0, map.Height - 1);
            //int x = (int)MathHelper.Clamp(mousePosition.X / map.TileSize, 0, map.Width - 1);

            if (state == State.PlacingTile)
            {
                int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize - brushSize, 0, map.Height - 1);
                int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize - brushSize, 0, map.Width - 1);

                Texture2D texture = null;
                if (selectedTileLeft == 0)
                    texture = ColorTexture.Gray;
                else if (selectedTileLeft == 1)
                    texture = boulder1Texture;
                else if (selectedTileLeft == 2)
                    texture = tree1Texture;

                int maxX = (int)MathHelper.Clamp(X + brushSize * 2 + 1, 0, map.Width - 1);
                int maxY = (int)MathHelper.Clamp(Y + brushSize * 2 + 1, 0, map.Height - 1);

                for (int x = X; x < maxX; x++)
                {
                    for (int y = Y; y < maxY; y++)
                    {
                        spriteBatch.Draw(texture, map.Tiles[y, x].Rectangle, Color.White * .65f);

                        line.ClearVectors();
                        line.CreateBox(map.Tiles[y, x].Rectangle);
                        //line.Render(spriteBatch);
                        line.RenderWithZoom(spriteBatch, camera.Zoom);
                    }
                }
            }
            else if (state == State.PlacingResource)
            {
                if (selectedResource == 0)
                {
                    int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize - 1, 0, map.Width - 3);
                    int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize - 1, 0, map.Height - 3);

                    int width = 3 * map.TileSize, height = 3 * map.TileSize;

                    //spriteBatch.Draw(roksTexture, new Rectangle(X * map.TileSize, Y * map.TileSize, 3 * map.TileSize, 3 * map.TileSize), Color.White * .65f);
                    spriteBatch.Draw(roksTexture, new Rectangle(X * map.TileSize + width / 2, Y * map.TileSize + height / 2, width, height), null, Color.White * .65f, -camera.Rotation, new Vector2(roksTexture.Width / 2, roksTexture.Height / 2), SpriteEffects.None, 0f);

                    if (blocked)
                        //spriteBatch.Draw(roksTexture, new Rectangle(X * map.TileSize, Y * map.TileSize, 3 * map.TileSize, 3 * map.TileSize), Color.Red * .5f);
                        spriteBatch.Draw(roksTexture, new Rectangle(X * map.TileSize + width / 2, Y * map.TileSize + height / 2, width, height), null, Color.Red * .5f, -camera.Rotation, new Vector2(roksTexture.Width / 2, roksTexture.Height / 2), SpriteEffects.None, 0f);

                }
            }
            else if (state == State.PlacingStartingPoint)
            {
                int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize - 2, 0, map.Width - 5);
                int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize - 2, 0, map.Height - 5);

                int width = 5 * map.TileSize, height = 5 * map.TileSize;

                //spriteBatch.Draw(startingPointTexture, new Rectangle(X * map.TileSize, Y * map.TileSize, 5 * map.TileSize, 5 * map.TileSize), Color.White * .65f);
                spriteBatch.Draw(startingPointTexture, new Rectangle(X * map.TileSize + width / 2, Y * map.TileSize + height / 2, width, height), null, Color.White * .65f, -camera.Rotation, new Vector2(startingPointTexture.Width / 2, startingPointTexture.Height / 2), SpriteEffects.None, 0f);


                if (blocked)
                    //spriteBatch.Draw(startingPointTexture, new Rectangle(X * map.TileSize, Y * map.TileSize, 5 * map.TileSize, 5 * map.TileSize), Color.Red * .5f);
                    spriteBatch.Draw(startingPointTexture, new Rectangle(X * map.TileSize + width / 2, Y * map.TileSize + height / 2, width, height), null, Color.Red * .5f, -camera.Rotation, new Vector2(startingPointTexture.Width / 2, startingPointTexture.Height / 2), SpriteEffects.None, 0f);

            }
            else if (state == State.Erasing)
            {
                int Y = (int)MathHelper.Clamp(mousePosition.Y / map.TileSize - brushSize, 0, map.Height - 1);
                int X = (int)MathHelper.Clamp(mousePosition.X / map.TileSize - brushSize, 0, map.Width - 1);

                Rectangle eraser = new Rectangle(X * map.TileSize, Y * map.TileSize, (brushSize * 2 + 1) * map.TileSize, (brushSize * 2 + 1) * map.TileSize);
                line.ClearVectors();
                line.CreateBox(eraser);
                //line.Render(spriteBatch);
                line.RenderWithZoom(spriteBatch, camera.Zoom);
                spriteBatch.Draw(eraserTexture, eraser, Color.White * .65f);
            }
        }

        void drawMinimap(SpriteBatch spriteBatch)
        {
            //float aspectRatio = (float)map.Width / map.Height;

            // draw minimap border then minimap
            //spriteBatch.Draw(ColorTexture.Black, new Rectangle(minimapPosX - minimapBorderSize, minimapPosY - minimapBorderSize, minimapSize + minimapBorderSize * 2, minimapSize + minimapBorderSize * 2), Color.White);
            spriteBatch.Draw(ColorTexture.Black, new Rectangle(minimapPosX - minimapBorderSize, minimapPosY - minimapBorderSize, uiViewport.Width, minimapSize + minimapBorderSize * 2), Color.White);
            //spriteBatch.Draw(fullMapTexture, minimap, Color.White);

            Rectangle rectangle = new Rectangle(0, 0, 3, 3);
            foreach (MapTile tile in map.Tiles)
            {
                rectangle.X = (int)Math.Round(tile.CenterPointX * minimapToMapRatioX + minimapPosX);
                rectangle.Y = (int)Math.Round(tile.CenterPointY * minimapToMapRatioY + minimapPosY);

                if (tile.Type == 0)
                    spriteBatch.Draw(ColorTexture.Gray, rectangle, Color.White);
                else if (tile.Type == 1)
                    spriteBatch.Draw(boulder1Texture, rectangle, Color.White);
                else if (tile.Type == 2)
                    spriteBatch.Draw(tree1Texture, rectangle, Color.White);
            }

            // rectangle for pixels on minimap
            //Rectangle fogRectangle = new Rectangle(0, 0, (int)(map.TileSize * minimapToMapRatioX), (int)(map.TileSize * minimapToMapRatioY));
            Rectangle fogRectangle = new Rectangle(0, 0, 2, 2);

            // draw fog on minimap
            /*foreach (MapTile tile in map.Tiles)
            {
                if (!tile.Visible)
                {
                    fogRectangle.X = (int)Math.Round(tile.CenterPointX * minimapToMapRatioX + minimapPosX);
                    fogRectangle.Y = (int)Math.Round(tile.CenterPointY * minimapToMapRatioY + minimapPosY);
                    spriteBatch.Draw(transparentBlackTexture, fogRectangle, Color.White);
                }
            }*/
            /*int tileX;
            for (int x = 0; x < minimapSize; x += 2)
            {
                fogRectangle.X = minimapPosX + x;
                tileX = (int)(x / minimapToMapRatioX / map.TileSize);
                for (int y = 0; y < minimapSize; y += 2)
                {
                    MapTile tile = map.Tiles[(int)(y / minimapToMapRatioY / map.TileSize), tileX];
                    if (!tile.Visible)
                    {
                        fogRectangle.Y = minimapPosY + y;
                        if (tile.Revealed)
                            spriteBatch.Draw(ColorTexture.Black, fogRectangle, Color.White * .25f);
                        else
                            spriteBatch.Draw(ColorTexture.Black, fogRectangle, Color.White * .5f);
                    }
                }
            }*/

            // update size of screen indicator box
            /*minimapScreenIndicatorBox.Width = (int)(worldViewport.Width * minimapToMapRatioX / camera.Zoom);
            minimapScreenIndicatorBox.Height = (int)(worldViewport.Height * minimapToMapRatioY / camera.Zoom);

            // calculate position of screen indicator box
            minimapScreenIndicatorBox.CenterPoint = new Vector2(camera.Pos.X * minimapToMapRatioX + minimapPosX, camera.Pos.Y * minimapToMapRatioY + minimapPosY);
            minimapScreenIndicatorBox.Rotation = -camera.Rotation;
            minimapScreenIndicatorBox.CalculateCorners();

            // draw screen indicator box on minimap
            minimapScreenIndicatorBoxLine.ClearVectors();
            minimapScreenIndicatorBoxLine.AddVector(minimapScreenIndicatorBox.UpperLeftCorner);
            minimapScreenIndicatorBoxLine.AddVector(minimapScreenIndicatorBox.UpperRightCorner);
            minimapScreenIndicatorBoxLine.AddVector(minimapScreenIndicatorBox.LowerRightCorner);
            minimapScreenIndicatorBoxLine.AddVector(minimapScreenIndicatorBox.LowerLeftCorner);
            minimapScreenIndicatorBoxLine.AddVector(minimapScreenIndicatorBox.UpperLeftCorner);
            minimapScreenIndicatorBoxLine.Render(spriteBatch);*/
        }

        void drawMidLinesOnMinimap(SpriteBatch spriteBatch)
        {
            float oldAlpha = line.Alpha;
            line.Alpha = .5f;

            line.ClearVectors();
            line.AddVector(new Vector2(map.Width * map.TileSize / 2 * minimapToMapRatioX + minimapPosX, minimapPosY));
            line.AddVector(new Vector2(map.Width * map.TileSize / 2 * minimapToMapRatioX + minimapPosX, map.Height * map.TileSize * minimapToMapRatioY + minimapPosY));
            line.Render(spriteBatch);

            line.ClearVectors();
            line.AddVector(new Vector2(minimapPosX, map.Height * map.TileSize / 2 * minimapToMapRatioY + minimapPosY));
            line.AddVector(new Vector2(map.Width * map.TileSize * minimapToMapRatioX + minimapPosX, map.Height * map.TileSize / 2 * minimapToMapRatioY + minimapPosY));
            line.Render(spriteBatch);

            line.Alpha = oldAlpha;
        }

        // buttons
        void drawUI(SpriteBatch spriteBatch)
        {
            // save button
            line.ClearVectors();
            line.CreateBox(saveButton.Rectangle);
            line.Render(spriteBatch);
            if (saveButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, saveButton.Rectangle, Color.White * .15f);
            string saveString = "Save";
            Vector2 saveStringSize = buttonFont.MeasureString(saveString);
            spriteBatch.DrawString(buttonFont, saveString, new Vector2(saveButton.X + saveButton.Width / 2 - saveStringSize.X / 2, saveButton.Y + saveButton.Height / 2 - saveStringSize.Y / 2), Color.White);

            // exit button
            line.ClearVectors();
            line.CreateBox(exitButton.Rectangle);
            line.Render(spriteBatch);
            if (exitButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, exitButton.Rectangle, Color.White * .15f);
            string exitString = "Exit";
            Vector2 exitStringSize = buttonFont.MeasureString(exitString);
            spriteBatch.DrawString(buttonFont, exitString, new Vector2(exitButton.X + exitButton.Width / 2 - exitStringSize.X / 2, exitButton.Y + exitButton.Height / 2 - exitStringSize.Y / 2), Color.White);

            // starting point button
            line.ClearVectors();
            line.CreateBox(startingPointButton.Rectangle);
            line.Render(spriteBatch);
            spriteBatch.Draw(startingPointTexture, startingPointButton.Rectangle, Color.White);
            if (startingPointButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, startingPointButton.Rectangle, Color.White * .15f);
            string startingPointString = "Starting point";
            Vector2 startingPointStringSize = smallFont.MeasureString(startingPointString);
            spriteBatch.DrawString(smallFont, startingPointString, new Vector2((int)(startingPointButton.X + startingPointButton.Width / 2 - startingPointStringSize.X / 2), (int)(startingPointButton.Y - startingPointStringSize.Y - 3)), Color.White);

            // eraser button
            line.ClearVectors();
            line.CreateBox(eraserButton.Rectangle);
            line.Render(spriteBatch);
            spriteBatch.Draw(eraserTexture, eraserButton.Rectangle, Color.White);
            if (eraserButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, eraserButton.Rectangle, Color.White * .15f);
            string eraserString = "Eraser";
            Vector2 eraserStringSize = smallFont.MeasureString(eraserString);
            spriteBatch.DrawString(smallFont, eraserString, new Vector2((int)(eraserButton.X + eraserButton.Width / 2 - eraserStringSize.X / 2), (int)(eraserButton.Y - eraserStringSize.Y - 3)), Color.White);

            // mirror and rotate buttons
            spriteBatch.Draw(mirrorLeftToRightButton.Texture, mirrorLeftToRightButton.Rectangle, Color.White);
            line.ClearVectors();
            line.CreateBox(mirrorLeftToRightButton.Rectangle);
            line.Render(spriteBatch);
            if (mirrorLeftToRightButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, mirrorLeftToRightButton.Rectangle, Color.White * .5f);

            spriteBatch.Draw(mirrorTopToBottomButton.Texture, mirrorTopToBottomButton.Rectangle, Color.White);
            line.ClearVectors();
            line.CreateBox(mirrorTopToBottomButton.Rectangle);
            line.Render(spriteBatch);
            if (mirrorTopToBottomButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, mirrorTopToBottomButton.Rectangle, Color.White * .5f);

            string mirrorLeftToRightString = "Mirror";
            Vector2 mirrorLeftToRightStringSize = smallFont.MeasureString(mirrorLeftToRightString);
            spriteBatch.DrawString(smallFont, mirrorLeftToRightString, new Vector2(mirrorLeftToRightButton.X - mirrorLeftToRightStringSize.X - 10, mirrorLeftToRightButton.Y + mirrorLeftToRightButton.Height / 2 - mirrorLeftToRightStringSize.Y / 2), Color.White);

            spriteBatch.Draw(rotateLeftToRightButton.Texture, rotateLeftToRightButton.Rectangle, Color.White);
            line.ClearVectors();
            line.CreateBox(rotateLeftToRightButton.Rectangle);
            line.Render(spriteBatch);
            if (rotateLeftToRightButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, rotateLeftToRightButton.Rectangle, Color.White * .5f);

            spriteBatch.Draw(rotateTopToBottomButton.Texture, rotateTopToBottomButton.Rectangle, Color.White);
            line.ClearVectors();
            line.CreateBox(rotateTopToBottomButton.Rectangle);
            line.Render(spriteBatch);
            if (rotateTopToBottomButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, rotateTopToBottomButton.Rectangle, Color.White * .5f);

            string rotateLeftToRightString = "Rotate";
            Vector2 rotateLeftToRightStringSize = smallFont.MeasureString(rotateLeftToRightString);
            spriteBatch.DrawString(smallFont, rotateLeftToRightString, new Vector2(rotateLeftToRightButton.X - rotateLeftToRightStringSize.X - 10, rotateLeftToRightButton.Y + rotateLeftToRightButton.Height / 2 - rotateLeftToRightStringSize.Y / 2), Color.White);

            spriteBatch.Draw(rotateButton.Texture, rotateButton.Rectangle, Color.White);
            line.ClearVectors();
            line.CreateBox(rotateButton.Rectangle);
            line.Render(spriteBatch);
            if (rotateButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, rotateButton.Rectangle, Color.White * .5f);

            string rotateString = "4-way rotate";
            Vector2 rotateStringSize = smallFont.MeasureString(rotateString);
            spriteBatch.DrawString(smallFont, rotateString, new Vector2(rotateButton.X - rotateStringSize.X - 10, rotateButton.Y + rotateButton.Height / 2 - rotateStringSize.Y / 2), Color.White);

            // brush size buttons
            string brushSizeString = "Brush size";
            Vector2 brushSizeStringSize = smallFont.MeasureString(brushSizeString);
            spriteBatch.DrawString(smallFont, brushSizeString, new Vector2((int)(brushSize0Button.X + brushSize0Button.Width / 2 - brushSizeStringSize.X / 2), (int)(brushSize0Button.Y - brushSizeStringSize.Y - 1)), Color.White);

            spriteBatch.Draw(brushSize0Button.Texture, brushSize0Button.Rectangle, Color.White);

            int width = (int)(brushSize0Button.Width * .25f);
            int height = (int)(brushSize0Button.Height * .25f);
            spriteBatch.Draw(ColorTexture.Red, new Rectangle(brushSize0Button.X + brushSize0Button.Width / 2 - width / 2, brushSize0Button.Y + brushSize0Button.Height / 2 - height / 2, width, height), Color.White * .8f);

            line.ClearVectors();
            line.CreateBox(brushSize0Button.Rectangle);
            line.Render(spriteBatch);
            if (brushSize0Button.Pressing)
                spriteBatch.Draw(ColorTexture.White, brushSize0Button.Rectangle, Color.White * .5f);

            spriteBatch.Draw(brushSize1Button.Texture, brushSize1Button.Rectangle, Color.White);

            width = (int)(brushSize1Button.Width * .4f);
            height = (int)(brushSize1Button.Height * .4f);
            spriteBatch.Draw(ColorTexture.Red, new Rectangle(brushSize1Button.X + brushSize1Button.Width / 2 - width / 2, brushSize1Button.Y + brushSize1Button.Height / 2 - height / 2, width, height), Color.White * .8f);

            line.ClearVectors();
            line.CreateBox(brushSize1Button.Rectangle);
            line.Render(spriteBatch);
            if (brushSize1Button.Pressing)
                spriteBatch.Draw(ColorTexture.White, brushSize1Button.Rectangle, Color.White * .5f);

            spriteBatch.Draw(brushSize2Button.Texture, brushSize2Button.Rectangle, Color.White);

            width = (int)(brushSize2Button.Width * .5f);
            height = (int)(brushSize2Button.Height * .5f);
            spriteBatch.Draw(ColorTexture.Red, new Rectangle(brushSize2Button.X + brushSize2Button.Width / 2 - width / 2, brushSize2Button.Y + brushSize2Button.Height / 2 - height / 2, width, height), Color.White * .8f);

            line.ClearVectors();
            line.CreateBox(brushSize2Button.Rectangle);
            line.Render(spriteBatch);
            if (brushSize2Button.Pressing)
                spriteBatch.Draw(ColorTexture.White, brushSize2Button.Rectangle, Color.White * .5f);

            if (brushSize == 0)
                spriteBatch.Draw(whiteBoxTexture, brushSize0Button.Rectangle, Color.White);
            else if (brushSize == 1)
                spriteBatch.Draw(whiteBoxTexture, brushSize1Button.Rectangle, Color.White);
            else if (brushSize == 2)
                spriteBatch.Draw(whiteBoxTexture, brushSize2Button.Rectangle, Color.White);

            // selection area boxes
            line.ClearVectors();
            line.CreateBox(tileSelectionArea1);
            line.Render(spriteBatch);

            line.ClearVectors();
            line.CreateBox(tileSelectionArea2);
            line.Render(spriteBatch);

            line.ClearVectors();
            line.CreateBox(resourceSelectionArea);
            line.Render(spriteBatch);

            string passableString = "Passable tiles";
            Vector2 passableStringSize = tileSelectionFont.MeasureString(passableString);
            spriteBatch.DrawString(tileSelectionFont, passableString, new Vector2(tileSelectionArea1.X + tileSelectionArea1.Width / 2 - passableStringSize.X / 2, tileSelectionArea1.Y), Color.White);

            string impassableString = "Impassable tiles";
            Vector2 impassableStringSize = tileSelectionFont.MeasureString(impassableString);
            spriteBatch.DrawString(tileSelectionFont, impassableString, new Vector2(tileSelectionArea2.X + tileSelectionArea2.Width / 2 - impassableStringSize.X / 2, tileSelectionArea2.Y), Color.White);

            string resourcesString = "Resources";
            Vector2 resourcesStringSize = tileSelectionFont.MeasureString(resourcesString);
            spriteBatch.DrawString(tileSelectionFont, resourcesString, new Vector2(resourceSelectionArea.X + resourceSelectionArea.Width / 2 - resourcesStringSize.X / 2, resourceSelectionArea.Y), Color.White);

            foreach (SimpleButton button in tileButtons)
            {
                spriteBatch.Draw(button.NormalTexture, button.Rectangle, Color.White);
                if (button.Pressing)
                    spriteBatch.Draw(ColorTexture.Black, button.Rectangle, Color.White * .15f);
            }

            foreach (SimpleButton button in resourceButtons)
            {
                spriteBatch.Draw(button.NormalTexture, button.Rectangle, Color.White);
                if (button.Pressing)
                    spriteBatch.Draw(ColorTexture.Black, button.Rectangle, Color.White * .15f);
            }

            if (state == State.PlacingTile)
                spriteBatch.Draw(whiteBoxTexture, tileButtons[selectedTileLeft].Rectangle, Color.White);
            else if (state == State.PlacingResource)
            {
                spriteBatch.Draw(whiteBoxTexture, resourceButtons[selectedResource].Rectangle, Color.White);
            }
            else if (state == State.PlacingStartingPoint)
            {
                spriteBatch.Draw(whiteBoxTexture, startingPointButton.Rectangle, Color.White);
            }
            else if (state == State.Erasing)
            {
                spriteBatch.Draw(whiteBoxTexture, eraserButton.Rectangle, Color.White);
            }
        }

        string savePathString = "";
        void drawSaveMenu(SpriteBatch spriteBatch)
        {
            if (state != State.Saving)
                return;

            // big box
            spriteBatch.Draw(ColorTexture.Black, saveMenuRectangle, Color.White);

            // save button
            line.ClearVectors();
            line.CreateBox(saveMenuSaveButton.Rectangle);
            line.Render(spriteBatch);
            if (saveMenuSaveButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, saveMenuSaveButton.Rectangle, Color.White * .15f);
            string saveString = "Save";
            Vector2 saveStringSize = buttonFont.MeasureString(saveString);
            spriteBatch.DrawString(buttonFont, saveString, new Vector2(saveMenuSaveButton.X + saveMenuSaveButton.Width / 2 - saveStringSize.X / 2, saveMenuSaveButton.Y + saveMenuSaveButton.Height / 2 - saveStringSize.Y / 2), Color.White);

            // exit button
            line.ClearVectors();
            line.CreateBox(saveMenuExitButton.Rectangle);
            line.Render(spriteBatch);
            if (saveMenuExitButton.Pressing)
                spriteBatch.Draw(ColorTexture.White, saveMenuExitButton.Rectangle, Color.White * .15f);
            string exitString = "Exit";
            Vector2 exitStringSize = buttonFont.MeasureString(exitString);
            spriteBatch.DrawString(buttonFont, exitString, new Vector2(saveMenuExitButton.X + saveMenuExitButton.Width / 2 - exitStringSize.X / 2, saveMenuExitButton.Y + saveMenuExitButton.Height / 2 - exitStringSize.Y / 2), Color.White);

            spriteBatch.DrawString(buttonFont, savePathString, new Vector2(saveMenuRectangle.X, saveMenuRectangle.Y), Color.White);
        }
    }

    enum State
    {
        PlacingTile, PlacingResource, PlacingStartingPoint, Erasing, Saving
    }
}

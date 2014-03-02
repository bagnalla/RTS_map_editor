using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace rts_map_editor
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        public static Game1 Game { get; private set; }
        public GameState CurrentGameState { get; private set; }

        public GraphicsDeviceManager Graphics { get; private set; }
        SpriteBatch spriteBatch;

        public Game1()
        {
            Game = this;
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Graphics.PreferredBackBufferWidth = 1024;
            Graphics.PreferredBackBufferHeight = 576;
            Graphics.ApplyChanges();

            Window.Title = "Map Editor";

            IsMouseVisible = true;

            ColorTexture.Initialize(GraphicsDevice);

            //CurrentGameState = new Editor(RtsEventHandler);
            CurrentGameState = new StartMenu(StartMenuEventHandler);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            //    this.Exit();

            // TODO: Add your update logic here

            CurrentGameState.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            //GraphicsDevice.Clear(Color.CornflowerBlue);

            CurrentGameState.Draw(spriteBatch);

            base.Draw(gameTime);
        }

        void StartMenuEventHandler(Object sender, EventArgs e)
        {
            GameStateArgs args = (GameStateArgs)e;
            if (args.Args.Length > 0)
            {
                if (args.Args.Length == 3)
                {
                    if (args.Args[0] == "new")
                    {
                        CurrentGameState = new Editor(RtsEventHandler, int.Parse(args.Args[1]), int.Parse(args.Args[2]));
                    }
                }
                else if (args.Args[0] == "load")
                {
                    CurrentGameState = new LoadMenu(LoadMenuEventHandler);
                }
            }
        }
        void LoadMenuEventHandler(Object sender, EventArgs e)
        {
            GameStateArgs args = (GameStateArgs)e;
            if (args.Args.Length > 0)
            {
                CurrentGameState = new Editor(RtsEventHandler, args.Args[0]);
            }
        }
        void RtsEventHandler(Object sender, EventArgs e)
        {
            if (e is GameStateArgs)
            {
                GameStateArgs args = (GameStateArgs)e;
                if (args.Args.Length > 0)
                {
                    if (args.Args[0] == "exit")
                    {
                        Game.Exit();
                    }
                }
            }
        }
    }
}

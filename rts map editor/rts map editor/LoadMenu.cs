using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
    class LoadMenu : GameState
    {
        static SpriteFont buttonFont;

        Viewport viewport;

        MouseState mouseState;
        KeyboardState keyboardState;

        int buttonWidth, buttonHeight = 50;

        public LoadMenu(EventHandler callback)
            : base(callback)
        {
            viewport = GraphicsDevice.Viewport;

            if (!contentLoaded)
            {
                buttonFont = Content.Load<SpriteFont>("buttonFont");

                contentLoaded = true;
            }

            readMapFiles();

            int posY = 5;
            if (files.Count > 0)
                buttonHeight = (int)MathHelper.Min(40, viewport.Height / files.Count - 5);

            foreach (string file in files)
            {
                Vector2 stringSize = buttonFont.MeasureString(file.Substring(12));
                buttonWidth = (int)(stringSize.X + 15);

                SimpleButton button = new SimpleButton(new Rectangle(viewport.Width / 2 - buttonWidth / 2, posY, buttonWidth, buttonHeight), ColorTexture.Black, null, null);
                SimpleButton.AddButton(button);

                posY += buttonHeight + 5;
            }
        }

        List<String> files;
        void readMapFiles()
        {
            if (!Directory.Exists("C:\\rts maps\\"))
                Directory.CreateDirectory("C:\\rts maps\\");

            files = new List<string>(Directory.GetFiles("C:\\rts maps"));

            for (int i = 0; i < files.Count; )
            {
                string file = files[i];

                if (file.Substring(file.Length - 4) != ".muh")
                {
                    files.Remove(file);
                }
                else
                {
                    files[i] = file.Substring(0, file.Length - 4);
                    i++;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            mouseState = Mouse.GetState();
            keyboardState = Keyboard.GetState();

            SimpleButton.UpdateAll(mouseState, keyboardState);

            SimpleButton[] buttons = SimpleButton.AllButtons;
            for (int i = 0; i < buttons.Length; i++)
            {
                SimpleButton button = buttons[i];

                if (button.Triggered)
                {
                    cleanup();
                    returnControl(files[i] + ".muh");
                    return;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            GraphicsDevice.Clear(Color.Gray);

            spriteBatch.Begin();

            SimpleButton[] buttons = SimpleButton.AllButtons;
            //foreach (SimpleButton button in SimpleButton.AllButtons)
            for (int i = 0; i < buttons.Length; i++)
            {
                SimpleButton button = buttons[i];

                spriteBatch.Draw(button.Texture, button.Rectangle, Color.White);
                if (button.Pressing)
                    spriteBatch.Draw(ColorTexture.White, button.Rectangle, Color.White * .25f);

                string fileName = files[i].Substring(12);
                Vector2 stringSize = buttonFont.MeasureString(fileName);
                spriteBatch.DrawString(buttonFont, fileName, new Vector2((int)(button.X + button.Width / 2 - stringSize.X / 2), (int)(button.Y + button.Height / 2 - stringSize.Y / 2)), Color.White);
            }

            spriteBatch.End();
        }

        void cleanup()
        {
            SimpleButton.RemoveAllButtons();
        }
    }
}

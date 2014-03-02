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
    class StartMenu : GameState
    {
        static SpriteFont buttonFont;

        Viewport viewport;

        MouseState mouseState;
        KeyboardState keyboardState;

        SimpleButton loadButton, newButton;
        SimpleButton widthUpButton, widthDownButton, heightUpButton, heightDownButton;

        int buttonWidth = 100, buttonHeight = 52;
        int mapWidth = 96, mapHeight = 96;

        public StartMenu(EventHandler callback)
            : base(callback)
        {
            viewport = GraphicsDevice.Viewport;

            if (!contentLoaded)
            {
                buttonFont = Content.Load<SpriteFont>("buttonFont");

                contentLoaded = true;
            }

            loadButton = new SimpleButton(new Rectangle(viewport.Width / 10 * 4 - buttonWidth / 2, viewport.Height / 2 - buttonHeight / 2, buttonWidth, buttonHeight), ColorTexture.Black, null, null);
            SimpleButton.AddButton(loadButton);

            newButton = new SimpleButton(new Rectangle(viewport.Width / 10 * 6 - buttonWidth / 2, viewport.Height / 2 - buttonHeight / 2, buttonWidth, buttonHeight), ColorTexture.Black, null, null);
            SimpleButton.AddButton(newButton);

            int width = buttonWidth / 4;
            int height = buttonHeight / 4;

            widthUpButton = new SimpleButton(new Rectangle(newButton.X + newButton.Width, newButton.Y, width, height), ColorTexture.Black, null, null);
            SimpleButton.AddButton(widthUpButton);

            widthDownButton = new SimpleButton(new Rectangle(newButton.X + newButton.Width, newButton.Y + height, width, height), ColorTexture.Black, null, null);
            SimpleButton.AddButton(widthDownButton);

            heightUpButton = new SimpleButton(new Rectangle(newButton.X + newButton.Width, newButton.Y + height * 2, width, height), ColorTexture.Black, null, null);
            SimpleButton.AddButton(heightUpButton);

            heightDownButton = new SimpleButton(new Rectangle(newButton.X + newButton.Width, newButton.Y + height * 3, width, height), ColorTexture.Black, null, null);
            SimpleButton.AddButton(heightDownButton);
        }

        public override void Update(GameTime gameTime)
        {
            mouseState = Mouse.GetState();
            keyboardState = Keyboard.GetState();

            SimpleButton.UpdateAll(mouseState, keyboardState);

            if (newButton.Triggered)
            {
                cleanup();
                returnControl("new", mapWidth.ToString(), mapHeight.ToString());
                return;
            }

            if (loadButton.Triggered)
            {
                cleanup();
                returnControl("load");
                return;
            }

            if (widthUpButton.Triggered)
            {
                //if ((mapWidth + 16) * mapHeight <= 37000)
                //    mapWidth += 16;
                mapWidth = (int)MathHelper.Clamp(mapWidth + 16, 48, 192);
            }
            if (widthDownButton.Triggered)
            {
                //if (mapWidth - 16 >= 48)
                //    mapWidth -= 16;
                mapWidth = (int)MathHelper.Clamp(mapWidth - 16, 48, 192);
            }
            if (heightUpButton.Triggered)
            {
                //if ((mapHeight + 16) * mapWidth <= 37000)
                //    mapHeight += 16;
                mapHeight = (int)MathHelper.Clamp(mapHeight + 16, 48, 192);
            }
            if (heightDownButton.Triggered)
            {
                //if (mapHeight - 16 >= 48)
                //    mapHeight -= 16;
                mapHeight = (int)MathHelper.Clamp(mapHeight - 16, 48, 192);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            GraphicsDevice.Clear(Color.Gray);

            spriteBatch.Begin();

            foreach (SimpleButton button in SimpleButton.AllButtons)
            {
                //if (button == loadButton || button == newButton)
                    spriteBatch.Draw(button.Texture, button.Rectangle, Color.White);
                //else
                 //   spriteBatch.Draw(button.Texture, button.Rectangle, Color.White * .90f);
                if (button.Pressing)
                    spriteBatch.Draw(ColorTexture.White, button.Rectangle, Color.White * .25f);
            }

            Util.DrawStringAtCenterOfRectangle(spriteBatch, buttonFont, "Load Map", loadButton.Rectangle, Color.White);
           
            Util.DrawStringAtCenterOfRectangle(spriteBatch, buttonFont, "New Map", newButton.Rectangle, Color.White);
            
            string upString = "+";
            Vector2 upStringSize = buttonFont.MeasureString(upString);
            spriteBatch.DrawString(buttonFont, upString, new Vector2((int)(widthUpButton.X + widthUpButton.Width / 2 - upStringSize.X / 2), (int)(widthUpButton.Y + widthUpButton.Height / 2 - upStringSize.Y / 2)), Color.White);
            spriteBatch.DrawString(buttonFont, upString, new Vector2((int)(heightUpButton.X + heightUpButton.Width / 2 - upStringSize.X / 2), (int)(heightUpButton.Y + heightUpButton.Height / 2 - upStringSize.Y / 2)), Color.White);

            string downString = "-";
            Vector2 downStringSize = buttonFont.MeasureString(downString);
            spriteBatch.DrawString(buttonFont, downString, new Vector2((int)(widthDownButton.X + widthDownButton.Width / 2 - downStringSize.X / 2), (int)(widthDownButton.Y + widthDownButton.Height / 2 - downStringSize.Y / 2)), Color.White);
            spriteBatch.DrawString(buttonFont, downString, new Vector2((int)(heightDownButton.X + heightDownButton.Width / 2 - downStringSize.X / 2), (int)(heightDownButton.Y + heightDownButton.Height / 2 - downStringSize.Y / 2)), Color.White);

            string widthString = "Width: " + mapWidth;
            Vector2 widthStringSize = buttonFont.MeasureString(widthString);
            spriteBatch.DrawString(buttonFont, widthString, new Vector2((int)(widthUpButton.X + widthUpButton.Width + 5), (int)(newButton.Y + newButton.Height / 4 - widthStringSize.Y / 2)), Color.White);

            string heightString = "Height: " + mapHeight;
            Vector2 heightStringSize = buttonFont.MeasureString(heightString);
            spriteBatch.DrawString(buttonFont, heightString, new Vector2((int)(widthUpButton.X + widthUpButton.Width + 5), (int)(newButton.Y + newButton.Height / 4 * 3 - heightStringSize.Y / 2)), Color.White);

            spriteBatch.End();
        }

        void cleanup()
        {
            SimpleButton.RemoveAllButtons();
        }
    }
}

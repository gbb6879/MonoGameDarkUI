using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DarkUI.Core.Controls;
using DarkUI.MonoGame;

namespace GameProject
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private UIRenderer _uiRenderer;
        private UIManager _uiManager;
        private SpriteFont _defaultFont;
        private MouseState _prevMouseState;
        
        // UI Panels
        private UIPanel _toolPanel;
        private UIPanel _propertyPanel;
        private UIPanel _explorerPanel;
        private UIPanel _contentPanel;
        
        // UI Controls
        private UILabel _statusLabel;
        private UIButton _createPanelButton;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            // Load the default font
            _defaultFont = Content.Load<SpriteFont>("defaultFont");
            
            // Initialize UI renderer and manager
            _uiRenderer = new UIRenderer(GraphicsDevice, _spriteBatch, _defaultFont);
            _uiManager = new UIManager(_uiRenderer);
            _uiManager.SetScreenBounds(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            
            // Create a status panel at the bottom
            UIPanel statusPanel = new UIPanel(0, _graphics.PreferredBackBufferHeight - 30, _graphics.PreferredBackBufferWidth, 30, "Status");
            statusPanel.ShowTitle = false;
            statusPanel.IsDockable = false;
            
            _statusLabel = new UILabel(10, 5, _graphics.PreferredBackBufferWidth - 20, 20, "Drag panels to dock them to the edges");
            _statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            statusPanel.AddControl(_statusLabel);
            
            // Create a tool panel (left side)
            _toolPanel = new UIPanel(10, 10, 200, 300, "Tools");
            _toolPanel.DockSize = 0.15f; // 15% of screen width when docked
            
            for (int i = 0; i < 5; i++)
            {
                UIButton toolButton = new UIButton(20, 40 + (i * 40), 160, 30, $"Tool {i+1}");
                toolButton.Click += (sender, e) => {
                    _statusLabel.Text = $"Selected {((UIButton)sender).Text}";
                };
                _toolPanel.AddControl(toolButton);
            }
            
            // Create a property panel (right side)
            _propertyPanel = new UIPanel(_graphics.PreferredBackBufferWidth - 210, 10, 200, 400, "Properties");
            _propertyPanel.DockSize = 0.2f; // 20% of screen width when docked
            
            for (int i = 0; i < 4; i++)
            {
                UILabel propLabel = new UILabel(10, 40 + (i * 60), 180, 20, $"Property {i+1}:");
                propLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                
                UIButton propButton = new UIButton(10, 65 + (i * 60), 180, 25, $"Value {i+1}");
                propButton.Click += (sender, e) => {
                    _statusLabel.Text = $"Changed {propLabel.Text} to {((UIButton)sender).Text}";
                };
                
                _propertyPanel.AddControl(propLabel);
                _propertyPanel.AddControl(propButton);
            }
            
            // Create an explorer panel (bottom half)
            _explorerPanel = new UIPanel(220, 320, 400, 200, "Explorer");
            _explorerPanel.DockSize = 0.25f; // 25% of screen height when docked
            
            UILabel explorerInfo = new UILabel(20, 40, 360, 120, 
                "This panel demonstrates fluid docking.\n\n" +
                "Drag any panel by its title bar and move it to the edge of the screen to dock.\n\n" + 
                "You can dock panels to any edge: left, right, top, or bottom, or drag to the center for full-screen.");
            explorerInfo.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            _explorerPanel.AddControl(explorerInfo);
            
            // Create a content panel (top-middle)
            _contentPanel = new UIPanel(220, 10, 400, 300, "Content");
            
            UILabel contentInfo = new UILabel(20, 40, 360, 30, "Drag this panel to any edge to dock it.");
            contentInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            _createPanelButton = new UIButton(120, 100, 160, 30, "Create Floating Panel");
            _createPanelButton.Click += (sender, e) => {
                CreateFloatingPanel();
            };
            
            UIButton resetButton = new UIButton(120, 150, 160, 30, "Reset All Panels");
            resetButton.Click += (sender, e) => {
                ResetPanels();
            };
            
            _contentPanel.AddControl(contentInfo);
            _contentPanel.AddControl(_createPanelButton);
            _contentPanel.AddControl(resetButton);
            
            // Add panels to the UI manager
            _uiManager.AddControl(_toolPanel);
            _uiManager.AddControl(_propertyPanel);
            _uiManager.AddControl(_explorerPanel);
            _uiManager.AddControl(_contentPanel);
            _uiManager.AddControl(statusPanel);
        }
        
        private void CreateFloatingPanel()
        {
            // Generate a random position
            System.Random random = new System.Random();
            int x = random.Next(50, _graphics.PreferredBackBufferWidth - 250);
            int y = random.Next(50, _graphics.PreferredBackBufferHeight - 250);
            
            UIPanel floatingPanel = new UIPanel(x, y, 200, 200, "Floating Panel");
            
            UILabel infoLabel = new UILabel(20, 40, 160, 60, "This is a floating panel.\nDrag me to an edge to dock.");
            infoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            UIButton closeButton = new UIButton(65, 120, 70, 30, "Close");
            closeButton.Click += (sender, e) => {
                _uiManager.RemoveControl(floatingPanel);
                _statusLabel.Text = "Floating panel closed";
            };
            
            floatingPanel.AddControl(infoLabel);
            floatingPanel.AddControl(closeButton);
            
            _uiManager.AddControl(floatingPanel);
            _statusLabel.Text = "Created new floating panel";
        }
        
        private void ResetPanels()
        {
            // Reset all panels to their original positions
            _toolPanel.SetDock(UIPanel.DockStyle.None);
            _propertyPanel.SetDock(UIPanel.DockStyle.None);
            _explorerPanel.SetDock(UIPanel.DockStyle.None);
            _contentPanel.SetDock(UIPanel.DockStyle.None);
            
            _toolPanel.Bounds = new System.Drawing.Rectangle(10, 10, 200, 300);
            _propertyPanel.Bounds = new System.Drawing.Rectangle(_graphics.PreferredBackBufferWidth - 210, 10, 200, 400);
            _explorerPanel.Bounds = new System.Drawing.Rectangle(220, 320, 400, 200);
            _contentPanel.Bounds = new System.Drawing.Rectangle(220, 10, 400, 300);
            
            _statusLabel.Text = "All panels reset to original positions";
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
                
            // Get mouse state
            MouseState mouseState = Mouse.GetState();
            bool mouseDown = mouseState.LeftButton == ButtonState.Pressed;
            Point mousePosition = new Point(mouseState.X, mouseState.Y);
            
            // Update UI
            _uiManager.Update(gameTime, mousePosition, mouseDown);
            
            // Store mouse state for next frame
            _prevMouseState = mouseState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw UI
            _uiManager.Draw(gameTime);

            base.Draw(gameTime);
        }
    }
}

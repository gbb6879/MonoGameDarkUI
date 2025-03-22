using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DarkUI.Core.Controls;
using DarkUI.MonoGame;
using DarkUI.Core.Logging;
using System;

namespace GameProject
{
    public class Game1 : Game
    {
        // Static instance for global access
        public static Game1 Instance { get; private set; }
        
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private UIRenderer _uiRenderer;
        private UIManager _uiManager;
        private SpriteFont _defaultFont;
        private MouseState _prevMouseState;
        private KeyboardState _prevKeyboardState;
        
        // UI Panels
        private UIPanel _toolPanel;
        private UIPanel _propertyPanel;
        private UIPanel _explorerPanel;
        private UIPanel _contentPanel;
        
        // UI Controls
        private UILabel _statusLabel;
        private UIButton _createPanelButton;
        private UIButton _crashButton;
        private UIButton _viewLogButton;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            
            // Set static instance
            Instance = this;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
            
            // Initialize the crash logger
            CrashLogger.Initialize();
            
            // Set up global exception handling
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
                var exception = args.ExceptionObject as Exception;
                if (exception != null)
                {
                    CrashLogger.LogException(exception);
                }
            };
            
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
#pragma warning disable CA1416 // Validate platform compatibility
            _statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
#pragma warning restore CA1416
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
#pragma warning disable CA1416 // Validate platform compatibility
                propLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
#pragma warning restore CA1416
                
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
#pragma warning disable CA1416 // Validate platform compatibility
            explorerInfo.TextAlign = System.Drawing.ContentAlignment.TopLeft;
#pragma warning restore CA1416
            _explorerPanel.AddControl(explorerInfo);
            
            // Create a content panel (top-middle)
            _contentPanel = new UIPanel(220, 10, 400, 300, "Content");
            
            UILabel contentInfo = new UILabel(20, 40, 360, 30, "Drag this panel to any edge to dock it.");
#pragma warning disable CA1416 // Validate platform compatibility
            contentInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
#pragma warning restore CA1416
            
            _createPanelButton = new UIButton(120, 100, 160, 30, "Create Floating Panel");
            _createPanelButton.Click += (sender, e) => {
                CreateFloatingPanel();
            };
            
            _crashButton = new UIButton(120, 200, 160, 30, "Test Crash Log");
            _crashButton.Click += (sender, e) => {
                TriggerTestException();
            };
            
            _viewLogButton = new UIButton(120, 250, 160, 30, "View Crash Logs");
            _viewLogButton.Click += (sender, e) => {
                ShowCrashLogViewer();
            };
            
            UIButton resetButton = new UIButton(120, 150, 160, 30, "Reset All Panels");
            resetButton.Click += (sender, e) => {
                ResetPanels();
            };
            
            _contentPanel.AddControl(contentInfo);
            _contentPanel.AddControl(_createPanelButton);
            _contentPanel.AddControl(resetButton);
            _contentPanel.AddControl(_crashButton);
            _contentPanel.AddControl(_viewLogButton);
            
            // Add all the panels to the UI manager
            _uiManager.AddControl(_contentPanel);
            _uiManager.AddControl(statusPanel);
            
            // Add docked panels using the new method to ensure proper docking
            _uiManager.AddDockedPanel(_toolPanel, UIPanel.DockStyle.Left);
            _uiManager.AddDockedPanel(_propertyPanel, UIPanel.DockStyle.Right);
            _uiManager.AddDockedPanel(_explorerPanel, UIPanel.DockStyle.Bottom);
        }
        
        private void CreateFloatingPanel()
        {
            // Generate a random position
            System.Random random = new System.Random();
            int x = random.Next(50, _graphics.PreferredBackBufferWidth - 250);
            int y = random.Next(50, _graphics.PreferredBackBufferHeight - 250);
            
            UIPanel floatingPanel = new UIPanel(x, y, 200, 200, "Floating Panel");
            
            UILabel infoLabel = new UILabel(20, 40, 160, 60, "This is a floating panel.\nDrag me to an edge to dock.");
#pragma warning disable CA1416 // Validate platform compatibility
            infoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
#pragma warning restore CA1416
            
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

        private void TriggerTestException()
        {
            try
            {
                // Create a test exception
                _statusLabel.Text = "Triggering test exception...";
                
                // Generate a division by zero exception
                int zero = 0;
                int result = 10 / zero; // This will cause a DivideByZeroException
            }
            catch (Exception ex)
            {
                // Log the exception
                CrashLogger.LogException(ex);
                
                // Show the error in the status label
                _statusLabel.Text = $"Exception logged: {ex.Message}";
            }
        }

        private void ShowCrashLogViewer()
        {
            // Position the log viewer in the center of the screen
            int logWidth = 600;
            int logHeight = 400;
            int logX = (_graphics.PreferredBackBufferWidth - logWidth) / 2;
            int logY = (_graphics.PreferredBackBufferHeight - logHeight) / 2;
            
            // Create and add the crash log panel
            CrashLogPanel logPanel = new CrashLogPanel(logX, logY, logWidth, logHeight);
            _uiManager.AddControl(logPanel);
            
            // Update status
            _statusLabel.Text = "Opened crash log viewer";
        }
        
        // Remove a crash log panel from the UI
        public void RemoveCrashLogPanel(UIPanel panel)
        {
            if (panel != null && _uiManager != null)
            {
                _uiManager.RemoveControl(panel);
                _statusLabel.Text = "Closed crash log viewer";
            }
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            
            // Toggle debug mode
            if (keyboardState.IsKeyDown(Keys.F1) && !_prevKeyboardState.IsKeyDown(Keys.F1))
            {
                _uiManager.ShowDebugInfo = !_uiManager.ShowDebugInfo;
                
                // Log panel state when debug is enabled
                if (_uiManager.ShowDebugInfo)
                {
                    _uiManager.LogPanelState();
                }
            }
            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
                keyboardState.IsKeyDown(Keys.Escape))
                Exit();
                
            // Update UI
            _uiManager.Update(gameTime, mouseState.Position, mouseState.LeftButton == ButtonState.Pressed);
            
            // Store mouse state for next frame
            _prevMouseState = mouseState;
            _prevKeyboardState = keyboardState;

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

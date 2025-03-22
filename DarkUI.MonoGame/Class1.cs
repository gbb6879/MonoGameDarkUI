using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DarkUI.Core;
using DarkUI.Core.Controls;
using System.Linq;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Color = Microsoft.Xna.Framework.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingColor = System.Drawing.Color;

namespace DarkUI.MonoGame
{
    public class UIRenderer
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _defaultFont;
        private Dictionary<DrawingColor, Color> _colorCache = new Dictionary<DrawingColor, Color>();
        private readonly Dictionary<string, SpriteFont> _fonts = new Dictionary<string, SpriteFont>();
        private Texture2D _whitePixel;
        
        // Cached textures for UI elements
        private Texture2D _dockPreviewTexture;
        
        // Dock preview overlay
        private Rectangle _dockPreviewRect = Rectangle.Empty;
        private Color _dockPreviewColor = new Color(0, 120, 215, 100); // Semi-transparent blue
        
        // Animation properties
        private const float AnimationSpeed = 8.0f; // Speed of animation (higher = faster)
        private Dictionary<UIPanel, Rectangle> _targetBounds = new Dictionary<UIPanel, Rectangle>();
        private Dictionary<UIPanel, Rectangle> _currentBounds = new Dictionary<UIPanel, Rectangle>();
        
        public UIRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont defaultFont)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _defaultFont = defaultFont;
            
            // Initialize textures
            InitializeTextures(graphicsDevice);
        }
        
        private void InitializeTextures(GraphicsDevice graphicsDevice)
        {
            // Create single white pixel texture for rectangle drawing
            _whitePixel = new Texture2D(graphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
            
            // Create dock preview texture
            _dockPreviewTexture = new Texture2D(graphicsDevice, 1, 1);
            _dockPreviewTexture.SetData(new[] { Color.White });
        }
        
        public void RegisterFont(string name, SpriteFont font)
        {
            _fonts[name] = font;
        }
        
        // No longer needed since _defaultFont is readonly
        // public void SetDefaultFont(SpriteFont font)
        // {
        //     _defaultFont = font;
        // }
        
        public void Begin()
        {
            _spriteBatch.Begin();
        }
        
        public void End()
        {
            _spriteBatch.End();
        }
        
        public void DrawControl(UIControl control)
        {
            // Force all children of docked panels to be visible too
            bool isChildOfDockedPanel = false;
            if (control.Parent is UIPanel parentPanel && parentPanel.Dock != UIPanel.DockStyle.None)
            {
                isChildOfDockedPanel = true;
                control.Visible = true;
                
                // Debug visibility logging for child controls of docked panels
                if (control.GetType().Name != "UILabel" && control.GetType().Name != "UIButton")
                {
                    Console.WriteLine($"Child control {control.GetType().Name} of {parentPanel.Title} set to visible");
                }
            }
            
            // Skip invisible controls, but never skip children of docked panels
            if (!control.Visible && !isChildOfDockedPanel)
                return;
                
            if (control is UIPanel panel)
            {
                // Log panel drawing
                if (panel.Dock != UIPanel.DockStyle.None)
                {
                    Console.WriteLine($"Drawing docked panel {panel.Title} with dock style {panel.Dock}");
                }
                
                // Update animated position if needed
                UpdatePanelAnimation(panel, null);
                
                DrawPanel(panel);
                
                // Draw docking preview if this panel is being dragged
                if (panel.IsDragging() && panel.ShowDockingPreview)
                {
                    DockZone activeDockZone = panel.GetActiveDockZone();
                    if (activeDockZone != null)
                    {
                        // Draw a semi-transparent preview of where the panel will dock
                        switch (activeDockZone.DockStyle)
                        {
                            case UIPanel.DockStyle.Left:
                                _dockPreviewRect = new Rectangle(
                                    activeDockZone.ParentBounds.X,
                                    activeDockZone.ParentBounds.Y,
                                    (int)(activeDockZone.ParentBounds.Width * panel.DockSize),
                                    activeDockZone.ParentBounds.Height);
                                break;
                            case UIPanel.DockStyle.Right:
                                _dockPreviewRect = new Rectangle(
                                    activeDockZone.ParentBounds.X + activeDockZone.ParentBounds.Width - (int)(activeDockZone.ParentBounds.Width * panel.DockSize),
                                    activeDockZone.ParentBounds.Y,
                                    (int)(activeDockZone.ParentBounds.Width * panel.DockSize),
                                    activeDockZone.ParentBounds.Height);
                                break;
                            case UIPanel.DockStyle.Top:
                                _dockPreviewRect = new Rectangle(
                                    activeDockZone.ParentBounds.X,
                                    activeDockZone.ParentBounds.Y,
                                    activeDockZone.ParentBounds.Width,
                                    (int)(activeDockZone.ParentBounds.Height * panel.DockSize));
                                break;
                            case UIPanel.DockStyle.Bottom:
                                _dockPreviewRect = new Rectangle(
                                    activeDockZone.ParentBounds.X,
                                    activeDockZone.ParentBounds.Y + activeDockZone.ParentBounds.Height - (int)(activeDockZone.ParentBounds.Height * panel.DockSize),
                                    activeDockZone.ParentBounds.Width,
                                    (int)(activeDockZone.ParentBounds.Height * panel.DockSize));
                                break;
                            case UIPanel.DockStyle.Fill:
                                _dockPreviewRect = new Rectangle(
                                    activeDockZone.ParentBounds.X,
                                    activeDockZone.ParentBounds.Y,
                                    activeDockZone.ParentBounds.Width,
                                    activeDockZone.ParentBounds.Height);
                                break;
                        }
                        
                        _spriteBatch.Draw(_dockPreviewTexture, 
                            new Rectangle(
                                _dockPreviewRect.X, 
                                _dockPreviewRect.Y, 
                                _dockPreviewRect.Width, 
                                _dockPreviewRect.Height), 
                            _dockPreviewColor);
                    }
                }
                
                // Draw all child controls
                foreach (var child in panel.Controls)
                {
                    // Ensure child controls of docked panels are visible
                    if (panel.Dock != UIPanel.DockStyle.None)
                    {
                        child.Visible = true;
                    }
                    
                    DrawControl(child);
                }
            }
            else if (control is UIButton button)
            {
                DrawButton(button);
            }
            else if (control is UILabel label)
            {
                DrawLabel(label);
            }
        }
        
        public void UpdatePanelAnimation(UIPanel panel, GameTime gameTime)
        {
            // Get elapsed time for smoother animation
            float deltaTime = gameTime != null ? (float)gameTime.ElapsedGameTime.TotalSeconds : 1.0f/60.0f;
            float speed = AnimationSpeed * deltaTime * 60.0f; // Normalize to 60fps
            
            // Initialize tracking for new panels
            if (!_targetBounds.ContainsKey(panel))
            {
                _targetBounds[panel] = new Rectangle(
                    panel.Bounds.X,
                    panel.Bounds.Y,
                    panel.Bounds.Width,
                    panel.Bounds.Height);
                    
                _currentBounds[panel] = _targetBounds[panel];
            }
            
            // Update target if panel position changed
            if (_targetBounds[panel].X != panel.Bounds.X ||
                _targetBounds[panel].Y != panel.Bounds.Y ||
                _targetBounds[panel].Width != panel.Bounds.Width ||
                _targetBounds[panel].Height != panel.Bounds.Height)
            {
                _targetBounds[panel] = new Rectangle(
                    panel.Bounds.X,
                    panel.Bounds.Y,
                    panel.Bounds.Width,
                    panel.Bounds.Height);
            }
            // If panel is being dragged, don't animate - use actual position
            if (panel.IsDragging())
            {
                _currentBounds[panel] = new Rectangle(
                    panel.Bounds.X,
                    panel.Bounds.Y,
                    panel.Bounds.Width,
                    panel.Bounds.Height);
                return;
            }
            
            // Animate towards target position if not already there
            Rectangle current = _currentBounds[panel];
            Rectangle target = _targetBounds[panel];
            
            if (current != target)
            {
                // Calculate new position with smooth easing
                int newX = (int)MathHelper.Lerp(current.X, target.X, speed);
                int newY = (int)MathHelper.Lerp(current.Y, target.Y, speed);
                int newWidth = (int)MathHelper.Lerp(current.Width, target.Width, speed);
                int newHeight = (int)MathHelper.Lerp(current.Height, target.Height, speed);
                
                // Check if we're close enough to snap to target - use safe Abs implementation
                bool xClose = SafeAbs(newX - target.X) <= 1;
                bool yClose = SafeAbs(newY - target.Y) <= 1;
                bool widthClose = SafeAbs(newWidth - target.Width) <= 1;
                bool heightClose = SafeAbs(newHeight - target.Height) <= 1;
                
                if (xClose && yClose && widthClose && heightClose)
                {
                    _currentBounds[panel] = target;
                }
                else
                {
                    _currentBounds[panel] = new Rectangle(newX, newY, newWidth, newHeight);
                }
            }
        }
        
        // Safe implementation of Math.Abs that handles Int32.MinValue
        private int SafeAbs(int value)
        {
            if (value == int.MinValue)
                return int.MaxValue; // Return max value as a reasonable approximation
            return Math.Abs(value);
        }
        
        private void DrawPanel(UIPanel panel)
        {
            // Only skip truly invisible panels that are NOT docked
            if (!panel.Visible && !panel.IsDocked && !panel.IsDragging())
                return;
            
            // Force visibility for docked panels
            if (panel.IsDocked)
            {
                panel.Visible = true;
            }
            
            // Use animated bounds for drawing
            Rectangle bounds = _currentBounds.ContainsKey(panel) ? 
                _currentBounds[panel] : 
                new Rectangle(panel.Bounds.X, panel.Bounds.Y, panel.Bounds.Width, panel.Bounds.Height);
            
            // Draw panel background with more opaque color
            Color backgroundColor = ToXnaColor(Colors.BackgroundColor);
            backgroundColor.A = 255; // Full opacity
            
            DrawRectangle(
                new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                backgroundColor,
                true);
            
            // Draw panel border with more opaque color
            Color borderColor = ToXnaColor(Colors.BorderColor);
            borderColor.A = 255; // Full opacity
            
            DrawRectangle(
                new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                borderColor,
                false);
            
            // Draw title bar if needed
            if (panel.ShowTitle)
            {
                // Draw title background with more opacity
                Color titleBgColor = ToXnaColor(Colors.DarkBackgroundColor);
                titleBgColor.A = 255; // Full opacity
                
                DrawRectangle(
                    new Rectangle(bounds.X, bounds.Y, bounds.Width, Theme.PanelTitleHeight),
                    titleBgColor,
                    true);
                
                // Draw title text with more opacity
                Color textColor = ToXnaColor(Colors.TextColor);
                textColor.A = 255; // Full opacity
                
                Vector2 textSize = _defaultFont.MeasureString(panel.Title);
                float x = bounds.X + Theme.ControlPadding;
                float y = bounds.Y + (Theme.PanelTitleHeight - textSize.Y) / 2;
                
                _spriteBatch.DrawString(_defaultFont, panel.Title, new Vector2(x, y), textColor);
            }
            
            // Draw child controls
            foreach (var control in panel.Controls)
            {
                // Don't draw invisible controls
                if (!control.Visible)
                    continue;
                    
                // Force visibility for all controls in docked panels
                if (panel.IsDocked)
                {
                    control.Visible = true;
                }
                
                // Translate control's position to be relative to the panel
                DrawingRectangle originalBounds = control.Bounds;
                control.Bounds = new DrawingRectangle(
                    bounds.X + control.Bounds.X,
                    bounds.Y + control.Bounds.Y + (panel.ShowTitle ? Theme.PanelTitleHeight : 0),
                    control.Bounds.Width,
                    control.Bounds.Height);
                
                DrawControl(control);
                
                // Restore original bounds
                control.Bounds = originalBounds;
            }
        }
        
        private void DrawButton(UIButton button)
        {
            // Skip if not visible
            if (!button.Visible)
                return;
                
            Color backgroundColor;

            if (!button.Enabled)
            {
                backgroundColor = ToXnaColor(Colors.ButtonColor) * 0.7f;
                backgroundColor.A = 255; // Ensure full opacity
            }
            else if (button.IsToggleButton && button.Checked)
            {
                backgroundColor = ToXnaColor(Colors.ButtonPressedColor);
                backgroundColor.A = 255; // Ensure full opacity
            }
            else if (button.IsMouseDown)
            {
                backgroundColor = ToXnaColor(Colors.ButtonPressedColor);
                backgroundColor.A = 255; // Ensure full opacity
            }
            else if (button.IsMouseOver)
            {
                backgroundColor = ToXnaColor(Colors.ButtonHoverColor);
                backgroundColor.A = 255; // Ensure full opacity
            }
            else
            {
                backgroundColor = ToXnaColor(Colors.ButtonColor);
                backgroundColor.A = 255; // Ensure full opacity
            }
            
            // Draw button background
            DrawRectangle(
                new Rectangle(button.Bounds.X, button.Bounds.Y, button.Bounds.Width, button.Bounds.Height),
                backgroundColor,
                true);
            
            // Draw button border
            Color borderColor = ToXnaColor(Colors.BorderColor);
            borderColor.A = 255; // Ensure full opacity
            
            DrawRectangle(
                new Rectangle(button.Bounds.X, button.Bounds.Y, button.Bounds.Width, button.Bounds.Height),
                borderColor,
                false);
            
            // Draw text
            Color textColor = button.Enabled 
                ? ToXnaColor(Colors.TextColor) 
                : ToXnaColor(Colors.DisabledTextColor);
            
            // Ensure full opacity
            textColor.A = 255;
            
            Vector2 textSize = _defaultFont.MeasureString(button.Text);
            float x = button.Bounds.X + (button.Bounds.Width - textSize.X) / 2;
            float y = button.Bounds.Y + (button.Bounds.Height - textSize.Y) / 2;
            
            _spriteBatch.DrawString(_defaultFont, button.Text, new Vector2(x, y), textColor);
        }
        
        private void DrawLabel(UILabel label)
        {
            // Skip if not visible
            if (!label.Visible)
                return;
                
            if (string.IsNullOrEmpty(label.Text))
                return;
                
            // Get text color with full opacity
            Color textColor = ToXnaColor(Colors.TextColor);
            textColor.A = 255; // Ensure full opacity
            
            Vector2 textSize = _defaultFont.MeasureString(label.Text);
            float x, y;
            
            // Calculate x position based on alignment
            switch (label.TextAlign)
            {
                case System.Drawing.ContentAlignment.TopLeft:
                case System.Drawing.ContentAlignment.MiddleLeft:
                case System.Drawing.ContentAlignment.BottomLeft:
                    x = label.Bounds.X;
                    break;
                    
                case System.Drawing.ContentAlignment.TopCenter:
                case System.Drawing.ContentAlignment.MiddleCenter:
                case System.Drawing.ContentAlignment.BottomCenter:
                    x = label.Bounds.X + (label.Bounds.Width - textSize.X) / 2;
                    break;
                    
                case System.Drawing.ContentAlignment.TopRight:
                case System.Drawing.ContentAlignment.MiddleRight:
                case System.Drawing.ContentAlignment.BottomRight:
                    x = label.Bounds.X + label.Bounds.Width - textSize.X;
                    break;
                    
                default:
                    x = label.Bounds.X;
                    break;
            }
            
            // Calculate y position based on alignment
            switch (label.TextAlign)
            {
                case System.Drawing.ContentAlignment.TopLeft:
                case System.Drawing.ContentAlignment.TopCenter:
                case System.Drawing.ContentAlignment.TopRight:
                    y = label.Bounds.Y;
                    break;
                    
                case System.Drawing.ContentAlignment.MiddleLeft:
                case System.Drawing.ContentAlignment.MiddleCenter:
                case System.Drawing.ContentAlignment.MiddleRight:
                    y = label.Bounds.Y + (label.Bounds.Height - textSize.Y) / 2;
                    break;
                    
                case System.Drawing.ContentAlignment.BottomLeft:
                case System.Drawing.ContentAlignment.BottomCenter:
                case System.Drawing.ContentAlignment.BottomRight:
                    y = label.Bounds.Y + label.Bounds.Height - textSize.Y;
                    break;
                    
                default:
                    y = label.Bounds.Y;
                    break;
            }
            
            _spriteBatch.DrawString(_defaultFont, label.Text, new Vector2(x, y), textColor);
        }
        
        public void DrawRectangle(Rectangle rectangle, Color color, bool filled = true)
        {
            if (filled)
            {
                _spriteBatch.Draw(_whitePixel, rectangle, color);
            }
            else
            {
                // Top
                _spriteBatch.Draw(_whitePixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1), color);
                // Bottom
                _spriteBatch.Draw(_whitePixel, new Rectangle(rectangle.X, rectangle.Y + rectangle.Height - 1, rectangle.Width, 1), color);
                // Left
                _spriteBatch.Draw(_whitePixel, new Rectangle(rectangle.X, rectangle.Y, 1, rectangle.Height), color);
                // Right
                _spriteBatch.Draw(_whitePixel, new Rectangle(rectangle.X + rectangle.Width - 1, rectangle.Y, 1, rectangle.Height), color);
            }
        }
        
        public void DrawRectangle(Rectangle rectangle, Color color, int borderThickness)
        {
            // Draw outline with specified thickness
            for (int i = 0; i < borderThickness; i++)
            {
                Rectangle outlineRect = new Rectangle(
                    rectangle.X - i,
                    rectangle.Y - i,
                    rectangle.Width + (i * 2),
                    rectangle.Height + (i * 2));
                
                // Top
                _spriteBatch.Draw(_whitePixel, new Rectangle(outlineRect.X, outlineRect.Y, outlineRect.Width, 1), color);
                // Bottom
                _spriteBatch.Draw(_whitePixel, new Rectangle(outlineRect.X, outlineRect.Y + outlineRect.Height - 1, outlineRect.Width, 1), color);
                // Left
                _spriteBatch.Draw(_whitePixel, new Rectangle(outlineRect.X, outlineRect.Y, 1, outlineRect.Height), color);
                // Right
                _spriteBatch.Draw(_whitePixel, new Rectangle(outlineRect.X + outlineRect.Width - 1, outlineRect.Y, 1, outlineRect.Height), color);
            }
        }
        
        private Color ToXnaColor(DrawingColor color)
        {
            // Check if the color is already in cache
            if (_colorCache.TryGetValue(color, out Color cachedColor))
                return cachedColor;
                
            // Create new color
            var xnaColor = new Color(color.R, color.G, color.B, color.A);
            
            // Add to cache using a new dictionary to avoid readonly field error
            var newCache = new Dictionary<DrawingColor, Color>(_colorCache)
            {
                [color] = xnaColor
            };
            
            // Replace the cache reference
            _colorCache = newCache;
            
            return xnaColor;
        }
        
        public SpriteFont GetFont(string name)
        {
            if (_fonts.TryGetValue(name, out SpriteFont font))
            {
                return font;
            }
            return _defaultFont;
        }
        
        public void DrawString(SpriteFont font, string text, Vector2 position, Color color)
        {
            _spriteBatch.DrawString(font, text, position, color);
        }
    }
    
    // Helper extensions for converting between System.Drawing and MonoGame types
    public static class RectangleExtensions
    {
        public static Rectangle ToXnaRectangle(this DrawingRectangle rect)
        {
            return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }
        
        public static DrawingRectangle ToDrawingRectangle(this Rectangle rect)
        {
            return new DrawingRectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }
        
        public static Color ToXnaColor(this DrawingColor color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }
    }
    
    public class UIManager
    {
        private readonly UIRenderer _renderer;
        private readonly List<UIControl> _controls = new List<UIControl>();
        private Rectangle _screenBounds;
        private List<DockZone> _dockZones = new List<DockZone>();
        private Rectangle? _remainingSpace;
        
        // Constants for docking zones
        private const int DockEdgeThreshold = 100;  // Size of the edge zone for docking
        private const int CenterDockZoneSize = 150; // Size of center zone for fill docking
        
        // Track docked panels
        private Dictionary<UIPanel.DockStyle, List<UIPanel>> _dockedPanels = new Dictionary<UIPanel.DockStyle, List<UIPanel>>();
        
        // Debug flags
        public bool ShowDebugInfo = true;
        
        public UIManager(UIRenderer renderer)
        {
            _renderer = renderer;
            
            // Initialize docked panels tracking
            foreach (UIPanel.DockStyle style in Enum.GetValues(typeof(UIPanel.DockStyle)))
            {
                _dockedPanels[style] = new List<UIPanel>();
            }
        }
        
        public void SetScreenBounds(int width, int height)
        {
            _screenBounds = new Rectangle(0, 0, width, height);
            
            // Create dock zones for screen edges
            _dockZones.Clear();
            
            // Left zone - covers the left edge of the screen
            _dockZones.Add(new DockZone(
                new Rectangle(0, 0, DockEdgeThreshold, height).ToDrawingRectangle(),
                new Rectangle(0, 0, width, height).ToDrawingRectangle(),
                UIPanel.DockStyle.Left));
                
            // Right zone
            _dockZones.Add(new DockZone(
                new Rectangle(width - DockEdgeThreshold, 0, DockEdgeThreshold, height).ToDrawingRectangle(),
                new Rectangle(0, 0, width, height).ToDrawingRectangle(),
                UIPanel.DockStyle.Right));
                
            // Top zone
            _dockZones.Add(new DockZone(
                new Rectangle(0, 0, width, DockEdgeThreshold).ToDrawingRectangle(),
                new Rectangle(0, 0, width, height).ToDrawingRectangle(),
                UIPanel.DockStyle.Top));
                
            // Bottom zone
            _dockZones.Add(new DockZone(
                new Rectangle(0, height - DockEdgeThreshold, width, DockEdgeThreshold).ToDrawingRectangle(),
                new Rectangle(0, 0, width, height).ToDrawingRectangle(),
                UIPanel.DockStyle.Bottom));
                
            // Center zone for Fill docking
            int centerSize = CenterDockZoneSize;
            _dockZones.Add(new DockZone(
                new Rectangle(
                    (width - centerSize) / 2,
                    (height - centerSize) / 2,
                    centerSize,
                    centerSize).ToDrawingRectangle(),
                new Rectangle(0, 0, width, height).ToDrawingRectangle(),
                UIPanel.DockStyle.Fill));
                
            // Recalculate all docked panels
            RecalculateDockedPanels();
        }
        
        private void RecalculateDockedPanels()
        {
            Console.WriteLine("RecalculateDockedPanels called - docked panels count:");
            
            // Verify that all panels with a dock style are in the correct list
            foreach (var control in _controls)
            {
                if (control is UIPanel panel)
                {
                    // Remove from any existing lists first
                    foreach (var style in _dockedPanels.Keys)
                    {
                        _dockedPanels[style].Remove(panel);
                    }
                    
                    // Add to the correct list based on current dock style
                    if (panel.Dock != UIPanel.DockStyle.None)
                    {
                        _dockedPanels[panel.Dock].Add(panel);
                        panel.Visible = true; // Force visibility
                        Console.WriteLine($"Added panel {panel.Title} to dock style {panel.Dock}");
                    }
                }
            }
            
            // Debug print dock panel counts
            foreach (var style in _dockedPanels.Keys)
            {
                Console.WriteLine($"Dock style {style}: {_dockedPanels[style].Count} panels");
            }
            
            // Ensure all docked panels are visible
            foreach (var styleList in _dockedPanels.Values)
            {
                foreach (var panel in styleList)
                {
                    panel.Visible = true;
                    Console.WriteLine($"Setting panel {panel.Title} to visible");
                }
            }
            
            // Check for overlapping panels first
            CheckForOverlappingPanels();
            
            // Process each dock style in a specific order to avoid conflicts
            // Process Fill last as it should take remaining space after other docks
            var dockOrder = new[] {
                UIPanel.DockStyle.Left,
                UIPanel.DockStyle.Right,
                UIPanel.DockStyle.Top,
                UIPanel.DockStyle.Bottom,
                UIPanel.DockStyle.Fill
            };
            
            // Start with full screen bounds
            var remainingBounds = new System.Drawing.Rectangle(
                _screenBounds.X, 
                _screenBounds.Y, 
                _screenBounds.Width, 
                _screenBounds.Height);
                
            // Process each dock style
            foreach (var dockStyle in dockOrder)
            {
                if (_dockedPanels.TryGetValue(dockStyle, out var panelsInDock) && panelsInDock.Count > 0)
                {
                    // Sort panels by their docking order
                    panelsInDock.Sort((a, b) => a.TabIndex.CompareTo(b.TabIndex));
                    
                    // Process based on dock style
                    switch (dockStyle)
                    {
                        case UIPanel.DockStyle.Left:
                            var leftWidth = 0;
                            foreach (var panel in panelsInDock)
                            {
                                int width = (int)(remainingBounds.Width * panel.DockSize);
                                panel.Bounds = new System.Drawing.Rectangle(
                                    remainingBounds.X + leftWidth, 
                                    remainingBounds.Y,
                                    width,
                                    remainingBounds.Height);
                                leftWidth += width;
                            }
                            // Update remaining bounds
                            remainingBounds = new System.Drawing.Rectangle(
                                remainingBounds.X + leftWidth,
                                remainingBounds.Y,
                                remainingBounds.Width - leftWidth,
                                remainingBounds.Height);
                            break;
                            
                        case UIPanel.DockStyle.Right:
                            var rightWidth = 0;
                            foreach (var panel in panelsInDock)
                            {
                                int width = (int)(remainingBounds.Width * panel.DockSize);
                                panel.Bounds = new System.Drawing.Rectangle(
                                    remainingBounds.Right - width - rightWidth, 
                                    remainingBounds.Y,
                                    width,
                                    remainingBounds.Height);
                                rightWidth += width;
                            }
                            // Update remaining bounds
                            remainingBounds = new System.Drawing.Rectangle(
                                remainingBounds.X,
                                remainingBounds.Y,
                                remainingBounds.Width - rightWidth,
                                remainingBounds.Height);
                            break;
                            
                        case UIPanel.DockStyle.Top:
                            var topHeight = 0;
                            foreach (var panel in panelsInDock)
                            {
                                int height = (int)(remainingBounds.Height * panel.DockSize);
                                panel.Bounds = new System.Drawing.Rectangle(
                                    remainingBounds.X, 
                                    remainingBounds.Y + topHeight,
                                    remainingBounds.Width,
                                    height);
                                topHeight += height;
                            }
                            // Update remaining bounds
                            remainingBounds = new System.Drawing.Rectangle(
                                remainingBounds.X,
                                remainingBounds.Y + topHeight,
                                remainingBounds.Width,
                                remainingBounds.Height - topHeight);
                            break;
                            
                        case UIPanel.DockStyle.Bottom:
                            var bottomHeight = 0;
                            foreach (var panel in panelsInDock)
                            {
                                int height = (int)(remainingBounds.Height * panel.DockSize);
                                panel.Bounds = new System.Drawing.Rectangle(
                                    remainingBounds.X, 
                                    remainingBounds.Bottom - height - bottomHeight,
                                    remainingBounds.Width,
                                    height);
                                bottomHeight += height;
                            }
                            // Update remaining bounds
                            remainingBounds = new System.Drawing.Rectangle(
                                remainingBounds.X,
                                remainingBounds.Y,
                                remainingBounds.Width,
                                remainingBounds.Height - bottomHeight);
                            break;
                            
                        case UIPanel.DockStyle.Fill:
                            // Fill panels just take the remaining space
                            foreach (var panel in panelsInDock)
                            {
                                panel.Bounds = remainingBounds;
                            }
                            break;
                    }
                }
            }
            _remainingSpace = remainingBounds.ToXnaRectangle();
        }
        
        private void CheckForOverlappingPanels()
        {
            List<UIPanel> allDockedPanels = new List<UIPanel>();
            
            // Get all docked panels
            foreach (var style in _dockedPanels.Keys)
            {
                if (style != UIPanel.DockStyle.None)
                {
                    allDockedPanels.AddRange(_dockedPanels[style]);
                }
            }
            
            // Check each panel against all others to find overlaps
            for (int i = 0; i < allDockedPanels.Count; i++)
            {
                for (int j = i + 1; j < allDockedPanels.Count; j++)
                {
                    var panel1 = allDockedPanels[i];
                    var panel2 = allDockedPanels[j];
                    
                    // Convert to System.Drawing.Rectangle for intersection test
                    var rect1 = panel1.Bounds;
                    var rect2 = panel2.Bounds;
                    
                    if (rect1.IntersectsWith(rect2))
                    {
                        // Found overlap, adjust TabIndex to fix z-order
                        // The panel with higher TabIndex will be drawn on top
                        // Make sure panels with different dock styles don't have the same TabIndex
                        if (panel1.Dock == panel2.Dock && panel1.TabIndex == panel2.TabIndex)
                        {
                            panel2.TabIndex = panel1.TabIndex + 1;
                        }
                        
                        // Log overlap for debugging
                        if (ShowDebugInfo)
                        {
                            System.Diagnostics.Debug.WriteLine($"Overlap detected between panels: " +
                                $"{panel1.Dock} at {rect1} and {panel2.Dock} at {rect2}");
                        }
                    }
                }
            }
        }
        
        public void AddControl(UIControl control)
        {
            _controls.Add(control);
            
            // For panels, set up docking zones
            if (control is UIPanel panel && panel.IsDockable)
            {
                // Set the dock zones
                panel.SetDockZones(_dockZones);
                
                // Add event handler for dock changes
                panel.OnDockStateChanged += (sender, e) => 
                {
                    UIPanel changedPanel = (UIPanel)sender;
                    
                    // Remove from previous dock style list
                    foreach (var style in _dockedPanels.Keys)
                    {
                        _dockedPanels[style].Remove(changedPanel);
                    }
                    
                    // Add to new dock style list
                    if (changedPanel.Dock != UIPanel.DockStyle.None)
                    {
                        _dockedPanels[changedPanel.Dock].Add(changedPanel);
                        
                        // Recalculate all docked panels to prevent overlaps
                        RecalculateDockedPanels();
                    }
                    
                    // When a panel is docked, bring it to the front
                    BringToFront(panel);
                };
                
                // Set initial docking state
                if (panel.Dock != UIPanel.DockStyle.None)
                {
                    _dockedPanels[panel.Dock].Add(panel);
                }
                
                // Add a TabIndex property if not already set
                if (panel.TabIndex <= 0)
                {
                    panel.TabIndex = _controls.Count;
                }
                
                // Update docked panels
                RecalculateDockedPanels();
            }
        }
        
        public void RemoveControl(UIControl control)
        {
            _controls.Remove(control);
        }
        
        public void Update(GameTime gameTime, Microsoft.Xna.Framework.Point mousePosition, bool mouseDown)
        {
            // Convert MonoGame point to System.Drawing point
            System.Drawing.Point drawingMousePosition = new System.Drawing.Point(mousePosition.X, mousePosition.Y);
            
            // Check for modal panels first
            bool hasModalPanel = false;
            UIPanel modalPanel = null;
            
            // Find the topmost modal panel if any
            for (int i = _controls.Count - 1; i >= 0; i--)
            {
                if (_controls[i] is UIPanel panel && panel.IsModal)
                {
                    hasModalPanel = true;
                    modalPanel = panel;
                    break;
                }
            }
            
            if (hasModalPanel && modalPanel != null)
            {
                // Only update the modal panel
                modalPanel.Update(new System.Drawing.Point(mousePosition.X, mousePosition.Y), mouseDown);
            }
            else
            {
                // Update all controls in reverse order (top-most first)
                for (int i = _controls.Count - 1; i >= 0; i--)
                {
                    if (i < _controls.Count)  // Guard against collection modification
                    {
                        _controls[i].Update(drawingMousePosition, mouseDown);
                    }
                    
                    // If this control was interacted with, bring it to front
                    if (_controls[i] is UIPanel panel && (panel.IsMouseOver || panel.IsDragging()))
                    {
                        // Only bring to front if this is the first update where it's being interacted with
                        if (i < _controls.Count - 1) // Not already at the top
                        {
                            var control = _controls[i];
                            _controls.RemoveAt(i);
                            _controls.Add(control); // Add to end (top)
                            break; // Exit the loop since we've modified the collection
                        }
                    }
                }
            }
            
            // Update animations
            foreach (var control in _controls)
            {
                if (control is UIPanel panel)
                {
                    _renderer.UpdatePanelAnimation(panel, gameTime);
                }
            }
        }
        
        public void Draw(GameTime gameTime)
        {
            _renderer.Begin();
            
            // Get all docked panels to make sure they're visible before drawing
            List<UIPanel> dockedPanels = new List<UIPanel>();
            foreach (var style in _dockedPanels.Keys)
            {
                if (style != UIPanel.DockStyle.None)
                {
                    foreach (var panel in _dockedPanels[style])
                    {
                        // Force all docked panels to be visible
                        panel.Visible = true;
                        dockedPanels.Add(panel);
                    }
                }
            }
            
            // Sort controls by Z-index (TabIndex)
            var sortedControls = new List<UIControl>(_controls);
            sortedControls.Sort((a, b) => a.TabIndex.CompareTo(b.TabIndex));
            
            // First pass: Draw all non-panel controls and non-dragging panels
            foreach (var control in sortedControls)
            {
                // Skip panels that are being dragged - they'll be drawn last
                if (control is UIPanel panel && panel.IsDragging())
                    continue;
                
                // Skip invisible controls, but not docked panels
                bool isDocked = (control is UIPanel p) && p.IsDocked;
                if (!control.Visible && !isDocked)
                    continue;
                
                // Make sure docked panels are visible
                if (isDocked)
                {
                    ((UIPanel)control).Visible = true;
                }
                
                // Draw control
                _renderer.DrawControl(control);
            }
            
            // Second pass: Draw panels that are being dragged (on top)
            foreach (var control in sortedControls)
            {
                if (control is UIPanel panel && panel.IsDragging() && panel.Visible)
                {
                    _renderer.UpdatePanelAnimation(panel, gameTime);
                    _renderer.DrawControl(panel);
                    
                    // Debug: Show available dock zones for dragged panel
                    if (ShowDebugInfo)
                    {
                        foreach (var zone in panel.GetAvailableDockZones())
                        {
                            var zoneRect = zone.ZoneBounds.ToXnaRectangle();
                            Color debugColor = Color.Yellow;
                            debugColor.A = 80; // Semi-transparent
                            _renderer.DrawRectangle(zoneRect, debugColor, 2);
                        }
                    }
                }
            }
            
            // Draw debug info if enabled
            if (ShowDebugInfo)
            {
                DrawDebugInfo();
            }
            
            _renderer.End();
        }
        
        private void DrawDebugInfo()
        {
            int y = 10;
            int lineHeight = 20;
            
            // Display layout area
            _renderer.DrawRectangle(_screenBounds, Color.DarkGray, false);
            
            // Draw remaining area
            if(_remainingSpace.HasValue)
                _renderer.DrawRectangle(_remainingSpace.Value, Color.Gray * 0.5f, false);
            
            // Draw docked panels info
            _renderer.DrawString(_renderer.GetFont("default"), "Docked Panels:", new Vector2(10, y), Color.White);
            y += lineHeight;
            
            // Draw dock zone outlines
            foreach (var zone in _dockZones)
            {
                Color zoneColor;
                switch (zone.DockStyle)
                {
                    case UIPanel.DockStyle.Left:
                        zoneColor = Color.Red;
                        break;
                    case UIPanel.DockStyle.Right:
                        zoneColor = Color.Green;
                        break;
                    case UIPanel.DockStyle.Top:
                        zoneColor = Color.Blue;
                        break;
                    case UIPanel.DockStyle.Bottom:
                        zoneColor = Color.Yellow;
                        break;
                    case UIPanel.DockStyle.Fill:
                        zoneColor = Color.Purple;
                        break;
                    default:
                        zoneColor = Color.White;
                        break;
                }
                zoneColor.A = 80; // Semi-transparent
                _renderer.DrawRectangle(zone.ZoneBounds.ToXnaRectangle(), zoneColor, false);
            }
            
            // Draw info about each docked panel
            foreach (var style in _dockedPanels.Keys)
            {
                if (style != UIPanel.DockStyle.None && _dockedPanels[style].Count > 0)
                {
                    _renderer.DrawString(_renderer.GetFont("default"), $"{style}: {_dockedPanels[style].Count} panels", 
                        new Vector2(10, y), Color.White);
                    y += lineHeight;
                    
                    foreach (var panel in _dockedPanels[style])
                    {
                        string visibility = panel.Visible ? "Visible" : "Hidden";
                        _renderer.DrawString(_renderer.GetFont("default"), $"  - {panel.Title}: {visibility}", 
                            new Vector2(20, y), panel.Visible ? Color.Green : Color.Red);
                        y += lineHeight;
                        
                        // Draw a highlight around the panel
                        Color highlightColor = panel.Visible ? Color.Green : Color.Red;
                        highlightColor.A = 100;
                        _renderer.DrawRectangle(
                            new Rectangle(panel.Bounds.X, panel.Bounds.Y, panel.Bounds.Width, panel.Bounds.Height),
                            highlightColor,
                            false);
                    }
                }
            }
            
            // Display general info
            _renderer.DrawString(_renderer.GetFont("default"), $"Total controls: {_controls.Count}", 
                new Vector2(10, _screenBounds.Height - 60), Color.White);
            _renderer.DrawString(_renderer.GetFont("default"), "Debug mode: ON - Press F1 to toggle", 
                new Vector2(10, _screenBounds.Height - 40), Color.White);
            _renderer.DrawString(_renderer.GetFont("default"), "Drag panel title bars to dock them to edges", 
                new Vector2(10, _screenBounds.Height - 20), Color.White);
        }
        
        private void BringToFront(UIPanel panel)
        {
            if (_controls.Contains(panel))
            {
                _controls.Remove(panel);
                _controls.Add(panel);
            }
        }
        
        public void AddDockedPanel(UIPanel panel, UIPanel.DockStyle dockStyle)
        {
            if (!_controls.Contains(panel))
            {
                _controls.Add(panel);
            }
            
            // Set the dock zones
            panel.SetDockZones(_dockZones);
            
            // Set the dock style
            panel.SetDock(dockStyle);
            
            // Force panel to be visible
            panel.Visible = true;
            
            // Recalculate all docked panels
            RecalculateDockedPanels();
            
            Console.WriteLine($"Added docked panel {panel.Title} with dock style {dockStyle}");
        }
        
        public void LogPanelState()
        {
            Console.WriteLine("\n========== PANEL STATE DEBUG INFO ==========");
            Console.WriteLine($"Total panels: {_controls.Count(c => c is UIPanel)}");
            
            int docked = 0;
            int visible = 0;
            int dragging = 0;
            
            foreach (var control in _controls)
            {
                if (control is UIPanel panel)
                {
                    Console.WriteLine($"Panel: {panel.Title}");
                    Console.WriteLine($"  - Dock Style: {panel.Dock}");
                    Console.WriteLine($"  - IsDocked: {panel.IsDocked}");
                    Console.WriteLine($"  - Visible: {panel.Visible}");
                    Console.WriteLine($"  - IsDragging: {panel.IsDragging()}");
                    Console.WriteLine($"  - Bounds: {panel.Bounds}");
                    Console.WriteLine($"  - Child Controls: {panel.Controls.Count}");
                    
                    if (panel.IsDocked) docked++;
                    if (panel.Visible) visible++;
                    if (panel.IsDragging()) dragging++;
                }
            }
            
            Console.WriteLine("\nSummary:");
            Console.WriteLine($"  - Docked panels: {docked}");
            Console.WriteLine($"  - Visible panels: {visible}");
            Console.WriteLine($"  - Dragging panels: {dragging}");
            Console.WriteLine("============================================\n");
        }
    }
}

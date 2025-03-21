using System;
using System.Collections.Generic;
using System.Drawing;

namespace DarkUI.Core.Controls
{
    public class UIPanel : UIControl
    {
        // Constants
        private const int TITLE_HEIGHT = 25;
        // Properties
        public string Title { get; set; }
        public bool ShowTitle { get; set; } = true;
        public DockStyle Dock { get; private set; } = DockStyle.None;
        public float DockSize { get; set; } = 0.25f; // Default to 25% when docked
        public bool IsDockable { get; set; } = true;
        public bool IsDraggable { get; set; } = true;
        public List<UIControl> Controls { get; private set; } = new List<UIControl>();
        
        // Docking properties
        public enum DockStyle
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            Fill
        }
        
        // Dragging state
        private bool _isDragging = false;
        private Point _dragStartPosition = Point.Empty;
        private Point _dragStartOffset = Point.Empty;
        private bool _isSnapped = false;
        
        // Docking properties
        private DockZone _activeDockZone = null;
        private List<DockZone> _availableDockZones = new List<DockZone>();
        public bool ShowDockingPreview { get; set; } = true;
        public bool IsModal { get; set; } = false;
        
        // Events
        public event EventHandler OnDockStateChanged;
        
        public UIPanel() : base()
        {
            Title = "Panel";
        }
        
        public UIPanel(int x, int y, int width, int height, string title) : base(x, y, width, height)
        {
            Title = title;
        }
        
        public void AddControl(UIControl control)
        {
            control.Parent = this;
            Controls.Add(control);
        }
        
        public void RemoveControl(UIControl control)
        {
            control.Parent = null;
            Controls.Remove(control);
        }
        
        public void SetDockZones(List<DockZone> dockZones)
        {
            // Store available dock zones for this panel
            _availableDockZones = dockZones;
        }
        
        public void SetDock(DockStyle dock)
        {
            // Store original dock style
            var oldDock = Dock;
            
            // If already set to this dock style, don't change
            if (oldDock == dock)
                return;
                
            // Update dock style
            Dock = dock;
            
            // Reset dragging state if docked
            if (dock != DockStyle.None)
            {
                _isDragging = false;
                _activeDockZone = null;
            }
            
            // Fire event if dock style changed
            OnDockStateChanged?.Invoke(this, EventArgs.Empty);
        }
        
        public override void Update(Point mousePosition, bool mouseDown)
        {
            base.Update(mousePosition, mouseDown);
            
            // When modal, don't update other controls if mouse is outside panel
            if (IsModal && !Bounds.Contains(mousePosition))
            {
                return;
            }
            
            // Check if mouse is in title bar
            if (IsDraggable && 
                new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, TITLE_HEIGHT).Contains(mousePosition))
            {
                // Start dragging on mouse down
                if (mouseDown && !_isDragging)
                {
                    _isDragging = true;
                    _dragStartOffset = new Point(mousePosition.X - Bounds.X, mousePosition.Y - Bounds.Y);
                }
                else if (mouseDown && _isDragging)
                {
                    // Update panel position
                    Bounds = new Rectangle(
                        mousePosition.X - _dragStartOffset.X,
                        mousePosition.Y - _dragStartOffset.Y,
                        Bounds.Width,
                        Bounds.Height);
                        
                    // When dragging, check if we're over a dock zone
                    _activeDockZone = null;
                    foreach (var zone in _availableDockZones)
                    {
                        if (zone.ZoneBounds.Contains(mousePosition))
                        {
                            _activeDockZone = zone;
                            break;
                        }
                    }
                }
                else if (!mouseDown)
                {
                    // Check if mouse is released while over a dock zone
                    if (_activeDockZone != null && _isDragging)
                    {
                        // Dock the panel
                        SetDock(_activeDockZone.DockStyle);
                        _activeDockZone = null;
                        _isDragging = false;
                    }
                    else
                    {
                        // Reset dragging state when mouse is released
                        _isDragging = false;
                        _activeDockZone = null;
                    }
                }
            }
            
            // Update child controls
            Point relativeMousePosition = new Point(
                mousePosition.X - Bounds.X,
                mousePosition.Y - Bounds.Y - (ShowTitle ? TITLE_HEIGHT : 0));
                
            bool isMouseInPanel = Bounds.Contains(mousePosition);
            
            foreach (var control in Controls)
            {
                if (isMouseInPanel || IsModal)
                {
                    control.Update(mousePosition, mouseDown);
                }
            }
        }
        
        public override void Draw(object graphics)
        {
            // Implementation handled by renderer
        }
        
        // Method to update bounds based on dock style
        public void UpdateDockedBounds(Rectangle parentBounds)
        {
            if (Dock == DockStyle.None)
                return;
                
            // Store original bounds to detect changes
            Rectangle originalBounds = Bounds;
                
            // Calculate new bounds based on dock style
            switch (Dock)
            {
                case DockStyle.Left:
                    Bounds = new Rectangle(
                        parentBounds.X, 
                        parentBounds.Y, 
                        (int)(parentBounds.Width * DockSize), 
                        parentBounds.Height);
                    break;
                case DockStyle.Right:
                    Bounds = new Rectangle(
                        parentBounds.X + parentBounds.Width - (int)(parentBounds.Width * DockSize), 
                        parentBounds.Y, 
                        (int)(parentBounds.Width * DockSize), 
                        parentBounds.Height);
                    break;
                case DockStyle.Top:
                    Bounds = new Rectangle(
                        parentBounds.X, 
                        parentBounds.Y, 
                        parentBounds.Width, 
                        (int)(parentBounds.Height * DockSize));
                    break;
                case DockStyle.Bottom:
                    Bounds = new Rectangle(
                        parentBounds.X, 
                        parentBounds.Y + parentBounds.Height - (int)(parentBounds.Height * DockSize), 
                        parentBounds.Width, 
                        (int)(parentBounds.Height * DockSize));
                    break;
                case DockStyle.Fill:
                    Bounds = new Rectangle(
                        parentBounds.X, 
                        parentBounds.Y, 
                        parentBounds.Width, 
                        parentBounds.Height);
                    break;
            }
            
            // Only trigger the event if bounds actually changed
            if (originalBounds != Bounds)
            {
                OnDockStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        // Method to check if a panel is currently being dragged
        public bool IsDragging()
        {
            return _isDragging;
        }
        
        public DockZone GetActiveDockZone()
        {
            return _activeDockZone;
        }
        
        // Method to get available dock zones
        public List<DockZone> GetAvailableDockZones()
        {
            return _availableDockZones;
        }
    }
    
    public class DockZone
    {
        public Rectangle ZoneBounds { get; set; }
        public Rectangle ParentBounds { get; set; }
        public UIPanel.DockStyle DockStyle { get; set; }
        
        public DockZone(Rectangle bounds, Rectangle parentBounds, UIPanel.DockStyle dockStyle)
        {
            ZoneBounds = bounds;
            ParentBounds = parentBounds;
            DockStyle = dockStyle;
        }
    }
}

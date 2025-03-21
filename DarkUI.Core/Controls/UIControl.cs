using System;
using System.Drawing;

namespace DarkUI.Core.Controls
{
    /// <summary>
    /// Base class for all UI controls
    /// </summary>
    public abstract class UIControl
    {
        // Properties
        public Rectangle Bounds { get; set; }
        public bool Visible { get; set; } = true;
        public bool Enabled { get; set; } = true;
        public object Tag { get; set; }
        public UIControl Parent { get; set; }
        public int TabIndex { get; set; } = 0;
        
        // Events
        public event EventHandler Click;
        public event EventHandler MouseEnter;
        public event EventHandler MouseLeave;
        public event EventHandler MouseDown;
        public event EventHandler MouseUp;
        
        // State tracking
        protected bool _isMouseOver = false;
        protected bool _isMouseDown = false;
        
        // Public state properties for renderer access
        public bool IsMouseOver => _isMouseOver;
        public bool IsMouseDown => _isMouseDown;
        
        public UIControl()
        {
            Bounds = new Rectangle(0, 0, 100, 30);
        }
        
        public UIControl(int x, int y, int width, int height)
        {
            Bounds = new Rectangle(x, y, width, height);
        }
        
        // Virtual methods that can be overridden by derived controls
        public virtual void Update(Point mousePosition, bool mouseDown)
        {
            if (!Enabled || !Visible)
                return;
            
            bool wasMouseOver = _isMouseOver;
            bool wasMouseDown = _isMouseDown;
            
            // Check if mouse is over this control
            _isMouseOver = Bounds.Contains(mousePosition);
            
            // Handle mouse enter/leave events
            if (_isMouseOver && !wasMouseOver)
                OnMouseEnter(EventArgs.Empty);
            else if (!_isMouseOver && wasMouseOver)
                OnMouseLeave(EventArgs.Empty);
            
            // Handle mouse down/up events
            if (_isMouseOver)
            {
                if (mouseDown && !_isMouseDown)
                {
                    _isMouseDown = true;
                    OnMouseDown(EventArgs.Empty);
                }
                else if (!mouseDown && _isMouseDown)
                {
                    _isMouseDown = false;
                    OnMouseUp(EventArgs.Empty);
                    OnClick(EventArgs.Empty);
                }
            }
            else
            {
                if (wasMouseDown)
                {
                    _isMouseDown = false;
                    OnMouseUp(EventArgs.Empty);
                }
            }
        }
        
        public abstract void Draw(object graphics); // Will be implemented in MonoGame-specific renderer
        
        // Protected methods to raise events
        protected virtual void OnClick(EventArgs e)
        {
            Click?.Invoke(this, e);
        }
        
        protected virtual void OnMouseEnter(EventArgs e)
        {
            MouseEnter?.Invoke(this, e);
        }
        
        protected virtual void OnMouseLeave(EventArgs e)
        {
            MouseLeave?.Invoke(this, e);
        }
        
        protected virtual void OnMouseDown(EventArgs e)
        {
            MouseDown?.Invoke(this, e);
        }
        
        protected virtual void OnMouseUp(EventArgs e)
        {
            MouseUp?.Invoke(this, e);
        }
    }
}

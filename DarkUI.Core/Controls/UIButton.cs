using System;
using System.Drawing;

namespace DarkUI.Core.Controls
{
    public class UIButton : UIControl
    {
        public string Text { get; set; }
        public bool IsToggleButton { get; set; }
        public bool Checked { get; set; }
        
        public UIButton() : base()
        {
            Text = "Button";
        }
        
        public UIButton(int x, int y, int width, int height, string text) : base(x, y, width, height)
        {
            Text = text;
        }
        
        public override void Draw(object graphics)
        {
            // This will be implemented in the MonoGame renderer
            // The base implementation just defines the interface
        }
        
        public override void Update(Point mousePosition, bool mouseDown)
        {
            base.Update(mousePosition, mouseDown);
            
            if (IsToggleButton && _isMouseOver && mouseDown)
            {
                Checked = !Checked;
            }
        }
    }
}

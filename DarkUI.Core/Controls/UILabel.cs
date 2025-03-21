using System;
using System.Drawing;

namespace DarkUI.Core.Controls
{
    public class UILabel : UIControl
    {
        public string Text { get; set; }
        public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleLeft;
        
        public UILabel() : base()
        {
            Text = "Label";
        }
        
        public UILabel(int x, int y, int width, int height, string text) : base(x, y, width, height)
        {
            Text = text;
        }
        
        public override void Draw(object graphics)
        {
            // This will be implemented in the MonoGame renderer
            // The base implementation just defines the interface
        }
    }
}

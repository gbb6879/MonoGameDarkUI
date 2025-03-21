using System;
using System.Drawing;

namespace DarkUI.Core
{
    public static class Colors
    {
        // Main colors
        public static readonly System.Drawing.Color BackgroundColor = System.Drawing.Color.FromArgb(60, 63, 65);
        public static readonly System.Drawing.Color DarkBackgroundColor = System.Drawing.Color.FromArgb(43, 43, 43);
        public static readonly System.Drawing.Color LightBackgroundColor = System.Drawing.Color.FromArgb(69, 73, 74);
        
        // Border colors
        public static readonly System.Drawing.Color BorderColor = System.Drawing.Color.FromArgb(85, 85, 85);
        public static readonly System.Drawing.Color LightBorderColor = System.Drawing.Color.FromArgb(100, 100, 100);
        
        // Text colors
        public static readonly System.Drawing.Color TextColor = System.Drawing.Color.FromArgb(220, 220, 220);
        public static readonly System.Drawing.Color DisabledTextColor = System.Drawing.Color.FromArgb(153, 153, 153);
        
        // Accent colors
        public static readonly System.Drawing.Color BlueAccent = System.Drawing.Color.FromArgb(104, 151, 187);
        public static readonly System.Drawing.Color GreenAccent = System.Drawing.Color.FromArgb(80, 175, 80);
        public static readonly System.Drawing.Color RedAccent = System.Drawing.Color.FromArgb(191, 97, 106);
        public static readonly System.Drawing.Color YellowAccent = System.Drawing.Color.FromArgb(230, 189, 0);
        
        // Button colors
        public static readonly System.Drawing.Color ButtonColor = System.Drawing.Color.FromArgb(65, 65, 65);
        public static readonly System.Drawing.Color ButtonHoverColor = System.Drawing.Color.FromArgb(75, 110, 175);
        public static readonly System.Drawing.Color ButtonPressedColor = System.Drawing.Color.FromArgb(65, 95, 155);
    }
    
    public static class Theme
    {
        public static float DefaultFontSize = 12.0f;
        public static string DefaultFontFamily = "Segoe UI";
        
        public static int ControlPadding = 5;
        public static int ControlMargin = 3;
        
        public static int ButtonHeight = 30;
        public static int ButtonBorderSize = 1;
        public static int ButtonRoundness = 2;
        
        public static int PanelPadding = 10;
        public static int PanelMargin = 5;
        public static int PanelBorderSize = 1;
        public static int PanelTitleHeight = 25;
    }
}

# MonoGame DarkUI

A MonoGame implementation of the DarkUI theme inspired by [RobinPerris/DarkUI](https://github.com/RobinPerris/DarkUI).

## Project Overview

This project provides a dark-themed UI framework for MonoGame applications. It includes:

- A set of core UI controls (buttons, panels, labels)
- A MonoGame renderer for these controls
- A UI manager to handle control interactions
- A consistent dark color theme
- Advanced docking system for panels

## Features

### Docking System
The framework includes a robust docking system for UI panels that allows:
- Docking panels to any edge of the screen (Left, Right, Top, Bottom)
- Fill docking to occupy remaining space
- Visual feedback during drag operations
- Automatic layout recalculation to prevent overlaps
- Debug visualization for development

## Project Structure

The solution is organized into the following projects:

- **DarkUI.Core**: Contains the core UI control definitions and theme colors
- **DarkUI.MonoGame**: Provides MonoGame-specific rendering and interaction for the UI controls
- **GameProject**: A sample MonoGame application demonstrating the use of the UI framework

## Getting Started

1. Clone this repository
2. Open the solution in Visual Studio
3. Build and run the GameProject to see the UI framework in action

## Controls

The following UI controls are currently implemented:

- **UIButton**: A standard button with optional toggle functionality
- **UIPanel**: A container for other controls with an optional title bar
- **UILabel**: A text display element

## Usage Example

```csharp
// Create UI renderer and manager
UIRenderer renderer = new UIRenderer(GraphicsDevice, spriteBatch, defaultFont);
UIManager uiManager = new UIManager(renderer);

// Create a panel
UIPanel panel = new UIPanel(50, 50, 400, 300, "Dark UI Demo");

// Add a button
UIButton button = new UIButton(20, 40, 150, 30, "Click Me");
button.Click += (sender, e) => {
    // Handle button click
};

// Add the button to the panel
panel.AddControl(button);

// Add the panel to the manager
uiManager.AddControl(panel);

// In your Update method:
uiManager.Update(gameTime, mousePosition, mouseDown);

// In your Draw method:
uiManager.Draw();
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [RobinPerris/DarkUI](https://github.com/RobinPerris/DarkUI) for the original inspiration
- [MonoGame](https://www.monogame.net/) for the awesome game framework

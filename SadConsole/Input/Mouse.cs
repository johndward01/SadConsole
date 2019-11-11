﻿using System;
using System.Collections.Generic;
using SadRogue.Primitives;

namespace SadConsole.Input
{
    /// <summary>
    /// The state of the mouse.
    /// </summary>
    public class Mouse
    {
        private TimeSpan _leftLastClickedTime;
        private TimeSpan _rightLastClickedTime;
        private Console _lastMouseConsole;

        /// <summary>
        /// The pixel position of the mouse on the screen.
        /// </summary>
        public Point ScreenPosition { get; set; }

        /// <summary>
        /// Indicates the middle mouse button is currently being pressed.
        /// </summary>
        public bool MiddleButtonDown { get; set; }

        /// <summary>
        /// Indicates the middle mouse button was clicked. (Held and then released)
        /// </summary>
        public bool MiddleClicked { get; set; }

        /// <summary>
        /// Inidcates the middle mouse button was double-clicked within one second.
        /// </summary>
        public bool MiddleDoubleClicked { get; set; }

        /// <summary>
        /// Indicates the left mouse button is currently being pressed.
        /// </summary>
        public bool LeftButtonDown { get; set; }

        /// <summary>
        /// Indicates the left mouse button was clicked. (Held and then released)
        /// </summary>
        public bool LeftClicked { get; set; }

        /// <summary>
        /// Inidcates the left mouse button was double-clicked within one second.
        /// </summary>
        public bool LeftDoubleClicked { get; set; }

        /// <summary>
        /// Indicates the right mouse button is currently being pressed.
        /// </summary>
        public bool RightButtonDown { get; set; }

        /// <summary>
        /// Indicates the right mouse button was clicked. (Held and then released)
        /// </summary>
        public bool RightClicked { get; set; }

        /// <summary>
        /// Indicates the right mouse buttion was double-clicked within one second.
        /// </summary>
        public bool RightDoubleClicked { get; set; }

        /// <summary>
        /// The cumulative value of the scroll wheel. 
        /// </summary>
        public int ScrollWheelValue { get; set; }

        /// <summary>
        /// The scroll wheel value change between frames.
        /// </summary>
        public int ScrollWheelValueChange { get; set; }

        /// <summary>
        /// Indicates that the mouse is currently within the bounds of the rendering area.
        /// </summary>
        public bool IsOnScreen => Settings.Rendering.RenderRect.Contains(ScreenPosition + Settings.Rendering.RenderRect.Position);

        /// <summary>
        /// Reads the mouse state from <see cref="GameHost.GetMouseState"/>.
        /// </summary>
        /// <param name="elapsedSeconds">Fractional seconds passed since Update was called.</param>
        public void Update(TimeSpan elapsedSeconds)
        {
            IMouseState currentState = GameHost.Instance.GetMouseState();

            // Update local state
            bool leftDown = currentState.IsLeftButtonDown;
            bool rightDown = currentState.IsRightButtonDown;
            bool middleDown = currentState.IsMiddleButtonDown;

            ScrollWheelValueChange = ScrollWheelValue - currentState.MouseWheel;
            ScrollWheelValue = currentState.MouseWheel;

            ScreenPosition = new Point((int)(currentState.ScreenPosition.X * Settings.Rendering.RenderScale.X), (int)(currentState.ScreenPosition.Y * Settings.Rendering.RenderScale.Y)) - new Point((int)(Settings.Rendering.RenderRect.X * Settings.Rendering.RenderScale.X), (int)(Settings.Rendering.RenderRect.Y * Settings.Rendering.RenderScale.Y));
            bool newLeftClicked = LeftButtonDown && !leftDown;
            bool newRightClicked = RightButtonDown && !rightDown;
            bool newMiddleClicked = MiddleButtonDown && !middleDown;

            if (!newLeftClicked)
            {
                LeftDoubleClicked = false;
            }

            if (!newRightClicked)
            {
                RightDoubleClicked = false;
            }

            if (!newMiddleClicked)
            {
                MiddleDoubleClicked = false;
            }

            if (LeftClicked && newLeftClicked && elapsedSeconds.TotalSeconds < 1000)
            {
                LeftDoubleClicked = true;
            }

            if (RightClicked && newRightClicked && elapsedSeconds.TotalSeconds < 1000)
            {
                RightDoubleClicked = true;
            }

            if (MiddleClicked && newMiddleClicked && elapsedSeconds.TotalSeconds < 1000)
            {
                MiddleDoubleClicked = true;
            }

            LeftClicked = newLeftClicked;
            RightClicked = newRightClicked;
            MiddleClicked = newMiddleClicked;
            _leftLastClickedTime = elapsedSeconds;
            _rightLastClickedTime = elapsedSeconds;
            LeftButtonDown = leftDown;
            RightButtonDown = rightDown;
            MiddleButtonDown = middleDown;
        }

        /// <summary>
        /// Clears the buttons, position, wheel information.
        /// </summary>
        public void Clear()
        {
            RightDoubleClicked = false;
            RightClicked = false;
            RightButtonDown = false;
            LeftDoubleClicked = false;
            LeftClicked = false;
            LeftButtonDown = false;
            MiddleDoubleClicked = false;
            MiddleClicked = false;
            MiddleButtonDown = false;
            ScrollWheelValue = 0;
            ScrollWheelValueChange = 0;
            ScreenPosition = new Point(0, 0);
        }

        /// <summary>
        /// Builds information about the mouse state based on the <see cref="Global.FocusedConsoles"/> or <see cref="Global.CurrentScreen"/>. Should be called each frame.
        /// </summary>
        public virtual void Process()
        {
            // Check if last mouse was marked exclusive
            if (_lastMouseConsole != null && _lastMouseConsole.IsExclusiveMouse)
            {
                var state = new MouseConsoleState(_lastMouseConsole, this);

                _lastMouseConsole.ProcessMouse(state);
            }

            // Check if the focused input console wants exclusive mouse
            else if (Global.FocusedConsoles.Console != null && Global.FocusedConsoles.Console.IsExclusiveMouse)
            {
                var state = new MouseConsoleState(Global.FocusedConsoles.Console, this);

                // if the last console to have the mouse is not our global, signal
                if (_lastMouseConsole != null && _lastMouseConsole != Global.FocusedConsoles.Console)
                {
                    _lastMouseConsole.LostMouse(state);
                    _lastMouseConsole = null;
                }

                Global.FocusedConsoles.Console.ProcessMouse(state);

                _lastMouseConsole = Global.FocusedConsoles.Console;
            }

            // Scan through each "console" in the current screen, including children.
            else if (Global.Screen != null)
            {
                bool foundMouseTarget = false;

                // Build a list of all consoles
                var consoles = new List<Console>();
                GetConsoles(Global.Screen, ref consoles);

                // Process top-most consoles first.
                consoles.Reverse();

                for (int i = 0; i < consoles.Count; i++)
                {
                    var state = new MouseConsoleState(consoles[i], this);

                    if (consoles[i].ProcessMouse(state))
                    {
                        if (_lastMouseConsole != null && _lastMouseConsole != consoles[i])
                        {
                            _lastMouseConsole.LostMouse(state);
                        }

                        foundMouseTarget = true;
                        _lastMouseConsole = consoles[i];
                        break;
                    }
                }

                if (!foundMouseTarget)
                {
                    _lastMouseConsole?.LostMouse(new MouseConsoleState(null, this));
                }
            }

        }

        private void GetConsoles(Console screen, ref List<Console> list)
        {
            if (!screen.IsVisible)
            {
                return;
            }

            if (screen.UseMouse)
            {
                list.Add(screen);
            }

            foreach (Console child in screen.Children)
            {
                GetConsoles(child, ref list);
            }
        }

        /// <summary>
        /// Unlocks the last console the mouse was locked to. Allows another conosle to become locked to the mouse.
        /// </summary>
        public void ClearLastMouseConsole()
        {
            _lastMouseConsole?.LostMouse(new MouseConsoleState(null, this));
            _lastMouseConsole = null;
        }

        /// <summary>
        /// Returns true when the mouse is currently over the provided console.
        /// </summary>
        /// <param name="console">The console to check.</param>
        /// <returns>True or false indicating if the mouse is over the console.</returns>
        public bool IsMouseOverConsole(Console console) => new MouseConsoleState(console, this).IsOnConsole;

        /// <summary>
        /// Clones this mouse into a new object.
        /// </summary>
        /// <returns>A clone.</returns>
        public Mouse Clone() => new Mouse()
        {
            ScreenPosition = ScreenPosition,
            LeftButtonDown = LeftButtonDown,
            LeftClicked = LeftClicked,
            LeftDoubleClicked = LeftDoubleClicked,
            RightButtonDown = RightButtonDown,
            RightClicked = RightClicked,
            RightDoubleClicked = RightDoubleClicked,
            MiddleButtonDown = MiddleButtonDown,
            MiddleClicked = MiddleClicked,
            MiddleDoubleClicked = MiddleDoubleClicked,
            ScrollWheelValue = ScrollWheelValue,
            ScrollWheelValueChange = ScrollWheelValueChange
        };
    }
}
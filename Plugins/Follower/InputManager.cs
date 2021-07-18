using ExileCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using static Follower.Globals;

namespace Follower
{
    static class InputManager
    {
        public static State.Machine Machine = new State.Machine();
        public static GameController GameController;
        public static Graphics Graphics;
        private static readonly InputSimulator input = new InputSimulator();
        internal static void Initialise(GameController gameController, Graphics graphics)
        {
            GameController = gameController;
            Graphics = graphics;
            Machine.EnableLogging((s) => Log(s));
        }

        private class KeyTracker : State
        {
            private static readonly InputSimulator input = new InputSimulator();
            public readonly VirtualKeyCode Key;
            public readonly Action Action;
            private bool downBefore = false;
            public KeyTracker(VirtualKeyCode key, Action action, State next = null) : base(next)
            {
                Key = key;
                Action = action;
            }
            public override State OnTick()
            {
                bool downNow = input.InputDeviceState.IsHardwareKeyDown(Key);
                // DrawTextAtPlayer($"KeyTracker({Key}): Tick({downNow})");
                if( downBefore && !downNow) Action();
                downBefore = downNow;
                return this;
            }
            public override string ToString() => $"TK-{Key}";
        }

        public static void OnRelease(VirtualKeyCode key, Action action) => Add(new KeyTracker(key, action));
        public static void PressKey(VirtualKeyCode Key, uint duration) => Add(new PressKey(Key, duration));
        public static void LeftClickAt(float x, float y, uint duration) => Add(new LeftClickAt(GameController.Window, x, y, duration));
        public static void RightClickAt(float x, float y, uint duration) => Add(new RightClickAt(GameController.Window, x, y, duration));
        public static void LeftClickAt(SharpDX.RectangleF rect, uint duration) => LeftClickAt(rect.Center.X, rect.Center.Y, duration);
        public static void RightClickAt(SharpDX.RectangleF rect, uint duration) => RightClickAt(rect.Center.X, rect.Center.Y, duration);
        internal static bool IsPressed(VirtualKeyCode key) => input.InputDeviceState.IsHardwareKeyDown(key);
        public static void Add(State state) => Machine.Add(state);
        public static void OnTick() => Machine.OnTick();
    }
}

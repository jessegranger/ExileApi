using ExileCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using static Assistant.Globals;

namespace Assistant
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
            // Machine.EnableLogging((s) => Log(s));
        }

        private class KeyTracker : State
        {
            private static readonly InputSimulator input = new InputSimulator();
            public readonly VirtualKeyCode vKey;
            public readonly Keys Key;
            public readonly Action Action;
            private bool downBefore = false;
            public KeyTracker(VirtualKeyCode key, Action action, State next = null) : base(next)
            {
                vKey = key;
                Key = VirtualKeyToKey(key);
                Action = action;
                Input.RegisterKey(Key);
            }
            public override State OnTick()
            {
                bool downNow = input.InputDeviceState.IsHardwareKeyDown(vKey) || Input.IsKeyDown(Key);
                // DrawTextAtPlayer($"KeyTracker({Key}): Tick({downNow})");
                if( downBefore && !downNow) Action();
                downBefore = downNow;
                return this;
            }
            public override string ToString() => $"TK-{vKey}";
            [DllImport("user32.dll")] private static extern short GetAsyncKeyState(Keys vKey);
        }

        static InputManager()
        {
            pressTimer.Start();
        }

        private static Stopwatch pressTimer = new Stopwatch();
        private static Dictionary<VirtualKeyCode, long> lastPressTime = new Dictionary<VirtualKeyCode, long>();

        public static VirtualKeyCode KeyToVirtualKey(System.Windows.Forms.Keys Key) => (VirtualKeyCode)(Key & System.Windows.Forms.Keys.KeyCode);
        public static System.Windows.Forms.Keys VirtualKeyToKey(VirtualKeyCode Key) => (System.Windows.Forms.Keys)Key;

        public static void OnRelease(VirtualKeyCode key, Action action) => Add(new KeyTracker(key, action));
        public static void PressKey(VirtualKeyCode Key, uint duration) => Add(new PressKey(Key, duration));
        public static void PressKey(VirtualKeyCode Key, uint duration, uint throttle_ms)
        {
            long now = pressTimer.ElapsedMilliseconds;
            if( lastPressTime.TryGetValue(Key, out long last) )
            {
                if (now - last < throttle_ms) return;
            }
            lastPressTime[Key] = now;
            Add(new PressKey(Key, duration));
        }
        public static void LeftClickAt(float x, float y, uint duration) => Add(new LeftClickAt(GameController.Window, x, y, duration));
        public static void RightClickAt(float x, float y, uint duration) => Add(new RightClickAt(GameController.Window, x, y, duration));
        public static void LeftClickAt(SharpDX.RectangleF rect, uint duration) => LeftClickAt(rect.Center.X, rect.Center.Y, duration);
        public static void RightClickAt(SharpDX.RectangleF rect, uint duration) => RightClickAt(rect.Center.X, rect.Center.Y, duration);
        internal static bool IsPressed(VirtualKeyCode key) => input.InputDeviceState.IsHardwareKeyDown(key);
        public static void Add(State state) => Machine.Add(state);
        private static bool AllowInputInChatBox = false;
        public static void OnTick() {
            if (!IsValid(GameController)) return;
            if (!AllowInputInChatBox)
            {
                var chat = GameController.IngameState.IngameUi.ChatBoxRoot;
                if (chat != null && chat.IsValid && chat.IsActive) return;
            }
            // advance the state machine that figures out which action to take each frame
            Machine.OnTick();
        }
        [DllImport("user32.dll")] private static extern short VkKeyScanA(char ch);
        public static VirtualKeyCode GetKeyCode(char c) => (VirtualKeyCode)VkKeyScanA(c);

        public static void ChatCommand(string v)
        {
            State start = new ActionState(() => { AllowInputInChatBox = true; });
            start
                .Then(new PressKey(VirtualKeyCode.RETURN, 30))
                .Then(v.Select((c) => new PressKey(GetKeyCode(c), 10)).ToArray())
                .Then(new PressKey(VirtualKeyCode.RETURN, 30))
                .Then(() => { AllowInputInChatBox = false; })
                ;
            Add(start);
        }
    }
}

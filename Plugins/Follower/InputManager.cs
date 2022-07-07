using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Nodes;
using SharpDX;
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

namespace Assistant {
	static class InputManager {
		public static State.Machine Machine = new State.Machine();
		private static readonly InputSimulator input = new InputSimulator();
		internal static void Initialise() {
			// Machine.EnableLogging((s) => Log(s));
		}

		private class KeyTracker : State {
			private static readonly InputSimulator input = new InputSimulator();
			public readonly VirtualKeyCode vKey;
			public readonly Keys Key;
			public readonly Action Action;
			private bool downBefore = false;
			public KeyTracker(VirtualKeyCode key, Action action, State next = null) : base(next) {
				vKey = key;
				Key = ToKey(key);
				Action = action;
				Input.RegisterKey(Key);
			}
			public override State OnTick() {
				bool downNow = input.InputDeviceState.IsHardwareKeyDown(vKey) || Input.IsKeyDown(Key);
				// DrawTextAtPlayer($"KeyTracker({Key}): Tick({downNow})");
				if ( Input.CheckKeyPressed(Key) || (downBefore && !downNow) ) Action();
				downBefore = downNow;
				return this;
			}
			public override string ToString() => $"KeyTracker:{vKey}";
			[DllImport("user32.dll")] private static extern short GetAsyncKeyState(Keys vKey);
		}

		private static Stopwatch pressTimer = Stopwatch.StartNew();
		private static Dictionary<VirtualKeyCode, long> lastPressTime = new Dictionary<VirtualKeyCode, long>();


		public static VirtualKeyCode ToVirtualKey(System.Windows.Forms.Keys Key) => (VirtualKeyCode)(Key & System.Windows.Forms.Keys.KeyCode);
		public static Keys ToKey(VirtualKeyCode Key) => (Keys)Key;

		public static void OnRelease(VirtualKeyCode key, Action action) => Add(new KeyTracker(key, action));
		public static void PressKey(VirtualKeyCode Key, uint duration) => Add(new PressKey(Key, duration));
		public static void PressKey(VirtualKeyCode Key, uint duration, uint throttle_ms) {
			long now = pressTimer.ElapsedMilliseconds;
			if ( lastPressTime.TryGetValue(Key, out long last) ) {
				if ( now - last < throttle_ms ) return;
			}
			lastPressTime[Key] = now;
			Add(new PressKey(Key, duration));
		}
		public static void LeftClickAt(float x, float y, uint duration) => Add(new LeftClickAt(x, y, duration));
		public static void RightClickAt(float x, float y, uint duration) => Add(new RightClickAt(x, y, duration));
		public static void LeftClickAt(SharpDX.RectangleF rect, uint duration) => LeftClickAt(rect.Center.X, rect.Center.Y, duration);
		public static void RightClickAt(SharpDX.RectangleF rect, uint duration) => RightClickAt(rect.Center.X, rect.Center.Y, duration);
		internal static bool IsPressed(VirtualKeyCode key) => input.InputDeviceState.IsHardwareKeyDown(key);
		public static void Add(State state) => Machine.Add(state);

		private static bool AllowInputInChatBox = false;
		public static void OnTick() {
			var game = GetGame();
			if ( !IsValid(game) ) {
				Log($"No game.");
				return;
			}
			if ( !AllowInputInChatBox ) {
				var chat = game.IngameState.IngameUi.ChatBoxRoot;
				if ( chat != null && chat.IsValid && chat.IsActive ) return;
			}
			// advance the state machine that figures out which action to take each frame
			Machine.OnTick();
		}
		[DllImport("user32.dll")] private static extern short VkKeyScanA(char ch);
		public static VirtualKeyCode GetKeyCode(char c) => (VirtualKeyCode)VkKeyScanA(c);
		public static VirtualKeyCode GetKeyCode(string s) => (VirtualKeyCode)VkKeyScanA(s[0]);

		public static void ChatCommand(string v) {
			State start = new ActionState(() => { AllowInputInChatBox = true; });
			start
					.Then(new PressKey(VirtualKeyCode.RETURN, 30))
					.Then(v.Select((c) => new PressKey(GetKeyCode(c), 10)).ToArray())
					.Then(new PressKey(VirtualKeyCode.RETURN, 30))
					.Then(() => { AllowInputInChatBox = false; })
					;
			Add(start);
		}

		public static void OnKeyCombo(string combo, Action action) {
			InputManager.Add(PlanFollowKeyCombo(combo, action));
		}
		private static State PlanFollowKeyCombo(string combo, Action action) {
			VirtualKeyCode[] keys = combo.Select(GetKeyCode).ToArray();
			foreach( var k in keys ) Input.RegisterKey(ToKey(k));
			uint curStep = 0;
			bool downBefore = false;
			Stopwatch sinceLastRelease = new Stopwatch();
			return State.From((state) => {
				var curKey = keys[curStep];
				// DrawTextAtPlayer($"Combo: step {curStep}/{curKey} of {string.Join(" ", keys)}");
				bool downNow = IsKeyDown(curKey);
				if( downBefore && !downNow ) { // on release:
					// Log($"Key {curKey} released. Advancing step...");
					curStep += 1;
					sinceLastRelease.Restart();
					if( curStep >= keys.Length ) {
						curStep = 0;
						action();
					}
				} else if( sinceLastRelease.ElapsedMilliseconds > 1000 ) {
					// Log($"Combo expired, resetting.");
					curStep = 0;
					sinceLastRelease.Stop();
					sinceLastRelease.Reset();
				}
				downBefore = downNow;
				return state;
			});
		}

		internal static State PlanMultiKey(Keys mainKey, params Keys[] otherKeys) => PlanMultiKey(ToVirtualKey(mainKey), otherKeys.Select(ToVirtualKey).ToArray());
		internal static State PlanMultiKey(VirtualKeyCode mainKey, params VirtualKeyCode[] otherKeys) {
			// press the mainKey, this code presses all the otherKeys
			bool downBefore = false;
			Input.RegisterKey(ToKey(mainKey));
			foreach ( var key in otherKeys ) Input.RegisterKey(ToKey(key));
			Log($"Creating MultiKey plan: {mainKey} {string.Join(" ", otherKeys)}");
			return State.From((state) => {
				bool downNow = IsKeyDown(mainKey);
				if ( ChatIsOpen() || ( downBefore && !downNow ) ) {
					foreach(var key in otherKeys) input.Keyboard.KeyUp(key);
				} else if( downNow && !downBefore ) {
					foreach(var key in otherKeys) input.Keyboard.KeyDown(key);
				}
				downBefore = downNow;
				return state;
			});
		}

		private static bool IsKeyDown(VirtualKeyCode key) => input.InputDeviceState.IsHardwareKeyDown(key) || Input.IsKeyDown(ToKey(key));
		private static bool IsKeyDown(Keys key) => IsKeyDown(ToVirtualKey(key));


		public static void EnableMovementKeys() => Add(PlanMovementKeys());
		internal static State PlanMovementKeys() {
			Input.RegisterKey(Keys.Left);
			Input.RegisterKey(Keys.Right);
			Input.RegisterKey(Keys.Up);
			Input.RegisterKey(Keys.Down);
			return State.From((state) => {
				var settings = GetSettings();
				if ( !(settings.UseArrowKeys?.Value ?? false) ) return state;
				bool left = IsKeyDown(VirtualKeyCode.LEFT);
				bool right = IsKeyDown(VirtualKeyCode.RIGHT);
				bool up = IsKeyDown(VirtualKeyCode.UP);
				bool down = IsKeyDown(VirtualKeyCode.DOWN);
				// we cant just add up vector components like normal because of the skewed perspective, so we hack in all 8 corners
				// this lets the diagonals align nicely with the game corridors
				Vector2 motion = (up && left) ? new Vector2(.36f, .27f) :
					(up && right) ? new Vector2(.65f, .26f) :
					(down && left) ? new Vector2(.35f, .63f) :
					(down && right) ? new Vector2(.62f, .61f) :
					(up) ? new Vector2(.5f, .24f) :
					(down) ? new Vector2(.5f, .65f) :
					(left) ? new Vector2(.36f, .45f) :
					(right) ? new Vector2(.65f, .45f) :
					Vector2.Zero;
				if ( motion == Vector2.Zero ) return state;
				var pos = ScreenRelativeToWindow(motion);
				// do the move immediately (in this frame)
				return new MoveMouse(pos.X, pos.Y, state).OnTick();
			});
		}

		internal static void ConfigureHotkey(ToggleHotkeyNode node, Action action) {
			Input.RegisterKey(node.Value);
			node.OnValueChanged = () => Input.RegisterKey(node.Value);
			Add(PlanHotkey(node, action));
		}
		internal static State PlanHotkey(ToggleHotkeyNode node, Action action) {
			bool downBefore = false;
			return State.From((state) => {
				bool downNow = IsKeyDown(node.Value);
				if ( downBefore && !downNow ) action();
				downBefore = downNow;
				return state;
			});
		}

		internal static State PlanChangeStashTab(int index, State next = null) {
			if ( !StashIsOpen() ) return next;
			if ( StashTab() == index ) return next;
			return new LeftClickAt(ScreenRelativeToWindow(new Vector2(.40f, .10f + (index * .022f))), 50, next);
		}


		/* TODO: The way this 'click on label' targeting should work (and doesn't yet):
		 * User presses down the looting key,
		 * mouse cursor zooms to label of the nearest item,
		 * arrow keys can now tweak which label is targeted, up/down/left/right
		 * each arrow key jumps the mouse cursor to the center of the "next" label in that direction,
		 * User releases the looting key, item is looted.
		 * So a quick press would work just like now, but a long press allows refinement.
		 * this would be the best user experience but is hard to code which label to move to
		 */

		public static void ClickOnNearestGroundLabel() {
			Add(PlanClickOnNearestGroundLabel());
		}
		internal static State PlanClickOnNearestGroundLabel(State next = null) {
			return new LeftClickAt(NearestGroundLabel()?.Label, 50, next);
		}
	}
}

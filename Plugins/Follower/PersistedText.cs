using System;
using System.Collections.Generic;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;
using System.Diagnostics;

namespace Assistant {
	internal class PersistedText {
		private class RenderItem {
			public Func<string> Text;
			public Func<Camera, Vector2> Pos;
			public Func<int> Height;
			public Func<long> Remaining;
			public RenderItem(Func<string> text, Func<Camera, Vector2> pos, int duration, int height = 12) {
				Text = text;
				Pos = pos;
				Height = () => height;
				if ( duration > 0 ) {
					Stopwatch timer = new Stopwatch();
					timer.Start();
					Remaining = () => duration - timer.ElapsedMilliseconds;
				} else {
					Remaining = () => long.MaxValue;
				}
			}
		}

		private static List<RenderItem> instances = new List<RenderItem>();
		public static void Add(Func<string> text, Func<Camera, Vector2> pos, int ms, int height = 12) => instances.Add(new RenderItem(text, pos, ms, height));
		public static void Add(Func<string> text, Func<Vector2> pos, int ms, int height = 12) => instances.Add(new RenderItem(text, c => pos(), ms, height));
		public static void Add(Func<string> text, Vector2 pos, int ms, int height = 12) => instances.Add(new RenderItem(text, c => pos, ms, height));
		public static void Add(string text, Func<Camera, Vector2> pos, int ms, int height = 12) => instances.Add(new RenderItem(() => text, pos, ms, height));
		public static void Add(string text, Func<Vector2> pos, int ms, int height = 12) => instances.Add(new RenderItem(() => text, c => pos(), ms, height));
		public static void Add(string text, Vector2 pos, int ms, int height = 12) => instances.Add(new RenderItem(() => text, c => pos, ms, height));
		public static void Add(string text, Func<Vector3> pos, int ms, int height = 12) => instances.Add(new RenderItem(() => text, c => c.WorldToScreen(pos()), ms, height));
		public static void Add(string text, Vector3 pos, int ms, int height = 12) => instances.Add(new RenderItem(() => text, c => c.WorldToScreen(pos), ms, height));
		public static void Render(Camera camera, Graphics G) {
			// clear out expired texts
			instances.RemoveAll(obj => obj.Remaining() <= 0);
			foreach ( var obj in instances ) {
				G.DrawText(obj.Text(), obj.Pos(camera), Color.Orange, obj.Height());
			}
		}

	}
}

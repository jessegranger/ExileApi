using System.Collections.Generic;
using ExileCore;
using SharpDX;

namespace Assistant
{
	partial class Assistant
	{
		public static class Console // there can be only One
		{
			public static Vector2 Pos;
			public static float LineHeight = 12.0f;
			public static uint MaxLines = 55;
			public static bool Hidden = false;
			public static List<string> Lines = new List<string>();
			static Console()
			{
				Pos = new Vector2(10, 50);
				Lines.Add("Console created.");
			}
			public static void Add(string text)
			{
				Lines.Add(text);
				if (Lines.Count > MaxLines)
				{
					Lines.RemoveRange(0, (int)(Lines.Count - MaxLines));
				}
			}
			public static void Clear() { Lines.Clear(); }
			public static void Render(Graphics G)
			{
				if (Hidden) return;
				float x = Pos.X;
				float y = Pos.Y;
				foreach (var line in Lines)
				{
					G.DrawText(line, new Vector2(x, y));
					y += LineHeight;
				}
			}

		}

	}
}

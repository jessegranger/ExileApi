using System.Collections.Generic;
using ExileCore;
using SharpDX;

namespace Follower
{
	partial class Follower
	{
		public class Console
		{
			public Vector2 Pos;
			public float LineHeight = 12.0f;
			public uint MaxLines = 55;
			public bool Hidden = false;
			public List<string> Lines = new List<string>();
			public Console(float x, float y)
			{
				Pos = new Vector2(x, y);
				Lines.Add("Console created.");
			}
			public void Add(string text)
			{
				if (Lines.Count > MaxLines)
				{
					Lines.RemoveRange(0, (int)(Lines.Count - MaxLines));
				}
				Lines.Add(text);
			}
			public void Clear() { Lines.Clear(); }
			public void Render(Graphics G)
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

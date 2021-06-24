using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Follower
{
	public static class Globals
	{
		static GameController Game;
		static Graphics Gfx;
		static bool IsInitialised;

		public static void Initialise(GameController game, Graphics gfx)
		{
			Game = game;
			Gfx = gfx;
			IsInitialised = true;
			console.Pos = ScreenRelativeToWindow(.01f, .25f);
		}

		public static void Render()
		{
			if (!IsInitialised) return;
			console.Render(Gfx);
			lineCounts.Clear();
		}
		public static void Tick(long dt)
		{
			if (!IsInitialised) return;
			UpdateBuffCache();
		}
		// cache the player buffs for one frame
		private static Dictionary<string, int> buffCache = new Dictionary<string, int>();
		private static void UpdateBuffCache()
		{
			buffCache.Clear();
			var buffs = Game.Player.GetComponent<Buffs>();
			foreach (var buff in buffs.BuffsList)
			{
				buffCache[buff.Name] = buff.Charges;
			}
		}
		public static bool HasBuff(params string[] buffNames) => buffNames.Any(k => buffCache.ContainsKey(k));
		public static bool HasAnyBuff(string buffPrefix) => buffCache.Keys.Any(x => x.StartsWith(buffPrefix));

		public static bool HasMod(Entity ent, string modName)
		{
			if (ent == null) return false;
			var mods = ent.GetComponent<Mods>();
			if (mods == null) return false;
			return HasMod(mods.ItemMods, modName);
		}
		public static bool HasMod(List<ItemMod> itemMods, string modName)
		{
			if (itemMods == null || modName == null ) return false;
			if( modName.EndsWith("*") )
            {
                return itemMods.Any(x => x.Name != null && x.Name.StartsWith(modName.Substring(0, modName.Length - 1)));
            }
            else
            {
                return itemMods.Any(x => x.Name != null && x.Name.Equals(modName));
            }
        }

		public static bool IsValid(NormalInventoryItem item) => (item != null && item.IsValid && item.Item != null && item.Item.Path != null);
		public static bool IsValid(Entity ent) => (ent != null && ent.Path != null && ent.IsValid);
		public static bool IsValid(ServerInventory.InventSlotItem item) => IsValid(item.Item);
        public static bool IsValid(LabelOnGround label) => label.Label != null && label.Label.Text != null && label.ItemOnGround != null;
		public static bool IsValid(GameController game) => game != null && game.Game != null;
		public static bool IsValid(ActorVaalSkill skill) => skill != null && skill.VaalSkillInternalName != null;

		public static ItemRarity GetItemRarity(Entity ent)
        {
			if(IsValid(ent))
            {
				var mods = ent.GetComponent<Mods>();
				if( mods != null )
                {
					return mods.ItemRarity;
                }
            }
			return ItemRarity.Normal;
        }
		public static Color GetColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Unique: return Color.Orange;
                case ItemRarity.Rare: return Color.Yellow;
                case ItemRarity.Magic: return Color.Blue;
            }
			return Color.White;
        }

        private static Follower.Console console = new Follower.Console(10, 50);
		public static void Log(params string[] words) { console.Add(string.Join(" ", words)); }
		public static void ToggleConsole() => console.Hidden = !console.Hidden;

		public static Vector2 ScreenRelativeToWindow(float x, float y)
		{
			var rect = Game.Window.GetWindowRectangleTimeCache;
			return new Vector2(x * rect.Width, y * rect.Height);
		}

		private static Dictionary<uint, uint> lineCounts = new Dictionary<uint, uint>();
		public static void DrawTextAtEnt(Entity ent, string text)
		{
			if (!IsValid(ent) || text == null) return;
			var camera = Game.Game.IngameState.Camera;
			var pos = camera.WorldToScreen(ent.Pos);
			uint lineCount = 0;
			lineCounts.TryGetValue(ent.Id, out lineCount);
			pos.Y += 12.0f * lineCount;
			try { Gfx.DrawText(string.Format("{0}", text), pos); }
			catch (Exception err)
			{
				Log(string.Format("Exception: {0}", err.Message));
			}
			lineCounts[ent.Id] = lineCount + 1;
		}
		public static void DrawTextAtPlayer(string text) => DrawTextAtEnt(Game.Player, text);
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point lpPoint);
	}
}

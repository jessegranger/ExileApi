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

namespace Assistant
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
			Assistant.Console.Pos = ScreenRelativeToWindow(.01f, .25f);
		}

		public static void Render()
		{
			if (!IsInitialised) return;
			Assistant.Console.Render(Gfx);
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
		public static bool TryGetBuffValue(string buffName, out int buffValue)
        {
			return buffCache.TryGetValue(buffName, out buffValue);
        }
		internal static void RenderBuffs()
        {
			foreach(var item in buffCache)
            {
				DrawTextAtPlayer($"Buff: {item.Key} : {item.Value}");
            }
        }
		internal static void LogBuffs()
        {
			foreach(var buff in buffCache.Keys)
            {
				Log($"Buff: {buff}");
            }
        }

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
				string prefix = modName.Substring(0, modName.Length - 1);
                return itemMods.Any(x => x.Name != null && x.Name.StartsWith(prefix));
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

		public static void Log(params string[] words) { Assistant.Console.Add(string.Join(" ", words)); }
		public static void ToggleConsole() => Assistant.Console.Hidden = !Assistant.Console.Hidden;

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

		public static bool IsFullLife(Entity ent)
        {
			var life = ent.GetComponent<Life>();
			if (life == null) return false;
			return life.CurHP == (life.MaxHP - life.ReservedFlatHP);
        }
		public static bool IsLowLife(Entity ent) {
			var life = ent.GetComponent<Life>();
			if (life == null) return false;
			int maxHP = life.MaxHP - life.ReservedFlatHP;
			return ((float)life.CurHP / maxHP) <= .50f;
		}
		public static bool IsLowES(Entity ent)
        {
            var life = ent.GetComponent<Life>();
			if (life == null) return false;
			return ((float)life.CurES / life.MaxES) <= .50f;
        }
		public static bool IsLowMana(Entity ent)
        {
            var life = ent.GetComponent<Life>();
			if (life == null) return false;
			int maxMana = life.MaxMana - life.ReservedFlatMana;
			return ((float)life.CurMana / maxMana) <= .50f;
        }
		public static bool IsFullMana(Entity ent)
        {
            var life = ent.GetComponent<Life>();
			if (life == null) return false;
			int maxMana = life.MaxMana - life.ReservedFlatMana;
			return life.CurMana == maxMana;
        }

        public static void DebugLife(Entity ent)
        {
            var life = ent.GetComponent<Life>();
            if (life == null)
            {
                DrawTextAtEnt(ent, "Life is null.");
                return;
            }
            DrawTextAtEnt(ent, $"HP: {life.CurHP} / {life.MaxHP} ({life.ReservedFlatHP} reserved {life.ReservedPercentHP}%)");
            DrawTextAtEnt(ent, $"Mana: {life.CurMana} / {life.MaxMana} ({life.ReservedFlatMana} reserved {life.ReservedPercentMana}%)");
            DrawTextAtEnt(ent, $"ES: {life.CurES} / {life.MaxES}");
            // DrawTextAtEnt(ent, $"Fields: {life.Field0} {life.Field1} {life.Field2} {life.Field3}");

		}

        public static bool IsInMap(AreaController area)
        {
            var a = area.CurrentArea;
			if (a == null || a.IsHideout || a.IsTown) return false;
			return true;
        }

		public static bool HasEnoughRage(int rage)
        {
			if(TryGetBuffValue("rage", out int current))
            {
				return current >= rage;
            }
			return false;
        }

		public static bool HasEnoughMana(Entity ent, int mana)
        {
			if (!IsValid(ent)) return false;
			var life = ent.GetComponent<Life>();
			if (life == null) return false;
			return life.CurMana > mana;
        }

    }
}

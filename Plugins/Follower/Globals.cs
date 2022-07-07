using ExileCore;
using ExileCore.PoEMemory;
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

namespace Assistant {
	public static class Globals {
		static GameController Game;
		static Graphics Gfx;
		static AssistantSettings Settings;
		static bool IsInitialised;

		public static void Initialise(GameController game, Graphics gfx, AssistantSettings settings) {
			Game = game;
			Gfx = gfx;
			Settings = settings;
			IsInitialised = true;
		}

		public const string PATH_STACKEDDECK = "Metadata/Items/DivinationCards/DivinationCardDeck";
		public const string PATH_SCROLL_WISDOM = "Metadata/Items/Currency/CurrencyIdentification";
		public const string PATH_CHISEL = "Metadata/Items/Currency/CurrencyMapQuality";
		public const string PATH_ALCHEMY = "Metadata/Items/Currency/CurrencyUpgradeToRare";
		public const string PATH_SCOUR = "Metadata/Items/Currency/CurrencyConvertToNormal";

		public static Graphics GetGraphics() => Gfx;
		public static GameController GetGame() => Game;
		public static AssistantSettings GetSettings() => Settings;

		public static Entity GetPlayer() => Game?.Player;

		public static void Render() {
			if ( !IsInitialised ) return;
			lineCounts.Clear();
		}
		public static void Tick(long dt) {
			if ( !IsInitialised ) return;
			UpdateBuffCache();
		}
		// cache the player buffs for one frame
		private static Dictionary<string, int> buffCache = new Dictionary<string, int>();
		private static void UpdateBuffCache() {
			buffCache.Clear();
			var buffs = Game.Player.GetComponent<Buffs>();
			foreach ( var buff in buffs.BuffsList ) {
				buffCache[buff.Name] = buff.Charges;
			}
		}
		public static bool HasBuff(params string[] buffNames) => buffNames.Any(buffCache.ContainsKey);
		public static bool HasAnyBuff(string buffPrefix) => buffCache.Keys.Any(x => x.StartsWith(buffPrefix));
		public static bool TryGetBuffValue(string buffName, out int buffValue) {
			return buffCache.TryGetValue(buffName, out buffValue);
		}
		internal static void RenderBuffs() {
			foreach ( var item in buffCache ) {
				DrawTextAtPlayer($"Buff: {item.Key} : {item.Value}");
			}
		}
		internal static void LogBuffs() {
			foreach ( var buff in buffCache.Keys ) {
				Log($"Buff: {buff}");
			}
		}

		public static bool HasMod(Entity ent, string modName) {
			if ( ent == null ) return false;
			var mods = ent.GetComponent<Mods>();
			if ( mods == null ) return false;
			return HasMod(mods.ItemMods, modName);
		}
		public static bool HasMod(List<ItemMod> itemMods, string modName) {
			if ( itemMods == null || modName == null ) return false;
			if ( modName.EndsWith("*") ) {
				string prefix = modName.Substring(0, modName.Length - 1);
				return itemMods.Any(x => x.Name != null && x.Name.StartsWith(prefix));
			} else {
				return itemMods.Any(x => x.Name != null && x.Name.Equals(modName));
			}
		}

		public static bool IsValid(RemoteMemoryObject item) => (item != null && item.Address != 0);
		public static bool IsValid(NormalInventoryItem item) => (item != null && item.IsValid && item.Item != null && item.Item.Path != null);
		public static bool IsValid(Entity ent) => (ent != null && ent.Path != null && ent.IsValid);
		public static bool IsValid(ServerInventory.InventSlotItem item) => IsValid(item.Item);
		public static bool IsValid(LabelOnGround label) => label.Label != null && label.Label.Text != null && label.ItemOnGround != null;
		public static bool IsValid(GameController game) => game != null && game.Game != null && game.IngameState != null;
		public static bool IsValid(ActorVaalSkill skill) => skill != null && skill.VaalSkillInternalName != null;
		public static bool IsValid(Vector2 vec) => vec != Vector2.Zero && !float.IsNaN(vec.X) && !float.IsInfinity(vec.X) && !float.IsNaN(vec.Y) && !float.IsInfinity(vec.Y);

		public static ItemRarity GetItemRarity(Entity ent) {
			if ( IsValid(ent) ) {
				var mods = ent.GetComponent<Mods>();
				if ( mods != null ) {
					return mods.ItemRarity;
				}
			}
			return ItemRarity.Normal;
		}
		public static Color GetColor(ItemRarity rarity) {
			switch ( rarity ) {
				case ItemRarity.Unique: return Color.Orange;
				case ItemRarity.Rare: return Color.Yellow;
				case ItemRarity.Magic: return Color.Blue;
			}
			return Color.White;
		}

		public static void Log(params string[] words) { DebugWindow.LogMsg(string.Join(" ", words)); }

		public static Vector2 ScreenRelativeToWindow(Vector2 pos) => ScreenRelativeToWindow(pos.X, pos.Y);
		public static Vector2 ScreenRelativeToWindow(float x, float y) {
			var rect = Game.Window.GetWindowRectangleTimeCache;
			return new Vector2(x * rect.Width, y * rect.Height);
		}
		public static Vector2 WindowToScreenRelative(float x, float y) {
			var rect = Game.Window.GetWindowRectangleTimeCache;
			return new Vector2(x / rect.Width, y / rect.Height);
		}
		public static Vector2 WindowToScreenRelative(Vector2 pos) => WindowToScreenRelative(pos.X, pos.Y);

		private static Dictionary<uint, uint> lineCounts = new Dictionary<uint, uint>();
		public static void DrawTextAtEnt(Entity ent, string text) {
			if ( !IsValid(ent) || text == null ) return;
			var camera = Game.Game.IngameState.Camera;
			var pos = camera.WorldToScreen(ent.Pos);
			uint lineCount = 0;
			lineCounts.TryGetValue(ent.Id, out lineCount);
			pos.Y += 12.0f * lineCount;
			try { Gfx.DrawText(string.Format("{0}", text), pos); } catch ( Exception err ) {
				Log(string.Format("Exception: {0}", err.Message));
			}
			lineCounts[ent.Id] = lineCount + 1;
		}
		public static void DrawTextAtPlayer(string text) => DrawTextAtEnt(Game.Player, text);
		[DllImport("user32.dll")] public static extern bool GetCursorPos(out Point lpPoint);

		public static bool IsFullLife(Entity ent) {
			if ( ent == null ) return false;
			var life = ent.GetComponent<Life>();
			if ( life == null ) return false;
			return life.CurHP == (life.MaxHP - life.TotalReservedHP);
		}
		public static bool IsLowLife(Entity ent) {
			if ( ent == null ) return false;
			var life = ent.GetComponent<Life>();
			if ( life == null ) return false;
			int maxHP = life.MaxHP - life.TotalReservedHP;
			return ((float)life.CurHP / maxHP) <= .50f;
		}
		public static bool IsMissingLife(Entity ent, int amount) {
			if ( ent == null ) return false;
			var life = ent.GetComponent<Life>();
			if ( life == null ) return false;
			int maxHP = life.MaxHP - life.TotalReservedHP;
			if ( HasBuff("petrified_blood") ) {
				maxHP = Math.Min(maxHP, life.MaxHP / 2);
			}
			int missing = maxHP - life.CurHP;
			return missing >= amount;
		}
		public static bool IsLowES(Entity ent) {
			var life = ent.GetComponent<Life>();
			if ( life == null ) return false;
			return ((float)life.CurES / life.MaxES) <= .50f;
		}
		public static bool IsLowMana(Entity ent) {
			var life = ent.GetComponent<Life>();
			if ( life == null ) return false;
			int maxMana = life.MaxMana - life.TotalReservedMana;
			return ((float)life.CurMana / maxMana) <= .50f;
		}
		public static bool IsFullMana(Entity ent) {
			var life = ent.GetComponent<Life>();
			if ( life == null ) return false;
			int maxMana = life.MaxMana - life.TotalReservedMana;
			return life.CurMana == maxMana;
		}

		public static void DebugLife(Entity ent) {
			var life = ent.GetComponent<Life>();
			if ( life == null ) {
				DrawTextAtEnt(ent, "Life is null.");
				return;
			}
			DrawTextAtEnt(ent, $"HP: {life.CurHP} / {life.MaxHP} ({life.TotalReservedHP} reserved {life.ReservedPercentHP}%)");
			DrawTextAtEnt(ent, $"Mana: {life.CurMana} / {life.MaxMana} ({life.TotalReservedMana} reserved {life.ReservedPercentMana}%)");
			DrawTextAtEnt(ent, $"ES: {life.CurES} / {life.MaxES}");

		}

		public static bool IsInMap(AreaController area) {
			var a = area.CurrentArea;
			if ( a == null || a.IsHideout || a.IsTown ) return false;
			return true;
		}

		public static bool HasEnoughRage(int rage) {
			if ( TryGetBuffValue("rage", out int current) ) {
				return current >= rage;
			}
			return false;
		}

		public static bool HasEnoughMana(Entity ent, int mana) {
			if ( !IsValid(ent) ) return false;
			var life = ent.GetComponent<Life>();
			if ( life == null ) return false;
			return life.CurMana > mana;
		}

		public static bool IsAlive(Entity ent) => IsValid(ent) && (ent.GetComponent<Life>()?.CurHP ?? 0) > 0;

		public static int GetWitherStacks(Entity ent) => IsValid(ent) ? (ent.GetComponent<Buffs>()?.BuffsList.Where(b => b.Name.Equals("withered")).Count() ?? 0) : 0;

		public static bool TryGetGameStat(Entity ent, GameStat stat, out int result) {
			result = 0;
			if( ent?.GetComponent<Stats>()?.StatDictionary.TryGetValue(stat, out result) ?? false ) {
				return true;
			}
			return false;
		}

		public static bool StashIsOpen() => GetGame()?.IngameState?.IngameUi?.StashElement?.IsVisible ?? false;

		public static bool BackpackIsOpen() => GetGame()?.IngameState?.IngameUi?.InventoryPanel?.IsVisible ?? false;

		public static IEnumerable<T> Empty<T>() { yield break; }
		public static IEnumerable<NormalInventoryItem> BackpackItems() => GetGame()?.IngameState?.IngameUi?.InventoryPanel[InventoryIndex.PlayerInventory]?.VisibleInventoryItems ?? Empty<NormalInventoryItem>();

		public static IEnumerable<NormalInventoryItem> StashItems() => GetGame()?.IngameState?.IngameUi?.StashElement?.VisibleStash?.VisibleInventoryItems ?? Empty<NormalInventoryItem>();

		public static IEnumerable<LabelOnGround> GroundLabels() => (GetGame()?.IngameState?.IngameUi?.ItemsOnGroundLabels ?? Empty<LabelOnGround>()).Where(x => x.Address != 0 && x.IsVisible);

		public static int StashTab() => GetGame()?.IngameState?.IngameUi?.StashElement?.IndexVisibleStash ?? 0;

		public static LabelOnGround NearestGroundLabel() => GroundLabels()
				.Where(IsValid)
				.Where(label => label.ItemOnGround?.Type != ExileCore.Shared.Enums.EntityType.Npc)
				.OrderBy(label => Vector3.DistanceSquared(GetPlayer().Pos, label.ItemOnGround?.Pos ?? Vector3.Zero))
				.FirstOrDefault();

		public static bool ChatIsOpen() {
			var game = GetGame();
			var chat = game.IngameState.IngameUi.ChatBoxRoot;
			return chat != null && chat.IsValid && chat.IsActive;
		}
	}
}

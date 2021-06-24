using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Follower.Globals;

namespace Follower
{
	static class Inventory
	{
		static GameController Game;
		static Graphics Gfx;
		static bool IsInitialised;

		public static void Initialise(GameController game, Graphics gfx)
		{
			Game = game;
			Gfx = gfx;
			IsInitialised = true;
		}

		public static NormalInventoryItem FindFirstInventoryItem(string path)
		{
			if (!IsInitialised) return null;
			var panel = Game.Game.IngameState.IngameUi.InventoryPanel;
			if (panel == null) return null;
			if (!panel.IsVisible) return null;
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if (playerInventory == null) return null;
			var items = playerInventory.VisibleInventoryItems;
			if (items == null) return null;
			return items.FirstOrDefault(x => x.Item != null && x.Item.IsValid && x.Item.Path != null && x.Item.Path.Equals(path));
		}

		public static NormalInventoryItem FindFirstInventoryItemWithoutMods(string path, params string[] modNames)
		{
			if (!IsInitialised) return null;
			var panel = Game.Game.IngameState.IngameUi.InventoryPanel;
			if (panel == null) return null;
			if (!panel.IsVisible) return null;
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if (playerInventory == null) return null;
			IEnumerable<NormalInventoryItem> items;
			if (path.EndsWith("*"))
			{
				path = path.Substring(0, path.Length - 1);
				items = playerInventory.VisibleInventoryItems.Where(x => x.Item.Path.StartsWith(path));
			}
			else
			{
				items = playerInventory.VisibleInventoryItems.Where(x => x.Item.Path.Equals(path));
			}
			return items.FirstOrDefault(x =>
			{
				return CountMatchingMods(x.Item.GetComponent<Mods>().ItemMods, modNames) < modNames.Length;
			});
		}

		public static NormalInventoryItem FindFirstNonMatch(string path, ItemQuery query)
		{
			if (!IsInitialised) return null;
			var panel = Game.Game.IngameState.IngameUi.InventoryPanel;
			if (panel == null) return null;
			if (!panel.IsVisible) return null;
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if (playerInventory == null) return null;
			return playerInventory.VisibleInventoryItems.FirstOrDefault((item) => IsValid(item) && item.Item.Path.Equals(path) && ! query.Matches(item));
		}

		public static int CountMatchingMods(List<ItemMod> itemMods, params string[] targetMods)
		{
			int count = 0;
			foreach (string mod in targetMods)
			{
				if (HasMod(itemMods, mod)) count++;
			}
			return count;
		}

		public static bool UseItemOnItem(string path, NormalInventoryItem item)
		{
			if (item == null)
			{
				Log("UseItemOnItem: target is null");
				return false;
			}
			Log(string.Format("UseItemOnItem({0}, {1})", path, item.Item.Path));
			var useItem = FindFirstInventoryItem(path);
			if (useItem == null)
			{
				Log(string.Format("UseItemOnItem: Cannot find any {0} to use.", path));
				return false;
			}

			Log(string.Format("UseItemOnItem: Using item at {0} on item at {1}.", useItem.GetClientRect().Center, item.GetClientRect().Center));

			var centerOne = useItem.GetClientRect().Center;
			var centerTwo = item.GetClientRect().Center;
			InputManager.Add(new RightClickAt(Game.Window, centerOne.X, centerOne.Y, 20,
				new LeftClickAt(Game.Window, centerTwo.X, centerTwo.Y, 20)));
			return true;
		}

	}
}

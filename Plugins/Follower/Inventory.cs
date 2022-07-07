using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput.Native;
using static Assistant.Globals;

namespace Assistant {
	static class Inventory {

		// the physical layout of the backpack:
		private static NormalInventoryItem[,] inventoryMap = new NormalInventoryItem[12, 5]; // one Entity appears more than once if it takes up more than one grid
		public static void Initialise() {
			InputManager.OnRelease(VirtualKeyCode.VK_I, () => {
				try { RefreshBackpack(); } catch ( Exception e ) { Log(e.StackTrace); }
			});
		}
		private static void MarkOccupied(NormalInventoryItem[,] map, NormalInventoryItem ent, uint x, uint y, uint w, uint h) {
			for(int i = 0; i < w; i++ ) {
				for(int j = 0; j < h; j++) {
					map[x + i, y + j] = ent;
				}
			}
		}
		internal static void RefreshBackpack() {
			var game = GetGame();
			var panel = game?.IngameState?.IngameUi?.InventoryPanel ?? null;
			if ( panel == null ) {
				Log("No panel.");
				return;
			}
			var map = new NormalInventoryItem[12, 5];
			var backpack = panel[InventoryIndex.PlayerInventory];
			foreach(var item in backpack?.VisibleInventoryItems ?? Empty<NormalInventoryItem>() ) {
				uint x = (uint)Math.Max(0, item.InventPosX);
				uint y = (uint)Math.Max(0, item.InventPosY);
				uint w = (uint)Math.Max(0, item.ItemWidth);
				uint h = (uint)Math.Max(0, item.ItemHeight);
				MarkOccupied(map, item, x, y, w, h);
			}
			inventoryMap = map;

			// debug output:
			for ( uint dy = 0; dy < 5; dy++ ) {
				string line = "";
				for ( uint dx = 0; dx < 12; dx++ ) {
					if ( inventoryMap[dx, dy] != null ) line += "x";
					else line += "_";
				}
				Log(line);
			}
		}

		public static NormalInventoryItem FindFirstItem(string path) {
			var game = GetGame();
			if ( game == null ) return null;
			var panel = game.IngameState.IngameUi.InventoryPanel;
			if ( panel == null ) return null;
			if ( !panel.IsVisible ) return null;
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if ( playerInventory == null ) return null;
			var items = playerInventory.VisibleInventoryItems;
			if ( items == null ) return null;
			return items.FirstOrDefault(x => x.Item != null && x.Item.IsValid && x.Item.Path != null && x.Item.Path.Equals(path));
		}

		public static NormalInventoryItem FindFirstItemWithoutMods(string path, params string[] modNames) {
			var game = GetGame();
			if ( game == null ) return null;
			var panel = game.IngameState.IngameUi.InventoryPanel;
			if ( panel == null ) return null;
			if ( !panel.IsVisible ) return null;
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if ( playerInventory == null ) return null;
			IEnumerable<NormalInventoryItem> items;
			if ( path.EndsWith("*") ) {
				path = path.Substring(0, path.Length - 1);
				items = playerInventory.VisibleInventoryItems.Where(x => x.Item.Path.StartsWith(path));
			} else {
				items = playerInventory.VisibleInventoryItems.Where(x => x.Item.Path.Equals(path));
			}
			return items.FirstOrDefault(x => {
				return CountMatchingMods(x.Item.GetComponent<Mods>().ItemMods, modNames) < modNames.Length;
			});
		}

		public static NormalInventoryItem FindFirstNonMatch(string path, ItemQuery query) {
			var game = GetGame();
			if ( game == null ) return null;
			var panel = game.IngameState.IngameUi.InventoryPanel;
			if ( panel == null ) return null;
			if ( !panel.IsVisible ) return null;
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if ( playerInventory == null ) return null;
			foreach ( var item in playerInventory.VisibleInventoryItems ) {
				if ( !IsValid(item) ) continue;
				if ( !item.Item.Path.Equals(path) ) continue;
				if ( !query.Matches(item) ) return item;
			}
			return null;
		}

		public static int CountMatchingMods(List<ItemMod> itemMods, params string[] targetMods) {
			int count = 0;
			foreach ( string mod in targetMods ) {
				if ( HasMod(itemMods, mod) ) count++;
			}
			return count;
		}

		internal static State PlanUseItemOnItem(string path, NormalInventoryItem item, int clicks = 1, State next = null) {
			if ( !IsValid(item) ) {
				Log("UseItemOnItem: target is invalid");
				return null;
			}
			Log($"UseItemOnItem({path}, {item.Item.Path})");
			Vector2 targetItemPosition = item.GetClientRect().Center;

			// leftClick will be a sequence of states like: SHIFT, CLICK, CLICK, CLICK, SHIFT UP
			// but we build it in reverse (so we can chain the Next pointers easily)
			// start with the tail piece: CLICK, SHIFT UP
			State leftClick = new LeftClickAt(targetItemPosition, inputSpeed, new KeyUp(VirtualKeyCode.LSHIFT, new Delay(inputSpeed, next)));
			// add in the right number of intermediate: CLICK
			while( clicks-- > 1 ) {
				leftClick = new LeftClickAt(targetItemPosition, 50, leftClick);
			}
			// attach the header: SHIFT DOWN
			leftClick = new KeyDown(VirtualKeyCode.LSHIFT, new Delay(50, leftClick));
			// finish with the plan to use the stash item
			return PlanUseItem(path, leftClick);
		}

		internal static State PlanUseItem(NormalInventoryItem item, State next = null) => new RightClickAt(item, inputSpeed, next);

		internal static State PlanUseItem(string path, State next = null) {
			var useItem = FindFirstItem(path);
			if ( IsValid(useItem) ) {
				return PlanUseItem(useItem, next);
			}
			Log($"UseItem: Cannot find any {path} to use.");
			return null;
		}

		public static NormalInventoryItem FindFirstStashItem(string path) => StashItems().Where(IsValid).Where((i) => i.Item.Path.Equals(path)).FirstOrDefault();

		internal static State PlanUseStashItem(string path, State next = null) {
			if ( !StashIsOpen() ) return null;
			var useItem = FindFirstStashItem(path);
			if( IsValid(useItem) ) {
				return new RightClickAt(useItem, inputSpeed, next);
			}
			Log($"UseStashItem: Cannot find any {path} to use.");
			return null;
		}
		internal static State PlanUseStashItemOnItem(string path, NormalInventoryItem item, int clicks = 1, State next = null) {
			var pos = item.GetClientRect().Center;
			State leftClick = new LeftClickAt(pos, inputSpeed, new KeyUp(VirtualKeyCode.LSHIFT, new Delay(inputSpeed, next)));
			while( clicks-- > 1 ) { // add in the right number of intermediate: CLICK
				leftClick = new LeftClickAt(pos, 50, leftClick);
			}
			leftClick = new KeyDown(VirtualKeyCode.LSHIFT, new Delay(50, leftClick));
			return PlanUseStashItem(path, leftClick);
		}

		public static bool UseItemOnItem(string path, NormalInventoryItem item, int clicks = 1) {
			State plan = PlanUseItemOnItem(path, item, clicks);
			if ( plan != null ) {
				InputManager.Add(plan);
				return true;
			}
			return false;
		}

		internal static State PlanStashAll(State next = null) {
			var doNotStashSet = new HashSet<NormalInventoryItem>();
			var doNotIdentifySet = new HashSet<NormalInventoryItem>();
			var doNotIncubateSet = new HashSet<NormalInventoryItem>();
			var doNotOpenSet = new HashSet<NormalInventoryItem>();
			return State.From("StashAll", (state) => {
				var needs = new Dictionary<string, int>(restockNeeds);
				if ( !StashIsOpen() ) {
					Log("Stash not open.");
					return null;
				}
				if ( !BackpackIsOpen() ) {
					Log("Backpack is not open.");
					return null;
				}
				RefreshBackpack();
				foreach ( var item in BackpackItems() ) {
					if ( doNotStashSet.Contains(item) ) { continue; }
					if ( !IsValid(item) ) { continue; }
					var ent = item.Item;
					var mods = ent.GetComponent<Mods>();
					if ( (!(mods?.Identified ?? true)) && (!doNotIdentifySet.Contains(item)) ) {
						doNotIdentifySet.Add(item); // never try to identify it twice
						return PlanUseItemOnItem(PATH_SCROLL_WISDOM, item, 1, state);
					}
					if ( needs.TryGetValue(ent.Path, out int need) && need > 0 ) {
						needs[ent.Path] -= ent.GetComponent<Stack>()?.Size ?? 1;
						continue;
					}
					if ( ent.Path.Equals(PATH_STACKEDDECK) ) continue; // a second pass will open these
					if ( ent.Path.StartsWith("Metadata/Items/Currency/CurrencyIncubation") && !doNotIncubateSet.Contains(item) ) {
						doNotIncubateSet.Add(item);
						return PlanApplyIncubator(item, state);
					}
					var pos = item.GetClientRect().Center;
					if ( !IsValid(pos) ) { continue; }
					Log($"Stashing {ent.Path} with a ctrl-left-click at: {pos}");
					doNotStashSet.Add(item); // never try to stash it twice
					return new CtrlLeftClickAt(pos.X, pos.Y, inputSpeed, state);
				}
				RefreshBackpack();
				// do a second pass now to expand decks and stash
				foreach ( var item in BackpackItems() ) {
					if ( !IsValid(item) ) { continue; }
					var ent = item.Item;
					var deckPosition = item.GetClientRect().Center;
					if( ent.Path.Equals(PATH_STACKEDDECK) ) {
						Vector2 pos2 = GetFreeSlot(1, 1);
						if ( pos2 == Vector2.Zero ) {
							Log("No more open space found.");
							return next;
						}
						pos2 = ScreenRelativeToWindow(pos2.X, pos2.Y);
						return new RightClickAt(deckPosition, inputSpeed,
							new LeftClickAt(pos2, inputSpeed,
							new Delay(350, state)));
					}
				}
				return next;
			});
		}
		internal static void StashAll(State next = null) {
			InputManager.Add(PlanStashAll(next)); //  PlanRestockFromStash(PlanOpenAllStackedDecks())));
		}

		private static Dictionary<string, int> restockNeeds = new Dictionary<string, int>() {
			{  "Metadata/Items/Currency/CurrencyIdentification", 40 },
			{  "Metadata/Items/Currency/CurrencyPortal", 40 },
			{  "Metadata/Items/Currency/CurrencyCorruptMonolith", 9 },
		};
		private static readonly VirtualKeyCode[] numberKeys = new VirtualKeyCode[] {
			VirtualKeyCode.VK_0,
			VirtualKeyCode.VK_1,
			VirtualKeyCode.VK_2,
			VirtualKeyCode.VK_3,
			VirtualKeyCode.VK_4,
			VirtualKeyCode.VK_5,
			VirtualKeyCode.VK_6,
			VirtualKeyCode.VK_7,
			VirtualKeyCode.VK_8,
			VirtualKeyCode.VK_9,
		};
		internal static uint inputSpeed = 40;
		internal static State PlanRestockFromStash(State next = null) {
			return State.From((state) => {
				if ( !BackpackIsOpen() ) return null;
				if ( !StashIsOpen() ) return null;
				RefreshBackpack();
				var needs = new Dictionary<string, int>(restockNeeds);
				var targets = new Dictionary<string, NormalInventoryItem>();
				// first scan what we already have
				foreach ( var item in BackpackItems() ) {
					var ent = item.Item;
					var path = ent.Path;
					if ( needs.ContainsKey(path) ) {
						var stack = ent.GetComponent<Stack>();
						var size = stack?.Size ?? 1;
						var max = stack?.Info?.MaxStackSize ?? 1;
						needs[path] -= size;
						// make note while we scan of where we need to restock partial stacks
						targets[path] = item;
					}
				}
				foreach ( var item in StashItems() ) {
					if ( !IsValid(item) ) continue;
					var ent = item.Item;
					needs.TryGetValue(ent.Path, out int need);
					if ( need > 0 ) {
						Vector2 sourcePos = item.GetClientRect().Center;
						if ( !IsValid(sourcePos) ) continue;
						Vector2 targetPos = Vector2.Zero;
						if ( targets.TryGetValue(ent.Path, out NormalInventoryItem target) ) {
							targetPos = target.GetClientRect().Center;
						} else {
							var slot = GetFreeSlot(1, 1);
							if ( IsValid(slot) ) {
								targetPos = ScreenRelativeToWindow(slot);
							}
						}
						if ( !IsValid(targetPos) ) continue;
						var stackSize = item.Item.GetComponent<Stack>()?.Info?.MaxStackSize ?? 1;
						if ( need < 10 ) {
							// TODO: if need >= 10, it needs to play out a sequence of numberKey[i]'s
							return new ShiftLeftClickAt(sourcePos, inputSpeed,
								new PressKey(numberKeys[need], inputSpeed,
								new PressKey(VirtualKeyCode.RETURN, inputSpeed,
								new LeftClickAt(targetPos, inputSpeed,
								new Delay(100,
									state)))));
						} else {
							return new CtrlLeftClickAt(sourcePos, inputSpeed, 
								new Delay(100, state));
						}
					}
				}
				return next;
			});
		}

		internal static void RestockFromStash() {
			InputManager.Add(PlanRestockFromStash());
		}

		private static bool IsOccupied(uint x, uint y, uint w, uint h) {
			uint ex = Math.Min(12, x + w);
			uint ey = Math.Min(5, y + h);
			for ( uint dy = y; dy < ey; dy++ ) {
				for ( uint dx = x; dx < ex; dx++ ) {
					if ( inventoryMap[dx, dy] != null ) return true;
				}
			}
			return false;
		}

		private static Vector2 TopLeftRelativePosition = new Vector2(.625f, .545f);
		private static Vector2 TileRelativeSize = new Vector2(.03f, .049f);
		private static Vector2 TileRelativeCenter(uint tx, uint ty) => new Vector2(TopLeftRelativePosition.X + ((tx + .5f) * TileRelativeSize.X), TopLeftRelativePosition.Y + ((ty + .5f) * TileRelativeSize.Y));
		internal static Vector2 GetFreeSlot(uint w, uint h) {
			Log($"GetFreeSlot({w},{h})");
			for ( uint dy = 0; dy < 5; dy++ ) {
				for ( uint dx = 0; dx < 12; dx++ ) {
					if ( !IsOccupied(dx, dy, w, h) ) {
						Log($"Found free slot {dx}, {dy}");
						return TileRelativeCenter(dx, dy);
					}
				}
			}
			return Vector2.Zero;
		}
		internal static void HighlightFreeSlot(uint w, uint h) {
			Log($"HighlightFreeSlot({w},{h})");
			RefreshBackpack();
			var pos = GetFreeSlot(w, h);
			if ( pos == Vector2.Zero ) PersistedText.Add("[No Free Slots]", ScreenRelativeToWindow(.5f, .5f), 4000);
			else PersistedText.Add("[Free]", ScreenRelativeToWindow(pos.X, pos.Y), 4000);
		}

		internal static NormalInventoryItem GetItemUnderCursor() {
			Vector2 pos = WindowToScreenRelative(Input.MousePosition);
			pos -= TopLeftRelativePosition;
			pos /= TileRelativeSize;
			Log($"Cursor is over inventory slot: {pos}");
			return inventoryMap[(int)pos.X, (int)pos.Y];
		}

		internal static void OpenAllStackedDecks() {
			InputManager.Add((State)PlanOpenAllStackedDecks);
		}
		internal static State PlanOpenAllStackedDecks(State next) {
			return State.From((state) => {
				if ( !BackpackIsOpen() ) return null;
				RefreshBackpack();
				var pos = FindFirstItem(PATH_STACKEDDECK)?.GetClientRect().Center ?? Vector2.Zero;
				if ( pos == Vector2.Zero ) {
					Log("No more decks to open.");
					return next;
				}
				Vector2 pos2 = GetFreeSlot(1, 1);
				if ( pos2 == Vector2.Zero ) {
					Log("No more open space found.");
					return next;
				}
				pos2 = ScreenRelativeToWindow(pos2.X, pos2.Y);
				return new RightClickAt(pos.X, pos.Y, inputSpeed,
					new LeftClickAt(pos2.X, pos2.Y, inputSpeed,
					new Delay(350, state)));
			});
		}

		public static IEnumerable<NormalInventoryItem> EquippedItems() {
			var panel = GetGame()?.IngameState?.IngameUi?.InventoryPanel;
			if ( panel == null || !panel.IsVisible ) yield break;
			var equippedInventories = new InventoryIndex[] {
						InventoryIndex.Helm,
						InventoryIndex.Amulet,
						InventoryIndex.Chest,
						InventoryIndex.LWeapon,
						InventoryIndex.RWeapon,
						InventoryIndex.LWeapon,
						InventoryIndex.RRing,
						InventoryIndex.LRing,
						InventoryIndex.Gloves,
						InventoryIndex.Belt,
						InventoryIndex.Boots
					};
			foreach ( var equipIndex in equippedInventories ) {
				var equipInventory = panel[equipIndex];
				if ( equipInventory == null ) {
					Log($"Warn: Equipment Inventory {equipIndex} is null");
					continue;
				}
				var equipItems = equipInventory.VisibleInventoryItems;
				if ( equipItems == null ) {
					Log($"Warn: Equipment Inventory {equipIndex} has null items");
					continue;
				}
				Log($"Inventory {equipIndex}: {equipInventory.IsValid} {equipItems.Count}");
				var equipItem = equipItems.FirstOrDefault();
				if ( equipItem == null ) {
					Log($"Warn: Equipment Inventory {equipIndex} has null first item");
					continue;
				}
				var theItem = equipItem.Item;
				if ( theItem == null ) {
					Log($"Warn: Equipment Inventory {equipIndex} does not reference a game item");
					continue;
				}
				yield return equipItem;
			}
			yield break;
		}

		internal static State PlanApplyIncubator(NormalInventoryItem incubator, State next = null) {
			var equipItem = EquippedItems().Where(item => IsValid(item) && item.Item.GetComponent<Mods>().IncubatorName == null).FirstOrDefault();
			if( equipItem == null ) {
				Log($"All items have incubators already.");
				return next;
			}
			return new RightClickAt(incubator, inputSpeed, new LeftClickAt(equipItem, inputSpeed, new Delay(200, next)));
		}


	}
}

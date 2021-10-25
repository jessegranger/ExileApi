using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput.Native;
using static Assistant.Globals;

namespace Assistant
{
    class Roller
    {
        static GameController Game;
        static Graphics Gfx;
        public static bool IsInitialised;
        public static bool Paused = false;
        public static long IntervalMilliseconds = 500;
        public static long MaxSteps = long.MaxValue;
        private static long StepCount = 0;

        public static void SetTarget(string itemPath, params string[] modNames)
        {
            rollItemPath = itemPath;
            rollQuery = new ItemQuery();
            rollQuery.MatchAll(modNames);
        }
        public static void SetTarget(string itemPath, int count, params string[] modNames)
        {
            rollItemPath = itemPath;
            rollQuery = new ItemQuery();
            rollQuery.MatchCount(count, modNames);
        }
		public static void SetTarget(string itemPath, ItemQuery targetQuery)
        {
			rollItemPath = itemPath;
			rollQuery = targetQuery;
        }
        private static string rollItemPath = null;
        private static ItemQuery rollQuery = null;

        public enum Mode
        {
            Alteration,
            Alchemy,
            Chaos
        }
        private static Mode mode = Mode.Alteration;
        public static void SetMode(Mode mode) => Roller.mode = mode;
        private static Stopwatch rollTimer = new Stopwatch();

		public static bool Ready() => IsInitialised && !Paused && rollItemPath != null && rollQuery != null;
        public static bool Pause(string reason)
        {
            Log("Pausing: " + reason);
            return Paused = true;
        }
        public static bool Resume()
        {
            Log("Resuming Roller...");
			StepCount = 0;
            return Paused = false;
        }

        public static void Initialise(GameController game, Graphics gfx)
        {
            Game = game;
            Gfx = gfx;
            IsInitialised = true;
        }
        public static void Tick(long dt)
        {
            if (!Ready()) return;
            if (InputManager.IsPressed(VirtualKeyCode.ESCAPE))
            {
                Paused = true;
                return;
            }
            if (rollTimer.IsRunning && rollTimer.ElapsedMilliseconds < IntervalMilliseconds) return;
            rollTimer.Restart();
            if (StepCount >= MaxSteps)
            {
                StepCount = 0;
                Paused = true;
                return;
            }
            StepCount += 1;
            switch (rollItemPath)
            {
                case "Metadata/Items/Jewels/JewelPassiveTreeExpansionSmall": OneStepWithSmallClusterJewel(); break;
                case "Metadata/Items/Jewels/JewelPassiveTreeExpansionMedium": OneStepWithMediumClusterJewel(); break;
				case "Metadata/Items/Rings/RingAtlas3": OneStepForMagicRing(); break;
            }
        }
        public static void Render()
        {
            if (!IsInitialised) return;
        }


        static bool UpgradeToMagic(NormalInventoryItem item)
        {
            if (!Inventory.UseItemOnItem("Metadata/Items/Currency/CurrencyUpgradeToMagic", item))
            {
                Pause("Failed to use Orb of Transmutation.");
                return false;
            }
            return true;
        }
        static bool RerollMagic(NormalInventoryItem item)
        {
            if (!Inventory.UseItemOnItem("Metadata/Items/Currency/CurrencyRerollMagic", item))
            {
                Pause("Failed to use Orb of Alteration.");
                return false;
            }
            return true;
        }
        static bool AddModToMagic(NormalInventoryItem item)
        {
            if (!Inventory.UseItemOnItem("Metadata/Items/Currency/CurrencyAddModToMagic", item))
            {
                Pause("Failed to use Orb of Augmentation.");
                return false;
            }
            return true;
        }
        static bool UpgradeMagicToRare(NormalInventoryItem item)
        {
            if (!Inventory.UseItemOnItem("Metadata/Items/Currency/CurrencyUpgradeMagicToRare", item))
            {
                Pause("Failed to use Regal Orb.");
                return false;
            }
            return true;
        }
        static bool ConvertToNormal(NormalInventoryItem item)
        {
            if (!Inventory.UseItemOnItem("Metadata/Items/Currency/CurrencyConvertToNormal", item))
            {
                Pause("Failed to use Orb of Scouring.");
                return false;
            }
            return true;
        }

		static bool OneStepWithMediumClusterJewel()
        {
            if (rollItemPath == null) return false;
            var rollItem = Inventory.FindFirstNonMatch(rollItemPath, rollQuery);
            if (rollItem == null)
            {
                return Pause(string.Format("Cannot find any {0} item to roll.", rollItemPath));
            }
            if (!rollItem.Item.HasComponent<Mods>())
            {
                return Pause("Cannot use Roller on item with no mods.");
            }
            var panel = Game.IngameState.IngameUi.InventoryPanel;
            if (!panel.IsVisible)
            {
                return Pause("Cannot roll when inventory is closed.");
            }
            if (rollQuery.Matches(rollItem))
            {
                return Pause("Successfully rolled target mod.");
            }
            var mods = rollItem.Item.GetComponent<Mods>();
			if( mods == null )
            {
				return Pause("Item has null Mods component.");
            }
            Log(string.Format("(Medium Cluster) {0}", mods.ItemRarity));
			switch( mods.ItemRarity)
            {
				case ItemRarity.Normal:
					UpgradeToMagic(rollItem);
					break;
				case ItemRarity.Magic:
					var q = new ItemQuery();
					q.MatchAll("Affliction*");
					var afflictionCount = q.CountMatches(rollItem);
					q = new ItemQuery();
					q.MatchAll("AfflictionNotable*");
					var notableCount = q.CountMatches(rollItem);
					Log($"(Medium Cluster) Affliction Count: {afflictionCount} Notable Count: {notableCount}");
                    // if there's a notable but not one we are looking for, re-roll all of it
                    if (notableCount > 0) { RerollMagic(rollItem); }
                    else if ( afflictionCount == 1 ) { AddModToMagic(rollItem); }
                    else if ( afflictionCount == 2 ) { RerollMagic(rollItem);  }
                    else {
                        Log($"(Medium Cluster) TODO");
                    }
					break;
				case ItemRarity.Rare:
					ConvertToNormal(rollItem);
					break;
            }

			return true;
        }

		static bool OneStepForMagicRing()
        {
			if (rollItemPath == null) return false;
            var rollItem = Inventory.FindFirstNonMatch(rollItemPath, rollQuery);
			if( rollItem == null)
            {
				return Pause($"Cannot find any {rollItemPath} item to roll.");
            }
			if (!rollItem.Item.HasComponent<Mods>())
			{
				return Pause("Cannot use Roller on item with no mods.");
			}
			var panel = Game.IngameState.IngameUi.InventoryPanel;
			if (!panel.IsVisible)
			{
				return Pause("Cannot roll when inventory is closed.");
			}
			if (rollQuery.Matches(rollItem))
			{
				return Pause("Successfully rolled target mod.");
			}
			var mods = rollItem.Item.GetComponent<Mods>();
			if (mods == null)
			{
				return Pause("Item has null Mods component.");
			}
			return true;
        }
        static bool OneStepWithSmallClusterJewel()
        {
            if (rollItemPath == null) return false;
            var rollItem = Inventory.FindFirstNonMatch(rollItemPath, rollQuery);
            if (rollItem == null)
            {
                return Pause(string.Format("Cannot find any {0} item to roll.", rollItemPath));
            }
            if (!rollItem.Item.HasComponent<Mods>())
            {
                return Pause("Cannot use Roller on item with no mods.");
            }
            var panel = Game.IngameState.IngameUi.InventoryPanel;
            if (!panel.IsVisible)
            {
                return Pause("Cannot roll when inventory is closed.");
            }
            if (rollQuery.Matches(rollItem))
            {
                return Pause("Successfully rolled target mod.");
            }
            var mods = rollItem.Item.GetComponent<Mods>();
			if( mods == null )
            {
				return Pause("Item has null Mods component.");
            }
            Log(string.Format("(Small Cluster) {0}", mods.ItemRarity));
            // if the target item:
            switch (mods.ItemRarity)
            {
                case ItemRarity.Normal:
                    UpgradeToMagic(rollItem);
                    break;
                case ItemRarity.Magic:
                    var q = new ItemQuery();
                    q.MatchAll("AfflictionNotable*"); // it has some notable, but not the one we want
                    if (q.Matches(rollItem)) { RerollMagic(rollItem); }
                    else
                    {
                        if (mods.ItemMods.Count == 2)
                        {
                            AddModToMagic(rollItem);
                        }
                        else RerollMagic(rollItem);
                    }
                    break;
                case ItemRarity.Rare: // rare, without the matching mods, scour
                    ConvertToNormal(rollItem);
                    break;
                default:
                    return Pause(string.Format("Unknown rarity value: {0}", mods.ItemRarity));
            }
            return true;
        }

        /*
		private Stopwatch rollTimer = new Stopwatch();
		private void SetRollTargetItem(string targetItemPath, params string[] modNames)
		{
			rollItemPath = targetItemPath;
			rollMods = modNames;
		}
		private string rollItemPath = null;
		private string[] rollMods = new string[] { "Blessed Rebirth", "Life from Death" };
		private void DoOneItemRollStepWithAlchemy()
		{
			if (rollItemPath == null) return;
			if (rollTimer.IsRunning && rollTimer.ElapsedMilliseconds < 500) return;
			if (rollMods.Length == 0 || rollMods.Length > 2)
			{
				Log("Cannot roll zero or more than two target mods this way.");
				rollItemPath = null;
				return;
			}
			var rollItem = Inventory.FindFirstInventoryItemWithoutMods(rollItemPath, rollMods);
			if (rollItem == null)
			{
				Log(string.Format("Cannot find any {0} item to roll.", rollItemPath));
				rollItemPath = null;
				return;
			}
			if (!rollItem.Item.HasComponent<Mods>())
			{
				Log("Cannot use Roller on item with no mods.");
				rollItemPath = null;
				return;
			}
			var panel = GameController.Game.IngameState.IngameUi.InventoryPanel;
			if (!panel.IsVisible)
			{
				Log("Cannot roll when inventory is closed.");
				rollItemPath = null;
				return;
			}
			var mods = rollItem.Item.GetComponent<Mods>();
			var modCount = mods.ItemMods.Count;
			var targetModCount = rollMods.Length;
			var matchingModCount = CountMatchingMods(mods.ItemMods, rollMods);
			if (matchingModCount == targetModCount)
			{
				Log("Item has all target mods.");
				rollItemPath = null;
				return;
			}
			rollTimer.Restart();
			Log(string.Format("DoOneItemRollStep {0} targetCount={1} matchingCount={2}", mods.ItemRarity, targetModCount, matchingModCount));
			// if the target item:
			switch (mods.ItemRarity)
			{
				case ItemRarity.Normal:
					if (!UseItemOnItem("Metadata/Items/Currency/CurrencyUpgradeToRare", rollItem))
					{
						rollItemPath = null;
					}
					break;
				case ItemRarity.Magic:
					if (!UseItemOnItem("Metadata/Items/Currency/CurrencyUpgradeMagicToRare", rollItem))
					{
						rollItemPath = null;
					}
					break;
				case ItemRarity.Rare: // rare, without the matching mods, scour
					if (!UseItemOnItem("Metadata/Items/Currency/CurrencyConvertToNormal", rollItem))
					{
						rollItemPath = null;
					}
					break;
				default:
					Log(string.Format("Unknown Rarity value: {0}", mods.ItemRarity));
					rollItemPath = null;
					break;
			}
		}
		private void DoOneItemRollStepWithAlterations()
		{
			if (rollItemPath == null) return;
			if (rollTimer.IsRunning && rollTimer.ElapsedMilliseconds < 500) return;
			if (rollMods.Length == 0 || rollMods.Length > 2)
			{
				Log("Cannot roll zero or more than two target mods this way.");
				rollItemPath = null;
				return;
			}
			var rollItem = FindFirstInventoryItemWithoutMods(rollItemPath, rollMods);
			if (rollItem == null)
			{
				Log(string.Format("Cannot find any {0} item to roll.", rollItemPath));
				rollItemPath = null;
				return;
			}
			if (!rollItem.Item.HasComponent<Mods>())
			{
				Log("Cannot use Roller on item with no mods.");
				rollItemPath = null;
				return;
			}
			var panel = GameController.Game.IngameState.IngameUi.InventoryPanel;
			if (!panel.IsVisible)
			{
				Log("Cannot roll when inventory is closed.");
				rollItemPath = null;
				return;
			}
			var mods = rollItem.Item.GetComponent<Mods>();
			var modCount = mods.ItemMods.Count;
			var targetModCount = rollMods.Length;
			var matchingModCount = CountMatchingMods(mods.ItemMods, rollMods);
			if (matchingModCount == targetModCount)
			{
				Log("Item has all target mods.");
				rollItemPath = null;
				return;
			}
			rollTimer.Restart();
			Log(string.Format("DoOneItemRollStep {0} targetCount={1} matchingCount={2}", mods.ItemRarity, targetModCount, matchingModCount));
			// if the target item:
			switch (mods.ItemRarity)
			{
				case ItemRarity.Normal:
					if (!UseItemOnItem("Metadata/Items/Currency/CurrencyUpgradeToMagic", rollItem))
					{
						rollItemPath = null;
					}
					break;
				case ItemRarity.Magic:
					if (modCount == 1)
					{
						if (!UseItemOnItem("Metadata/Items/Currency/CurrencyAddModToMagic", rollItem))
						{
							rollItemPath = null;
						}
					}
					else if (matchingModCount == 1)
					{
						if (!UseItemOnItem("Metadata/Items/Currency/CurrencyUpgradeMagicToRare", rollItem))
						{
							rollItemPath = null;
						}
					}
					else if (!UseItemOnItem("Metadata/Items/Currency/CurrencyRerollMagic", rollItem))
					{
						rollItemPath = null;
					}
					break;
				case ItemRarity.Rare: // rare, without the matching mods, scour
					if (!UseItemOnItem("Metadata/Items/Currency/CurrencyConvertToNormal", rollItem))
					{
						rollItemPath = null;
					}
					break;
				default:
					Log(string.Format("Unknown Rarity value: {0}", mods.ItemRarity));
					rollItemPath = null;
					break;
			}
		}
		private void DoOneMapRollStep()
		{
			if (rollItemPath == null) return;
			if (rollTimer.IsRunning && rollTimer.ElapsedMilliseconds < 500) return;
			if (rollMods.Length == 0 || rollMods.Length > 2)
			{
				Log("Cannot roll zero or more than two target mods this way.");
				rollItemPath = null;
				return;
			}
			var rollItem = FindFirstInventoryItemWithoutMods(rollItemPath, rollMods);
			if (rollItem == null)
			{
				Log(string.Format("Cannot find any {0} item to roll.", rollItemPath));
				rollItemPath = null;
				return;
			}
			if (!rollItem.Item.HasComponent<Mods>())
			{
				Log("Cannot use Roller on item with no mods.");
				rollItemPath = null;
				return;
			}
			var panel = GameController.Game.IngameState.IngameUi.InventoryPanel;
			if (!panel.IsVisible)
			{
				Log("Cannot roll when inventory is closed.");
				rollItemPath = null;
				return;
			}
			rollTimer.Restart();
			var mods = rollItem.Item.GetComponent<Mods>();
			var modCount = mods.ItemMods.Count;
			var targetModCount = rollMods.Length;
			var matchingModCount = CountMatchingMods(mods.ItemMods, rollMods);
			if (matchingModCount == targetModCount)
			{
				Log("Item has all target mods.");
				rollItemPath = null;
				return;
			}
			Log(string.Format("DoOneRollerStep {0} targetCount={1} matchingCount={2}", mods.ItemRarity, targetModCount, matchingModCount));
			// if the target item:
			switch (mods.ItemRarity)
			{
				case ItemRarity.Normal:
					var quality = rollItem.Item.GetComponent<Quality>();
					if (quality != null && quality.ItemQuality < 20)
					{
						if (!UseItemOnItem("Metadata/Items/Currency/CurrencyMapQuality", rollItem))
						{
							rollItemPath = null;
						}
					}
					else if (!UseItemOnItem("Metadata/Items/Currency/CurrencyUpgradeToRare", rollItem))
					{
						rollItemPath = null;
					}
					break;
				case ItemRarity.Magic:
					if (modCount == 1 && targetModCount == 1)
					{
						if (!UseItemOnItem("Metadata/Items/Currency/CurrencyAddModToMagic", rollItem))
						{
							rollItemPath = null;
						}
					}
					else
					{
						if (!UseItemOnItem("Metadata/Items/Currency/CurrencyRerollMagic", rollItem))
						{
							rollItemPath = null;
						}
					}
					break;
				case ItemRarity.Rare:
					if (!UseItemOnItem("Metadata/Items/Currency/CurrencyConvertToNormal", rollItem))
					{
						rollItemPath = null;
					}
					break;
				default:
					Log(string.Format("Unknown Rarity value: {0}", mods.ItemRarity));
					rollItemPath = null;
					break;
			}
		}
		*/

    }
}

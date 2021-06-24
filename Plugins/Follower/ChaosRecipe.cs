using ExileCore;
using SharpDX;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Enums;
using System.Collections.Generic;
using ExileCore.PoEMemory.Elements;
using static Follower.Globals;

namespace Follower
{
	partial class Follower
	{
		class ChaosRecipe
		{
			// the recipe:
			NormalInventoryItem mainHand; // Weapons/OneHandWeapons or Weapons/TwoHandWeapons
			NormalInventoryItem offHand; // Weapons/OneHandWeapons or Armours/Shields or null
			NormalInventoryItem helmet; // Metadata/Items/Armours/Helmets
			NormalInventoryItem boots; // Metadata/Items/Armours/Boots
			NormalInventoryItem gloves; // Metadata/Items/Armours/Gloves
			NormalInventoryItem body; // Metadata/Items/Armours/BodyArmours
			NormalInventoryItem ring1; // Metadata/Items/Rings x2
			NormalInventoryItem ring2; // Metadata/Items/Rings x2
			NormalInventoryItem amulet; // Metadata/Items/Amulets
			NormalInventoryItem belt; // Metadata/Items/Belts
			public string IsReady()
			{
				string ret = "Needs ";
				if (!IsValid(mainHand)) ret += "Main Hand, ";
				if (IsOneHanded(mainHand) && !IsValid(offHand)) ret += "Off Hand,";
				if (!IsValid(helmet)) ret += "Helmet, ";
				if (!IsValid(boots)) ret += "Boots, ";
				if (!IsValid(gloves)) ret += "Gloves, ";
				if (!IsValid(body)) ret += "Body Armour, ";
				if (!IsValid(ring1)) ret += "Ring, ";
				if (!IsValid(ring2)) ret += "Ring2, ";
				if (!IsValid(amulet)) ret += "Amulet, ";
				if (!IsValid(belt)) ret += "Belt";
				if (ret.Equals("Needs ")) ret = "Ready";
				return ret;
			}
			public void Render(Graphics G)
			{
				if( IsValid(mainHand) && HasParent(mainHand) ) G.DrawFrame(mainHand.GetClientRect(), Color.White, 2);
				if( IsValid(offHand) && HasParent(offHand) ) G.DrawFrame(offHand.GetClientRect(), Color.White, 2);
				if( IsValid(helmet) && HasParent(helmet) ) G.DrawFrame(helmet.GetClientRect(), Color.White, 2);
				if( IsValid(boots) && HasParent(boots) ) G.DrawFrame(boots.GetClientRect(), Color.White, 2);
				if( IsValid(gloves) && HasParent(gloves) ) G.DrawFrame(gloves.GetClientRect(), Color.White, 2);
				if( IsValid(body) && HasParent(body) ) G.DrawFrame(body.GetClientRect(), Color.White, 2);
				if( IsValid(ring1) && HasParent(ring1) ) G.DrawFrame(ring1.GetClientRect(), Color.White, 2);
				if( IsValid(ring2) && HasParent(ring2) ) G.DrawFrame(ring2.GetClientRect(), Color.White, 2);
				if( IsValid(amulet) && HasParent(amulet) ) G.DrawFrame(amulet.GetClientRect(), Color.White, 2);
				if( IsValid(belt) && HasParent(belt) ) G.DrawFrame(belt.GetClientRect(), Color.White, 2);
				var pos = Vector2.Zero;
				G.DrawText(string.Format("Chaos Recipe: {0}", IsReady()), Vector2.Zero); pos.Y += 12f;

			}
			private void Log(params string[] strings)
			{
				strings[0] = "ChaosRecipe:" + strings[0];
				Globals.Log(strings);
			}
			public ChaosRecipe Reset()
			{
				Log("Reset()");
				mainHand = null;
				offHand = null;
				helmet = null;
				boots = null;
				gloves = null;
				body = null;
				ring1 = null;
				ring2 = null;
				amulet = null;
				belt = null;
				return this;
			}
			public void Add(StashElement stash)
			{
				if (stash == null)
				{
					Log("Add(StashElement null) ignored.");
					return;
				}
				Add(stash.VisibleStash?.VisibleInventoryItems);
			}
			public void Add(IEnumerable<NormalInventoryItem> items)
			{
				if (items == null)
				{
					Log("Add(Enumerable null) ignored.");
					return;
				}
				foreach (var item in items) Add(item);
			}
			public void Add(NormalInventoryItem item)
			{
				if (!IsValid(item))
				{
					Log("Add(invalid item) ignored.");
					return;
				}
				var path = item.Item.Path.Split('/');
				var last = path[path.Length - 1];
				var mods = item.Item.GetComponent<Mods>();
				if (mods == null) {
					// Log(string.Format("Add({0}) ignored (no mods).", last));
					return;
				}
				if (mods.Identified) {
					// Log(string.Format("Add({0}) ignored (identified).", last));
					return;
				}
				if( mods.ItemRarity != ItemRarity.Rare)
				{
					// Log(string.Format("Add({0}) ignored ({1}).", last, mods.ItemRarity));
					return;
				}
				if (mods.ItemLevel < 60) { return; }
				if( mods.ItemLevel > 74) { return; }
				switch (path[2])
				{
					case "Weapons":
						// Log(string.Format("Add({0}) finding {1}/{2} slot in the recipe:", last, path[2], path[3]));
						switch (path[3])
						{
							case "OneHandWeapons":
								if( !IsValid(mainHand) ) mainHand = item;
								else if (IsOneHanded(mainHand) && !IsValid(offHand) ) offHand = item;
								break;
							case "TwoHandWeapons":
								if (mainHand == null)
								{
									mainHand = item;
									offHand = null;
								}
								break;
						}
						break;
					case "Armours":
						switch (path[3])
						{
							case "BodyArmours": body = IsValid(body) ? body : item; break;
							case "Boots": boots = IsValid(boots) ? boots : item; break;
							case "Gloves": gloves = IsValid(gloves) ? gloves : item; break;
							case "Helmets":
								helmet = IsValid(helmet) ? helmet : item;
								break;
							case "Shields":
								if (mainHand == null || IsOneHanded(mainHand))
								{
									offHand = IsValid(offHand) ? offHand : item;
								}
								break;
						}
						break;
					case "Rings":
						if (!IsValid(ring1)) ring1 = item;
						else if (!IsValid(ring2)) ring2 = item;
						break;
					case "Amulets": amulet = IsValid(amulet) ? amulet : item; break;
					case "Belts": belt = IsValid(belt) ? belt : item; break;
				}
			}
			private static bool IsOneHanded(NormalInventoryItem item) => IsValid(item) && item.Item.Path.Contains("Weapons/OneHandWeapons/");
		}

	}
}

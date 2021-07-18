using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using Input = ExileCore.Input;
using SharpDX;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.Diagnostics;
using WindowsInput;
using WindowsInput.Native;
using ExileCore.PoEMemory.Elements.InventoryElements;
using static Follower.Globals;

namespace Follower
{
    partial class Follower : BaseSettingsPlugin<FollowerSettings>
    {

        public Follower()
        {
            Name = "Follower";
        }
        public bool Paused { get; set; } = true;
        public override bool Initialise()
        {
            Globals.Initialise(GameController, Graphics);
            Inventory.Initialise(GameController, Graphics);
            Roller.Initialise(GameController, Graphics);
            InputManager.Initialise(GameController, Graphics);
            FlaskManager.Initialise(GameController, Settings);
            SkillManager.Initialise(GameController, Graphics, Settings);

            BuffManager.Initialise(GameController, Graphics, Settings);
            BuffManager.MaintainVaalBuff(Settings.UseVaalGrace, "vaal_grace", "vaal_aura_dodge", VirtualKeyCode.VK_T);
            // BuffManager.MaintainVaalBuff(Settings.UseVaalImpurityOfIce, "cold_impurity", "cold_impurity_buff", VirtualKeyCode.VK_Q);

            GameController.EntityListWrapper.EntityAdded += OnEntityAdded;
            GameController.EntityListWrapper.EntityRemoved += OnEntityRemoved;
            GameController.EntityListWrapper.EntityRemoved += Labels_OnEntityRemoved;
            PersistedText.Add(GetQuestNotes, (c) => ScreenRelativeToWindow(.33f, .96f), 0);

            InputManager.OnRelease(VirtualKeyCode.ESCAPE, () => Paused = true);
            InputManager.OnRelease(VirtualKeyCode.PAUSE, () => Paused = !Paused);
            InputManager.OnRelease(VirtualKeyCode.F11, IdentifyOne);
            InputManager.OnRelease(VirtualKeyCode.F7, () =>
            {
                InventoryReport();
                Roller.MaxSteps = 50;
                // Roller.SetTarget("Metadata/Items/Jewels/JewelPassiveTreeExpansionSmall", 1, "AfflictionNotableFettle", "AfflictionNotableToweringThreat");
                // Roller.SetTarget("Metadata/Items/Jewels/JewelPassiveTreeExpansionSmall", 1, "AfflictionNotableEnduringComposure");
                Roller.SetTarget("Metadata/Items/Jewels/JewelPassiveTreeExpansionMedium", 1, "AfflictionNotableCookedAlive", "AfflictionNotableMasterOfFire");
                Roller.Resume();
            });
            InputManager.OnRelease(VirtualKeyCode.F6, Test);
            InputManager.OnRelease(VirtualKeyCode.F4, ToggleConsole);

            InputManager.OnRelease(VirtualKeyCode.F2, () =>
            {
                if (showRecipe = !showRecipe) currentRecipe.Reset().Add(GameController.Game.IngameState.IngameUi.StashElement);
            });
            GameController.Area.OnAreaChange += Area_OnAreaChange;
            timeInZone.Start();

            Log("Assistant initialised.");
            return true;
        }

        private Stopwatch timeInZone = new Stopwatch();
        private void Area_OnAreaChange(AreaInstance obj)
        {
            Log(string.Format("Leaving Zone after {0}", timeInZone.Elapsed.ToString(@"mm\:ss")));
            timeInZone.Restart();
            remainingLoot.Clear();
        }

        private void Test()
        {
            // MaintainBuffWithFlask("FlaskUtility10", 4500, "flask_utility_stone");
            var player = GameController.Player;
            if (player == null)
            {
                Log("No Player");
                return;
            }
            do
            {
                var panel = GameController.IngameState.IngameUi.InventoryPanel;
                if (panel == null)
                {
                    Log("No panel.");
                    break;
                }
                if (!panel.IsVisible)
                {
                    Log("Not visible.");
                    break;
                }
                var playerInventory = panel[InventoryIndex.PlayerInventory];
                if (playerInventory == null)
                {
                    Log("No player inventory.");
                    break;
                }
                foreach (var item in playerInventory.VisibleInventoryItems)
                {
                    if (!IsValid(item.Item)) continue;
                    Log(string.Format("Item: {0}", item.Item.Path));
                    var mods = item.Item.GetComponent<Mods>();
                    if (mods == null || mods.ItemMods == null) continue;
                    foreach (var mod in mods.ItemMods)
                    {
                        Log($" - Mod: {mod.Name} {mod.Group} {mod.Value1}");
                    }
                    Charges charges = item.Item.GetComponent<Charges>();
                    if( charges != null )
                    {
                        Log($" - Charges: {charges.NumCharges}/{charges.ChargesMax}");
                    }
                    Quality quality = item.Item.GetComponent<Quality>();
                    if( quality != null )
                    {
                        Log($" - Quality: {quality.ItemQuality}");
                    }
                    if( FlaskManager.IsFlask(item.Item))
                    {
                        Log($" - Flask: {FlaskManager.ParseFlask(item.Item).ToString()}");
                    }
                }
            } while (false);

            var stats = GameController.Player.GetComponent<Stats>();
            if (stats == null)
            {
                Log("No Stats");
                return;
            }
            var dict = stats.ParseStats();
            var keys = dict.Keys.ToArray();
            foreach (GameStat key in keys) if (key.ToString().Contains("serv")) Log(string.Format("Key: {0} {1}", key, dict[key]));

            var actor = player.GetComponent<Actor>();
            // foreach (var skill in actor.ActorSkills) Log($"Skill: {skill.Name} IsOnSkillBar:{skill.IsOnSkillBar} IsOnCooldown:{skill.IsOnCooldown} IsUsing:{skill.IsUsing} CanBeUsed:{skill.CanBeUsed}");
            foreach (var skill in actor.ActorVaalSkills) Log(string.Format("Vaal Skill: {0} Souls:{1}/{2}", skill.VaalSkillInternalName, skill.CurrVaalSouls, skill.VaalMaxSouls));

            Log(string.Format("Deployed Objects: {0}", actor.DeployedObjectsCount));
            /*
            foreach (var obj in actor.DeployedObjects) { Log(string.Format("Deployed Object: Id:{0} SkillKey:{1}", obj.ObjectId, obj.SkillKey)); }
            var ui = GameController.IngameState.IngameUi;
            foreach (var label in ui.ItemsOnGroundLabelElement.LabelsOnGround)
            {
                Log(string.Format("Label: {0} Id:{3} Visible:{1} CanPickUp:{2}", label.Label?.Text ?? "null", label.IsVisible, label.CanPickUp, label.ItemOnGround.Id));
            }
            InputManager.Add(
                new KeyDown(VirtualKeyCode.VK_1,
                new Delay(20,
                new KeyUp(VirtualKeyCode.VK_1,
                new Delay(20,
                new KeyDown(VirtualKeyCode.VK_2,
                new Delay(20,
                new KeyUp(VirtualKeyCode.VK_2))))))));
            */

        }

        private static bool HasParent(NormalInventoryItem item) => (item?.Parent?.Address ?? 0) != 0;
        private ChaosRecipe currentRecipe = new ChaosRecipe();
        private bool showRecipe = false;

        private Vector2 DrawLineAt(string text, Vector2 pos) => DrawLineAt(text, pos, Color.White);
        private Vector2 DrawLineAt(string text, Vector2 pos, Color color)
        {
            Graphics.DrawText(text, pos, color);
            pos.Y += 12f;
            return pos;
        }

        private void IdentifyOne()
        {
            var panel = GameController.Game.IngameState.IngameUi.InventoryPanel;
            if (panel == null) return;
            if (!(panel.IsValid && panel.IsVisible)) return;
            var playerInventory = panel[InventoryIndex.PlayerInventory];
            if (playerInventory == null) return;
            var items = playerInventory.VisibleInventoryItems;
            if (items == null) return;
            foreach (var item in items)
            {
                if (item == null) continue;
                var ent = item.Item;
                if (ent == null) continue;
                if (!ent.HasComponent<Mods>()) continue;
                var mods = ent.GetComponent<Mods>();
                if (mods.Identified) continue;
                Inventory.UseItemOnItem("Metadata/Items/Currency/CurrencyIdentification", item);
                break;
            }
        }

        private void InventoryReport() => InventoryReport(null);
        private void InventoryReport(InventoryElement panel)
        {
            // var pos = ScreenRelativeToWindow(.2f, .5f);
            if (panel == null) panel = GameController.Game.IngameState.IngameUi.InventoryPanel;
            if (panel == null) return;
            var playerInventory = panel[InventoryIndex.PlayerInventory];
            if (playerInventory == null) return;
            var items = playerInventory.VisibleInventoryItems;
            if (items == null) return;
            var query = new ItemQuery();
            // query.MatchCount(1, "AfflictionNotableFettle", "AfflictionNotableToweringThreat", "AfflictionNotableViciousGuard");
            query.MatchCount(1, "AfflictionNotableFettle", "AfflictionNotableViciousGuard");
            // query.MatchAll("AfflictionNotable*");
            foreach (var item in items)
            {
                // pos = DrawLineAt(string.Format("{0}", item.Item.Path), pos);
                // Log(string.Format("{0}", item.Item.Path));
                var ent = item.Item;
                if (ent == null) continue;
                var mods = ent.GetComponent<Mods>();
                if (mods == null) continue;
                Log(string.Format("{0} {1} {2} iLvl:{3}", item.Item.Path, mods.Identified ? "Identified" : "Unidentified", mods.ItemRarity, mods.ItemLevel));
                if (mods.ItemMods == null) continue;
                foreach (var mod in mods.ItemMods)
                {
                    Log(string.Format("- {0} '{1}' {2} {3}", mod.Name, mod.DisplayName, mod.Level, String.Join(", ", mod.Values)));

                }
                Log(string.Format("Matches: {0}", query.Matches(item)));

            }
        }



        private string GetQuestNotes()
        {
            var area = GameController.Area.CurrentArea;
            string ret = $"{area.Name} - "; // string.Format("{0}{1} - ", (Paused ? "[Paused] " : ""), area.Name);
            if (area.Act == 1)
            {
                switch (area.Name)
                {
                    case "The Twilight Strand": ret += "Don't forget to reset your loot filter."; break;
                    case "The Coast": ret += "Get the waypoint. Proceed to Mud Flats."; break;
                    case "The Mud Flats": ret += "Get three nests. Proceed to Submerged Passage."; break;
                    case "The Tidal Island": ret += "Get Level 4, kill Hailrake, logout."; break;
                    case "The Submerged Passage": ret += "(after Hailrake) Portal at Depths, exit to Ledge."; break;
                    case "The Flooded Depths": ret += "Kill Dweller and log, return to Lower Prison"; break;
                    case "The Ledge": ret += "Race to The Climb. Kill nothing."; break;
                    case "The Climb": ret += "Get waypoint. Fawn unlocks Navali. Race to Lower Prison."; break;
                    case "The Lower Prison": ret += "Return to Depths, then do the Trial here."; break;
                    case "The Upper Prison": ret += "Kill Brutus, Proceed to Prisoner's Gate."; break;
                    case "Prisoner's Gate": ret += "Gem reward in town. Proceed to Ship Graveyard."; break;
                    case "The Ship Graveyard": ret += "Get waypoint! Find Allflame, find Cavern, kill Fairgraves, logout."; break;
                    case "The Cavern of Wrath": ret += "Exit to Cavern of Anger"; break;
                    case "The Cavern of Anger": ret += "Kill Merveil, proceed to Southern Forest."; break;
                }
            }
            else if (area.Act == 2)
            {
                switch (area.Name)
                {
                    case "The Southern Forest": ret += "Race NW to Forest Encampment. Kill nothing."; break;
                    case "The Riverways": ret += "Get the waypoint on the road, Oak is NW, Alira is SW."; break;
                    case "The Western Forest": ret += "Alira (same side as waypoint), Blackguards, Weaver, log out."; break;
                    case "The Old Fields": ret += "Place Portal at The Den, proceed to Crossroads."; break;
                    case "The Crossroads": ret += "Bridge [for Kraityn], Ruins [for Trial], then Chamber of Sins."; break;
                    case "The Fellshrine Ruins": ret += "Follow the road to The Crypt Level 1."; break;
                    case "The Crypt Level 1": ret += "Finish the Trial. Do not enter Level 2."; break;
                    case "The Chamber of Sins Level 1": ret += "Get waypoint. Enter Level 2."; break;
                    case "The Chamber of Sins Level 2": ret += "Finish the Trial. Kill Fidelitas (NW), take Gem, log out."; break;
                    case "The Wetlands": ret += "Oak in the center, waypoint and exit behind him."; break;
                    case "The Vaal Ruins": ret += "Find and break the Seal. Exit to Northern Forest."; break;
                    case "The Northern Forest": ret += "Get waypoint, go NW to the Caverns. Do not enter Dread Thicket."; break;
                }

            }
            else
            {
                ret += "No Notes.";
            }
            return ret + " (" + timeInZone.Elapsed.ToString(@"mm\:ss") + ")";
        }

        private class Sighting
        {
            public Entity Ent;
            public long FirstDamageTime;
            public long AddedMS;
            public long AddedHP;
        }
        private Stopwatch seenTimer = new Stopwatch();
        private Dictionary<uint, Sighting> seenMonsters = new Dictionary<uint, Sighting>();
        // private Dictionary<uint, Entity> seenDoors = new Dictionary<uint, Entity>();
        // private Dictionary<uint, Entity> seenPortals = new Dictionary<uint, Entity>();
        private void OnEntityRemoved(Entity ent)
        {
            if (ent == null) return;
            if (ent.Path == null) return;
            seenMonsters.Remove(ent.Id);
            // seenDoors.Remove(ent.Id);
            // seenPortals.Remove(ent.Id);
        }
        public void OnEntityDeath(Entity ent)
        {
            Sighting seen = seenMonsters[ent.Id];
            long damage = seen.AddedHP;
            long ms = 1 + (seenTimer.ElapsedMilliseconds - seen.FirstDamageTime);
            if (ms > 100 && damage > 1)
            {
                float dps = damage * 1000f / ms;
                string path = ent.Path.Split('/').Last();
                PersistedText.Add(string.Format("{0}", formatNumber(dps)), ent.Pos, 3000);
                Log(string.Format("{0} Took {1} damage in {2} ms DPS: {3:F2}", path, seen.AddedHP, ms, dps));
            }
            seenMonsters.Remove(ent.Id);
        }
        public string formatNumber(float num)
        {
            string suffix = "";
            if (num > 1024)
            {
                suffix = "K";
                num /= 1024;
                if (num > 1024)
                {
                    suffix = "M";
                    num /= 1024;
                }
            }
            return num.ToString("N2") + suffix;
        }
        public void CheckDeaths()
        {
            Sighting[] seen = seenMonsters.Values.ToArray();
            foreach (Sighting s in seen)
            {
                Life life = s.Ent.GetComponent<Life>();
                if (life == null) continue;
                if (s.FirstDamageTime == 0 && life.CurHP < s.AddedHP)
                {
                    s.FirstDamageTime = seenTimer.ElapsedMilliseconds;
                }
                if (life.CurHP < 1)
                {
                    OnEntityDeath(s.Ent);
                }
            }
            // DrawTextAtPlayer(string.Format("Tracking {0} monsters.", seenMonsters.Count));
        }
        private void OnEntityAdded(Entity ent)
        {
            if (!seenTimer.IsRunning) seenTimer.Start();
            if (ent == null) return;
            if (ent.Path == null) return;
            if (ent.Path.StartsWith("Metadata/Monsters"))
            {
                if (ent.Rarity >= MonsterRarity.Rare)
                {
                    Life life = ent.GetComponent<Life>();
                    if (life != null)
                    {
                        seenMonsters[ent.Id] = new Sighting() { AddedHP = life.CurHP, AddedMS = seenTimer.ElapsedMilliseconds, Ent = ent };
                    }
                }
            }
            else if (ent.Path.StartsWith("Metadata/Effect"))
            {
                // Log(ent.Path + ": " + ent.RenderName);
            }
            /*
			if (ent.Path.Contains("Door")) {
				seenDoors[ent.Id] = ent;
				Log(string.Format("DOOR: {0}", ent.Path));
			}
			if (ent.Path.Contains("ortal")) {
				seenPortals[ent.Id] = ent;
				Log(string.Format("PORTAL: {0}", ent.Path));
			}
			*/
            // Interesting paths:
            // MultiplexPortal (map portals in the base)
            // 
        }


        public Entity ClosestPlayer()
        {
            var player = GameController.Player;
            if (player == null) return null;
            Entity closest = null;
            float closest_distance = float.MaxValue;
            foreach (var ent in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Player])
            {
                if (ent == null || ent.Id == player.Id) continue;
                float dist = ent.DistancePlayer;
                if (dist < closest_distance)
                {
                    closest_distance = dist;
                    closest = ent;
                }
            }
            return closest;
        }

        public Entity Leader = null;

        public class MovingAverage
        {
            public float Value = 0f;
            public int Period;
            public MovingAverage(int period) => Period = period;
            public void Add(float sample) => Value = ((Value * (Period - 1)) + sample) / Period;
        }
        private MovingAverage xpPerMS = new MovingAverage(1000);
        private long xpLastFrame = -1;
        private long[] xpToNextLevel = new long[]
        {
            0, 525, 1760, 3781, 7184, 12186, 19324, 29377, 43181, 61693, 85990, 117506, 157384, 207736, 269997, 346462, 439268, 551295, 685171, 843709, 1030734, 1249629, 1504995, 1800847, 2142652, 2535122, 2984677, 3496798, 4080655, 4742836, 5490247, 6334393, 7283446, 8384398, 9541110, 10874351, 12361842, 14018289, 15859432, 17905634, 20171471, 22679999, 25456123, 28517857, 31897771, 35621447, 39721017, 44225461, 49176560, 54607467, 60565335, 67094245, 74247659, 82075627, 90631041, 99984974, 110197515, 121340161, 133497202, 146749362, 161191120, 176922628, 194049893, 212684946, 232956711, 255001620, 278952403, 304972236, 333233648, 363906163, 397194041, 433312945, 472476370, 514937180, 560961898, 610815862, 664824416, 723298169, 786612664, 855129128, 929261318, 1009443795, 1096169525, 1189918242, 1291270350, 1400795257, 1519130326, 1646943474, 1784977296, 1934009687, 2094900291, 2268549086, 2455921256, 2658074992, 2876116901, 3111280300, 3364828162, 3638186694, 3932818530, 4250334444
        };
        private void UpdateXP(long dt)
        {
            var player = GameController.Player;
            if (player == null) return;
            var p = player.GetComponent<Player>();
            if (p == null) return;
            int safeZone = (int)(3 + (p.Level / 16));
            var area = GameController.Area;
            string warning = "";
            if (p.Level - area.CurrentArea.RealLevel > safeZone)
            {
                warning = "(area too low) ";
            }
            else if (area.CurrentArea.RealLevel - p.Level > safeZone)
            {
                warning = "(area too high) ";
            }

            /*
			var levelDiff = Math.Max(Math.Abs(p.Level - area.CurrentArea.RealLevel), 0);
			if (levelDiff > safeZone && !(area.CurrentArea.IsHideout || area.CurrentArea.IsTown))
			{
				xpMultiplier = Math.Max(0.01f, (float)Math.Pow((p.Level + 5) / (p.Level + 5 + Math.Pow(levelDiff, 2.5)), 1.5));
				DrawTextAtPlayer(string.Format("Xp Multi: {0:F2}", xpMultiplier));
			} else
			{
				xpMultiplier = 1.0f;
			}
			*/

            if (xpLastFrame == -1) xpLastFrame = p.XP;
            else
            {
                float xpRate = (p.XP - xpLastFrame) / dt;
                xpLastFrame = p.XP;
                xpPerMS.Add(xpRate);
            }
            long xpToLevel = xpToNextLevel[p.Level] - p.XP;
            if (xpPerMS.Value > 0f)
            {
                var msToLevel = xpToLevel / xpPerMS.Value;
                var hrToLevel = msToLevel / (1000 * 60 * 60);
                // DrawTextAtPlayer(string.Format("Next: {0} TTL: {1:F2} hrs", xpToLevel, hrToLevel));
                Graphics.DrawText(string.Format("{0}{1:F2} hrs", warning, hrToLevel), ScreenRelativeToWindow(.7f, .98f), FontAlign.Right);
            }
        }

        private Dictionary<uint, SeenLoot> remainingLoot = new Dictionary<uint, SeenLoot>();
        private class SeenLoot
        {
            public string Label;
            public Entity Ent;
            public ItemRarity Rarity;
            public Vector3 LastKnownPos;
            public bool IsInMemory() => IsValid(Ent);
        }
        private void Labels_OnEntityRemoved(Entity ent)
        {
            if (remainingLoot.TryGetValue(ent.Id, out SeenLoot loot))
            {
                // if it was removed while we are right next to it, remove entirely
                // TODO: while any player is next to it
                if (ent.DistancePlayer < 20f)
                {
                    Log(string.Format("You picked up [{0}] {1}.", ent.Id, loot.Label));
                    remainingLoot.Remove(ent.Id);
                }
                else // if it was removed while far away just mark it as unloaded
                {
                    Log(string.Format("Marking loot as missed: {0}", loot.Label));
                    loot.Ent = null;
                }
            }
        }

        private void UpdateLabels()
        {
            if (InputManager.IsPressed(VirtualKeyCode.LMENU)) return;
            // pull all labels on the ground into the remainingLoot structure
            var ui = GameController.IngameState.IngameUi;
            // DrawTextAtPlayer(string.Format("Checking {0} labels", ui.ItemsOnGroundLabelElement.LabelsOnGround.Count));
            var labels = ui.ItemsOnGroundLabelElement;
            if (labels == null || labels.LabelsOnGround == null ) return;
            foreach (var label in labels.LabelsOnGround)
            {
                if (!IsValid(label)) continue;
                if (label.IsVisible && label.CanPickUp)
                {
                    Entity ent = label.ItemOnGround;
                    if (!IsValid(ent)) continue;
                    string path = label.ItemOnGround.Path;
                    if (!path.StartsWith("Metadata/Misc")) continue;
                    uint id = label.ItemOnGround.Id;
                    string text = label.Label.Text;
                    SeenLoot loot;
                    if (!remainingLoot.TryGetValue(id, out loot))
                    {
                        ItemRarity rarity = GetItemRarity(label.ItemOnGround);
                        Log(string.Format("New label: {0} {1} {2}", text, label.ItemOnGround.Path, label.ItemOnGround.Rarity));
                        remainingLoot[id] = loot = new SeenLoot() { Label = text, Rarity = rarity };
                    }
                    loot.Ent = ent;
                    loot.LastKnownPos = label.ItemOnGround.Pos;
                }
            }
        }

        private class CountedItemEntry
        {
            public int Count;
            public ItemRarity Rarity;
            public string Text;
        }

        private void RenderLabels()
        {
            Vector2 labelPos = ScreenRelativeToClient(new Vector2(.85f, .66f));
            Dictionary<string, CountedItemEntry> counts = new Dictionary<string, CountedItemEntry>();
            foreach (var ent in remainingLoot.Values)
            {
                if (ent.Ent != null) continue;
                if (!counts.TryGetValue(ent.Label, out CountedItemEntry entry))
                {
                    entry = new CountedItemEntry() { Count = 1, Rarity = ent.Rarity, Text = ent.Label };
                    counts.Add(ent.Label, entry);
                }
                else
                {
                    entry.Count += 1;
                }
            }
            if (counts.Count > 0)
            {
                labelPos = DrawLineAt(string.Format("Missed Loot:"), labelPos);
                foreach (string text in counts.Keys)
                {
                    CountedItemEntry entry = counts[text];
                    Color color = GetColor(entry.Rarity);
                    labelPos = DrawLineAt(string.Format("({1}) {0}", text, entry.Count), labelPos, color);
                    if (text.Contains("\n"))
                    {
                        labelPos.Y += 12f;
                    }
                }
            }
        }


        Stopwatch tickTimer = new Stopwatch();
        public override Job Tick()
        {
            if (!IsValid(GameController.Player))
            {
                Paused = true;
                return null;
            }
            try
            {
                long dt = tickTimer.ElapsedMilliseconds | 1;
                tickTimer.Restart();

                Globals.Tick(dt);
                CheckDeaths();

                UpdateLabels();

                UpdateXP(dt);

                FlaskManager.OnTick();
                BuffManager.OnTick();
                InputManager.OnTick();

                Roller.Tick(dt);

            }
            catch (Exception e)
            {
                Log(e.ToString());
            }

            return null;
        }


        private Vector2 ScreenRelativeToClient(Vector2 pos)
        {
            var rect = GameController.Window.GetWindowRectangleTimeCache;
            return new Vector2(rect.X + (pos.X * rect.Width), rect.Y + (pos.Y * rect.Height));
        }
        private Vector2 ScreenClientToRelative(Vector2 pos)
        {
            var rect = GameController.Window.GetWindowRectangleTimeCache;
            return new Vector2((pos.X - rect.X) / rect.Width, (pos.Y - rect.Y) / rect.Height);
        }


        public override void Render()
        {
            try
            {
                var ui = GameController.Game.IngameState.IngameUi;
                var camera = GameController.Game.IngameState.Camera;
                Globals.Render();
                if (Leader != null) { DrawTextAtEnt(Leader, "<<Leader>>"); }
                PersistedText.Render(camera, Graphics);
                // if( GetRelativeCursorPos(out Vector2 cursor) ) DrawTextAtPlayer(string.Format("Cursor: {0:F2} {1:F2}", cursor.X, cursor.Y));
                // DrawTextAtPlayer(string.Format("rollTimer: {0}", rollTimer.ElapsedMilliseconds));
                foreach (Sighting s in seenMonsters.Values)
                {
                    var pos = camera.WorldToScreen(s.Ent.BoundsCenterPos);
                    if (s.Ent.Rarity == MonsterRarity.Unique)
                    {
                        var rect = new RectangleF(pos.X - 40f, pos.Y - 60f, 80f, 120f);
                        Graphics.DrawFrame(rect, Color.Orange, 4);
                    }
                    else
                    {
                        var rect = new RectangleF(pos.X - 30f, pos.Y - 40f, 60f, 80f);
                        Graphics.DrawFrame(rect, Color.Yellow, 1);
                    }
                }

                var stash = GameController.Game.IngameState.IngameUi.StashElement;
                if (showRecipe && stash != null && stash.IsValid && stash.IsVisible && stash.IndexVisibleStash == 0)
                    currentRecipe.Render(Graphics);

                RenderLabels();

                if( Settings.ShowBuffNames.Value )
                {
                    RenderBuffs();
                }

                // DrawTextAtPlayer(string.Format("XP Rate: {0:N}/hr", xpPerMS.Value * (1000 * 60 * 60)));

            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

    }
}

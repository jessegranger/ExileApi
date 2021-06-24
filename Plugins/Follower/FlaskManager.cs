using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WindowsInput.Native;
using static Follower.Globals;

namespace Follower
{
    static class FlaskManager
    {

        private static Dictionary<string, int> baseHealAmount = new Dictionary<string, int>()
        {
            { "FlaskLife1", 70 },
            { "FlaskLife2", 150 },
            { "FlaskLife3", 250 },
            { "FlaskLife4", 360 },
            { "FlaskLife5", 640 },
            { "FlaskLife6", 830 },
            { "FlaskLife7", 1000 },
            { "FlaskLife8", 1200 },
            { "FlaskLife9", 1990 },
            { "FlaskLife10", 1460 },
            { "FlaskLife11", 2400 },
            { "FlaskLife12", 2080 },
        };
        private static Dictionary<string, int> baseManaAmount = new Dictionary<string, int>()
        {
            { "FlaskMana1", 50 },
            { "FlaskMana2", 70 },
            { "FlaskMana3", 90 },
            { "FlaskMana4", 120 },
            { "FlaskMana5", 170 },
            { "FlaskMana6", 250 },
            { "FlaskMana7", 350 },
            { "FlaskMana8", 480 },
            { "FlaskMana9", 700 },
            { "FlaskMana10", 1100 },
            { "FlaskMana11", 1400 },
            { "FlaskMana12", 1800 },
        };

        private class Flask
        {
            public static Stopwatch GlobalTimer = new Stopwatch();
            static Flask() => GlobalTimer.Start();
            public Stopwatch Timer = new Stopwatch();
            public VirtualKeyCode Key;
            public string PathName;
            public int CurrentCharges;
            public int MaxCharges;
            public int ChargesPerUse;
            public int LifeHealAmount;
            public int ManaHealAmount;
            public bool IsInstant;
            public bool IsInstantOnLowLife;
            public bool IsFull => CurrentCharges >= MaxCharges;
            public bool HasEnoughCharge => CurrentCharges >= ChargesPerUse;
            public bool IsUsable(int cooldown) => HasEnoughCharge && GlobalTimer.ElapsedMilliseconds > 200 && ((!Timer.IsRunning) || Timer.ElapsedMilliseconds > cooldown);
            public Flask(VirtualKeyCode key)
            {
                Key = key;
            }
            public bool UseFlask(int cooldown)
            {
                if (IsUsable(cooldown))
                {
                    GlobalTimer.Restart();
                    Timer.Restart();
                    InputManager.PressKey(Key, 30);
                    return true;
                }
                return false;
            }
        }
        private static Flask[] flasks = new Flask[]
        {
            new Flask(VirtualKeyCode.VK_1),
            new Flask(VirtualKeyCode.VK_2),
            new Flask(VirtualKeyCode.VK_3),
            new Flask(VirtualKeyCode.VK_4),
            new Flask(VirtualKeyCode.VK_5),
        };

        private static GameController api;
        private static FollowerSettings Settings;
        private static bool Paused = true;

        public static void Initialise(GameController gameController, FollowerSettings settings)
        {
            api = gameController;
            Settings = settings;
            // F3 starts it and refreshes flask mods at the same time
            InputManager.OnRelease(VirtualKeyCode.F3, () => updateFlaskMods = !(Paused = false));
            // pause just toggles operation
            InputManager.OnRelease(VirtualKeyCode.PAUSE, () => Paused = !Paused);
            PersistedText.Add(GetStatusText, (c) => ScreenRelativeToWindow(.25f, .89f), 0);
        }

        private static string GetStatusText() => $"Flasks [{(Paused ? "Paused" : "Running")}]";

        private static Dictionary<string, Flask> conditionMap = new Dictionary<string, Flask>();
        private static bool TryGetFlaskForCondition(string condition, out Flask flask) => conditionMap.TryGetValue(condition, out flask);
        private static void LinkConditionToFlask(string condition, Flask flask)
        {
            Log($"Using flask for condition: {condition}");
            conditionMap[condition] = flask;
        }
        private static bool updateFlaskMods = true;
        public static void RefreshFlaskMods() => updateFlaskMods = true;
        private static void UpdateFlasks()
        {
            if (!IsValid(api)) return;
            var playerInventories = api.Game.IngameState.ServerData.PlayerInventories;
            if (playerInventories == null) return;
            var flaskInventory = playerInventories.FirstOrDefault(x => x.Inventory != null && x.Inventory.InventType == InventoryTypeE.Flask);
            if (flaskInventory == null) return;
            var serverInventory = flaskInventory.Inventory;
            if (serverInventory == null || serverInventory.InventorySlotItems == null) return;
            foreach (var item in serverInventory.InventorySlotItems)
            {
                if (!IsValid(item)) continue;
                var flaskEnt = item.Item;
                if (!IsValid(flaskEnt)) continue;
                Charges charges = flaskEnt.GetComponent<Charges>();
                if (charges == null) continue;
                Flask flask = flasks[item.PosX];
                flask.CurrentCharges = charges.NumCharges;
                flask.ChargesPerUse = charges.ChargesPerUse;
                flask.MaxCharges = charges.ChargesMax;
                if (updateFlaskMods)
                {
                    int amount = 0;
                    string pathName = flaskEnt.Path.Substring(flaskEnt.Path.LastIndexOf("/") + 1);
                    flask.PathName = pathName;
                    var quality = flaskEnt.GetComponent<Quality>();
                    // if this is a known Life flask
                    if (baseHealAmount.TryGetValue(pathName, out amount))
                    {
                        flask.LifeHealAmount = amount;
                        if (quality != null)
                        {
                            flask.LifeHealAmount = (int)((float)amount * ((100 + quality.ItemQuality) / 100f));
                        }
                    }
                    else flask.LifeHealAmount = 0;

                    // handle Mana flasks
                    if (baseManaAmount.TryGetValue(pathName, out amount))
                    {
                        flask.ManaHealAmount = amount;
                        if (quality != null)
                        {
                            flask.ManaHealAmount = (int)((float)amount * ((100 + quality.ItemQuality) / 100f));
                        }
                    }
                    else flask.ManaHealAmount = 0;

                    Mods flaskMods = flaskEnt.GetComponent<Mods>();
                    foreach (var mod in flaskMods.ItemMods)
                    {
                        Log($"Flask Mod: {mod.Name} {mod.Value1}");
                        switch (mod.Name)
                        {
                            case "FlaskExtraCharges":
                                flask.MaxCharges += mod.Value1;
                                break;
                            case "FlaskInstantRecoveryOnLowLife":
                                flask.IsInstantOnLowLife = true;
                                flask.LifeHealAmount = (int)((float)flask.LifeHealAmount * .75f);
                                break;
                            case "FlaskFullInstantRecovery":
                                flask.IsInstant = true;
                                flask.LifeHealAmount = (int)((float)flask.LifeHealAmount * .34f);
                                break;
                            case "FlaskPartialInstantRecovery":
                                flask.IsInstant = true;
                                flask.LifeHealAmount = (int)((float)flask.LifeHealAmount * .25f);
                                break;
                            case "FlaskRemovesBleeding": LinkConditionToFlask("bleeding", flask); break;
                            case "FlaskRemovesShock": LinkConditionToFlask("shocked", flask); break;
                            case "FlaskCurseImmunity": LinkConditionToFlask("cursed", flask); break;
                            case "FlaskDispellsChill": LinkConditionToFlask("frozen", flask); break;
                            case "FlaskDispellsBurning": LinkConditionToFlask("burning", flask); break;
                            case "FlaskDispellsPoison": LinkConditionToFlask("poisoned", flask); break;
                        }
                    }
                    Log($"Flask: {pathName} Charges: [{charges.NumCharges}/{charges.ChargesMax}] Quality: {quality.ItemQuality}% HP:{flask.LifeHealAmount} MP:{flask.ManaHealAmount}");
                }
            }
            updateFlaskMods = false;
        }

        public static void OnTick()
        {
            if (api == null) return;
            if (Paused) return;
            UpdateFlasks();
            CheckFlasks();
        }

        private static void CheckFlasks()
        {
            if (HasBuff("grace_period")) return;
            var area = api.Area.CurrentArea;
            if (area.IsTown || area.IsHideout) return;
            var player = api.Player;
            if (!IsValid(player)) return;
            Life life = player.GetComponent<Life>();
            if (life == null) return;
            if (Settings.AutoCureDebuffs.Value && CheckConditions(life)) return;
            if (Settings.UseLifeFlasks.Value && CheckLifeFlasks(life)) return;
            if (Settings.UseManaFlasks.Value && CheckManaFlasks(life)) return;
            if (Settings.AutoUseFullPotions.Value && CheckFullFlasks()) return;
        }

        private static bool CheckFullFlasks()
        {
            foreach(var flask in flasks)
            {
                if( flask.IsFull && flask.LifeHealAmount == 0 && flask.ManaHealAmount == 0 && flask.IsUsable(2000) )
                {
                    return flask.UseFlask(2000);
                }
            }
            return false;
        }
        private static bool CheckConditions(Life life)
        {
            foreach(var condition in conditionMap.Keys)
            {
                if (HasBuff(condition))
                {
                    var flask = conditionMap[condition];
                    if (flask.IsUsable(300))
                    {
                        return flask.UseFlask(300);
                    }
                }
            }
            if( HasAnyBuff("curse_") )
            {
                if (conditionMap.TryGetValue("cursed", out Flask flask))
                {
                    if (flask.IsUsable(4800))
                    {
                        return flask.UseFlask(4800);
                    }
                }
            }
            return false;
        }
        private static bool CheckManaFlasks(Life life)
        {
            var maxMana = life.MaxMana - life.ReservedFlatMana;
            var curMana = life.CurMana;
            var manaThreshold = maxMana * .4f;
            if (curMana < manaThreshold)
            {
                foreach (var flask in flasks)
                {
                    if (!(flask.ManaHealAmount > 0 && flask.IsUsable(200))) continue;
                    return flask.UseFlask(200);
                }
            }
            return false;
        }

        private static bool CheckLifeFlasks(Life life)
        {
            var maxHp = life.MaxHP - life.ReservedFlatHP;
            var curHp = life.CurHP;
            var lifeThreshold = maxHp / 2;
            DrawTextAtPlayer($"CheckLifeFlasks: {curHp} > {lifeThreshold}");
            if (curHp <= lifeThreshold)
            {
                Flask instantFlask = null;
                Flask slowFlask = null;
                foreach (var flask in flasks)
                {
                    if (!(flask.LifeHealAmount > 0 && flask.IsUsable(200))) continue;
                    if (flask.IsInstant || (flask.IsInstantOnLowLife && life.HPPercentage < .5))
                    {
                        instantFlask = instantFlask ?? flask;
                    }
                    else
                    {
                        slowFlask = slowFlask ?? flask;
                    }
                }
                if (instantFlask != null) { return instantFlask.UseFlask(200); }
                else if (slowFlask != null) { return slowFlask.UseFlask(200); }
            }
            return false;
        }

    }

}

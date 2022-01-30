using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput.Native;
using static Assistant.Globals;

namespace Assistant
{
    static class BuffManager
    {
        private static GameController api;
        private static Graphics Graphics;
        private static AssistantSettings Settings;
        private static bool Paused = true;
        internal static void Initialise(GameController game, Graphics gfx, AssistantSettings settings)
        {
            api = game;
            Graphics = gfx;
            Settings = settings;
            InputManager.OnRelease(VirtualKeyCode.F3, () => Paused = false);
            InputManager.OnRelease(VirtualKeyCode.PAUSE, () => Paused = !Paused);
            // PersistedText.Add(GetStatusText, (c) => ScreenRelativeToWindow(.72f, .85f), 0);
        }

        private static string GetStatusText() => $"BuffManager[{(Paused ? "Paused" : "Running")}]";

        private static List<BuffToMaintain> vaalBuffsToMaintain = new List<BuffToMaintain>();
        private static List<BuffToMaintain> buffsToMaintain = new List<BuffToMaintain>();
        private static List<BuffToMaintain> buffsToClear = new List<BuffToMaintain>();
        public static void MaintainVaalBuff(ToggleNode config, string skillName, string buffName, VirtualKeyCode key)
        {
            vaalBuffsToMaintain.Add(new BuffToMaintain() { Node = config, SkillName = skillName, BuffName = buffName, Key = key });
        }
        public static void MaintainVaalBuff(ToggleNode config, string skillName, string buffName, VirtualKeyCode key, Func<bool> cond)
        {
            vaalBuffsToMaintain.Add(new BuffToMaintain() { Node = config, SkillName = skillName, BuffName = buffName, Key = key, Condition = cond });
        }
        public static void MaintainBuff(ToggleNode config, string skillName, string buffName, VirtualKeyCode key, Func<bool> cond)
        {
            buffsToMaintain.Add(new BuffToMaintain() { Node = config, SkillName = skillName, BuffName = buffName, Key = key, Condition = cond });
        }
        public static void MaintainBuff(ToggleNode config, string skillName, string buffName, VirtualKeyCode key) => MaintainBuff(config, skillName, buffName, key, () => true);
        public static void ClearBuff(ToggleNode config, string buffName, VirtualKeyCode key, Func<bool> cond) => buffsToClear.Add(new BuffToMaintain() { Node = config, BuffName = buffName, Key = key, Condition = cond });
        private class BuffToMaintain
        {
            public ToggleNode Node;
            public string SkillName;
            public string BuffName;
            public VirtualKeyCode Key;
            public Func<bool> Condition = () => true;
        }

        public static void OnTick()
        {
            if (Paused) return;
            if (HasBuff("grace_period")) return;
            foreach(var buff in vaalBuffsToMaintain)
            {
                // string status = $"Maintain Vaal: {buff.BuffName} ";
                if (buff.Node?.Value ?? false)
                {
                    bool needsBuff = false;
                    try
                    {
                        needsBuff = buff.Condition();
                    }
                    catch (Exception err)
                    {
                        // status += $"Exception: {err.ToString()}";
                        Log(err.ToString());
                    }
                    if( needsBuff )
                    {
                        if (!HasBuff(buff.BuffName))
                        {
                            SkillManager.TryUseVaalSkill(buff.SkillName, buff.Key);
                        // } else { status += "Has Buff.";
                        }
                    // } else { status += "Not needed.";
                    }
                // } else { status += "Disabled.";
                }
                // DrawTextAtPlayer(status);
            }
            foreach(var buff in buffsToMaintain)
            {
                if (!(buff.Node?.Value ?? false)) continue;
                try { if (!buff.Condition()) continue; }
                catch(Exception err)
                {
                    Log($"Exception: {err.ToString()}");
                    continue;
                }
                if (HasBuff(buff.BuffName))
                {
                    continue;
                }
                SkillManager.TryUseSkill(buff.SkillName, buff.Key);
            }
            foreach(var buff in buffsToClear)
            {
                if (!(buff.Node?.Value ?? false)) continue;
                try { if (!buff.Condition()) continue; }
                catch (Exception err)
                {
                    Log($"Exception: {err.ToString()}");
                    continue;
                }
                if (!HasBuff(buff.BuffName)) continue;
                InputManager.PressKey(buff.Key, 40, 1000);
            }
        }
    }
}

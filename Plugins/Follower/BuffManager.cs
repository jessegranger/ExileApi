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
using static Follower.Globals;

namespace Follower
{
    static class BuffManager
    {
        private static GameController api;
        private static Graphics Graphics;
        private static FollowerSettings Settings;
        private static bool Paused = true;
        internal static void Initialise(GameController game, Graphics gfx, FollowerSettings settings)
        {
            api = game;
            Graphics = gfx;
            Settings = settings;
            InputManager.OnRelease(VirtualKeyCode.F3, () => Paused = false);
            InputManager.OnRelease(VirtualKeyCode.PAUSE, () => Paused = !Paused);
        }

        private static List<BuffToMaintain> vaalBuffsToMaintain = new List<BuffToMaintain>();
        public static void MaintainVaalBuff(ToggleNode config, string skillName, string buffName, VirtualKeyCode key)
        {
            vaalBuffsToMaintain.Add(new BuffToMaintain() { Node = config, SkillName = skillName, BuffName = buffName, Key = key });
        }
        private class BuffToMaintain
        {
            public ToggleNode Node;
            public string SkillName;
            public string BuffName;
            public VirtualKeyCode Key;
        }

        private static bool TryGetVaalSkill(string skillName, out ActorVaalSkill skill)
        {
            var actor = api.Player.GetComponent<Actor>();
            foreach (var s in actor.ActorVaalSkills)
            {
                if (!IsValid(s)) continue;
                if (s.VaalSkillInternalName.Equals(skillName))
                {
                    skill = s;
                    return true;
                }
            }
            skill = null;
            return false;
        }

        public static void OnTick()
        {
            if (Paused)
            {
                DrawTextAtPlayer("BuffManager [Paused]");
                return;
            }
            if (HasBuff("grace_period")) return;
            foreach(var buff in vaalBuffsToMaintain)
            {
                if(buff.Node.Value)
                {
                    if (HasBuff(buff.BuffName)) continue;
                    else if (TryGetVaalSkill(buff.SkillName, out ActorVaalSkill skill) && skill.CurrVaalSouls == skill.VaalMaxSouls)
                    {
                        InputManager.PressKey(buff.Key, 30);
                    }
                }
            }
        }
    }
}

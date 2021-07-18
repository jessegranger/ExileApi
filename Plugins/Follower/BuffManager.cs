﻿using ExileCore;
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
            PersistedText.Add(GetStatusText, (c) => ScreenRelativeToWindow(.005f, .085f), 0);
        }

        private static string GetStatusText() => $"BuffManager[{(Paused ? "Paused" : "Running")}]";

        private static List<BuffToMaintain> vaalBuffsToMaintain = new List<BuffToMaintain>();
        private static List<BuffToMaintain> buffsToMaintain = new List<BuffToMaintain>();
        public static void MaintainVaalBuff(ToggleNode config, string skillName, string buffName, VirtualKeyCode key)
        {
            vaalBuffsToMaintain.Add(new BuffToMaintain() { Node = config, SkillName = skillName, BuffName = buffName, Key = key });
        }
        public static void MaintainBuff(ToggleNode config, string skillName, string buffName, VirtualKeyCode key)
        {
            buffsToMaintain.Add(new BuffToMaintain() { Node = config, SkillName = skillName, BuffName = buffName, Key = key });
        }
        private class BuffToMaintain
        {
            public ToggleNode Node;
            public string SkillName;
            public string BuffName;
            public VirtualKeyCode Key;
        }

        public static void OnTick()
        {
            if (Paused) return;
            if (HasBuff("grace_period")) return;
            foreach(var buff in vaalBuffsToMaintain)
            {
                if (!(buff.Node?.Value ?? false)) continue;
                if (HasBuff(buff.BuffName)) continue;
                SkillManager.TryUseVaalSkill(buff.SkillName, buff.Key);
            }
            foreach(var buff in buffsToMaintain)
            {
                if (!(buff.Node?.Value ?? false)) continue;
                if (HasBuff(buff.BuffName)) continue;
                SkillManager.TryUseSkill(buff.SkillName, buff.Key);
            }
        }
    }
}
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace Follower
{
	class FollowerSettings : ISettings
	{
    public ToggleNode Enable { get; set; }

		public ToggleNode EldritchBattery { get; set; }

		public ToggleNode UseGuardSkill { get; set; }

		public ToggleNode UseESForDefenseTriggers { get; set; }

		public ToggleNode UseDefenseFlasks { get; set; }

		public ToggleNode UseLifeFlasks { get; set; }

		public ToggleNode UseManaFlasks { get; set; }

		public ToggleNode AutoCureDebuffs { get; set; }
		public ToggleNode ShowBuffNames { get; set; }

		public ToggleNode AutoUseFullPotions { get; set; }

		public ToggleNode UseVaalGrace { get; set; }

		public ToggleNode UseVaalImpurityOfIce { get; set; }

		public int LifeFlaskThreshold { get; set; } = 3000;

	}
}
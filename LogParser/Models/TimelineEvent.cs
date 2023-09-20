using LogParser.Enums;

namespace LogParser.Models
{
	public class TimelineEvent
	{
		public int Timestamp { get; set; }
		public int CasterId { get; set; }
		public string CasterName { get; set; }
		public int TargetId { get; set; }
		public string TargetName { get; set; }
		public int SkillId { get; set; }
		public string SkillName { get; set; }
		public PlayerAction Action { get; set; }

		public int ReactionTime { get; set; } = -1;
		public int TimeBeforeStop { get; set; }
		public int RuptedById { get; set; }
		public string RuptedByName { get; set; }
	}
}

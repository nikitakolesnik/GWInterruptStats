namespace LogParser.Models
{
	public class Rupt
	{
		public int Timestamp { get; set; }
		public int ReactionTime { get; set; } = -1;
		public bool Hit { get; set; }
		public string RuptSkill { get; set; }
		public string TargetPlayer { get; set; }
		public string TargetSkill { get; set; } = string.Empty;
	}
}
using Newtonsoft.Json;

namespace LogParser.Models
{
	public class SkillCasted
	{
		[JsonProperty("cast_time")]
		public double CastTime { get; set; }
		public int Caster { get; set; }
		[JsonProperty("instance_timestamp")]
		public int InstanceTimestamp { get; set; }
		[JsonProperty("skill_id")]
		public int SkillId { get; set; }
		[JsonProperty("skill_name")]
		public string SkillName { get; set; }
		public int Target { get; set; }
	}
}

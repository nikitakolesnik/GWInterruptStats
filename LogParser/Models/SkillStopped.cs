using LogParser.Enums;
using Newtonsoft.Json;

namespace LogParser.Models
{
	public class SkillStopped
	{
		[JsonProperty("action")]
		public PlayerAction Action { get; set; }
		[JsonProperty("instance_timestamp")]
		public int InstanceTimestamp { get; set; }
	}
}

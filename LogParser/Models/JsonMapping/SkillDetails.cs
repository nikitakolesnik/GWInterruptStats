using Newtonsoft.Json;

namespace LogParser.Models.JsonMapping
{
	public class SkillDetails
	{
		[JsonProperty("a")]
		public int AdrenCost { get; set; }
		[JsonProperty("c")]
		public double CastTime { get; set; }
		[JsonProperty("co")]
		public int UnknownPropertyCo { get; set; }
		[JsonProperty("e")]
		public int EnergyCost { get; set; }
		[JsonProperty("q")]
		public int UnknownPropertyQ { get; set; }
		[JsonProperty("r")]
		public int RechargeTime { get; set; }
		[JsonProperty("sp")]
		public int UnknownPropertySp { get; set; }
	}
}

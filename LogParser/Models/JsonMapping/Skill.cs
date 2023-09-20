using Newtonsoft.Json;

namespace LogParser.Models.JsonMapping
{
	public class Skill
	{
		[JsonProperty("a")]
		public int UnknownPropertyA { get; set; }
		[JsonProperty("c")]
		public int UnknownPropertyC { get; set; }
		[JsonProperty("cd")]
		public string ConciseDescription { get; set; }
		[JsonProperty("d")]
		public string Description { get; set; }
		[JsonProperty("n")]
		public string Name { get; set; }
		[JsonProperty("p")]
		public int? Profession { get; set; }
		[JsonProperty("t")]
		public int UnknownPropertyT { get; set; }
		[JsonProperty("z")]
		public SkillDetails SkillDetails { get; set; }
	}
}

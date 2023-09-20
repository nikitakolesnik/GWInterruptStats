using Newtonsoft.Json;
using System.Collections.Generic;

namespace LogParser.Models.JsonMapping
{
	public class Agent
	{
		[JsonProperty("agent_id")]
		public int AgentId { get; set; }
		public string Name { get; set; }
		[JsonProperty("skills_casted")]
		public List<SkillCasted> SkillsCasted { get; set; }
		[JsonProperty("skills_stopped")]
		public List<SkillStopped> SkillsStopped { get; set; }
		public string Team { get; set; }
		public string Profession { get; set; }
	}
}

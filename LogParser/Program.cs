using LogParser.Enums;
using LogParser.Models;
using LogParser.Models.JsonMapping;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogParser
{
	class Program
	{
		private const string _inputSkillsPath = "../../../../skills.json";
		private const string _inputLogPath = "../../../../WkB-Max-semifinals.json";
		private const string _outputTimelinePath = "../../../../output-timeline.txt";
		private const string _outputStatsPath = "../../../../output-stats.txt";
		private static readonly string[] _interrupts = { "Power Spike", "Complicate", "Psychic Instability(PvP)", "Signet of Disruption", "Cry of Frustration", "Leech Signet", "Power Drain", "Power Lock", "Psychic Distraction", "Tease", "Web of Disruption", "Simple Thievery", "Signet of Distraction", "Power Block", "Power Flux", "Power Leak", "Power Leech", "Power Return", "Signet of Clumsiness" };
		private static readonly string[] _npcs = { "Archer", "Knight", "Footman", "Bodyguard", "Guild Lord", "Lesser Flame Sentinel" };

		private static string FormatTime(int timestamp)
		{
			string output = "[";

			if (timestamp < 60_000)
			{
				timestamp = 60_000 - timestamp;
				output += "   ";
			}
			else
			{
				timestamp -= 60_000;
				int minutes = timestamp / 60_000;

				if (minutes < 10)
					output += "0";

				output += minutes;
				output += ":";
			}

			int seconds = (timestamp / 1_000) % 60;
			int milliseconds = timestamp % 1_000;
			if (seconds < 10)
				output += "0";

			output += seconds;
			output += ".";

			if (milliseconds < 100)
				output += "0";
			if (milliseconds < 10)
				output += "0";

			output += milliseconds;
			output += "]";

			return output;
		}

		static void Main(string[] args)
		{
			// Parse the match log into an object of only players

			AgentCollection agents = JsonConvert.DeserializeObject<AgentCollection>(File.ReadAllText(_inputLogPath));
			agents.Agents.RemoveAll(x => x == null || (!x.Name.Contains('(') /* not a player */ && !_npcs.Contains(x.Name) /* not an npc */));


			// Parse the list of all skills into those which can't be interrupted

			SkillCollection skills = JsonConvert.DeserializeObject<SkillCollection>(File.ReadAllText(_inputSkillsPath));
			skills.Skills.RemoveAll(x => x == null || x.Profession == 0);

			List<string> zeroCastTimeSkills = skills.Skills
				//.Where(x => x.Description.Substring(0, 6) == "Stance")
				.Where(x => x?.SkillDetails.CastTime == 0 // no cast time
					&& x.UnknownPropertyT != 14)          // not an attack skill
				.Select(x => x.Name)
				.ToList();

			#region json format example
			//{
			//	"a":51,
			//	"c":0,
			//	"cd":"Skill. While in the guise of Mysterious Assassin, you have new skills, Health, and attributes.",
			//	"d":"Skill. While in the guise of Mysterious Assassin, you have new skills, Health, and attributes.",
			//	"n":"Mysterious Assassin",
			//	"p":7,
			//	"t":29,
			//	"z":
			//	{
			//		"co":0,
			//		"q":0,
			//		"sp":1048576
			//	}
			//}
			#endregion


			// Output discovered players

			Console.WriteLine("Players detected: ");
			foreach (Agent player in agents.Agents.OrderBy(x => x.Team))
			{
				Console.BackgroundColor = (player.Team == "Blue") ? ConsoleColor.Blue : ConsoleColor.Red;
				Console.WriteLine($"{player.Name}");
			}
			Console.ResetColor();
			Console.WriteLine();


			// Create timeline

			List<TimelineEvent> timeline = new();
			foreach (Agent player in agents.Agents)
			{
				foreach (SkillCasted skill in player.SkillsCasted)
				{
					timeline.Add(new TimelineEvent
					{
						Timestamp = skill.InstanceTimestamp,
						CasterId = skill.Caster,
						CasterName = player.Name,
						TargetId = skill.Target,
						TargetName = agents.Agents.FirstOrDefault(x => x.AgentId == skill.Target)?.Name ?? "[NON-PLAYER]",
						SkillId = skill.SkillId,
						SkillName = string.IsNullOrEmpty(skill.SkillName) ? "[UNLABELED_SKILL]" : skill.SkillName,
						Action = PlayerAction.Start
					});
				foreach (SkillStopped stop in player.SkillsStopped)
					timeline.Add(new TimelineEvent
					{
						Timestamp = stop.InstanceTimestamp,
						CasterId = player.AgentId,
						CasterName = player.Name,
						Action = stop.Action,
					});
				}
			}
			timeline = timeline.OrderBy(x => x.Timestamp).ToList();


			// Initialize container of interrupt stats for each player

			Dictionary<string, List<Rupt>> ruptStats = new();
			foreach (Agent player in agents.Agents)
			{
				if (player.SkillsCasted.Any(x => _interrupts.Contains(x.SkillName)))
				//if (player.Profession.Substring(0, 2) == "Me")
				{
					Console.WriteLine($"Logged mesmer rupt stats for {player.Name}");
					ruptStats.Add(player.Name, new());
				}
			}


			// Find each rupt, go forward some time to see if anything was interrupted, save the details

			const int interval = 250; // max milliseconds a rupt can happen before the cast is stopped
			for (int curr = 0; curr < timeline.Count; curr++) // For each timeline event
			{
				if (_interrupts.Contains(timeline[curr].SkillName)  // If this action is an interrupt being used,
					&& timeline[curr].Action == PlayerAction.Start) // And (though not needed) the action is a successful cast start,
				{
					Rupt rupt = new()
					{
						Timestamp = timeline[curr].Timestamp,
						RuptSkill = timeline[curr].SkillName,
						TargetPlayer = timeline[curr].TargetName
					};

					// Starting from each rupt, go forward to see if it hit or not

					int next = curr + 1;

					while (timeline[next].Timestamp <= timeline[curr].Timestamp + interval // For future events up to the interval,
						&& next < timeline.Count)                                          // And not past the end,
					{
						if (timeline[next].Action == PlayerAction.Interrupt            // If this is a player being interrupted,
							&& timeline[next].CasterName == timeline[curr].TargetName) // And it's the targeted player,
						{
							// If it hit: starting from each rupt again, go backwards to see target skill & reaction time

							for (int prev = curr; prev >= 0; --prev) // For each past event,
							{
								if (timeline[prev].CasterName == timeline[curr].TargetName     // If this was the interrupted player,
									&& timeline[prev].Action == PlayerAction.Start             // And they were starting a cast,
									&& !zeroCastTimeSkills.Contains(timeline[prev].SkillName)) // And this skill isn't instant,
								{
									rupt.TargetSkill = timeline[prev].SkillName;
									rupt.ReactionTime = timeline[curr].Timestamp - timeline[prev].Timestamp;
									break;
								}//i
							}//f

							break;
						}//i

						next += 1;
					}//w

					// Save the rupt stats

					rupt.Hit = rupt.ReactionTime > -1;
					ruptStats[timeline[curr].CasterName].Add(rupt);
				}//i
			}//f


			// Write timeline to file

			using (TextWriter tw = new StreamWriter(_outputTimelinePath))
			{ 
				foreach (TimelineEvent tLEvent in timeline)
				{
					string action = string.Empty;
					switch (tLEvent.Action)
					{
						case (PlayerAction.Start):
							action = $"started casting {tLEvent.SkillName}";
							if (tLEvent.TargetId != 0)
								action += $" on {tLEvent.TargetName}";
							break;
						case (PlayerAction.Stop):
							action = $"stopped casting";
							break;
						case (PlayerAction.Interrupt):
							action = $"was interrupted";
							break;
					}
					tw.WriteLine($"{FormatTime(tLEvent.Timestamp)} {tLEvent.CasterName} {action}");
				}
			}


			// Write rupt stats to file

			using (TextWriter tw = new StreamWriter(_outputStatsPath))
			{
				foreach (KeyValuePair<string, List<Rupt>> profile in ruptStats)
				{
					int hits = profile.Value.Count(x => x.Hit);
					int misses = profile.Value.Count - hits;
					int accuracy = (hits + misses > 0) ? (int)(hits * 100.0 / (hits + misses)) : -1;

					profile.Value.Sort((x,y) => x.ReactionTime.CompareTo(y.ReactionTime));

					int fastestIndex = profile.Value.FindIndex(x => x.Hit);
					int slowestIndex = profile.Value.FindLastIndex(x => x.Hit);

					Dictionary<string, int> targetsHit = new();
					Dictionary<string, int> ruptsHit = new();
					Dictionary<string, int> skillsHit = new();
					Dictionary<string, int> targetsMissed = new();
					Dictionary<string, int> ruptsMissed = new();
					Dictionary<string, int> skillsMissed = new();

					// HITS
					foreach (Rupt ruptHit in profile.Value.Where(x => x.Hit))
					{
						//targets
						if (!targetsHit.ContainsKey(ruptHit.TargetPlayer))
							targetsHit.Add(ruptHit.TargetPlayer, 1);
						else
							targetsHit[ruptHit.TargetPlayer]++;

						//rupts
						if (!ruptsHit.ContainsKey(ruptHit.RuptSkill))
							ruptsHit.Add(ruptHit.RuptSkill, 1);
						else
							ruptsHit[ruptHit.RuptSkill]++;

						//skills
						if (!skillsHit.ContainsKey(ruptHit.TargetSkill))
							skillsHit.Add(ruptHit.TargetSkill, 1);
						else
							skillsHit[ruptHit.TargetSkill]++;
					}

					// MISSES
					foreach (Rupt ruptMiss in profile.Value.Where(x => !x.Hit))
					{
						//targets
						if (!targetsMissed.ContainsKey(ruptMiss.TargetPlayer))
							targetsMissed.Add(ruptMiss.TargetPlayer, 1);
						else
							targetsMissed[ruptMiss.TargetPlayer]++;

						//rupts
						if (!ruptsMissed.ContainsKey(ruptMiss.RuptSkill))
							ruptsMissed.Add(ruptMiss.RuptSkill, 1);
						else
							ruptsMissed[ruptMiss.RuptSkill]++;

						//skills
						if (!skillsMissed.ContainsKey(ruptMiss.TargetSkill))
							skillsMissed.Add(ruptMiss.TargetSkill, 1);
						else
							skillsMissed[ruptMiss.TargetSkill]++;
					}

					tw.WriteLine($"Stats for {profile.Key}:\r\n"
						+ $"\tHits:     {hits}\r\n"
						+ $"\tMisses:   {misses}\r\n"
						+ $"\tTotal:    {hits + misses}\r\n"
						+ $"\tAccuracy: {((accuracy == -1) ? "N/A" : accuracy)}%\r\n"
						+ "\t---\r\n"
						+ $"\tFastest reaction: {profile.Value[fastestIndex].ReactionTime} ms\t\r\n"
						+ $"\tSlowest reaction: {profile.Value[slowestIndex].ReactionTime} ms\r\n"
						+ $"\tMedian  reaction: {profile.Value[(fastestIndex+slowestIndex)/2].ReactionTime} ms\r\n"
						+ "\t---\r\n"
						+ "\tTargets hit:");

					foreach (KeyValuePair<string, int> target in targetsHit.OrderByDescending(x => x.Value))
						tw.WriteLine($"\t\t{target.Value} - {target.Key}");

					tw.WriteLine("\tTargets missed:");

					foreach (KeyValuePair<string, int> target in targetsMissed.OrderByDescending(x => x.Value))
						tw.WriteLine($"\t\t{target.Value} - {target.Key}");

					tw.WriteLine("\t---\r\n"
						+ "\tInterrupts hit:");

					foreach (KeyValuePair<string, int> skill in ruptsHit.OrderByDescending(x => x.Value))
						tw.WriteLine($"\t\t{skill.Value} - {skill.Key}");

					tw.WriteLine("\tInterrupts missed:");

					foreach (KeyValuePair<string, int> skill in ruptsMissed.OrderByDescending(x => x.Value))
						tw.WriteLine($"\t\t{skill.Value} - {skill.Key}");

					tw.WriteLine("\t---\r\n"
						+ "\tSkills hit:");

					foreach (KeyValuePair<string, int> skill in skillsHit.OrderByDescending(x => x.Value))
						tw.WriteLine($"\t\t{skill.Value} - {skill.Key}");

					tw.WriteLine("\tSkills missed:");

					foreach (KeyValuePair<string, int> skill in skillsMissed.OrderByDescending(x => x.Value))
						tw.WriteLine($"\t\t{skill.Value} - {skill.Key}");

					tw.WriteLine();
				}
			}


			// done

			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}
	}
}

using System.Collections.Generic;

namespace BasicProject.Services
{
    public class ApplicationMatchInfo
    {
        public int Score { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }

        public List<string> MatchedSkills { get; set; }
        public List<string> MissingSkills { get; set; }

        public int RuleScore { get; set; }
        public int AiScore { get; set; }

        public ApplicationMatchInfo()
        {
            MatchedSkills = new List<string>();
            MissingSkills = new List<string>();
        }
    }
}
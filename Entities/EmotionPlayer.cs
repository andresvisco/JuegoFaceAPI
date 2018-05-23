using Entities.Enums;
using Microsoft.ProjectOxford.Common.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class EmotionPlayer
    {
        public EmotionEnum Type
        {
            get
            {
                return _emotionScore.ToRankedList().Select(x => (EmotionEnum)Enum.Parse(typeof(EmotionEnum), x.Key)).First();
            }
        }

        public int Score
        {
            get
            {
                return _emotionScore.ToRankedList().Select(x => Convert.ToInt32(x.Value * 100)).First();
            }
        }

        private EmotionScores _emotionScore;

        public EmotionPlayer(EmotionScores emotionScore)
        {
            _emotionScore = emotionScore;
        }

    }
}

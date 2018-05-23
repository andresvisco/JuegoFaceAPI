using Entities.Enums;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Player
    {
        public Guid Id { get; set; }
        public int Score { get; set; }
        public FaceRectangle Position { get; set; }
        public EmotionPlayer DominantEmotion { get; set; }
        private const int _multiplier = 100;

        public FaceAttributes Attributes { get; set; }

        public Player(Guid id, FaceRectangle position, FaceAttributes attributes)
        {
            this.Id = id;
            this.Position = position;
            this.DominantEmotion = this.GetDominantEmotion(attributes.Emotion);
            this.Attributes = attributes;
            //this.AccumulateScore();
        }

        private EmotionPlayer GetDominantEmotion(EmotionScores emotionScore)
        {

            //Solo aumenta el score si esta feliz!
            // var data = new EmotionPlayer() { Type = EmotionEnum.Happiness, Score = Convert.ToInt32(emotionScore.Happiness * 1000) };
            //return emotionScore.ToRankedList().Select(kv => new EmotionPlayer { Type = (EmotionEnum)Enum.Parse(typeof(EmotionEnum), kv.Key), Score = Convert.ToInt32(kv.Value) }).First();

            //--
            //var data = emotionScore.ToRankedList().Select(kv => new EmotionPlayer { Type = (EmotionEnum)Enum.Parse(typeof(EmotionEnum), kv.Key), Score = Convert.ToInt32(kv.Value * _multiplier) }).First();
            //return data;

            return new EmotionPlayer(emotionScore);
        }

        public void AccumulateScore()
        {

            if (this.DominantEmotion.Type == EmotionEnum.Neutral)
            {
                this.Score = this.Score + (this.DominantEmotion.Score / 2);
            }

            if (this.DominantEmotion.Type == EmotionEnum.Happiness)
            {
                this.Score = this.Score + (this.DominantEmotion.Score);
            }

            //if (this.DominantEmotion.Type == EmotionEnum.Happiness || this.DominantEmotion.Type == EmotionEnum.Neutral)
            //{
            //    if (this.Score == 0)
            //    {
            //        this.Score = this.DominantEmotion.Score;
            //    }
            //    else
            //    {
            //        this.Score = this.Score + (this.DominantEmotion.Score);
            //    }
            //}
        }
        public void ResetPosition()
        {
            this.Position = new FaceRectangle();
        }
        public void Update(Player player)
        {
            this.Position = player.Position;
            this.DominantEmotion = player.DominantEmotion;
            //this.AccumulateScore();
        }
    }
}

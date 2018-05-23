// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision.Contract;
using Entities;
using Entities.Enums;

namespace LiveCameraSample
{
    public class Visualization
    {
        private static SolidColorBrush s_lineBrushRed = new SolidColorBrush(new System.Windows.Media.Color { R = 255, G = 0, B = 0, A = 255 });
        private static SolidColorBrush s_lineBrushGreenYellow = new SolidColorBrush(new System.Windows.Media.Color { R = 0, G = 255, B = 0, A = 255 });
        private static SolidColorBrush s_lineBrushBlue = new SolidColorBrush(new System.Windows.Media.Color { R = 0, G = 0, B = 255, A = 255 });
        private static SolidColorBrush s_lineBrushGreen = new SolidColorBrush(new System.Windows.Media.Color { R=11, G=255,B=72, A=255 });

        private static SolidColorBrush s_lineBrush = new SolidColorBrush(new System.Windows.Media.Color { R = 255, G = 185, B = 0, A = 255 });
        private static SolidColorBrush s_winLineBrush = new SolidColorBrush(new System.Windows.Media.Color { R = 245, G = 0, B = 0, A = 255 });
        private static Typeface s_typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        private static BitmapSource DrawOverlay(BitmapSource baseImage, Action<DrawingContext, double> drawAction)
        {
            double annotationScale = baseImage.PixelHeight / 320;

            DrawingVisual visual = new DrawingVisual();
            DrawingContext drawingContext = visual.RenderOpen();

            drawingContext.DrawImage(baseImage, new Rect(0, 0, baseImage.Width, baseImage.Height));

            drawAction(drawingContext, annotationScale);

            drawingContext.Close();

            RenderTargetBitmap outputBitmap = new RenderTargetBitmap(
                baseImage.PixelWidth, baseImage.PixelHeight,
                baseImage.DpiX, baseImage.DpiY, PixelFormats.Pbgra32);

            outputBitmap.Render(visual);

            return outputBitmap;
        }

        public static BitmapSource DrawTags(BitmapSource baseImage, Tag[] tags)
        {
            if (tags == null)
            {
                return baseImage;
            }

            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {
                double y = 0;
                foreach (var tag in tags)
                {
                    // Create formatted text--in a particular font at a particular size
                    FormattedText ft = new FormattedText(tag.Name,
                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight, s_typeface,
                        42 * annotationScale, Brushes.Black);
                    // Instead of calling DrawText (which can only draw the text in a solid colour), we
                    // convert to geometry and use DrawGeometry, which allows us to add an outline. 
                    var geom = ft.BuildGeometry(new System.Windows.Point(10 * annotationScale, y));
                    drawingContext.DrawGeometry(s_lineBrush, new Pen(Brushes.Black, 2 * annotationScale), geom);
                    // Move line down
                    y += 42 * annotationScale;
                }
            };

            return DrawOverlay(baseImage, drawAction);
        }

        public static BitmapSource DrawFaces(BitmapSource baseImage, Microsoft.ProjectOxford.Face.Contract.Face[] faces, EmotionScores[] emotionScores, string[] celebName)
        {
            if (faces == null)
            {
                return baseImage;
            }

            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {
                for (int i = 0; i < faces.Length; i++)
                {
                    var face = faces[i];
                    if (face.FaceRectangle == null) { continue; }

                    Rect faceRect = new Rect(
                        face.FaceRectangle.Left, face.FaceRectangle.Top,
                        face.FaceRectangle.Width, face.FaceRectangle.Height);
                    string text = "";

                    if (face.FaceAttributes != null)
                    {
                        text += Aggregation.SummarizeFaceAttributes(face.FaceAttributes);
                    }

                    if (emotionScores?[i] != null)
                    {
                        text += Aggregation.SummarizeEmotion(emotionScores[i]);
                    }

                    if (celebName?[i] != null)
                    {
                        text += celebName[i];
                    }

                    faceRect.Inflate(6 * annotationScale, 6 * annotationScale);

                    double lineThickness = 4 * annotationScale;

                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(s_lineBrush, lineThickness),
                        faceRect);

                    if (text != "")
                    {
                        FormattedText ft = new FormattedText(text,
                            CultureInfo.CurrentCulture, FlowDirection.LeftToRight, s_typeface,
                            16 * annotationScale, Brushes.Black);

                        var pad = 3 * annotationScale;

                        var ypad = pad;
                        var xpad = pad + 4 * annotationScale;
                        var origin = new System.Windows.Point(
                            faceRect.Left + xpad - lineThickness / 2,
                            faceRect.Top - ft.Height - ypad + lineThickness / 2);
                        var rect = ft.BuildHighlightGeometry(origin).GetRenderBounds(null);
                        rect.Inflate(xpad, ypad);

                        drawingContext.DrawRectangle(s_lineBrush, null, rect);
                        drawingContext.DrawText(ft, origin);
                    }
                }
            };

            return DrawOverlay(baseImage, drawAction);
        }

        public static SolidColorBrush GetBrush(Player player)
        {
            var brush = s_lineBrush;

            switch (player.DominantEmotion.Type)
            {
                case EmotionEnum.Anger:
                    brush = s_lineBrushRed;
                    break;
                case EmotionEnum.Contempt:
                    brush = s_lineBrushBlue;
                    break;
                case EmotionEnum.Disgust:
                    break;
                case EmotionEnum.Fear:
                    brush = s_lineBrushBlue;
                    break;
                case EmotionEnum.Happiness:
                    brush = s_lineBrushGreen;
                    break;
                case EmotionEnum.Neutral:
                    brush = s_lineBrushBlue;
                    break;
                case EmotionEnum.Sadness:
                    brush = s_lineBrushRed;
                    break;
                case EmotionEnum.Surprise:
                    break;
                default:
                    brush = s_lineBrushBlue;
                    break;
            }
            return brush;
        }

        public static BitmapSource DrawPlayers(BitmapSource baseImage, List<Player> players, GameStateEnum gameState)
        {
            if (players == null)
            {
                return baseImage;
            }

            int playerMaxScore = (players.Count > 0) ? players.Max(p => p.Score) : 0;

            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {
                string emocionDominante = "";
                for (int i = 0; i < players.Count; i++)
                {
                    
                    var player = players[i];
                    if (player.Position == null) { continue; }

                    emocionDominante = player.DominantEmotion.Type.ToString();

                    switch (emocionDominante)
                    {
                        case "Happiness":
                            emocionDominante = "Contento";
                            break;

                        case "Anger":
                            emocionDominante = "Enojado";
                            break;
                        case "Neutral":
                            emocionDominante = "Neutral";
                            break;
                        case "Sadness":
                            emocionDominante = "Enojado";
                            break;

                            
                    }

                    Rect faceRect = new Rect(
                        player.Position.Left, player.Position.Top,
                        player.Position.Width, player.Position.Height);

                    //asco pero no importa, es una prueba, cambiarlo
                    string[] guidarray = player.Id.ToString("D").Split('-');

                    //string score = string.Format("Player {0} - Score: {1}", guidarray[guidarray.Length - 1], player.Score);

                    string score;
                    //string score = string.Format("{1} - Score: {0} ", player.Score, player.DominantEmotion.Type);
                    if (gameState == GameStateEnum.Started)
                    {
                        score = string.Format("{1} - Edad: {2} \nPuntaje: {0}  ", player.Score, emocionDominante, player.Attributes.Age);
                        //score = string.Format("{1} - Score: {0}  ", player.Score, player.DominantEmotion.Type);
                    }
                    else
                    {
                        score = string.Format("{0} \n Edad: {1}", emocionDominante, player.Attributes.Age);
                        //score = string.Format("{0}", player.DominantEmotion.Type);
                    }


                    faceRect.Inflate(6 * annotationScale, 6 * annotationScale);

                    double lineThickness = 4 * annotationScale;
                    // var brush = s_lineBrush;

                    var brush = GetBrush(player);

                    if (player.Score == playerMaxScore && gameState == GameStateEnum.Finished)
                    {
                        brush = s_winLineBrush;
                    }

                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(brush, lineThickness),
                        faceRect);


                    FormattedText ft = new FormattedText(score,
                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight, s_typeface,
                        16 * annotationScale, Brushes.Black);

                    var pad = 3 * annotationScale;

                    var ypad = pad;
                    var xpad = pad + 4 * annotationScale;
                    var origin = new System.Windows.Point(
                        faceRect.Left + xpad - lineThickness / 2,
                        faceRect.Top - ft.Height - ypad + lineThickness / 2);
                    var rect = ft.BuildHighlightGeometry(origin).GetRenderBounds(null);
                    rect.Inflate(xpad, ypad);

                    drawingContext.DrawRectangle(brush, null, rect);
                    drawingContext.DrawText(ft, origin);

                }
            };

            return DrawOverlay(baseImage, drawAction);
        }

        public static BitmapSource DrawResults(BitmapSource baseImage, List<Player> players)
        {
            if (players == null)
            {
                return baseImage;
            }

            int playerMaxScore = (players.Count > 0) ? players.Max(p => p.Score) : 0;

            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    if (player.Position == null) { continue; }

                    Rect faceRect = new Rect(
                        player.Position.Left, player.Position.Top,
                        player.Position.Width, player.Position.Height);
                    string msgDefault = "Menor Puntaje: {0}";



                    faceRect.Inflate(6 * annotationScale, 6 * annotationScale);

                    double lineThickness = 4 * annotationScale;
                    var brush = s_lineBrush;

                    if (player.Score == playerMaxScore)
                    {
                        brush = s_winLineBrush;
                        msgDefault = "Puntaje Ganador: {0}";
                    }
                    string score = string.Format(msgDefault, player.Score);
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(brush, lineThickness),
                        faceRect);


                    FormattedText ft = new FormattedText(score,
                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight, s_typeface,
                        16 * annotationScale, Brushes.Black);

                    var pad = 3 * annotationScale;

                    var ypad = pad;
                    var xpad = pad + 4 * annotationScale;
                    var origin = new System.Windows.Point(
                        faceRect.Left + xpad - lineThickness / 2,
                        faceRect.Top - ft.Height - ypad + lineThickness / 2);
                    var rect = ft.BuildHighlightGeometry(origin).GetRenderBounds(null);
                    rect.Inflate(xpad, ypad);

                    drawingContext.DrawRectangle(brush, null, rect);
                    drawingContext.DrawText(ft, origin);

                }
            };

            return DrawOverlay(baseImage, drawAction);
        }

        public static BitmapSource DrawScore(BitmapSource baseImage, int score)
        {
            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {
                int posX = Convert.ToInt32(baseImage.Width / 2);
                int posY = Convert.ToInt32(baseImage.Height / 10);
                int width = Convert.ToInt32(baseImage.Width / 10);
                int height = Convert.ToInt32(baseImage.Height / 10);

                double lineThickness = 4 * annotationScale;

                var pad = 3 * annotationScale;

                var ypad = pad;
                var xpad = pad + 4 * annotationScale;
                var origin = new System.Windows.Point(0,
                    0);
                var size = new System.Windows.Size(width, height);

                string strScore = string.Format("Puntaje Total: {0}", score);

                FormattedText ft = new FormattedText(strScore,
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, s_typeface,
                    16 * annotationScale, Brushes.Black);
                Rect imageRect = new Rect(origin, size);


                var rect = ft.BuildHighlightGeometry(origin).GetRenderBounds(null);
                rect.Inflate(xpad, ypad);

                drawingContext.DrawRectangle(s_lineBrush, null, rect);
                drawingContext.DrawText(ft, origin);


            };

            return DrawOverlay(baseImage, drawAction);
        }

        public static BitmapSource DrawTime(BitmapSource baseImage, int time)
        {
            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {


                int posX = Convert.ToInt32(baseImage.Width / 2);
                int posY = Convert.ToInt32(baseImage.Height / 10);
                int width = Convert.ToInt32(baseImage.Width / 10);
                int height = Convert.ToInt32(baseImage.Height / 10);


                double lineThickness = 4 * annotationScale;

                var pad = 3 * annotationScale;

                var ypad = pad;
                var xpad = pad + 4 * annotationScale;
                var origin = new System.Windows.Point(posX,
                    0);
                var size = new System.Windows.Size(width, height);

                string strScore = string.Format("Tiempo Restante: {0}", time);

                FormattedText ft = new FormattedText(strScore,
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, s_typeface,
                    16 * annotationScale, Brushes.Black);
                Rect imageRect = new Rect(origin, size);




                var rect = ft.BuildHighlightGeometry(origin).GetRenderBounds(null);
                rect.Inflate(xpad, ypad);

                drawingContext.DrawRectangle(s_lineBrush, null, rect);
                drawingContext.DrawText(ft, origin);


            };

            return DrawOverlay(baseImage, drawAction);
        }
    }
}

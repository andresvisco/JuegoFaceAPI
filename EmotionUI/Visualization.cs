using LiveCameraSample;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EmotionUI
{
    public class Visualization
    {
        private static SolidColorBrush s_lineBrush = new SolidColorBrush(new System.Windows.Media.Color { R = 255, G = 185, B = 0, A = 255 });
        private static Typeface s_typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        internal static BitmapSource DrawTime(BitmapSource baseImage, string textTime)
        {
            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {
                FormattedText formattedText = new FormattedText(
                textTime,
                CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface("Verdana"),
                32,
                Brushes.Black);
                drawingContext.DrawText(formattedText, new Point(250, 10));
            };
            return DrawOverlay(baseImage, drawAction);
        }

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

        public static BitmapSource DrawScore(BitmapSource baseImage, Face[] faces)
        {
            double acumulador = 0;
            if (faces == null)
            {
                return baseImage;
            }

            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {
                if (faces != null)
                {
                    for (int i = 0; i < faces.Length; i++)
                    {
                        var emotionDominat = Aggregation.GetDominantEmotion(faces[i].FaceAttributes.Emotion);
                        if (emotionDominat.Item1 == "Happiness")
                        {
                            acumulador += faces[i].FaceAttributes.Emotion.Happiness;
                        }
                    }
                    FormattedText ft = new FormattedText(acumulador.ToString(),
                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight, s_typeface,
                        50, Brushes.Black);

                    var origin = new System.Windows.Point(10, 10);
                    //var rect = ft.BuildHighlightGeometry(origin).GetRenderBounds(null);
                    //rect.Width = 50;
                    //rect.Height = 50;


                    //rect.Inflate(1, 1);

                    //drawingContext.DrawRectangle(s_lineBrush, null, rect);
                    drawingContext.DrawText(ft, origin);


                }

            };
            return DrawOverlay(baseImage, drawAction);
        }
    }
}

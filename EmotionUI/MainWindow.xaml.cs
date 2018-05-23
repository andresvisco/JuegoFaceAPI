using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Globalization;
using LiveCameraSample;

namespace EmotionUI
{
    public partial class MainWindow : Window
    {
        private static SolidColorBrush s_lineBrush = new SolidColorBrush(new System.Windows.Media.Color { R = 255, G = 185, B = 0, A = 255 });
        private static Typeface s_typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private TimerText timer;
        private BitmapImage logo = new BitmapImage();
        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient("8c79ce87ce8d47a78c4ffcb195fa9ded", "https://westus.api.cognitive.microsoft.com/face/v1.0");
        Face[] faces;      
        String[] faceDescriptions;  
        double resizeFactor;
        string ruta = "Resources/Images/fourface.jpg";
        string rutaCompleta = @"C:\Users\facundo.rodrigues\Documents\Visual Studio 2015\Projects\EmotionCam\Source\VideoFrameAnalysis\EmotionUI\Resources\Images\fourface.jpg";
        //string rutaCompleta = @"C:\Proyectos_Visual\EmotionCam\Source\VideoFrameAnalysis\EmotionUI\Resources\Images\fourface.jpg";

        public MainWindow()
        {
            InitializeComponent();
            LoadImg();
            
        }

        private async void LoadImg()
        {
            //BitmapImage logo = new BitmapImage();
            logo.BeginInit();
            logo.UriSource = new Uri(ruta, UriKind.Relative);
            logo.EndInit();

            FaceImage.Stretch = Stretch.Fill;
            FaceImage.Source = logo;

            //LeftImage.Source = new BitmapImage(new Uri("Resources/Images/fondo.jpg", UriKind.Relative));                       
        }

        // Returns a string that describes the given face.
        private string FaceDescription(Face face)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Face: ");

            // Add the gender, age, and smile.
            sb.Append(face.FaceAttributes.Gender);
            sb.Append(", ");
            sb.Append(face.FaceAttributes.Age);
            sb.Append(", ");
            sb.Append(String.Format("smile {0:F1}%, ", face.FaceAttributes.Smile * 100));

            // Add the emotions. Display all emotions over 10%.
            sb.Append("Emotion: ");
            EmotionScores emotionScores = face.FaceAttributes.Emotion;
            if (emotionScores.Anger >= 0.1f) sb.Append(String.Format("anger {0:F1}%, ", emotionScores.Anger * 100));
            if (emotionScores.Contempt >= 0.1f) sb.Append(String.Format("contempt {0:F1}%, ", emotionScores.Contempt * 100));
            if (emotionScores.Disgust >= 0.1f) sb.Append(String.Format("disgust {0:F1}%, ", emotionScores.Disgust * 100));
            if (emotionScores.Fear >= 0.1f) sb.Append(String.Format("fear {0:F1}%, ", emotionScores.Fear * 100));
            if (emotionScores.Happiness >= 0.1f) sb.Append(String.Format("happiness {0:F1}%, ", emotionScores.Happiness * 100));
            if (emotionScores.Neutral >= 0.1f) sb.Append(String.Format("neutral {0:F1}%, ", emotionScores.Neutral * 100));
            if (emotionScores.Sadness >= 0.1f) sb.Append(String.Format("sadness {0:F1}%, ", emotionScores.Sadness * 100));
            if (emotionScores.Surprise >= 0.1f) sb.Append(String.Format("surprise {0:F1}%, ", emotionScores.Surprise * 100));

            // Add glasses.
            sb.Append(face.FaceAttributes.Glasses);
            sb.Append(", ");

            // Add hair.
            sb.Append("Hair: ");

            // Display baldness confidence if over 1%.
            if (face.FaceAttributes.Hair.Bald >= 0.01f)
                sb.Append(String.Format("bald {0:F1}% ", face.FaceAttributes.Hair.Bald * 100));

            // Display all hair color attributes over 10%.
            HairColor[] hairColors = face.FaceAttributes.Hair.HairColor;
            foreach (HairColor hairColor in hairColors)
            {
                if (hairColor.Confidence >= 0.1f)
                {
                    sb.Append(hairColor.Color.ToString());
                    sb.Append(String.Format(" {0:F1}% ", hairColor.Confidence * 100));
                }
            }

            // Return the built string.
            return sb.ToString();
        }

        private async Task<Face[]> UploadAndDetectFaces(string imageFilePath)
        {
            // The list of Face attributes to return.
            IEnumerable<FaceAttributeType> faceAttributes =
                new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Emotion, FaceAttributeType.Glasses, FaceAttributeType.Hair };

            // Call the Face API.
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    Face[] faces = await faceServiceClient.DetectAsync(imageFileStream, returnFaceId: true, returnFaceLandmarks: false, returnFaceAttributes: faceAttributes);
                    return faces;
                }
            }
            // Catch and display Face API errors.
            catch (FaceAPIException f)
            {
                MessageBox.Show(f.ErrorMessage, f.ErrorCode);
                return new Face[0];
            }
            // Catch and display all other errors.
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
                return new Face[0];
            }
        }

        //private void FaceImage_MouseMove(object sender, MouseEventArgs e)
        //{
        //    // If the REST call has not completed, return from this method.
        //    if (faces == null)
        //        return;

        //    // Find the mouse position relative to the image.
        //    Point mouseXY = e.GetPosition(FaceImage);

        //    FaceImage.Source = DrawScore(logo, faces);


        //    ImageSource imageSource = FaceImage.Source;
        //    BitmapSource bitmapSource = (BitmapSource)imageSource;

        //    // Scale adjustment between the actual size and displayed size.
        //    var scale = FaceImage.ActualWidth / (bitmapSource.PixelWidth / resizeFactor);

        //    // Check if this mouse position is over a face rectangle.
        //    bool mouseOverFace = false;

        //    for (int i = 0; i < faces.Length; ++i)
        //    {
        //        FaceRectangle fr = faces[i].FaceRectangle;
        //        double left = fr.Left * scale;
        //        double top = fr.Top * scale;
        //        double width = fr.Width * scale;
        //        double height = fr.Height * scale;

        //        // Display the face description for this face if the mouse is over this face rectangle.
        //        if (mouseXY.X >= left && mouseXY.X <= left + width && mouseXY.Y >= top && mouseXY.Y <= top + height)
        //        {
        //            faceDescriptionStatusBar.Text = faceDescriptions[i];
        //            mouseOverFace = true;
        //            break;
        //        }
        //    }

        //    // If the mouse is not over a face rectangle.
        //    if (!mouseOverFace)
        //        faceDescriptionStatusBar.Text = "Coloque el puntero sobre la cara para la descripción.";
        //}

        public void StartTimer(int seconds)
        {
            Action ShowTimer = () =>
            {
                this.Dispatcher.Invoke(() => ShowChanges());
            };
            timer = new TimerText(seconds, ShowTimer);
            timer.Start();
        }

        public void ShowChanges()
        {
            FaceImage.Source = Visualization.DrawTime(logo, timer.contador.ToString());
            if (timer.contador == 0)
            {
                FaceImage.Source = Visualization.DrawScore(logo, faces);
            }
        }

        private async void StartGame_Click(object sender, RoutedEventArgs e)
        {
            // Detect any faces in the image.
            Title = "Detectando...";
            faces = await UploadAndDetectFaces(rutaCompleta);
            Title = String.Format("{0} Cara(s) detectadas.", faces.Length);
            StartTimer(10);

            if (faces.Length > 0)
            {
                // Prepare to draw rectangles around the faces.
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(logo,
                    new Rect(0, 0, logo.Width, logo.Height));


                double dpi = logo.DpiX;
                resizeFactor = 96 / dpi;
                faceDescriptions = new String[faces.Length];

                for (int i = 0; i < faces.Length; ++i)
                {
                    Face face = faces[i];

                    // Draw a rectangle on the face.
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 2),
                        new Rect(
                            face.FaceRectangle.Left * resizeFactor,
                            face.FaceRectangle.Top * resizeFactor,
                            face.FaceRectangle.Width * resizeFactor,
                            face.FaceRectangle.Height * resizeFactor
                            )
                    );

                    //drawingContext.DrawRoundedRectangle(Brushes.Transparent,
                    //                                    new Pen(Brushes.Red, 2),
                    //                                    new Rect(
                    //                                    face.FaceRectangle.Left * resizeFactor,
                    //                                    face.FaceRectangle.Top * resizeFactor,
                    //                                    face.FaceRectangle.Width * resizeFactor,
                    //                                    face.FaceRectangle.Height * resizeFactor
                    //                                    ),
                    //                                    150,
                    //                                    150);

                    // Store the face description.
                    faceDescriptions[i] = FaceDescription(face);
                }

                drawingContext.Close();

                // Display the image with the rectangle around the face.
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(logo.PixelWidth * resizeFactor),
                    (int)(logo.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);
                FaceImage.Source = faceWithRectBitmap;

                //// Set the status bar text.
                //faceDescriptionStatusBar.Text = "Coloque el puntero sobre la cara para la descripción.";
            }
        }
    }
}

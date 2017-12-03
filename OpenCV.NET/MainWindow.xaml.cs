using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using cv = OpenCvSharp;
using System.Threading;

namespace OpenCV.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static BitmapImage eye_l = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\eye_l.png"));
        public static BitmapImage eye_r = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\eye_r.png"));
        private static object TLock = new object();

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            new Thread(() =>
            {
                var eye_casc = new cv.CascadeClassifier("eye.xml");
                var face_casc = new cv.CascadeClassifier("fface_default.xml");

                var cap = new cv.VideoCapture(0);

                while (true)
                {
                    var img = new cv.Mat();
                    cap.Read(img);
                    var gray = img.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
                    var faces = face_casc.DetectMultiScale(gray, 1.3, 5);

                    foreach (var face in faces)
                    {
                        var rect = new cv.Rect(face.Location, face.Size);
                        img.Rectangle(rect, new cv.Scalar(255, 0, 0));
                        var sub_ing = gray[rect];
                        var sub_ing_rgb = img[rect];
                        var eyes = eye_casc.DetectMultiScale(sub_ing);
                        foreach (var eye in eyes)
                        {
                            var rect_eye = new cv.Rect(eye.Location, eye.Size);
                            //sub_ing_rgb.Rectangle(rect_eye, new cv.Scalar(0, 255, 0));

                            lock(TLock)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    var data = new cv.Mat(
                                        "eye_l.png",
                                        OpenCvSharp.ImreadModes.Color);
                                    data.CopyTo(sub_ing_rgb[rect_eye]);
                                });
                            }
                        }
                    }
                    Dispatcher.Invoke(() =>
                    {
                        OutImg.Source = cv.Extensions.BitmapSourceConverter.ToBitmapSource(img);
                    });
                }

                //cap.Release();
            })
            {
                IsBackground = true
            }.Start();
        }
    }
}

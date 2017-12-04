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
using System.Text.RegularExpressions;

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
        protected cv.VideoCapture cap = null;
        private bool released = false;
        private List<Item> _items = null;
        private static int scale_w = 20;
        private static int scale_h = 0;

        public MainWindow()
        {
            InitializeComponent();
            InitList();
            Init();
        }

        private void Init()
        {
            new Thread(() =>
            {
                var eye_casc = new cv.CascadeClassifier("eye.xml");
                var left_eye_casc = new cv.CascadeClassifier("left_eye.xml");
                var right_eye_casc = new cv.CascadeClassifier("right_eye.xml");
                var face_casc = new cv.CascadeClassifier("fface_default.xml");

                cap = new cv.VideoCapture(0);

                while (true)
                {
                    if(released)
                    {
                        break;
                    }
                    var img = new cv.Mat();
                    cap.Read(img);
                    var gray = img.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
                    var gaus = gray.AdaptiveThreshold(255, cv.AdaptiveThresholdTypes.GaussianC, cv.ThresholdTypes.Binary, 115, 1);
                    img = gaus;
                    var faces = face_casc.DetectMultiScale(gray, 1.3, 5);
                    RenderTargetBitmap eyes_lay = null;

                    foreach (var face in faces)
                    {
                        var rect = new cv.Rect(face.Location, face.Size);
                        //img.Rectangle(rect, new cv.Scalar(255, 0, 0));

                        var sub_ing = gray[rect];
                        var sub_ing_rgb = img[rect];

                        //left eye
                        var eyes = eye_casc.DetectMultiScale(sub_ing, 1.3, 2);
                        int count = 0;
                        foreach (var eye in eyes)
                        {
                            count++;
                            if(count > 2)
                            {
                                count = 0;
                                break;
                            }
                            var rect_eye = new cv.Rect(eye.Location, eye.Size);

                            if (eye.X + eye.Width < face.Width / 2)
                            {
                                //sub_ing_rgb.Rectangle(rect_eye, new cv.Scalar(0, 255, 0));
                                Dispatcher.Invoke(() =>
                                {
                                    eyes_lay = DrawImg(cv.Extensions.BitmapSourceConverter.ToBitmapSource(img), eye.X + face.X, eye.Y + face.Y, eye.Width, eye.Height, eye_l, scale_w, scale_h);
                                });
                            }
                        }

                        //left eye
                        count = 0;
                        foreach (var eye in eyes)
                        {
                            count++;
                            if (count > 2)
                            {
                                break;
                            }
                            var rect_eye = new cv.Rect(eye.Location, eye.Size);

                            if (eye.X + eye.Width > face.Width / 2)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    if(eyes_lay != null)
                                    {
                                        eyes_lay = DrawImg(eyes_lay, eye.X + face.X, eye.Y + face.Y, eye.Width, eye.Height, eye_r, scale_w, scale_h);
                                    }
                                    else
                                    {
                                        eyes_lay = DrawImg(cv.Extensions.BitmapSourceConverter.ToBitmapSource(img), eye.X + face.X, eye.Y + face.Y, eye.Width, eye.Height, eye_r, scale_w, scale_h);
                                    }
                                });
                            }
                        }

                    }



                    Dispatcher.Invoke(() =>
                    {
                        if(eyes_lay != null)
                        {
                            OutImg.Source = eyes_lay;
                        }
                        else
                        {
                            OutImg.Source = cv.Extensions.BitmapSourceConverter.ToBitmapSource(img);
                        }
                    });
                    //Thread.Sleep(100);
                    GC.Collect();
                }



            })
            {
                IsBackground = true
            }.Start();
        }

        private void InitList()
        {
            _items = new List<Item>();
            EyesList.ItemsSource = _items;
            var dir = Directory.GetCurrentDirectory() + "\\eyes_img_src";
            foreach (var v in Directory.GetDirectories(dir))
            {
                _items.Add(new Item()
                {
                    Prev = new BitmapImage(new Uri(v + "\\prev.jpg")),
                    Folder = v
                });
            }
        }

        private RenderTargetBitmap DrawImg(BitmapSource bg, int x, int y, int w, int h, BitmapSource im, int scale_w = 0, int scale_h = 0)
        {
            w += scale_w;
            h += scale_h;
            x -= scale_w / 2;
            y -= scale_h / 2;
            BitmapSource p = bg;
            var target = new RenderTargetBitmap(p.PixelWidth, p.PixelHeight,
                p.DpiX, p.DpiY, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();

            using (var r = visual.RenderOpen())
            {
                r.DrawImage(p, new Rect(0, 0, p.Width, p.Height));
                r.DrawImage(im, new Rect(x, y, w, h));
            }
            target.Render(visual);
            return target;
        }

        private RenderTargetBitmap DrawImg(RenderTargetBitmap bg, int x, int y, int w, int h, BitmapSource im, int scale_w = 0, int scale_h = 0)
        {
            w += scale_w;
            h += scale_h;
            x -= scale_w / 2;
            y -= scale_h / 2;
            var target = bg;
            var visual = new DrawingVisual();

            using (var r = visual.RenderOpen())
            {
                r.DrawImage(im, new Rect(x, y, w, h));
            }
            target.Render(visual);
            return target;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            released = true;
            cap.Release();
            cap.Dispose();
        }

        private void MakePhoto_Click(object sender, RoutedEventArgs e)
        {
            var photo = (BitmapSource)OutImg.Source;
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(photo));
            if(!Directory.Exists("PHOTOS"))
            {
                Directory.CreateDirectory("PHOTOS");
            }
            using (FileStream f = new FileStream("PHOTOS/photo" + 
                DateTime.Now.ToShortDateString() +
                "." + DateTime.Now.Hour +
                "." + DateTime.Now.Minute +
                "." + DateTime.Now.Second +
                ".png", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                encoder.Save(f);
            }
        }

        private void EyesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            eye_l = new BitmapImage(new Uri(_items[EyesList.SelectedIndex].Folder + "\\eye_l.png"));
            eye_r = new BitmapImage(new Uri(_items[EyesList.SelectedIndex].Folder + "\\eye_r.png"));
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                scale_w = int.Parse(ScaleW.Text);
            }
            catch { }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            try
            {
                scale_h = int.Parse(ScaleH.Text);
            }
            catch { }
        }
    }

    public class Item
    {
        public BitmapSource Prev { get; set; }
        public string Folder { get; set; }
    }
}

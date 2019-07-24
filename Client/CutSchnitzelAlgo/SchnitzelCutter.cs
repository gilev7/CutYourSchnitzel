using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace CutSchnitzelAlgo
{
    public static class SchnitzelCutter
    {
        ///storage/emulated/0/Android/data/Camera2Basic.Camera2Basic/files/
        //[DllImport("cvextern.dll", CharSet = CharSet.Unicode)]
        //public static extern Mat Imread(string path, ImreadModes mode);
        public static void CutSchnitzelImage(string folderPath, IEnumerable<Tuple<string, double>> input = null)
        {
            var path = folderPath + "pic.jpg";
            try
            {
                var mat = CvInvoke.Imread(path, ImreadModes.AnyColor);
                CvInvoke.CvtColor(mat, mat, ColorConversion.Bgr2Rgb);
                CvInvoke.CvtColor(mat, mat, ColorConversion.Rgb2Hsv);
                Image<Hsv, Byte> image = mat.ToImage<Hsv, Byte>();
                using (Image<Hsv, Byte> hsv = image.Convert<Hsv, Byte>())
                {
                    Image<Gray, Byte>[] channels = hsv.Split();
                    try
                    {
                        var botLimit = new ScalarArray(new MCvScalar(10, 125, 75));
                        var uprLimit = new ScalarArray(new MCvScalar(15, 255, 255));

                        var contours = new VectorOfVectorOfPoint();
                        Image<Hsv, byte> imageHsvDest = new Image<Hsv, byte>(image.Width, image.Width);
                        CvInvoke.InRange(mat, botLimit, uprLimit, imageHsvDest);
                        CvInvoke.FindContours(imageHsvDest, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                        var arrayList = new List<ContourArea>(contours.Size);
                        for (int i = 0; i < contours.Size; i++)
                        {
                            var contour = contours[i];
                            arrayList.Add(new ContourArea(CvInvoke.ContourArea(contour), contour));
                        }

                        var color = new MCvScalar(255, 0, 0);
                        var biggestContour = arrayList.OrderByDescending(x => x.Area).FirstOrDefault();

                        if (biggestContour == null || biggestContour.Area < 1000)
                        {
                            //This is not a schnitzel
                        }

                        var mask = Mat.Zeros(imageHsvDest.Rows, imageHsvDest.Cols, DepthType.Cv8U, 3);
                        CvInvoke.DrawContours(mask, new VectorOfVectorOfPoint(biggestContour.Contour), 0, color, -1);
                        var newPicPath = SchnitzelCutter.DividePicture(path, mask, biggestContour.Area, input.ToList());
                        //return newPicPath;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    finally
                    {
                        channels[0].Dispose();
                        channels[1].Dispose();
                        channels[2].Dispose();
                    }
                }
            }
            catch (Exception)
            {
                // do nothing
            }
        }

        private static string DividePicture(string path, Mat mask, double area, List<Tuple<string, double>> input)
        {
            if (input == null)
            {
                input = new List<Tuple<string, double>>()
                {
                    new Tuple<string, double>(string.Empty, 0.5),
                    new Tuple<string, double>(string.Empty, 0.5),
                };
            }

            var mat = CvInvoke.Imread(path, ImreadModes.AnyColor);
            CvInvoke.CvtColor(mat, mat, ColorConversion.Bgr2Rgb);
            Image<Hsv, Byte> image = mask.ToImage<Hsv, Byte>();
            var points = image.Data;
            int sum = 0;
            int cuttingX = 0;
            double requiredAreaPart = 0;
            var lines = new List<Tuple<string, Point, Point>>();
            input.RemoveAt(input.Count() - 1);
            foreach (var pair in input)
            {
                requiredAreaPart += pair.Item2;
                for (int x = 0; x < image.Rows; x++)
                {
                    int minY = 0;
                    int maxY = 0;

                    for (int y = 0; y < image.Cols; y++)
                    {

                        if (points[x, y, 0] > 0 || points[x, y, 1] > 0 || points[x, y, 2] > 0)
                        {
                            if (minY == 0 || y < minY)
                            {
                                minY = y;
                            }

                            if (y > maxY)
                            {
                                maxY = y;
                            }
                            if (sum++ > (int)area * requiredAreaPart)
                            {
                                cuttingX = x;
                            }
                        }
                    }

                    if (cuttingX > 0)
                    {
                        lines.Add(new Tuple<string, Point, Point>(pair.Item1, new Point(minY, cuttingX), new Point(maxY, cuttingX)));
                        cuttingX = 0;
                        sum = 0;
                        break;
                    }

                }
            }

            var color = new MCvScalar(0, 255, 0);
            foreach (var line in lines)
            {
                CvInvoke.Line(mat, line.Item2, line.Item3, color, 10);
            }
            
            var newImage = mat.ToImage<Rgb, Byte>();
            var fileName = "/storage/emulated/0/Android/data/Camera2Basic.Camera2Basic/files/pic.jpg";
            newImage.ToBitmap().Save(fileName);
             return fileName;
        }
    }

    internal class ContourArea
    {
        public ContourArea(double area, VectorOfPoint contour)
        {
            Area = area;
            Contour = contour;
        }

        public double Area { get; }
        public VectorOfPoint Contour { get; }
    }
}

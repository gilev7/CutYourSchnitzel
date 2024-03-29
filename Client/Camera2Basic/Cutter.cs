﻿extern alias draw;
using Point = draw::System.Drawing.Point;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using CvInvoke = Emgu.CV.CvInvoke;
using System.Runtime.InteropServices;

namespace Camera2Basic
{
    public static class Cutter
    {
        ///storage/emulated/0/Android/data/Camera2Basic.Camera2Basic/files/
        [DllImport("cvextern.dll")]
        public static extern Mat Imread(string path, ImreadModes mode);
        public static void CutSchnitzelImage(string folderPath, IEnumerable<Tuple<string, double>> input = null)
        {
            var path = folderPath + "pic.jpg";
            try
            {
                var mat = CvInvoke.Imread(path, ImreadModes.AnyColor);
                CvInvoke.CvtColor(mat, mat, ColorConversion.Bgr2Rgb);
                CvInvoke.CvtColor(mat, mat, ColorConversion.Rgb2Hsv);
                Image<Bgr, Byte> image = mat.ToImage<Bgr, Byte>();
                var test = image.Convert<Hsv, Byte>();
                Image<Gray, Byte>[] channels = test.Split();
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
                    var newPicPath = DividePicture(path, mask, biggestContour.Area, input);
                    //return newPicPath;
                }
                finally
                {
                    channels[0].Dispose();
                    channels[1].Dispose();
                    channels[2].Dispose();
                }
                
            }
            catch (Exception e)
            {
                // do nothing
            }
        }

        private static string DividePicture(string path, Mat mask, double area, IEnumerable<Tuple<string, double>> input)
        {
            var mat = CvInvoke.Imread(path, ImreadModes.AnyColor);
            CvInvoke.CvtColor(mat, mat, ColorConversion.Bgr2Rgb);
            Image<Hsv, Byte> image = mask.ToImage<Hsv, Byte>();
            var points = image.Data;
            int sum = 0;
            int cuttingRow = 0;
            int minCol = 0;
            int maxCol = 0;
            for (int row = 0; row < image.Rows; row++)
            {
                for (int col = 0; col < image.Cols; col++)
                {
                    if (points[row, col, 0] > 0 || points[row, col, 1] > 0 || points[row, col, 2] > 0)
                    {
                        if (minCol == 0 || col < minCol)
                        {
                            minCol = col;
                        }

                        if (col > maxCol)
                        {
                            maxCol = col;
                        }
                        if (sum++ > (int)area / 2)
                        {
                            cuttingRow = row;

                        }
                    }
                }

                if (cuttingRow > 0)
                {
                    break;
                }

                minCol = 0;
                maxCol = 0;
            }

            var color = new MCvScalar(0, 255, 0);
            CvInvoke.Line(mat, new Point(minCol, cuttingRow), new Point(maxCol, cuttingRow), color, 10);
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

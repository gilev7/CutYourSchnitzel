using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CutSchnitzelAlgo
{
    public static class SchnitzelCutter
    {
        public static string CutSchnitzelImage(string name, IEnumerable<Tuple<string, double>> input = null)
        {
            if (input == null)
            {
                input = new List<Tuple<string, double>>()
                {
                    new Tuple<string, double>(string.Empty, 0.5),
                    new Tuple<string, double>(string.Empty, 0.5),
                };
            }

            Bitmap bitMap = DownloadFromStorage(name);
            var newName = "modified-" + name;

            try
            {
                var imageCV = new Image<Bgr, byte>(bitMap);
                Mat mat = imageCV.Mat;

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

                        if (biggestContour == null || biggestContour.Contour == null || biggestContour.Area < 100000)
                        {
                            try
                            {
                                StringBuilder imageComments = new StringBuilder();
                                var imageCV2 = new Image<Bgr, byte>(bitMap);
                                var thisIsNotSchnitzelMat = imageCV2.Mat;
                                CvInvoke.CvtColor(thisIsNotSchnitzelMat, thisIsNotSchnitzelMat, ColorConversion.Bgr2Rgb);
                                CvInvoke.PutText(
                                   thisIsNotSchnitzelMat,
                                   "This is not a Schnitzel",
                                   new System.Drawing.Point(thisIsNotSchnitzelMat.Cols / 2, thisIsNotSchnitzelMat.Rows / 2),
                                   FontFace.HersheyPlain,
                                   2.0,
                                   new Rgb(0, 0, 255).MCvScalar, 3, LineType.Filled);

                                var newImage = thisIsNotSchnitzelMat.ToImage<Bgr, Byte>();
                                //var fileName = @"c:\temp\thisIsNotSchnizel.png";
                                //newImage.ToBitmap().Save(fileName);
                                UploadToStorage(newImage.ToJpegData(), newName);
                                return newName;
                            }
                            catch (Exception e)
                            {
                                return "error3: " + e.Message + "\n" + e.StackTrace;
                            }
                        }

                        var mask = Mat.Zeros(imageHsvDest.Rows, imageHsvDest.Cols, DepthType.Cv8U, 3);
                        CvInvoke.DrawContours(mask, new VectorOfVectorOfPoint(biggestContour.Contour), 0, color, -1);
                        var newByteImage = SchnitzelCutter.DividePicture(bitMap, mask, biggestContour.Area, input.ToList());
                        UploadToStorage(newByteImage, newName);
                        return newName;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return "error1: " + e.Message +"\n" +  e.StackTrace;
                    }
                    finally
                    {
                        channels[0].Dispose();
                        channels[1].Dispose();
                        channels[2].Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                return "error2: " + e.Message;
                // do nothing
            }
        }

        private static byte[] DividePicture(Bitmap bitMap, Mat mask, double area, List<Tuple<string, double>> input)
        {
            var imageCV = new Image<Bgr, byte>(bitMap);
            var mat = imageCV.Mat;
            //CvInvoke.CvtColor(mat, mat, ColorConversion.Bgr2Rgb);
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
            
            var newImage = mat.ToImage<Bgr, Byte>();
            //var fileName = "/storage/emulated/0/Android/data/Camera2Basic.Camera2Basic/files/pic.jpg";
            //newImage.ToBitmap().Save(fileName);
            return newImage.ToJpegData();
        }

        private static Bitmap DownloadFromStorage(string name)
        {
            var account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=t7qkustorengingiladtest2;AccountKey=/b8KfdN1R+mwaM4ixTfJcrL2erCc2XSTCYNk0ka7M0vSv/bmiFXmdJP+v7dNghB/JdRwXdJB6rSVWsITma+eXQ==;EndpointSuffix=core.windows.net");
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference("hackathon");
            container.CreateIfNotExistsAsync().GetAwaiter().GetResult();
            var blockBlob = container.GetBlockBlobReference(name);

            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStream(memoryStream);

                return (memoryStream == null) ? null : (Bitmap)Image.FromStream(memoryStream);
            }
        }

        public static string UploadToStorage(byte[] data, string name)
        {
            var account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=t7qkustorengingiladtest2;AccountKey=/b8KfdN1R+mwaM4ixTfJcrL2erCc2XSTCYNk0ka7M0vSv/bmiFXmdJP+v7dNghB/JdRwXdJB6rSVWsITma+eXQ==;EndpointSuffix=core.windows.net");
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference("hackathon");
            container.CreateIfNotExistsAsync().GetAwaiter().GetResult();
            var blockBlob = container.GetBlockBlobReference(name);
            
            using (var stream = new MemoryStream(data, writable: false))
            {
                blockBlob.UploadFromStream(stream);
            }

            return name;
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

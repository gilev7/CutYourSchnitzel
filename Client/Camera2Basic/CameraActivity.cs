using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Widget;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Camera2Basic
{
    [Activity (Label = "CutYourSchnitzel", MainLauncher = true, Icon = "@drawable/icon")]
	public class CameraActivity : Activity
	{
        private SeekBar m_Seekbar;
        protected override void OnResume()
        {
            SingletonConnector.cameraActivity = this;
            base.OnResume();
            ActionBar.Hide();
            SetContentView(Resource.Layout.activity_camera);

            FragmentManager.BeginTransaction().Replace(Resource.Id.container, Camera2BasicFragment.NewInstance()).Commit();            
        }


        protected override void OnCreate(Bundle bundle)
        {
            SingletonConnector.cameraActivity = this;

            base.OnCreate(bundle);
            ActionBar.Hide();
            SetContentView(Resource.Layout.activity_camera);

            if (bundle == null)
            {
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, Camera2BasicFragment.NewInstance()).Commit();
            }
        }

        public void SetSeekbar()
        {
            m_Seekbar = FindViewById<SeekBar>(Resource.Id.seekBarId);
            var textView = FindViewById<TextView>(Resource.Id.textSeekBarView);
            m_Seekbar.ProgressChanged += (object sender, SeekBar.ProgressChangedEventArgs e) =>
            {
                if (e.FromUser)
                {
                    textView.Text = string.Format("Schnitzel Cut Percent is {0}", e.Progress);
                }
            };

        }

        public void ChangeToImageView()
        {
            var percent = m_Seekbar.Progress;
            string name = SendLocalMediaToDatabase(percent);

            //HandlePicture();
            var intent = new Intent(this, typeof(ImageActivity));
            intent.PutExtra("imageName", name);

            StartActivity(intent);
        }

        private string SendLocalMediaToDatabase(int percent)
        {
            var filepath = "/storage/emulated/0/Android/data/Camera2Basic.Camera2Basic/files/pic.jpg";
            // for python
            //var url = "https://cutyourschnitzel.azurewebsites.net/";
            // for c#
            //var url = "http://schnitzelapp.azurewebsites.net/api/values";


            // provide read access to the file
            FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            // Create a byte array of file stream length
            byte[] ImageData = new byte[fs.Length];
            //Read block of bytes from stream into the byte array
            fs.Read(ImageData, 0, System.Convert.ToInt32(fs.Length));
            //Close the File Stream
            fs.Close();
            string _base64String = Convert.ToBase64String(ImageData);

            
            using (var client = new HttpClient())
            {
                Bitmap objBitmapImage = BitmapFactory.DecodeFile(filepath);

               //var mBitMap = MediaStore.Images.Media.GetBitmap(ContentResolver, Android.Net.Uri.Parse(filepath));
                byte[] bitmapData;
                using (var stream = new MemoryStream())
                {
                    objBitmapImage.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                    bitmapData = stream.ToArray();
                }
                var inputStream = new MemoryStream(bitmapData);

                var name = uploadToStorage(inputStream);

                StringContent content = new StringContent("=" + name, Encoding.UTF8, "application/x-www-form-urlencoded");
                client.BaseAddress = new Uri("http://schnitzelapp.azurewebsites.net");
                var result = client.PostAsync("/api/values", content).Result;
                string resultContent = result.Content.ReadAsStringAsync().Result;
                var correctString = resultContent.Substring(1, resultContent.Length - 2);

                downloadFromStorage("/storage/emulated/0/Android/data/Camera2Basic.Camera2Basic/files/" + correctString, correctString);

                return correctString;
                /**
                var result = client.PostAsync("/api/values", content).Result;
                string resultContent = result.Content.ReadAsStringAsync().Result;

                var correctString = resultContent.Substring(1, resultContent.Length - 2);
                try
                {
                    byte[] data = Convert.FromBase64String(correctString);
                    File.WriteAllBytes(filepath, data);
                }
                catch (Exception e)
                {
                    // do nothing
                }
                **/
            }

        }
        public void downloadFromStorage(string path, string name)
        {
            var account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=t7qkustorengingiladtest2;AccountKey=/b8KfdN1R+mwaM4ixTfJcrL2erCc2XSTCYNk0ka7M0vSv/bmiFXmdJP+v7dNghB/JdRwXdJB6rSVWsITma+eXQ==;EndpointSuffix=core.windows.net");
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference("hackathon");
            container.CreateIfNotExistsAsync().GetAwaiter().GetResult();
            var blockBlob = container.GetBlockBlobReference(name);
            blockBlob.DownloadToFileAsync(path, FileMode.OpenOrCreate).GetAwaiter().GetResult();
        }

        public string uploadToStorage(Stream stream)
        {
            var account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=t7qkustorengingiladtest2;AccountKey=/b8KfdN1R+mwaM4ixTfJcrL2erCc2XSTCYNk0ka7M0vSv/bmiFXmdJP+v7dNghB/JdRwXdJB6rSVWsITma+eXQ==;EndpointSuffix=core.windows.net");
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference("hackathon");
            container.CreateIfNotExistsAsync().GetAwaiter().GetResult();
            var name = Guid.NewGuid().ToString();
            var name2 = $"{name}.jpg";
            var blockBlob = container.GetBlockBlobReference(name2);
            blockBlob.UploadFromStreamAsync(stream).GetAwaiter().GetResult();
            return name2;
        }

        public void HandlePicture()
        {
            var filepath = "/storage/emulated/0/Android/data/Camera2Basic.Camera2Basic/files/";
            Cutter.CutSchnitzelImage(filepath);
        }
    }

    public class WebClientEx : WebClient
    {
        public int Timeout { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            request.Timeout = Timeout;
            return request;
        }
    }

    public static class SingletonConnector
    {
        public static CameraActivity cameraActivity;
    }
}



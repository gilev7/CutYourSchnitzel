using Android.App;
using Android.OS;
using Android.Widget;
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
            SendLocalMediaToDatabase(percent);
            
            //HandlePicture();
            StartActivity(typeof(ImageActivity));
        }

        private async void SendLocalMediaToDatabase(int percent)
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
            {//
                StringContent content = new StringContent("gilad", Encoding.ASCII, "application/x-www-form-urlencoded");
                client.BaseAddress = new Uri("http://schnitzelapp.azurewebsites.net");

                var result = client.PostAsync("/api/values", content).Result;
                string resultContent = result.Content.ReadAsStringAsync().Result;

                byte[] data = Convert.FromBase64String(resultContent);
                File.WriteAllBytes(filepath, data);
            }
            
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



using Android.App;
using Android.OS;
using Android.Widget;
using Java.IO;
using System;
using System.Net;

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
            //SendLocalMediaToDatabase();
            //var percent = m_Seekbar.Progress;
            HandlePicture();
            StartActivity(typeof(ImageActivity));
        }

        private async void SendLocalMediaToDatabase()
        {
            var filepath = "/storage/emulated/0/Android/data/Camera2Basic.Camera2Basic/files/pic.jpg";
            var url = "https://cutyourschnitzel.azurewebsites.net/";
            var webClient = new WebClientEx();
            var boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
            webClient.Headers.Add("Content-Type", "application/form-data; boundary=" + boundary);

            webClient.Timeout = 900000;

            byte[] resp = await webClient.UploadFileTaskAsync(url, filepath);

            using (var fileOutputStream = new FileOutputStream(filepath))
            {
                await fileOutputStream.WriteAsync(resp);
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



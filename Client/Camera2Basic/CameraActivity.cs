using Android;
using Android.App;
using Android.OS;
using Android.Widget;
using System;
using System.IO;
using System.Net;

namespace Camera2Basic
{
    [Activity (Label = "CutYourSchnitzel", MainLauncher = true, Icon = "@drawable/icon")]
	public class CameraActivity : Activity
	{
        private SeekBar m_Seekbar;
        protected override void OnResume()
        {
            base.OnResume();
            ActionBar.Hide();
            SetContentView(Resource.Layout.activity_camera);

            FragmentManager.BeginTransaction().Replace(Resource.Id.container, Camera2BasicFragment.NewInstance(this)).Commit();            
        }


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ActionBar.Hide();
            SetContentView(Resource.Layout.activity_camera);

            if (bundle == null)
            {
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, Camera2BasicFragment.NewInstance(this)).Commit();
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
            SendLocalMediaToDatabase();
            //var percent = m_Seekbar.Progress;
            StartActivity(typeof(ImageActivity));
        }

        private bool SendLocalMediaToDatabase()
        {
            var filepath = "/storage/emulated/0/Android/data/Camera2Basic.Camera2Basic/files/pic.jpg";
            var url = "http://10.93.58.44:5000/";
            byte[] file = File.ReadAllBytes(filepath);
            var webClient = new WebClient();
            var boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
            webClient.Headers.Add("Content-Type", "application/form-data; boundary=" + boundary);
            var contentType = "img/jpeg";
            var fileData = webClient.Encoding.GetString(file);
            var fileName = Path.GetFileNameWithoutExtension(filepath);
            var package = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n{3}\r\n--{0}--\r\n", boundary, fileName, contentType, fileData);
            var nfile = webClient.Encoding.GetBytes(package);

            byte[] resp = webClient.UploadData(url, "POST", nfile);


            return true;
        }

        public void HandlePicture()
        {

        }
    }
}



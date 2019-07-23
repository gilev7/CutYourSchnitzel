
using Android.App;
using Android.OS;
using Android.Widget;

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
        

        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			ActionBar.Hide ();
			SetContentView (Resource.Layout.activity_camera);

            m_Seekbar = FindViewById<SeekBar>(Resource.Id.seekBarId);
            //var textView = FindViewById<TextView>(Resource.Id.textSeekBar);
            //m_Seekbar.ProgressChanged += (object sender, SeekBar.ProgressChangedEventArgs e) => {
            //    if (e.FromUser)
            //    {
            //textView.Text = string.Format("Schnitzel Cut Percent is {0}", e.Progress);
            //}
            //};

            if (bundle == null) {
				FragmentManager.BeginTransaction ().Replace (Resource.Id.container, Camera2BasicFragment.NewInstance (this)).Commit ();
			}
        }

        public void ChangeToImageView()
        {
            //var percent = m_Seekbar.Progress;
            StartActivity(typeof(ImageActivity));
        }

        public void HandlePicture()
        {

        }
    }
}



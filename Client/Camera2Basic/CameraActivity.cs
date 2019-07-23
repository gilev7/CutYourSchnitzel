
using Android.App;
using Android.OS;

namespace Camera2Basic
{
    [Activity (Label = "Camera2Basic", MainLauncher = true, Icon = "@drawable/icon")]
	public class CameraActivity : Activity
	{
        
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

			if (bundle == null) {
				FragmentManager.BeginTransaction ().Replace (Resource.Id.container, Camera2BasicFragment.NewInstance (this)).Commit ();
			}
        }

        public void changeToImageView()
        {
            StartActivity(typeof(ImageActivity));
        }
    }
}



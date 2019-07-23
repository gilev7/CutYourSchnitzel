using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;

namespace Camera2Basic
{
    [Activity (Label = "Schnitzel Time", Icon = "@drawable/icon")]
	public class ImageActivity : Activity , View.IOnClickListener
    {
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
            initializeView();
        }
        protected override void OnStart()
        {
            base.OnStart();
            initializeView();
        }

        protected override void OnResume()
        {
            base.OnResume();
            initializeView();
        }

        public void initializeView()
        {
            
            ActionBar.Hide();
            SetContentView(Resource.Layout.image_view);

            var filepath = "/storage/emulated/0/Android/data/Camera2Basic.Camera2Basic/files/pic.jpg";
            Android.Net.Uri uri = Android.Net.Uri.FromFile(new Java.IO.File(filepath));

            var image = FindViewById<ImageView>(Resource.Id.pictureTaken);
            image.Click += delegate
            {
                changeToCameraView();
            };

            image.SetImageURI(uri);
        }

        public void OnClick(View v)
        {
            if (v.Id == Resource.Id.pictureTaken)
            {
                changeToCameraView();
            }
            else
            {

                EventHandler<DialogClickEventArgs> nullHandler = null;

                new AlertDialog.Builder(this)
                    .SetMessage("This sample demonstrates the basic use of the Camera2 API. ...")
                    .SetPositiveButton(Android.Resource.String.Ok, nullHandler)
                    .Show();
            }
        }

        public void changeToCameraView()
        {
            Finish();
        }
    }
}



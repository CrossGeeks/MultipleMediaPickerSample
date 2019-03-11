using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Database;
using Android.Provider;
using Plugin.CurrentActivity;
using Android.Content;
using Plugin.Permissions;
using MultiMediaPickerSample.Droid.Services;
using DLToolkit.Forms.Controls;
using FFImageLoading.Forms.Droid;

namespace MultiMediaPickerSample.Droid
{
    [Activity(Label = "MultiMediaPickerSample", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
            base.OnCreate(savedInstanceState);

            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(enableFastRenderer:true);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            LoadApplication(new App(MultiMediaPickerService.SharedInstance));
         
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            MultiMediaPickerService.SharedInstance.OnActivityResult(requestCode, resultCode, data);
        }

      
    }
}
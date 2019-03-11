using System;
using DLToolkit.Forms.Controls;
using MultiMediaPickerSample.Services;
using MultiMediaPickerSample.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace MultiMediaPickerSample
{
    public partial class App : Application
    {
        public App(IMultiMediaPickerService multiMediaPickerService)
        {
            InitializeComponent();
            FlowListView.Init();
            MainPage = new MainPage()
            {
                BindingContext = new MainViewModel(multiMediaPickerService)
            };
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}

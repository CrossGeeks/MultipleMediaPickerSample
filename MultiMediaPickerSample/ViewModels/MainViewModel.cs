using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MultiMediaPickerSample.Models;
using MultiMediaPickerSample.Services;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Xamarin.Forms;

namespace MultiMediaPickerSample.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        IMultiMediaPickerService _multiMediaPickerService;

        public ObservableCollection<MediaFile> Media { get; set; }
        public ICommand SelectImagesCommand { get; set; }
        public ICommand SelectVideosCommand { get; set; }

        public MainViewModel(IMultiMediaPickerService multiMediaPickerService)
        {
            _multiMediaPickerService = multiMediaPickerService;
            SelectImagesCommand = new Command(async (obj) =>
            {
                var hasPermission = await CheckPermissionsAsync();
                if (hasPermission)
                {
                    Media = new ObservableCollection<MediaFile>();
                    await _multiMediaPickerService.PickPhotosAsync();
                }
            });

            SelectVideosCommand = new Command(async (obj) =>
            {
                var hasPermission = await CheckPermissionsAsync();
                if (hasPermission)
                {

                    Media = new ObservableCollection<MediaFile>();

                    await _multiMediaPickerService.PickVideosAsync();

                }
            });

            _multiMediaPickerService.OnMediaPicked += (s, a) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Media.Add(a);

                });
               
            };
        }

        async Task<bool> CheckPermissionsAsync()
        {
            var retVal = false;
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Plugin.Permissions.Abstractions.Permission.Storage);
                if (status != PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Plugin.Permissions.Abstractions.Permission.Storage))
                    {
                        await App.Current.MainPage.DisplayAlert("Alert","Need Storage permission to access to your photos.","Ok");
                    }

                    var results = await CrossPermissions.Current.RequestPermissionsAsync(new[] { Plugin.Permissions.Abstractions.Permission.Storage });
                    status = results[Plugin.Permissions.Abstractions.Permission.Storage];
                }

                if (status == PermissionStatus.Granted)
                {
                    retVal = true;

                }
                else if (status != PermissionStatus.Unknown)
                {
                    await App.Current.MainPage.DisplayAlert("Alert","Permission Denied. Can not continue, try again.","Ok");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                await App.Current.MainPage.DisplayAlert("Alert", "Error. Can not continue, try again.", "Ok");
            }

            return retVal;

        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

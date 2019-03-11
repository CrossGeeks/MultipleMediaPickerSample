using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;
using GMImagePicker;
using MultiMediaPickerSample.Helpers;
using MultiMediaPickerSample.Models;
using MultiMediaPickerSample.Services;
using Photos;
using UIKit;

namespace MultiMediaPickerSample.iOS.Services
{
    public class MultiMediaPickerService : IMultiMediaPickerService
    {
        const string TemporalDirectoryName = "TmpMedia";

        //Events
        public event EventHandler<MediaFile> OnMediaPicked;
        public event EventHandler<IList<MediaFile>> OnMediaPickedCompleted;

        GMImagePickerController currentPicker;
        TaskCompletionSource<IList<MediaFile>> mediaPickTcs;

        public void Clean()
        {
            var documentsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), TemporalDirectoryName);

            if (Directory.Exists(documentsDirectory))
            {
                Directory.Delete(documentsDirectory);
            }
        }

        public async Task<IList<MediaFile>> PickPhotosAsync()
        {
            return await PickMediaAsync("Select Images", PHAssetMediaType.Image);
        }

        public async Task<IList<MediaFile>> PickVideosAsync()
        {
            return await PickMediaAsync("Select Videos",PHAssetMediaType.Video);
        }

        async Task<IList<MediaFile>> PickMediaAsync(string title,PHAssetMediaType type)
        {
           
            mediaPickTcs = new TaskCompletionSource<IList<MediaFile>>();
            currentPicker = new GMImagePickerController()
            {
                Title = title,
                MediaTypes = new[] { type }
            };

            currentPicker.FinishedPickingAssets += FinishedPickingAssets;

            var window = UIApplication.SharedApplication.KeyWindow;
            var vc = window.RootViewController;
            while (vc.PresentedViewController != null)
            {
                vc = vc.PresentedViewController;
            }

            await vc.PresentViewControllerAsync(currentPicker, true);

            var results = await mediaPickTcs.Task;

            currentPicker.FinishedPickingAssets -= FinishedPickingAssets;
            OnMediaPickedCompleted?.Invoke(this, results);
            return results;
        }

        async void FinishedPickingAssets(object sender, MultiAssetEventArgs args)
        {
            IList<MediaFile> results = new List<MediaFile>();
            TaskCompletionSource<IList<MediaFile>> tcs=new TaskCompletionSource<IList<MediaFile>>();

            var options = new PHImageRequestOptions()
            {
                NetworkAccessAllowed = true
            };

            options.Synchronous = false;
            options.ResizeMode = PHImageRequestOptionsResizeMode.Fast;
            options.DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat;
            bool completed = false;
            for (var i = 0; i < args.Assets.Length; i++)
            {
                var asset = args.Assets[i];

                string fileName = string.Empty;
                if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
                {
                    fileName = PHAssetResource.GetAssetResources(asset).FirstOrDefault().OriginalFilename;

                }

                switch (asset.MediaType)
                {
                    case PHAssetMediaType.Video:

                        PHImageManager.DefaultManager.RequestImageForAsset(asset, new SizeF(150.0f, 150.0f),
                            PHImageContentMode.AspectFill, options, async (img, info) =>
                            {
                                var startIndex = fileName.IndexOf(".", StringComparison.CurrentCulture);

                                string path = "";
                                if (startIndex != -1)
                                {
                                    path = FileHelper.GetOutputPath(MediaFileType.Image, TemporalDirectoryName, $"{fileName.Substring(0, startIndex)}-THUMBNAIL.JPG");
                                }
                                else
                                {
                                    path = FileHelper.GetOutputPath(MediaFileType.Image, TemporalDirectoryName, string.Empty);

                                }

                                if (!File.Exists(path))
                                {

                                    img.AsJPEG().Save(path, true);
                                }

                                    TaskCompletionSource<string> tvcs = new TaskCompletionSource<string>();

                                    var vOptions = new PHVideoRequestOptions();
                                    vOptions.NetworkAccessAllowed = true;
                                    vOptions.Version = PHVideoRequestOptionsVersion.Original;
                                    vOptions.DeliveryMode = PHVideoRequestOptionsDeliveryMode.FastFormat;


                                    PHImageManager.DefaultManager.RequestAvAsset(asset, vOptions, (avAsset, audioMix, vInfo) =>
                                    {
                                        var vPath = FileHelper.GetOutputPath(MediaFileType.Video, TemporalDirectoryName, fileName);

                                        if (!File.Exists(vPath))
                                        {
                                            AVAssetExportSession exportSession = new AVAssetExportSession(avAsset, AVAssetExportSession.PresetHighestQuality);

                                            exportSession.OutputUrl = NSUrl.FromFilename(vPath);
                                            exportSession.OutputFileType = AVFileType.QuickTimeMovie;


                                            exportSession.ExportAsynchronously(() =>
                                            {
                                                Console.WriteLine(exportSession.Status);

                                                tvcs.TrySetResult(vPath);
                                                //exportSession.Dispose();
                                            });

                                        }

                                    });

                                    var videoUrl = await tvcs.Task;
                                    var meFile = new MediaFile()
                                    {
                                        Type = MediaFileType.Video,
                                        Path = videoUrl,
                                        PreviewPath = path
                                    };
                                    results.Add(meFile);
                                    OnMediaPicked?.Invoke(this, meFile);

                                if (args.Assets.Length == results.Count && !completed)
                                {
                                    completed = true;
                                    tcs.TrySetResult(results);
                                }

                            });



                        break;
                    default:

                        PHImageManager.DefaultManager.RequestImageData(asset, options, (data, dataUti, orientation, info) =>
                        {

                        string path = FileHelper.GetOutputPath(MediaFileType.Image, TemporalDirectoryName, fileName);

                        if (!File.Exists(path))
                        {
                                Debug.WriteLine(dataUti);
                                var imageData = data;
                                //var image = UIImage.LoadFromData(imageData);

                                //if (imageScale < 1.0f)
                                //{
                                //    //begin resizing image
                                //    image = image.ResizeImageWithAspectRatio((float)imageScale);
                                //}

                                //if (imageQuality < 100)
                                //{
                                //    imageData = image.AsJPEG(Math.Min(imageQuality,100));
                                //}

                                imageData?.Save(path, true);
                               
                              
                            }

                            var meFile = new MediaFile()
                            {
                                Type = MediaFileType.Image,
                                Path = path,
                                PreviewPath = path
                            };

                            results.Add(meFile);
                            OnMediaPicked?.Invoke(this, meFile);
                            if (args.Assets.Length == results.Count && !completed)
                            {
                                completed = true;
                                tcs.TrySetResult(results);
                            }

                        });

                        break;
                }
            }

          
            mediaPickTcs?.TrySetResult(await tcs.Task);
        }

    }
}

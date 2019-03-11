using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Media;
using Android.Provider;
using Android.Widget;
using MultiMediaPickerSample.Droid.Helpers;
using MultiMediaPickerSample.Helpers;
using MultiMediaPickerSample.Models;
using MultiMediaPickerSample.Services;
using Plugin.CurrentActivity;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;

namespace MultiMediaPickerSample.Droid.Services
{
    public class MultiMediaPickerService : IMultiMediaPickerService
    {
        public static MultiMediaPickerService SharedInstance = new MultiMediaPickerService();
        int MultiPickerResultCode = 9793;
        const string TemporalDirectoryName = "TmpMedia";

        MultiMediaPickerService()
        {
        }

        public event EventHandler<MediaFile> OnMediaPicked;
        public event EventHandler<IList<MediaFile>> OnMediaPickedCompleted;

        TaskCompletionSource<IList<MediaFile>> mediaPickedTcs;

        public void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            ObservableCollection<MediaFile> mediaPicked = null;

            if (requestCode == MultiPickerResultCode)
            {
                if(resultCode == Result.Ok)
                {
                    mediaPicked = new ObservableCollection<MediaFile>();
                    if (data != null)
                    {
                        ClipData clipData = data.ClipData;
                        if (clipData != null)
                        {
                            for (int i = 0; i < clipData.ItemCount; i++)
                            {
                                ClipData.Item item = clipData.GetItemAt(i);
                                Android.Net.Uri uri = item.Uri;
                                var media=CreateMediaFileFromUri(uri);
                                if(media!=null)
                                {
                                    mediaPicked.Add(media);
                                    OnMediaPicked?.Invoke(this, media);
                                }

                            }
                        }
                        else
                        {
                            Android.Net.Uri uri = data.Data;
                            var media = CreateMediaFileFromUri(uri);
                            if (media != null)
                            {
                                mediaPicked.Add(media);
                                OnMediaPicked?.Invoke(this, media);
                            }
                        }

                        OnMediaPickedCompleted?.Invoke(this, mediaPicked);
                    }
                }

                mediaPickedTcs?.TrySetResult(mediaPicked);

            }
        }

        MediaFile CreateMediaFileFromUri(Android.Net.Uri uri)
        {
            MediaFile mediaFile = null;
            var type = CrossCurrentActivity.Current.Activity.ContentResolver.GetType(uri);

            var path = GetRealPathFromURI(uri);
            if (path != null)
            {
                string fullPath = string.Empty;
                string thumbnailImagePath = string.Empty;
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                var ext = System.IO.Path.GetExtension(path) ?? string.Empty;
                MediaFileType mediaFileType = MediaFileType.Image;

                if (type.StartsWith(Enum.GetName(typeof(MediaFileType), MediaFileType.Image), StringComparison.CurrentCultureIgnoreCase))
                {
                    var fullImage = ImageHelpers.RotateImage(path, 1);
                    var thumbImage = ImageHelpers.RotateImage(path, 0.25f);


                    fullPath = FileHelper.GetOutputPath(MediaFileType.Image, TemporalDirectoryName, $"{fileName}{ext}");
                    File.WriteAllBytes(fullPath, fullImage);
                   
                    thumbnailImagePath = FileHelper.GetOutputPath(MediaFileType.Image, TemporalDirectoryName, $"{fileName}-THUMBNAIL{ext}");
                    File.WriteAllBytes(thumbnailImagePath, thumbImage);

                }
                else if (type.StartsWith(Enum.GetName(typeof(MediaFileType), MediaFileType.Video), StringComparison.CurrentCultureIgnoreCase))
                {
                    fullPath = path;
                    var bitmap = ThumbnailUtils.CreateVideoThumbnail(path, ThumbnailKind.MiniKind);

                    thumbnailImagePath = FileHelper.GetOutputPath(MediaFileType.Image, TemporalDirectoryName, $"{fileName}-THUMBNAIL{ext}");
                    var stream = new FileStream(thumbnailImagePath, FileMode.Create);
                    bitmap?.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                    stream.Close();

                    mediaFileType = MediaFileType.Video;
                }

                if(!string.IsNullOrEmpty(fullPath) && !string.IsNullOrEmpty(thumbnailImagePath))
                {
                    mediaFile = new MediaFile()
                    {
                        Path = fullPath,
                        Type = mediaFileType,
                        PreviewPath = thumbnailImagePath
                    };
                }
               
            }

            return mediaFile;
        }

        public static string GetRealPathFromURI(Android.Net.Uri contentURI)
        {
            ICursor cursor = null;
            try
            {
             
                string mediaPath = string.Empty;
                cursor = CrossCurrentActivity.Current.Activity.ContentResolver.Query(contentURI, null, null, null, null);
                cursor.MoveToFirst();
                int idx = cursor.GetColumnIndex(MediaStore.MediaColumns.Data);

                if (idx != -1)
                {
                    var type=CrossCurrentActivity.Current.Activity.ContentResolver.GetType(contentURI);

                    int pIdx = cursor.GetColumnIndex(MediaStore.MediaColumns.Id);

                    var mData = cursor.GetString(idx);

                    mediaPath =  mData;

                }
                else
                {
                   
                    var docID = DocumentsContract.GetDocumentId(contentURI);
                    var doc = docID.Split(':');
                    var id = doc[1];
                    var whereSelect = MediaStore.Images.ImageColumns.Id + "=?";
                    var dataConst = MediaStore.Images.ImageColumns.Data;
                    var projections = new string[] { dataConst };
                    var internalUri = MediaStore.Images.Media.InternalContentUri;
                    var externalUri = MediaStore.Images.Media.ExternalContentUri;
                    switch (doc[0])
                    {
                        case "video":
                            internalUri = MediaStore.Video.Media.InternalContentUri;
                            externalUri = MediaStore.Video.Media.ExternalContentUri;
                            whereSelect = MediaStore.Video.VideoColumns.Id + "=?";
                            dataConst= MediaStore.Video.VideoColumns.Data;
                            break;
                        case "image":
                            whereSelect = MediaStore.Video.VideoColumns.Id + "=?";
                            projections = new string[] { MediaStore.Video.VideoColumns.Data };
                            break;
                    }

                    projections = new string[] { dataConst };
                    cursor = CrossCurrentActivity.Current.Activity.ContentResolver.Query(internalUri, projections, whereSelect, new string[] { id }, null);
                    if (cursor.Count == 0)
                    {
                        cursor = CrossCurrentActivity.Current.Activity.ContentResolver.Query(externalUri, projections, whereSelect, new string[] { id }, null);
                    }
                    var colDatax = cursor.GetColumnIndexOrThrow(dataConst);
                    cursor.MoveToFirst();

                    mediaPath = cursor.GetString(colDatax);
                }
                return mediaPath;
            }
            catch (Exception)
            {
                Toast.MakeText(CrossCurrentActivity.Current.Activity, "Unable to get path", ToastLength.Long).Show();

            }
            finally
            {
                if (cursor != null)
                {
                    cursor.Close();
                    cursor.Dispose();
                }
            }

            return null;

        }

        public void Clean()
        {

            var documentsDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), TemporalDirectoryName);

            if (Directory.Exists(documentsDirectory))
            {
                Directory.Delete(documentsDirectory);
            }
        }

        public async Task<IList<MediaFile>> PickPhotosAsync()
        {
            return await PickMediaAsync("image/*", "Select Images", MultiPickerResultCode);
        }

        public async Task<IList<MediaFile>> PickVideosAsync()
        {
            return await PickMediaAsync("video/*", "Select Videos", MultiPickerResultCode);
        }
       
        async Task<IList<MediaFile>> PickMediaAsync(string type,string title, int resultCode)
        {
           
            mediaPickedTcs = new TaskCompletionSource<IList<MediaFile>>();
           
            var imageIntent = new Intent(Intent.ActionPick);
            imageIntent.SetType(type);
            imageIntent.PutExtra(Intent.ExtraAllowMultiple, true);
            CrossCurrentActivity.Current.Activity.StartActivityForResult(Intent.CreateChooser(imageIntent, title), resultCode);

            return await mediaPickedTcs.Task;

        }

    }
}

﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Content.PM;
using Android;
using Android.Widget;
using Android.Views;
using Android.Support.Design.Widget;
using App.Utils;
using App.Widget;
using Android.Util;
using Android.Graphics;
using static Android.Widget.AdapterView;
using System;
using System.Linq;

namespace App.Main
{
    [Activity(Label = "@string/MainActivityTitle", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/AppTheme")]
    public class MainActivity : BaseActivity  
    {
        MainRecord record;
        const string BUNDLE_MAIN_RECORD = "MAIN_RECORD";

        private const int OPEN_SOURCE_IMAGE = 200;

        ImageView imageView;
        ImageFrame imageFrame;
        Button btnOpen;
        Button btnShot;
        Button btnEdit;
        Spinner spnColor;
        Button btnSave;
        View progressHolder;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            SetupContentLayout();


            if (bundle == null)
            {
                record = new MainRecord(ApplicationContext);
            }
            else
            {
                record = Record.ReadBundle<MainRecord>(bundle, BUNDLE_MAIN_RECORD);
            }

            // Setup controls
            imageView = FindViewById<ImageView>(Resource.Id.image_holder);
            imageFrame = FindViewById<ImageFrame>(Resource.Id.image_frame);
            btnOpen = FindViewById<Button>(Resource.Id.btn_open_image);
            btnOpen.Click += delegate { OpenImage(); };
            btnShot = FindViewById<Button>(Resource.Id.btn_take_photo);
            btnShot.Click += delegate { TakePhoto(); };
            btnEdit = FindViewById<Button>(Resource.Id.btn_edit_image);
            btnEdit.Click += delegate { EditImage(); };

            spnColor = FindViewById<Spinner>(Resource.Id.spn_color_mode);
            spnColor.Adapter = new SimpleImageArrayAdapter(SupportActionBar.ThemedContext, new int[]
            {
                Resource.Drawable.ic_camera_singlemode_grey,
                Resource.Drawable.ic_bw_cp_grey,
                Resource.Drawable.ic_greyscale_cp_grey,
                Resource.Drawable.ic_color_cp_grey,
            });


            // Restore spinner background overrided by "buttonBarButtonStyle"
            Context context = SupportActionBar.ThemedContext;
            var value = new TypedValue();
            context.Theme.ResolveAttribute(Android.Resource.Attribute.SpinnerStyle, value, true);
            var array = context.ObtainStyledAttributes(value.ResourceId, new int[] { Android.Resource.Attribute.Background });
            spnColor.Background = array.GetDrawable(0);
            array.Recycle();

            spnColor.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(Spinner_ItemSelected);

            btnSave = FindViewById<Button>(Resource.Id.btn_save_image);
            btnSave.Click += delegate { SaveImage(); };
            progressHolder = FindViewById(Resource.Id.progress_holder);

            UpdateView();
        }

        private void Spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            if (spinner == spnColor)
            {
                Processing[] items = new Processing[] { Processing.Original, Processing.BW, Processing.Gray, Processing.Color };
                if (e.Position > 0 && e.Position < items.Length)
                {
                    CropImage(items[e.Position]);
                }
                else
                {
                    Log.Error(AppLog.TAG, "Unknown processing mode " + e.Position.ToString());
                }
            }

        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            record.WriteBundle(outState, BUNDLE_MAIN_RECORD);
        }

        protected override void OnPause()
        {
            base.OnPause();
            record.VisibleActivity = null;
        }

        protected override void OnResume()
        {
            base.OnResume();
            record.VisibleActivity = this;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);
            return true;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            switch (requestCode)
            {
                case OPEN_SOURCE_IMAGE:
                    if (resultCode == Result.Ok)
                    {
                        Android.Net.Uri selectedImage = data.Data;
                        OnOpenImage(selectedImage);
                    }
                    break;
            }
        }
        
        private void OnOpenImage(Android.Net.Uri imageUri)
        {
            record.OpenSourceImage(imageUri, () =>
            {
                UpdateView();
            });
            UpdateView();

        }

        private void OpenImage()
        {
            SelectImages(OPEN_SOURCE_IMAGE, Resource.String.select_picture_title, false);
        }

        private void TakePhoto()
        {
            Snackbar.Make(imageView, "Camera doesn't supported yet.", Snackbar.LengthIndefinite).SetAction(Resource.String.action_close, (View view) => { }).Show();
        }

        private void EditImage()
        {

        }

        private void CropImage(Processing processing)
        {
            record.OnCropImage(processing, () =>
            {
                UpdateView();
            });
            UpdateView();
        }

        private void SaveImage()
        {

        }

        private void UpdateView()
        {
            progressHolder.Visibility = record.WaitMode ? ViewStates.Visible : ViewStates.Gone;

            // Setup image
            imageView.SetImageBitmap(record.DisplayBitmap);

            // Setup image frame
            if (record.ImageMode == MainRecord.ImageState.Source)
            {
                imageFrame.ImageMatrix = imageView.ImageMatrix;
                imageFrame.FramePoints = record.GetDocumentFrame();
                imageFrame.ImageBounds = record.DisplayBitmap != null ? new RectF(imageView.Drawable.Bounds) : null;
                imageFrame.Visibility = ViewStates.Visible;
            }
            else
            {
                imageFrame.Visibility = ViewStates.Gone;
            }

            // Setup buttons
            btnShot.Visibility = ViewStates.Gone;
            btnEdit.Visibility = (record.ImageMode != MainRecord.ImageState.InitNothing) ? ViewStates.Visible : ViewStates.Gone;
            spnColor.Visibility = (record.ImageMode != MainRecord.ImageState.InitNothing) ? ViewStates.Visible : ViewStates.Gone;
            btnSave.Visibility = (record.ImageMode == MainRecord.ImageState.Target) ? ViewStates.Visible : ViewStates.Gone;

            ShowError();
        }

        private void ShowError()
        {
            if (record.HasError)
            {
                Snackbar.Make(imageView, record.ErrorMessage, Snackbar.LengthIndefinite).SetAction(Resource.String.action_close, (View view) => { }).Show();
                record.ResetError();
            }
        }
    }
}



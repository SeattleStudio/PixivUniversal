﻿using Pixeez.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace PixivUWP.Pages.DetailPage
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WorkDetailPage : Windows.UI.Xaml.Controls.Page
    {
        public WorkDetailPage()
        {
            this.InitializeComponent();
        }
        Work Work;
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Work = e.Parameter as Work;
            PixivUWP.ProgressBarVisualHelper.SetYFHelperVisibility(pro, true);
            try
            {
                siz.Text = "("+Work.Height?.ToString() + "x" + Work.Width?.ToString()+")";
                des.Text = Work.Caption;
                title.Text = Work.Title;
                user.Text = Work.User.Name;
                tags.Text = new Converter.TagsToStr().Convert(Work.Tags, null, null, null).ToString();
                using (var stream = await Data.TmpData.CurrentAuth.Tokens.SendRequestAsync(Pixeez.MethodType.GET, Work.ImageUrls.Large))
                {
                    var bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                    await bitmap.SetSourceAsync((await stream.GetResponseStreamAsync()).AsRandomAccessStream());
                    bigimg.Source = bitmap;
                }
            }
            finally
            {
                PixivUWP.ProgressBarVisualHelper.SetYFHelperVisibility(pro, false);
            }
        }
    }
}

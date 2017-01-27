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
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            using (var stream = await Data.TmpData.CurrentAuth.Tokens.SendRequestAsync(Pixeez.MethodType.GET, (e.Parameter as Work).ImageUrls.Large))
            {
                var bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                await bitmap.SetSourceAsync((await stream.GetResponseStreamAsync()).AsRandomAccessStream());
                bigimg.Source = bitmap;
            }
        }
    }
}

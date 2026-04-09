using Microsoft.Maui.Controls.Maps;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoMapsPrototype
{
    public class CustomPin : Pin
    {
        public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(CustomPin));

        public ImageSource? ImageSource
        {
            get => (ImageSource?)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }
    }
}

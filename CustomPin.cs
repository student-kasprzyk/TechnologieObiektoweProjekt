using Microsoft.Maui.Controls.Maps;

namespace GeoMapsPrototype
{
    public class CustomPin : Pin
    {
        public static readonly BindableProperty ImageSourceProperty =
            BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(CustomPin));

        public static readonly BindableProperty RadiusProperty =
            BindableProperty.Create(nameof(Radius), typeof(double), typeof(CustomPin), 100.0);

        public static readonly BindableProperty IsTaskCompletedProperty =
            BindableProperty.Create(nameof(IsTaskCompleted), typeof(bool), typeof(CustomPin), false);

        public static readonly BindableProperty TaskDataProperty =
            BindableProperty.Create(nameof(TaskData), typeof(PinTask), typeof(CustomPin));

        public ImageSource? ImageSource
        {
            get => (ImageSource?)GetValue(ImageSourceProperty);
            set
            {
                SetValue(ImageSourceProperty, value);
                OnPropertyChanged(nameof(ImageSource));
            }
        }

        public double Radius
        {
            get => (double)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        public bool IsTaskCompleted
        {
            get => (bool)GetValue(IsTaskCompletedProperty);
            set => SetValue(IsTaskCompletedProperty, value);
        }

        public PinTask? TaskData
        {
            get => (PinTask?)GetValue(TaskDataProperty);
            set => SetValue(TaskDataProperty, value);
        }

        public CustomPin() { }
    }
}

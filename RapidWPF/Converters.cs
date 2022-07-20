﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Data;
using MVUnity;
using AnyCAD.Foundation;

namespace RapidWPF
{
    /// <summary>
    /// V3与实色画笔转换
    /// </summary>
    public class V3ToSolidBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            V3 v = (value as V3).Normalized;
            byte r = (byte)(int)(Math.Abs(v.X) * 255f);
            byte g = (byte)(int)(Math.Abs(v.Y) * 255f);
            byte b = (byte)(int)(Math.Abs(v.Z) * 255f);
            Color c = Color.FromRgb(r, g, b);
            return new SolidColorBrush(c);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is SolidColorBrush c)
            {
                double x = c.Color.R;
                double y = c.Color.G;
                double z = c.Color.B;
                return new V3(x, y, z).Normalized;
            }
            return V3.Zero;
        }
    }
    /// <summary>
    /// bool与Enum转换
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? false : value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }

    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value).ToString("F0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                double v = double.Parse(value as string);
                return v;
            }
            return null;
        }
    }

    public class V3ToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((V3)value).ToString("F2");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                V3 v = new V3(value as string);
                return v;
            }
            return null;
        }
    }

    public class V3ToVectorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            V3 vert = value as V3;
            return new Vector3((float)vert.X, (float)vert.Y, (float)vert.Z);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Vector3 vert = value as Vector3;
            return new V3(vert.x, vert.y, vert.z);
        }
    }

    public static class ConvertV3
    {
        public static Vector3 ToVector3(V3 value)
        {
            return new Vector3((float)value.X, (float)value.Y, (float)value.Z);
        }
    }
}

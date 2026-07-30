using CineCore.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CineCore.Converters
{
    // Ubah string poster_path jadi ImageSource yang AMAN.
    // - kosong / null           → null (biar placeholder "No Poster" yang tampil)
    // - "http.." / "https.."    → dipakai langsung (download async, gak nge-freeze UI)
    // - path relatif Laravel    → diprefix LaravelBaseUrl (kalau diisi); kalau kosong → placeholder
    // - URL rusak / gagal parse → null (gak crash, gak nyepam binding error)
    [ValueConversion(typeof(string), typeof(ImageSource))]
    public class PosterUrlConverter : IValueConverter
    {
        // Kalau poster di DB masih path relatif Laravel (mis. "/storage/posters/xx.jpg"),
        // isi base URL app Laravel-mu di sini biar posternya kebaca. Contoh:
        //   "http://127.0.0.1:8000"   (saat `php artisan serve`)
        //   "http://cinecore.test"    (URL Laragon)
        // Biarin "" kalau poster di DB sudah berupa URL lengkap.
        private const string LaravelBaseUrl = "";

        public object? Convert(object v, Type t, object p, CultureInfo c)
        {
            var url = (v as string)?.Trim();
            if (string.IsNullOrEmpty(url)) return null;

            bool isHttp = url.StartsWith("http://") || url.StartsWith("https://");
            // File lokal (dipilih admin lewat "Choose File"): path absolut Windows atau URI file:
            bool isFile = url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                          || System.IO.Path.IsPathRooted(url);

            if (!isHttp && !isFile)
            {
                // path relatif → butuh base URL Laravel; kalau gak diset, biarin placeholder
                if (string.IsNullOrEmpty(LaravelBaseUrl)) return null;
                url = LaravelBaseUrl.TrimEnd('/') + "/" + url.TrimStart('/');
            }

            try
            {
                if (isFile && !url.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                    url = new Uri(url).AbsoluteUri; // "C:\poster.jpg" -> "file:///C:/poster.jpg"

                var bmp = new BitmapImage();
                bmp.BeginInit();
                // File lokal di-cache OnLoad (biar file gak kekunci & preview langsung muncul),
                // URL http tetap OnDemand biar gak nge-freeze UI.
                bmp.CacheOption = isFile ? BitmapCacheOption.OnLoad : BitmapCacheOption.OnDemand;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.UriSource = new Uri(url, UriKind.RelativeOrAbsolute);
                bmp.EndInit();
                return bmp;
            }
            catch
            {
                return null; // URI gak valid → biarin placeholder yang tampil
            }
        }

        public object ConvertBack(object v, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is true ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => v is Visibility.Visible;
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is true ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => v is Visibility.Collapsed;
    }

    [ValueConversion(typeof(SeatState), typeof(Brush))]
    public class SeatStateToBrushConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
        {
            return v is SeatState state ? state switch
            {
                SeatState.Available => new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3E)),
                SeatState.Selected  => new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xFF)),
                SeatState.Booked    => new SolidColorBrush(Color.FromRgb(0x4A, 0x1A, 0x1A)),
                _ => Brushes.Gray
            } : Brushes.Gray;
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    [ValueConversion(typeof(SeatState), typeof(bool))]
    public class SeatStateToEnabledConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is SeatState s && s != SeatState.Booked;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => string.IsNullOrEmpty(v as string) ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    public class StringEqualityConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v?.ToString() == p?.ToString();
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => v is true ? p : Binding.DoNothing;
    }

    // Format DateTime pakai pola dari ConverterParameter, lalu UPPERCASE.
    // Contoh: ConverterParameter="ddd" -> "THU", "MMM" -> "JUL".
    [ValueConversion(typeof(DateTime), typeof(string))]
    public class DateUpperConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is DateTime dt && p is string fmt
                ? dt.ToString(fmt, CultureInfo.InvariantCulture).ToUpperInvariant()
                : string.Empty;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }
}

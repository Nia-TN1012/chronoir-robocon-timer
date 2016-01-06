using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Input;
using System.Windows;

namespace FRCTimer3 {

	/// <summary>
	///		bool値に応じてカーソルの表示・非表示を切り替えます。
	/// </summary>
	public sealed class BoolToCursorConverter : IValueConverter {
		public object Convert( object value, Type targetValue, object parameter, CultureInfo culture ) =>
			( value is bool && ( bool )value ) ? Cursors.None : Cursors.Arrow;

		public object ConvertBack( object value, Type targetValue, object parameter, CultureInfo culture ) => null;
	}

	/// <summary>
	///		bool値に応じてコントロールの表示・非表示を切り替えます。
	/// </summary>
	public sealed class BoolToVisibilityConverter : IValueConverter {
		public object Convert( object value, Type targetValue, object parameter, CultureInfo culture ) =>
			( value is bool && ( bool )value ) ? Visibility.Visible : Visibility.Collapsed;

		public object ConvertBack( object value, Type targetValue, object parameter, CultureInfo culture ) => null;
	}

}
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace FRCTimer3 {

	public class ExComboBox : ComboBox {

		[Description( "ユーザーはドロップダウンを開いてアイテムを選択できるかどうかの値を取得・設定します。" )]
		public bool IsSelectable {
			get { return ( bool )GetValue( IsSelectableProperty ); }
			set { SetValue( IsSelectableProperty, value ); }
		}

		static ExComboBox() {
			DefaultStyleKeyProperty.OverrideMetadata( typeof( ExComboBox ), new FrameworkPropertyMetadata( typeof( ExComboBox ) ) );
		}

		public ExComboBox() : base() {
			IsSelectable = true;
		}

		/// <summary>
		///		依存関係のプロパティを表します。
		/// </summary>
		public static readonly DependencyProperty IsSelectableProperty =
			DependencyProperty.Register( nameof( IsSelectable ), typeof( bool ), typeof( ExComboBox ) );
	}
}

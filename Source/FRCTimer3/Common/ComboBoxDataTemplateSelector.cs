using System.Windows;
using System.Windows.Controls;

namespace FRCTimer3 {

	/// <summary>
	///		ComboBoxのTextBox側とPopup側でそれぞれ異なるDataTemplateをセットするためのセレクタを表します。
	/// </summary>
	public class RedTeamComboBoxDataTemplateSelector : DataTemplateSelector {
		public override DataTemplate SelectTemplate( object item, DependencyObject container ) {
			ContentPresenter presenter = ( ContentPresenter )container;

			if( presenter.TemplatedParent is ComboBox ) {
				return ( DataTemplate )presenter.FindResource( "RedTeamInfoCombo" );
			}
			else {
				return ( DataTemplate )presenter.FindResource( "RedTeamInfoComboPopup" );
			}
		}
	}

	/// <summary>
	///		ComboBoxのTextBox側とPopup側でそれぞれ異なるDataTemplateをセットするためのセレクタを表します。
	/// </summary>
	public class BlueTeamComboBoxDataTemplateSelector : DataTemplateSelector {
		public override DataTemplate SelectTemplate( object item, DependencyObject container ) {
			ContentPresenter presenter = ( ContentPresenter )container;

			if( presenter.TemplatedParent is ComboBox ) {
				return ( DataTemplate )presenter.FindResource( "BlueTeamInfoCombo" );
			}
			else {
				return ( DataTemplate )presenter.FindResource( "BlueTeamInfoComboPopup" );
			}
		}
	}
}

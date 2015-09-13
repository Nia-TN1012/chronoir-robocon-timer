using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FRCTimer3 {
	/// <summary>
	///		TeamInfoEditDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class TeamInfoEditDialog : Window {

		/// <summary>
		///		TIEDModel
		/// </summary>
		TIEDModel tiedm;

		/// <summary>
		///		チーム情報
		/// </summary>
		/// <remarks>ダイアログを閉じた時に呼び出し元からアクセスします。</remarks>
		public TeamInfo Team { get; private set; }

		/// <summary>
		///		TeamInfoEditDialogの新しいインスタンスを生成します。
		/// </summary>
		/// <param name="append">チームの追加かどうかのフラグ（ true : 追加 / false : 編集 ）</param>
		/// <param name="ti">チームの情報</param>
		/// <param name="kg">既存のグループ名リスト</param>
		public TeamInfoEditDialog( bool append, TeamInfo ti, IEnumerable<string> kg ) {
			InitializeComponent();
			tiedm = new TIEDModel( append, ti, kg );
			DataContext = tiedm;
		}

		/// <summary>
		///		OKボタンをクリックした時のイベントです。
		/// </summary>
		private void OKButton_Click( object sender, RoutedEventArgs e ) {
			DialogResult = true;
			Team = new TeamInfo { TeamName = tiedm.TeamName, GroupName = tiedm.GroupName };
			Close();
		}

		/// <summary>
		///		キャンセルボタンをクリックした時のイベントです。
		/// </summary>
		private void CancelButton_Click( object sender, RoutedEventArgs e ) {
			DialogResult = false;
			Close();
		}
	}

	/// <summary>
	///		TeamInfoEditDialogのコントロールとバインディングするModelです。
	/// </summary>
	class TIEDModel : INotifyPropertyChanged {
		
		/// <summary>
		///		ダイアログのタイトル
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		///		OKボタンのラベル
		/// </summary>
		public string OKLabel { get; private set; }

		/// <summary>
		///		チーム名
		/// </summary>
		public string TeamName { get; set; }

		/// <summary>
		///		グループ名
		/// </summary>
		public string GroupName { get; set; }

		/// <summary>
		///		既存のグループ名リスト
		/// </summary>
		public IEnumerable<string> KnownGroup { get; private set; }

		/// <summary>
		///		プロパティを変更した時に発生するイベントハンドラです。
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		///		プロパティの変更を通知します。
		/// </summary>
		/// <param name="propertyName">プロパティ名（ 省略時、呼び出し元のプロパティ名 ）</param>
		public void NotifyPropertyChanged( [CallerMemberName]string propertyName = null ) {
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
		}

		/// <summary>
		///		TIEDModelの新しいインスタンスを生成します。
		/// </summary>
		/// <param name="append">チームの追加かどうかのフラグ（ true : 追加 / false : 編集 ）</param>
		/// <param name="ti">チームの情報</param>
		/// <param name="kg">既存のグループ名リスト</param>
		public TIEDModel( bool append, TeamInfo ti, IEnumerable<string> kg ) {
			Title = append ? "チーム名の追加" : "チーム名の変更";
			OKLabel = append ? "追加 (_A)" : "変更 (_A)";
			TeamName = ti.TeamName;
			GroupName = ti.GroupName;
			KnownGroup = kg;
		}

	}
}

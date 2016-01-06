using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;

namespace FRCTimer3 {
	/// <summary>
	///		MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window {

		/// <summary>
		///		効果音
		/// </summary>
		private Dictionary<FRCSoundEffectType, SoundPlayer> frcSoundEffect;

		/// <summary>
		///		MainWindowの新しいインスタンスを生成します。
		/// </summary>
		public MainWindow() {
			InitializeComponent();
			try {
				// チーム名リストを読み込みます。
				mainViewModel.Init();
				
				// 効果音の種類を示す列挙子と効果音を関連付けます。
				frcSoundEffect = new Dictionary<FRCSoundEffectType, SoundPlayer> {
					[FRCSoundEffectType.Ready] = new SoundPlayer( Properties.Resources.Ready ),
					[FRCSoundEffectType.Start] = new SoundPlayer( Properties.Resources.Start ),
					[FRCSoundEffectType.Last3] = new SoundPlayer( Properties.Resources.Last3sec ),
					[FRCSoundEffectType.Finish] = new SoundPlayer( Properties.Resources.Finish )
				};
			}
			catch( Exception e ) {
				MessageBox.Show(
					$"{mainViewModel.AppVer.ProductName}の初期化中にエラーが発生しました。\nアプリを終了します。\n（ 内容 : {e.Message} ）",
                    $"{mainViewModel.AppVer.ProductName}",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation
				);
				Close();
			}
        }

		/// <summary>
		///		チーム名リストを読み込んだ後のイベントです。
		/// </summary>
		private void mainViewModel_LoadTeamsListCompleted( object sender, NotifyResultEventArgs<LoadTeamsListResult, bool, bool> e ) {
			switch( e.Result ) {
				case LoadTeamsListResult.Succeed:
					e.SucceedAction?.Invoke( true );
					break;
				case LoadTeamsListResult.FileNotFound:
					MessageBox.Show(
						$"チーム名リストファイル（ {TeamsModel.FileName} ）がないため、再作成します。",
						$"{mainViewModel.AppVer.ProductName}",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation
					);
                    e.FailedAction?.Invoke( true );
					break;
				case LoadTeamsListResult.InvaildList:
					e.FailedAction?.Invoke(
						MessageBox.Show(
							$"チーム名リストファイル（ {TeamsModel.FileName} ）の内容が無効です。\nチーム名リストファイルを再作成しますか？",
							$"{mainViewModel.AppVer.ProductName}",
							MessageBoxButton.YesNo,
							MessageBoxImage.Exclamation,
							MessageBoxResult.No
						) == MessageBoxResult.Yes
					);
					break;
				case LoadTeamsListResult.OtherError:
					e.FailedAction?.Invoke(
						MessageBox.Show(
							$"チーム名リストファイル（ {TeamsModel.FileName} ）を読み込みに失敗しました。\nチーム名リストファイルを再作成しますか？",
							$"{mainViewModel.AppVer.ProductName}",
							MessageBoxButton.YesNo,
							MessageBoxImage.Exclamation,
							MessageBoxResult.No
						) == MessageBoxResult.Yes
					);
					break;
			}
		}

		/// <summary>
		///		チーム名リストを初期化した後のイベントです。
		/// </summary>
		private void mainViewModel_ResetTeamsListCompleted( object sender, NotifyResultEventArgs<SaveTeamsListResult, bool, bool> e ) {
			if( e.Result == SaveTeamsListResult.Failed ) {
				MessageBox.Show(
					$"チーム名リストのファイル（ {TeamsModel.FileName} ）を再作成できませんでした。",
					$"{mainViewModel.AppVer.ProductName}",
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation
				);
			}
			e.SucceedAction?.Invoke( false );
		}

		/// <summary>
		///		チーム名リストを保存した後のイベントです。
		/// </summary>
		private void mainViewModel_SaveTeamsListCompleted( object sender, NotifyResultEventArgs<SaveTeamsListResult, bool, bool> e ) {
			switch( e.Result ) {
				case SaveTeamsListResult.Succeed:
					MessageBox.Show(
						$"チーム名リストファイル（ {TeamsModel.FileName} ）を保存しました。",
                        $"{mainViewModel.AppVer.ProductName}",
						MessageBoxButton.OK,
						MessageBoxImage.Information
					);
					e.SucceedAction?.Invoke( false );
					break;
				case SaveTeamsListResult.Failed:
					MessageBox.Show(
						$"チーム名リストファイル（ {TeamsModel.FileName} ）を保存できませんでした。",
						$"{mainViewModel.AppVer.ProductName}",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation
					);
					break;
			}
		}

		/// <summary>
		///		時間定義ファイルを読み込んだ後のイベントです。
		/// </summary>
		private void mainViewModel_LoadTimeDefCompleted( object sender, NotifyResultEventArgs<LoadSettingsResult, bool, bool> e ) {
			switch( e.Result ) {
				case LoadSettingsResult.ValueOutOfRange:
					MessageBox.Show(
						$"時間定義ファイル（ {TimerModel.FileName} ）の読み込みに成功しましたが、一部で範囲外の値が含まれていました。\n（※範囲外の値の場合、デフォルト値を使用します。）",
						$"{mainViewModel.AppVer.ProductName}",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation
					);
					break;
				case LoadSettingsResult.FileNotFound:
					MessageBox.Show(
						$"時間定義ファイル（ {TimerModel.FileName} ）が存在しないため、再作成しました。",
						$"{mainViewModel.AppVer.ProductName}",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation
					);
                    break;
				case LoadSettingsResult.InvaildFormat:
					MessageBox.Show(
						$"時間定義ファイル（ {TimerModel.FileName} ）の内容が無効なため、再作成しました。",
						$"{mainViewModel.AppVer.ProductName}",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation
					);
					break;
				case LoadSettingsResult.JsonRemakeFailed:
					MessageBox.Show(
						$"時間定義ファイル（ {TimerModel.FileName} ）の再作成に失敗しました。",
						$"{mainViewModel.AppVer.ProductName}",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation
					);
					break;
				case LoadSettingsResult.OtherError:
					MessageBox.Show(
						$"時間定義ファイル（ {TimerModel.FileName} ）の読み込み失敗したため、再作成しました。",
						$"{mainViewModel.AppVer.ProductName}",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation
					);
					break;
			}
			e.SucceedAction?.Invoke( true );
		}

		/// <summary>
		///		効果音を鳴らす時のイベントです。
		/// </summary>
		private void mainViewModel_PlaySoundEffect( object sender, FRCSoundEffectTypeEventArgs e ) {
			if( e.FRCSoundEffect == FRCSoundEffectType.Stop )
				foreach( var se in frcSoundEffect )
					se.Value?.Stop();
			else {
				try { frcSoundEffect[e.FRCSoundEffect]?.Play(); }
				catch { }
			}
		}

		/// <summary>
		///		確認ダイアログを表示してコールバックを呼び出すイベントです。
		/// </summary>
		private void mainViewModel_ComfirmActtion( object sender, ComfirmEventArgs e ) {
			if( MessageBox.Show(
					e.Message,
					$"{mainViewModel.AppVer.ProductName}",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question,
					MessageBoxResult.No
				) == MessageBoxResult.Yes
			) e.Callback?.Invoke();
		}

		/// <summary>
		///		アプリを終了する時のイベントです。
		/// </summary>
		private void mainViewModel_ExitFRCTimer( object sender, CallbackEventArgs e ) {
			if( MessageBox.Show(
					"アプリを終了しますか？",
					$"{mainViewModel.AppVer.ProductName}",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question,
					MessageBoxResult.No
				) == MessageBoxResult.Yes
			) {
				e.Callback?.Invoke();
				Close();
			}
		}
	}
}

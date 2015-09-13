using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FRCTimer3 {

	/// <summary>
	///		タイマーの状態を表します。
	/// </summary>
	enum TimerState {
		/// <summary>
		///		自動機発進のリミット
		/// </summary>
		AutoMachineStartTime,
		/// <summary>
		///		通常
		/// </summary>
		Normal,
		/// <summary>
		///		残り10秒
		/// </summary>
		Last10sec,
		/// <summary>
		///		残り3秒
		/// </summary>
		Last3sec,
		/// <summary>
		///		停止
		/// </summary>
		Stop
	}

	/// <summary>
	///		メインのViewModelです。
	/// </summary>
	class MainViewModel : INotifyPropertyChanged {

		/// <summary>
		///		Chronoir Robocon Timerの状態と画面に表示するメッセージを関連付けます。
		/// </summary>
		Dictionary<FRCTimerState, string> message = new Dictionary<FRCTimerState, string> {
			[FRCTimerState.TeamSelect] = "Team Select",
			[FRCTimerState.SettingReady] = "Setting Ready ?",
			[FRCTimerState.SettingTime] = "Setting Time",
			[FRCTimerState.PlayPrepairing] = "Play Preparing",
			[FRCTimerState.PlayReady] = "Play Ready ?",
			[FRCTimerState.PlayTime] = "Play Time",
			[FRCTimerState.GameSet] = "Game Set !",
			[FRCTimerState.Victory] = "V-GOAL Congratulations !",
			[FRCTimerState.FRCTimerSetting] = null
		};

		/// <summary>
		///		タイマーの状態を表します。
		/// </summary>
		TimerState timerState = TimerState.Stop;

		/// <summary>
		///		DispatcherTimerは一定間隔ごとにイベントを発生させるクラスです。
		/// </summary>
		DispatcherTimer dpTimer;

		/// <summary>
		///		アプリの情報を表します。
		/// </summary>
		public FileVersionInfo AppVer { get; private set; }

		#region Model

		/// <summary>
		///		TeamsModel
		/// </summary>
		private TeamsModel teamsModel = new TeamsModel();

		/// <summary>
		///		TimerModel
		/// </summary>
		private TimerModel timerModel = new TimerModel();

		/// <summary>
		///		FRCCommandSetModel
		/// </summary>
		private FRCCommandSetModel frcCommandSetModel;

		#endregion

		#region 時間定義

		/// <summary>
		///		SettingReady / GameReady おいて、残り3秒を表します。
		/// </summary>
		private static TimeSpan ReadyLast3Seconds { get; set; }

		/// <summary>
		///		SettingTimeにおいて、残り10秒を表します。
		/// </summary>
		private static TimeSpan SettingLast10Seconds { get; set; }

		/// <summary>
		///		SettingTimeにおいて、残り3秒を表します。
		/// </summary>
		private static TimeSpan SettingLast3Seconds { get; set; }

		/// <summary>
		///		PlayTimeにおいて、残り10秒を表します。
		/// </summary>
		private static TimeSpan PlayLast10Seconds { get; set; }

		/// <summary>
		///		GameTimeにおいて、残り3秒を表します。
		/// </summary>
		private static TimeSpan PlayLast3Seconds { get; set; }

		#endregion

		/// <summary>
		///		Chronoir Robocon Timerの現在の状態を取得します。
		/// </summary>
		public FRCTimerState NowState { get; private set; } = FRCTimerState.TeamSelect;

		/// <summary>
		///		MainViewModelの新しいインスタンスを生成します。
		/// </summary>
		public MainViewModel() {
			// TeamsModelのイベントハンドラに処理を登録します。
			teamsModel.PropertyChanged += ( sender, e ) =>
				PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( e.PropertyName ) );
			teamsModel.LoadTeamsListCompleted += ( sender, e ) => {
				RedTeamIndex = BlueTeamIndex = -1;
				LoadTeamsListCompleted?.Invoke(
					this,
					new NotifyResultEventArgs<LoadTeamsListResult, bool, bool>(
						e.Result,
						// 読み込みに成功したら、チーム名選択のコンボボックスに反映させます。
						( _ ) => {
							RedTeamIndex = 0;
							BlueTeamIndex = TeamsList.Count >= 2 ? 1 : 0;
							NotifyPropertyChanged( nameof( TeamsList ) );
							NotifyPropertyChanged( nameof( RedTeamIndex ) );
							NotifyPropertyChanged( nameof( BlueTeamIndex ) );
						},
						// 読み込みに失敗したら、初期値に設定します。
						teamsModel.ResetTeamsListAsync
					)
				);
			};
			teamsModel.ResetTeamsListCompleted += ( sender, e ) =>
				ResetTeamsListCompleted?.Invoke(
					this,
					new NotifyResultEventArgs<SaveTeamsListResult, bool, bool>(
						e.Result,
						// 初期化したら、チーム名選択のコンボボックスに反映させます。
						( _ ) => {
							RedTeamIndex = 0;
							BlueTeamIndex = TeamsList.Count >= 2 ? 1 : 0;
							NotifyPropertyChanged( nameof( TeamsList ) );
							NotifyPropertyChanged( nameof( RedTeamIndex ) );
							NotifyPropertyChanged( nameof( BlueTeamIndex ) );
						},
						null
					)
				);
			teamsModel.SaveTeamsListCompleted += ( sender, e ) =>
				SaveTeamsListCompleted?.Invoke(
					this,
					new NotifyResultEventArgs<SaveTeamsListResult, bool, bool>(
						e.Result,
						// 保存に成功したら、チーム名選択のコンボボックスに反映させます。
						( _ ) => {
							RedTeamIndex = 0;
							BlueTeamIndex = TeamsList.Count >= 2 ? 1 : 0;
							NotifyPropertyChanged( nameof( TeamsList ) );
							NotifyPropertyChanged( nameof( RedTeamIndex ) );
							NotifyPropertyChanged( nameof( BlueTeamIndex ) );
						},
						null
					)
				);

			timerModel.LoadSettingsCompleted += ( sender, e ) =>
				LoadSettingsCompleted?.Invoke(
					this,
					new NotifyResultEventArgs<LoadSettingsResult, bool, bool>(
						e.Result,
						( _ ) => {
							NotifyPropertyChanged( nameof( DisplayTimer ) );
							ReadyLast3Seconds = TimerModel.ReadyTime - TimeSpan.FromSeconds( 3 );
							SettingLast10Seconds = TimerModel.SettingTime - TimeSpan.FromSeconds( 10 );
							SettingLast3Seconds = TimerModel.SettingTime - TimeSpan.FromSeconds( 3 );
							PlayLast10Seconds = TimerModel.PlayTime - TimeSpan.FromSeconds( 10 );
							PlayLast3Seconds = TimerModel.PlayTime - TimeSpan.FromSeconds( 3 );
							message[FRCTimerState.Victory] = TimerModel.VictoryMessage;
						},
						null
					)
				);

			// DispatcherTimerを設定します。
			dpTimer = new DispatcherTimer( DispatcherPriority.Normal );
			dpTimer.Interval = TimeSpan.FromMilliseconds( 30.0 );   // 30msec.間隔
			dpTimer.Tick += DpTimer_Tick;

			// FRCCommandSetModelにラベルとコマンドを追加します。
			frcCommandSetModel = new FRCCommandSetModel(
				new CommandButton( "セッティング開始 (_S)", CMDStartSetting ),
				new CommandButton( "セッティングをスキップ (_K)", CMDSkipSetting ),
				new CommandButton( "試合開始 (_S)", CMDStartPlaying ),
				new CommandButton( "中止 (_C)", CMDCancel ),
				new CommandButton( "チーム選択に戻る (_B)", CMDBackToTeamSelect ),
				new CommandButton( "試合終了 (_V)", CMDToVictory ),
				new CommandButton( "アプリの設定 (_O)", CMDConfiguration ),
				new CommandButton( "アプリ終了 (_X)", CMDAppClose ),
				new CommandButton( "チーム名リストを保存して閉じる (_S)", CMDSaveTeamsList ),
				new CommandButton( "チーム名リストを保存せずに閉じる (_C)", CMDCloseSetting ),
				new CommandButton( "チーム名リストを保存する (_P)", CMDApplyTeamsList )
			);

			AppVer = FileVersionInfo.GetVersionInfo( System.Reflection.Assembly.GetExecutingAssembly().Location );
		}

		/// <summary>
		///		チーム名リストと時間定義を読み込みます
		/// </summary>
		public void Init() {
			teamsModel.LoadTeamsListAsync();
			timerModel.LoadSettingsAsync();
		}

		#region TeamsModel関連

		/// <summary>
		///		チーム名リストを取得します
		/// </summary>
		public ObservableCollection<TeamInfo> TeamsList => teamsModel.Teams;

		/// <summary>
		///		赤チームで選択しているチームのインデックスを取得・設定します
		/// </summary>
		public int RedTeamIndex { get; set; } = -1;

		/// <summary>
		///		青チームで選択しているチームのインデックスを取得・設定します
		/// </summary>
		public int BlueTeamIndex { get; set; } = -1;

		#endregion

		#region ディスプレイ関連

		/// <summary>
		///		ディスプレイに表示するメッセージを取得します
		/// </summary>
		public string DisplayMessage => message[NowState];

		/// <summary>
		///		ディスプレイに表示するメッセージの色を取得します
		/// </summary>
		public SolidColorBrush DisplayMessageColor { get; private set; } = Brushes.Yellow;

		/// <summary>
		///		ディスプレイに表示する残り時間を取得します
		/// </summary>
		public string DisplayTimer {
			get {
				TimeSpan ts = TimeSpan.Zero;
				switch( NowState ) {
					case FRCTimerState.TeamSelect:
						ts = TimerModel.SettingTime;
						break;
					case FRCTimerState.SettingReady:
						// 秒単位に切り上げます。
						ts = TimeSpan.FromSeconds( Math.Ceiling( ( TimerModel.ReadyTime - timerModel.Duration ).TotalSeconds ) );
						break;
					case FRCTimerState.SettingTime:
						ts = TimerModel.SettingTime - timerModel.Duration;
						break;
					case FRCTimerState.PlayPrepairing:
						ts = TimerModel.PlayTime;
						break;
					case FRCTimerState.PlayReady:
						ts = TimeSpan.FromSeconds( Math.Ceiling( ( TimerModel.ReadyTime - timerModel.Duration ).TotalSeconds ) );
						break;
					case FRCTimerState.PlayTime:
						ts = TimerModel.PlayTime - timerModel.Duration;
						break;
				}

				// Readyの時は秒のみを表示します。
				if( NowState == FRCTimerState.SettingReady || NowState == FRCTimerState.PlayReady ) {
					return ts.ToString( "%s" );
				}

				// それ以外は「00:00.00」形式です。
				return ts.ToString( @"mm\:ss\.ff" );
			}
		}

		/// <summary>
		///		ディスプレイに表示するタイマーの色を取得します
		/// </summary>
		public SolidColorBrush DisplayTimerColor { get; private set; } = Brushes.White;

		#endregion

		/// <summary>
		///		タイマーが動いているかどうかを取得します。
		/// </summary>
		public bool IsTimerRunning { get; private set; } = false;

		/// <summary>
		///		チーム名を選択できるかどうかを取得します。
		/// </summary>
		/// <remarks>
		///		チーム名リストのEnabledにバインドしています。
		/// </remarks>
		public bool CanSelectTeam => NowState == FRCTimerState.TeamSelect;

		/// <summary>
		///		DispatcherTimerでのイベント処理です。
		/// </summary>
		private void DpTimer_Tick( object sender, EventArgs e ) {

			// セッティングタイム準備及び試合準備の時
			if( NowState == FRCTimerState.SettingReady || NowState == FRCTimerState.PlayReady ) {
				// 残り3秒でサウンドを鳴らします。
				if( timerState == TimerState.Normal && timerModel.Duration > ReadyLast3Seconds ) {
					PlaySoundEffect?.Invoke( this, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Ready ) );
					timerState = TimerState.Last3sec;
				}
				// 残り0秒でセッティングタイム or 試合を開始します。
				else if( timerModel.Duration >= TimerModel.ReadyTime ) {
					if( NowState == FRCTimerState.SettingReady ) {
						timerState = TimerState.Normal;
						NowState = FRCTimerState.SettingTime;
					}
					else {
						DisplayTimerColor =　TimerModel.AutoMachineLanchTimeLimit > TimeSpan.Zero ? Brushes.Aqua : Brushes.White;
						timerState = TimerState.AutoMachineStartTime;
						NowState = FRCTimerState.PlayTime;
					}
					PlaySoundEffect?.Invoke( this, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Start ) );
					timerModel.Start();
					NotifyFRCTimerPropertyChanged();
				}
			}
			// セッティングタイムの時
			else if( NowState == FRCTimerState.SettingTime ) {
				// 残り10秒でタイマー表示の色を変更します。
				if( timerState == TimerState.Normal && timerModel.Duration >= SettingLast10Seconds ) {
					timerState = TimerState.Last10sec;
					DisplayTimerColor = Brushes.HotPink;
					NotifyPropertyChanged( nameof( DisplayTimerColor ) );
				}
				// 残り3秒でサウンドを鳴らします。
				else if( timerState == TimerState.Last10sec && timerModel.Duration >= SettingLast3Seconds ) {
					timerState = TimerState.Last3sec;
					PlaySoundEffect?.Invoke( this, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Last3 ) );
				}
				// 残り0秒でセッティングタイムを終了します。
				else if( timerModel.Duration >= TimerModel.SettingTime ) {
					dpTimer.Stop();
					timerState = TimerState.Stop;
					NowState = FRCTimerState.PlayPrepairing;
					DisplayMessageColor = Brushes.Lime;
					DisplayTimerColor = Brushes.White;
					IsTimerRunning = false;
					PlaySoundEffect?.Invoke( this, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Finish ) );
					timerModel.Stop();
					NotifyFRCTimerPropertyChanged();
				}
			}
			// 試合中の時
			else if( NowState == FRCTimerState.PlayTime ) {
				// 自動機発進のタイムリミットの経過でタイマー表示の色を変更します。
				if( timerState == TimerState.AutoMachineStartTime && timerModel.Duration >= TimerModel.AutoMachineLanchTimeLimit ) {
					timerState = TimerState.Normal;
					DisplayTimerColor = Brushes.White;
					NotifyPropertyChanged( nameof( DisplayTimerColor ) );
				}
				// 残り10秒でタイマー表示の色を変更します。
				if( ( timerState == TimerState.Normal || timerState == TimerState.AutoMachineStartTime ) && timerModel.Duration >= PlayLast10Seconds ) {
					timerState = TimerState.Last10sec;
					DisplayTimerColor = Brushes.HotPink;
					NotifyPropertyChanged( nameof( DisplayTimerColor ) );
				}
				// 残り3秒でサウンドを鳴らします。
				else if( timerState == TimerState.Last10sec && timerModel.Duration >= PlayLast3Seconds ) {
					timerState = TimerState.Last3sec;
					PlaySoundEffect?.Invoke( this, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Last3 ) );
				}
				// 残り0秒で試合を終了します。
				else if( timerModel.Duration >= TimerModel.PlayTime ) {
					dpTimer.Stop();
					timerState = TimerState.Stop;
					NowState = FRCTimerState.GameSet;
					DisplayTimerColor = Brushes.White;
					IsTimerRunning = false;
					PlaySoundEffect?.Invoke( this, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Finish ) );
					timerModel.Stop();
					NotifyFRCTimerPropertyChanged();
				}
			}

			NotifyPropertyChanged( nameof( DisplayTimer ) );
		}

		#region イベント関連

		/// <summary>
		///		プロパティを変更した時に発生するイベントハンドラです。
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		///		チーム名リストを読み込んだ後に発生するイベントハンドラです。
		/// </summary>
		public event NotifyResultEventHandler<LoadTeamsListResult, bool, bool> LoadTeamsListCompleted;

		/// <summary>
		///		時間定義ファイをを読み込んだ後に発生するイベントハンドラです。
		/// </summary>
		public event NotifyResultEventHandler<LoadSettingsResult, bool, bool> LoadSettingsCompleted;

		/// <summary>
		///		チーム名リストを初期化した後に発生するイベントハンドラです。
		/// </summary>
		public event NotifyResultEventHandler<SaveTeamsListResult, bool, bool> ResetTeamsListCompleted;

		/// <summary>
		///		チーム名リストを保存した後に発生するイベントハンドラです。
		/// </summary>
		public event NotifyResultEventHandler<SaveTeamsListResult, bool, bool> SaveTeamsListCompleted;

		/// <summary>
		///		アプリを終了する時に発生するイベントハンドラです。
		/// </summary>
		public event CallbackEventHandler ExitFRCTimer;

		/// <summary>
		///		確認ダイアログを表示する時に発生するイベントハンドラです。
		/// </summary>
		public event ComfirmEventHandler ComfirmAction;

		/// <summary>
		///		効果音を鳴らす時に発生するイベントハンドラです。
		/// </summary>
		public event FRCSoundEffectTypeEventHandler PlaySoundEffect;

		/// <summary>
		///		プロパティの変更を通知します。
		/// </summary>
		/// <param name="propertyName">プロパティ名（ 省略時、呼び出し元のプロパティ名 ）</param>
		private void NotifyPropertyChanged( [CallerMemberName]string propertyName = null ) {
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
		}

		/// <summary>
		///		アプリのメイン画面で使用しているプロパティの変更をまとめて通知します。
		/// </summary>
		private void NotifyFRCTimerPropertyChanged() {
			NotifyPropertyChanged( nameof( CommanddSetList ) );
			NotifyPropertyChanged( nameof( DisplayMessage ) );
			NotifyPropertyChanged( nameof( DisplayMessageColor ) );
			NotifyPropertyChanged( nameof( DisplayTimer ) );
			NotifyPropertyChanged( nameof( DisplayTimerColor ) );
			NotifyPropertyChanged( nameof( IsTimerRunning ) );
			NotifyPropertyChanged( nameof( CanSelectTeam ) );
		}

		#endregion

		#region コマンド関連

		#region コマンド

		// コマンドオブジェクトは遅延初期化させます。

		/// <summary>
		///		セッティング開始するコマンド
		/// </summary>
		private ICommand cmdStartSetting;
		/// <summary>
		///		セッティング開始するコマンドを表します。
		/// </summary>
		public ICommand CMDStartSetting => cmdStartSetting ?? ( cmdStartSetting = new StartSettingCommand( this ) );

		/// <summary>
		///		セッティングスキップするコマンド
		/// </summary>
		private ICommand cmdSkipSetting;
		/// <summary>
		///		セッティングスキップするコマンドを表します。
		/// </summary>
		public ICommand CMDSkipSetting => cmdSkipSetting ?? ( cmdSkipSetting = new SkipSettingCommand( this ) );

		/// <summary>
		///		試合開始するコマンド
		/// </summary>
		private ICommand cmdStartPlaying;
		/// <summary>
		///		試合開始するコマンドを表します。
		/// </summary>
		public ICommand CMDStartPlaying => cmdStartPlaying ?? ( cmdStartPlaying = new StartPlayingCommand( this ) );

		/// <summary>
		///		セッティング、試合を中止するコマンド
		/// </summary>
		private ICommand cmdCancel;
		/// <summary>
		///		セッティング、試合を中止するコマンドを表します。
		/// </summary>
		public ICommand CMDCancel => cmdCancel ?? ( cmdCancel = new CancelCommand( this ) );

		/// <summary>
		///		チーム名選択に戻るコマンド
		/// </summary>
		private ICommand cmdBackToTeamSelect;
		/// <summary>
		///		チーム名選択に戻るコマンドを表します。
		/// </summary>
		public ICommand CMDBackToTeamSelect => cmdBackToTeamSelect ?? ( cmdBackToTeamSelect = new BackToTeamSelectCommand( this ) );

		/// <summary>
		///		Victory画面に移動するコマンド
		/// </summary>
		private ICommand cmdToVictory;
		/// <summary>
		///		Victory画面に移動するコマンドを表します。
		/// </summary>
		public ICommand CMDToVictory => cmdToVictory ?? ( cmdToVictory = new VictoryCommand( this ) );

		/// <summary>
		///		設定画面に移動するコマンド
		/// </summary>
		private ICommand cmdConfiguration;
		/// <summary>
		///		設定画面に移動するコマンドを表します。
		/// </summary>
		public ICommand CMDConfiguration => cmdConfiguration ?? ( cmdConfiguration = new OpenFRCSettingCommand( this ) );

		/// <summary>
		///		アプリ終了のコマンド
		/// </summary>
		private ICommand cmdAppClose;
		/// <summary>
		///		アプリ終了のコマンドを表します。
		/// </summary>
		public ICommand CMDAppClose => cmdAppClose ?? ( cmdAppClose = new AppCloseCommand( this ) );

		/*
		 *		TeamSelect -> セッティングスタート、セッティングをスキップ、About、終了
		 *		SettingReady / PlayReady -> 中止、終了
		 *		SettingTime -> 中止、終了
		 *		PlayPrepairing -> ゲームスタート、チーム選択に戻る、終了
		 *		PlayTime -> Vゴール、中止、終了
		 *		GameSet、VGoal -> チーム選択に戻る、終了
		 *		
		 *		注：TeamSelect以外はチーム選択のコンボボックスをロックします。
		 */

		/// <summary>
		///		ボタンコマンドのリストを取得します。
		/// </summary>
		/// <remarks>
		///		ItemsControlのソースにこのプロパティをバインドし、
		///		ItemTemplate/DataTemplate内のButtonのプロパティに
		///		CommandButtonのプロパティをバインドします。
		/// </remarks>
		public CommandButton[] CommanddSetList =>
			frcCommandSetModel.FRCCommandSetList[NowState];

		#endregion

		#region StartSettingCommand

		/// <summary>
		///		セッティングを開始するコマンドです。
		/// </summary>
		private class StartSettingCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		StartSettingCommandの新しいインスタンスを生成します。
			/// </summary>
			public StartSettingCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
            }

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラーです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.StartSetting();
			}

		}

		/// <summary>
		///		セッティングを開始します。
		/// </summary>
		private void StartSetting() {
			NowState = FRCTimerState.SettingReady;
			timerState = TimerState.Normal;
			IsTimerRunning = true;
			timerModel.Start();
			NotifyFRCTimerPropertyChanged();
			dpTimer.Start();
		}

		#endregion

		#region SkipSettingCommand

		/// <summary>
		///		セッティングをスキップし、試合準備に移行するコマンドです。
		/// </summary>
		private class SkipSettingCommand : ICommand {
			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		SkipSettingCommandの新しいインスタンスを生成します。
			/// </summary>
			public SkipSettingCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
				CanExecuteChanged?.Invoke( sender, e );
            }

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラーです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.ComfirmAction?.Invoke( this, new ComfirmEventArgs( "セッティングをスキップし、試合準備画面に移りますか？", mainVM.SkipSetting ) );
			}
		}

		/// <summary>
		///		セッティングをスキップします。
		/// </summary>
		private void SkipSetting() {
			NowState = FRCTimerState.PlayPrepairing;
			DisplayMessageColor = Brushes.Lime;
			NotifyFRCTimerPropertyChanged();
		}

		#endregion

		#region StartPlayingCommand

		/// <summary>
		///		試合開始するコマンドです。
		/// </summary>
		private class StartPlayingCommand : ICommand {
			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		StartPlayingCommandの新しいインスタンスを生成します。
			/// </summary>
			public StartPlayingCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラーです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.StartPlay();
			}
		}

		/// <summary>
		///		セッティングを開始します。
		/// </summary>
		private void StartPlay() {
			NowState = FRCTimerState.PlayReady;
			timerState = TimerState.Normal;
			IsTimerRunning = true;
			timerModel.Start();
			NotifyFRCTimerPropertyChanged();
			dpTimer.Start();
		}

		#endregion

		#region CancelCommand

		/// <summary>
		///		セッティング・試合を中止するコマンドです。
		/// </summary>
		private class CancelCommand : ICommand {
			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		CancelCommandの新しいインスタンスを生成します。
			/// </summary>
			public CancelCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) {
				return true;
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラーです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				if( mainVM.NowState == FRCTimerState.SettingReady || mainVM.NowState == FRCTimerState.SettingTime )
					mainVM.ComfirmAction?.Invoke( this, new ComfirmEventArgs( "セッティングを中止し、チーム選択に戻りますか？", mainVM.CancelOperation ) );
				else mainVM.ComfirmAction?.Invoke( this, new ComfirmEventArgs( "試合を中止しますか？", mainVM.CancelOperation ) );
			}
		}

		/// <summary>
		///		セッティング・試合を中止します。
		/// </summary>
		private void CancelOperation() {
			// タイマーが動作している場合、停止します。
			if( IsTimerRunning ) {
				dpTimer.Stop();
				timerModel.Stop();
				IsTimerRunning = false;
			}
			// セッティングタイムの場合、チーム名選択に戻ります。
			if( NowState == FRCTimerState.SettingReady || NowState == FRCTimerState.SettingTime ) {
				NowState = FRCTimerState.TeamSelect;
			}
			// 試合の場合、試合準備に戻ります。
			else if( NowState == FRCTimerState.PlayReady || NowState == FRCTimerState.PlayTime ) {
				NowState = FRCTimerState.PlayPrepairing;
			}
			DisplayTimerColor = Brushes.White;
			PlaySoundEffect?.Invoke( this, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Stop ) );
			NotifyFRCTimerPropertyChanged();
		}

		#endregion

		#region BackToTeamSelectCommand

		/// <summary>
		///		チーム選択に戻るコマンドです。
		/// </summary>
		private class BackToTeamSelectCommand : ICommand {
			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		BackToTeamSelectCommandの新しいインスタンスを生成します。
			/// </summary>
			public BackToTeamSelectCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラーです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				if( mainVM.NowState == FRCTimerState.PlayPrepairing )
					mainVM.ComfirmAction?.Invoke( this, new ComfirmEventArgs( "チーム選択に戻りますか？", mainVM.BackToTeamSelect ) );
				else mainVM.BackToTeamSelect();
			}
		}

		/// <summary>
		///		チーム選択に戻ります。
		/// </summary>
		private void BackToTeamSelect() {
			NowState = FRCTimerState.TeamSelect;
			DisplayMessageColor = Brushes.Yellow;
			DisplayTimerColor = Brushes.White;
			NotifyFRCTimerPropertyChanged();
		}

		#endregion

		#region VictoryCommand

		/// <summary>
		///		Victory画面に移行するコマンドです。
		/// </summary>
		private class VictoryCommand : ICommand {
			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		VictoryCommandの新しいインスタンスを生成します。
			/// </summary>
			public VictoryCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラーです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.ComfirmAction?.Invoke( this, new ComfirmEventArgs( "試合終了しますか？", mainVM.ChangeToVictory ) );
			}
		}

		/// <summary>
		///		Victory画面に移動します。
		/// </summary>
		private void ChangeToVictory() {
			if( IsTimerRunning ) {
				dpTimer.Stop();
				timerModel.Stop();
				IsTimerRunning = false;
			}
			NowState = FRCTimerState.Victory;
			DisplayTimerColor = Brushes.White;
			PlaySoundEffect?.Invoke( this, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Finish ) );
			NotifyFRCTimerPropertyChanged();
		}

		#endregion

		#region OpenFRCSettingCommand

		/// <summary>
		///		FRCSettingを開くコマンドです。
		/// </summary>
		private class OpenFRCSettingCommand : ICommand {
			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		OpenFRCSettingCommandの新しいインスタンスを生成します。
			/// </summary>
			public OpenFRCSettingCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラーです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.OpenFRCTimerSetting();
            }
		}

		/// <summary>
		///		アプリの設定画面に移動します。
		/// </summary>
		private void OpenFRCTimerSetting() {
			NowState = FRCTimerState.FRCTimerSetting;
			TeamsListSet = new ObservableCollection<TeamInfo>( TeamsList );
			SelectedTeam = TeamsListSet.Count > 0 ? 0 : -1;
			NotifyFRCTimerSettingAllPropertyChanged();
			NotifyPropertyChanged( nameof( SelectedTeam ) );
		}

		#endregion

		#region AppCloseCommand

		/// <summary>
		///		アプリを終了するコマンドです。
		/// </summary>
		private class AppCloseCommand : ICommand {
			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		AppCloseCommandの新しいインスタンスを生成します。
			/// </summary>
			public AppCloseCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.ExitFRCTimer?.Invoke( mainVM, new CallbackEventArgs( mainVM.Close ) );
			}
		}

		/// <summary>
		///		アプリを終了します。
		/// </summary>
		private void Close() {
			dpTimer.Stop();
			timerModel.Stop();
			PlaySoundEffect?.Invoke( this, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Stop ) );
		}

		#endregion

		#endregion

		#region アプリの設定

		/// <summary>
		///		アプリ設定中であるかどうかを表します。
		/// </summary>
		public bool FRCTimerIsSetting => NowState == FRCTimerState.FRCTimerSetting;

		/// <summary>
		///		リストビューで選択しているチーム名の位置を表します。
		/// </summary>
		public int SelectedTeam { get; set; } = -1;

		/// <summary>
		///		リストビューにバインディングするチーム名リストです。
		/// </summary>
		public ObservableCollection<TeamInfo> TeamsListSet { get; set; }

		/// <summary>
		///		アプリの設定画面で使用しているプロパティの変更をまとめて通知します。
		/// </summary>
		public void NotifyFRCTimerSettingAllPropertyChanged() {
			NotifyPropertyChanged( nameof( FRCTimerIsSetting ) );
			NotifyPropertyChanged( nameof( CommanddSetList ) );
			NotifyPropertyChanged( nameof( TeamsListSet ) );
			NotifyPropertyChanged( nameof( SelectedTeam ) );
		}

		#region コマンド

		#region アプリ下部のボタン用

		/// <summary>
		///		チーム名リストを保存して戻るコマンド
		/// </summary>
		private ICommand cmdSaveTeamsList;
		/// <summary>
		///		チーム名リストを保存して戻るコマンドを表します。
		/// </summary>
		public ICommand CMDSaveTeamsList => cmdSaveTeamsList ?? ( cmdSaveTeamsList = new SaveTeamsListCommand( this ) );

		/// <summary>
		///		チーム名リストを保存せずに戻るコマンド
		/// </summary>
		private ICommand cmdCloseSetting;
		/// <summary>
		///		チーム名リストを保存せずに戻るコマンドを表します。
		/// </summary>
		public ICommand CMDCloseSetting => cmdCloseSetting ?? ( cmdCloseSetting = new CloseSettingCommand( this ) );

		/// <summary>
		///		チーム名リストを保存するコマンド
		/// </summary>
		private ICommand cmdApplyTeamsList;
		/// <summary>
		///		チーム名リストを保存するコマンドを表します。
		/// </summary>
		public ICommand CMDApplyTeamsList => cmdApplyTeamsList ?? ( cmdApplyTeamsList = new ApplyTeamsListCommand( this ) );

		#endregion

		#region 設定画面のボタン用

		/// <summary>
		///		チーム名をリストに追加するコマンド
		/// </summary>
		private ICommand cmdAddTeam;
		/// <summary>
		///		チーム名をリストに追加するコマンドを表します。
		/// </summary>
		public ICommand CMDAddTeam => cmdAddTeam ?? ( cmdAddTeam = new AddTeamCommand( this ) );

		/// <summary>
		///		チーム名を変更するコマンド
		/// </summary>
		private ICommand cmdRenameTeam;
		/// <summary>
		///		チーム名を変更するコマンドを表します。
		/// </summary>
		public ICommand CMDRenameTeam => cmdRenameTeam ?? ( cmdRenameTeam = new RenameTeamCommand( this ) );

		/// <summary>
		///		チーム名をリストから削除するコマンド
		/// </summary>
		private ICommand cmdRemoveTeam;
		/// <summary>
		///		チーム名をリストから削除するコマンドを表します。
		/// </summary>
		public ICommand CMDRemoveTeam => cmdRemoveTeam ?? ( cmdRemoveTeam = new RemoveTeamCommand( this ) );

		/// <summary>
		///		チーム名を1つ上に移動するコマンド
		/// </summary>
		private ICommand cmdMoveUpTeam;
		/// <summary>
		///		チーム名を1つ上に移動するコマンドを表します。
		/// </summary>
		public ICommand CMDMoveUpTeam => cmdMoveUpTeam ?? ( cmdMoveUpTeam = new MoveUpTeamCommand( this ) );

		/// <summary>
		///		チーム名を1つ下に移動するコマンド
		/// </summary>
		private ICommand cmdMoveDownTeam;
		/// <summary>
		///		チーム名を1つ下に移動するコマンドを表します。
		/// </summary>
		public ICommand CMDMoveDownTeam => cmdMoveDownTeam ?? ( cmdMoveDownTeam = new MoveDownTeamCommand( this ) );

		/// <summary>
		///		グループ名ごとにソートするコマンド
		/// </summary>
		private ICommand cmdSortTeamsByGroup;
		/// <summary>
		///		グループ名ごとにソートするコマンドを表します。
		/// </summary>
		public ICommand CMDSortTeamsByGroup => cmdSortTeamsByGroup ?? ( cmdSortTeamsByGroup = new SortTeamsByGroupCommand( this ) );

		/// <summary>
		///		チーム名リストを元に戻すコマンド
		/// </summary>
		private ICommand cmdRollbackTeamsList;
		/// <summary>
		///		チーム名リストを元に戻すコマンドを表します。
		/// </summary>
		public ICommand CMDRollbackTeamsList => cmdRollbackTeamsList ?? ( cmdRollbackTeamsList = new RollbackTeamsListCommand( this ) );

		/// <summary>
		///		チーム名リストを初期化するコマンド
		/// </summary>
		private ICommand cmdResetTeamsList;
		/// <summary>
		///		チーム名リストを初期化するコマンドを表します。
		/// </summary>
		public ICommand CMDResetTeamsList => cmdResetTeamsList ?? ( cmdResetTeamsList = new ResetTeamsListCommand( this ) );

		#endregion

		#endregion

		#region SaveTeamsListCommand

		/// <summary>
		///		チーム名リストを保存して、メイン画面に戻るコマンドです。
		/// </summary>
		private class SaveTeamsListCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		SaveTeamsListCommandの新しいインスタンスを生成します。
			/// </summary>
			public SaveTeamsListCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => mainVM.TeamsListSet?.Count > 0;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.ComfirmAction?.Invoke( this, new ComfirmEventArgs( "チーム名リストの変更を保存し、アプリの設定画面を閉じますか？", mainVM.SaveTeamListAndCloseSetting ) );
			}
		}

		/// <summary>
		///		チーム名リストを保存し、メイン画面に戻ります。
		/// </summary>
		private void SaveTeamListAndCloseSetting() {
			SaveTeamList();
			CloseFRCSetting();
        }

		#endregion

		#region CloseSettingCommand

		/// <summary>
		///		チーム名リストを保存せず、メイン画面に戻るコマンドです。
		/// </summary>
		private class CloseSettingCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		CloseSettingCommandの新しいインスタンスを生成します。
			/// </summary>
			public CloseSettingCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.ComfirmAction?.Invoke(
					this,
					new ComfirmEventArgs(
						"アプリの設定画面を閉じますか？\n（注意：保存していない変更部分は失われます。）",
						mainVM.CloseFRCSetting
					)
				);
			}
		}

		/// <summary>
		///		チーム名リストを保存せず、メイン画面に戻ります。
		/// </summary>
		private void CloseFRCSetting() {
			NowState = FRCTimerState.TeamSelect;
			TeamsListSet = null;
			NotifyPropertyChanged( nameof( FRCTimerIsSetting ) );
			NotifyFRCTimerPropertyChanged();
		}

		#endregion

		#region ApplyTeamsListCommand

		/// <summary>
		///		チーム名リストを保存するコマンドです。
		/// </summary>
		private class ApplyTeamsListCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		ApplyTeamsListCommandの新しいインスタンスを生成します。
			/// </summary>
			public ApplyTeamsListCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => mainVM.TeamsListSet?.Count > 0;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.ComfirmAction?.Invoke( this, new ComfirmEventArgs( "チーム名リストの変更を保存しますか？", mainVM.SaveTeamList ) );
			}
		}

		/// <summary>
		///		チーム名リストを保存します。
		/// </summary>
		private void SaveTeamList() {
			teamsModel.SaveTeamsListAsync( TeamsListSet );
		}

		#endregion

		#region AddTeam

		/// <summary>
		///		チーム名をリストに追加するコマンドです。
		/// </summary>
		private class AddTeamCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		AddTeamCommandの新しいインスタンスを生成します。
			/// </summary>
			public AddTeamCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.TeamsListSetAddItem();
			}
		}

		/// <summary>
		///		チーム名をリストにチームを追加します。
		/// </summary>
		private void TeamsListSetAddItem() {
			// チーム情報を入力するダイアログを生成します。
			TeamInfoEditDialog tied = new TeamInfoEditDialog(
				true,
				new TeamInfo { TeamName = "New Team", GroupName = "New Group" },
				TeamsListSet.Select( _ => _.GroupName ).Distinct()
			);
			// ダイアログを表示します。
			tied.ShowDialog();
			// OKボタンを押していたら、チーム名をリストに追加します。
			if( tied.DialogResult ?? false ) {
				TeamsListSet.Add( tied.Team );
				NotifyPropertyChanged( nameof( TeamsListSet ) );
			}
		}

		#endregion

		#region RenameTeam

		/// <summary>
		///		チーム名をリストにあるチーム名を編集するコマンドです。
		/// </summary>
		private class RenameTeamCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		RenameTeamCommandの新しいインスタンスを生成します。
			/// </summary>
			public RenameTeamCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.TeamsListSetRenameItem();
			}
		}

		/// <summary>
		///		チーム名をリストで選択したチームの情報を編集します。
		/// </summary>
		private void TeamsListSetRenameItem() {
			// チーム情報を編集するダイアログを生成します。
			try {
				TeamInfoEditDialog tied = new TeamInfoEditDialog(
					false,
					TeamsListSet[SelectedTeam],
					TeamsListSet.Select( _ => _.GroupName ).Distinct()
				);
				// ダイアログを表示します。
				tied.ShowDialog();
				// OKボタンを押していたら、チーム名をリストに反映させます。
				if( tied.DialogResult ?? false ) {
					TeamsListSet[SelectedTeam] = tied.Team;
					NotifyPropertyChanged( nameof( TeamsListSet ) );
					NotifyPropertyChanged( nameof( SelectedTeam ) );
				}
			}
			catch { }
		}

		#endregion

		#region RemoveTeam

		/// <summary>
		///		チーム名をリストから削除するコマンドです。
		/// </summary>
		private class RemoveTeamCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		RemoveTeamCommandの新しいインスタンスを生成します。
			/// </summary>
			public RemoveTeamCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.TeamsListSetRemoveItem();
			}
		}

		/// <summary>
		///		選択したチームをチーム名をリストから削除します。
		/// </summary>
		private void TeamsListSetRemoveItem() {
			try {
				TeamsListSet.RemoveAt( SelectedTeam );
				NotifyPropertyChanged( nameof( TeamsListSet ) );
				NotifyPropertyChanged( nameof( SelectedTeam ) );
			}
			catch { }
		}

		#endregion

		#region MoveUpTeam

		/// <summary>
		///		チーム名を1つ上に移動するコマンドです。
		/// </summary>
		private class MoveUpTeamCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		MoveUpTeamCommandの新しいインスタンスを生成します。
			/// </summary>
			public MoveUpTeamCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => mainVM.TeamsListSet?.Count > 1;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.TeamsListSetMoveUpItem();
			}
		}

		/// <summary>
		///		選択したチームを1つ上に移動します。
		/// </summary>
		private void TeamsListSetMoveUpItem() {
			try {
				// リストの先頭の場合、末尾に移動します。
				TeamsListSet.Move( SelectedTeam, SelectedTeam > 0 ? SelectedTeam - 1 : TeamsListSet.Count - 1 );
				NotifyPropertyChanged( nameof( TeamsListSet ) );
				NotifyPropertyChanged( nameof( SelectedTeam ) );
			}
			catch { }
		}

		#endregion

		#region MoveDownTeam

		/// <summary>
		///		チーム名を1つ下に移動するコマンドです。
		/// </summary>
		private class MoveDownTeamCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		MoveDownTeamCommandの新しいインスタンスを生成します。
			/// </summary>
			public MoveDownTeamCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => mainVM.TeamsListSet?.Count > 1;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter">コマンドのパラメーター</param>
			public void Execute( object parameter ) {
				mainVM.TeamsListSetMoveDownItem();
			}
		}

		/// <summary>
		///		選択したチームを1つ下に移動します。
		/// </summary>
		private void TeamsListSetMoveDownItem() {
			try {
				// リストの末尾の場合、先頭に移動します。
				TeamsListSet.Move( SelectedTeam, SelectedTeam < TeamsListSet.Count - 1 ? SelectedTeam + 1 : 0 );
				NotifyPropertyChanged( nameof( TeamsListSet ) );
				NotifyPropertyChanged( nameof( SelectedTeam ) );
			}
			catch { }
		}

		#endregion

		#region GatherTeamByGroup

		/// <summary>
		///		グループ名ごとにまとめてソートするコマンドです。
		/// </summary>
		private class SortTeamsByGroupCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		SortTeamsByGroupCommandの新しいインスタンスを生成します。
			/// </summary>
			public SortTeamsByGroupCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => mainVM.TeamsListSet?.Count > 1;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter"></param>
			public void Execute( object parameter ) {
				mainVM.TeamsListSetGatherTeamByGroupItem();
			}
		}

		/// <summary>
		///		チーム名リストをグループ名単位でソートします。
		/// </summary>
		private void TeamsListSetGatherTeamByGroupItem() {
			TeamsListSet = new ObservableCollection<TeamInfo>( TeamsListSet.OrderBy( _ => _.GroupName ) );
			NotifyPropertyChanged( nameof( TeamsListSet ) );
			NotifyPropertyChanged( nameof( SelectedTeam ) );
		}

		#endregion

		#region RollbackTeamsList

		/// <summary>
		///		チーム名リストを元に戻すコマンドです。
		/// </summary>
		private class RollbackTeamsListCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		RollbackTeamsListCommandの新しいインスタンスを生成します。
			/// </summary>
			public RollbackTeamsListCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter"></param>
			public void Execute( object parameter ) {
				mainVM.ComfirmAction?.Invoke( this, new ComfirmEventArgs( "設定画面内のチーム名リストを変更前の状態に復元しますか？", mainVM.RollbackTeamsList ) );
			}
		}

		/// <summary>
		///		チーム名リストをアプリの設定画面を開く前の状態に戻します。
		/// </summary>
		private void RollbackTeamsList() {
			TeamsListSet = new ObservableCollection<TeamInfo>( TeamsList );
			SelectedTeam = 0;
			NotifyPropertyChanged( nameof( TeamsListSet ) );
			NotifyPropertyChanged( nameof( SelectedTeam ) );
		}

		#endregion

		#region ResetTeamsList

		/// <summary>
		///		チーム名リストを初期化するコマンドです。
		/// </summary>
		private class ResetTeamsListCommand : ICommand {

			/// <summary>
			///		MainViewModelの参照
			/// </summary>
			private MainViewModel mainVM;

			/// <summary>
			///		ResetTeamsListCommandの新しいインスタンスを生成します。
			/// </summary>
			public ResetTeamsListCommand( MainViewModel mvm ) {
				mainVM = mvm;
				// ViewModelのプロパティ変更通知と連動させます。
				mainVM.PropertyChanged += ( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
			}

			/// <summary>
			///		CanExecuteが変更されたことを通知するイベントハンドラです。
			/// </summary>
			public event EventHandler CanExecuteChanged;

			/// <summary>
			///		実行可能かどうか判定します。
			/// </summary>
			/// <param name="parameter">コマンドパラメーター</param>
			/// <returns>true：実行できます。 / false：実行できません。</returns>
			/// <remarks>バインド先のコントロールのIsEnabledに対応しています。</remarks>
			public bool CanExecute( object parameter ) => true;

			/// <summary>
			///		コマンドを実行します。
			/// </summary>
			/// <param name="parameter"></param>
			public void Execute( object parameter ) {
				mainVM.ComfirmAction?.Invoke( this, new ComfirmEventArgs( "設定画面内のチーム名リストを初期化しますか？\n※ メイン画面に反映させたい時は、初期化後に保存します。", mainVM.ResetTeamsList ) );
			}
		}

		/// <summary>
		///		チーム名リストを初期化します。
		/// </summary>
		private void ResetTeamsList() {
			TeamsListSet.Clear();
			for( int i = 1; i <= 5; i++ ) {
				TeamsListSet.Add( new TeamInfo { TeamName = $"Team {i}", GroupName = "Team Group" } );
			}
			SelectedTeam = 0;
			NotifyPropertyChanged( nameof( TeamsListSet ) );
			NotifyPropertyChanged( nameof( SelectedTeam ) );
		}

		#endregion

		#endregion

	}
}

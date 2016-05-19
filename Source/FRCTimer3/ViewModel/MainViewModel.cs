using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FRCTimer3 {

	/// <summary>
	///		ViewModelクラスを定義します。
	/// </summary>
	class MainViewModel : ViewModelBase {

		/// <summary>
		///		Chronoir Robocon Timerの状態と画面に表示するメッセージのペアのリストを表します。
		/// </summary>
		private Dictionary<FRCTimerState, string> message = new Dictionary<FRCTimerState, string> {
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
		///		アプリの情報を取得します。
		/// </summary>
		public FileVersionInfo AppVer { get; private set; }

		#region Model

		/// <summary>
		///		TeamsModelクラスを表します。
		/// </summary>
		private TeamsModel teamsModel = new TeamsModel();

		/// <summary>
		///		TimerModelクラスを表します。
		/// </summary>
		private TimerModel timerModel = new TimerModel();

		/// <summary>
		///		FRCCommandSetModelクラスを表します。
		/// </summary>
		private FRCCommandSetModel frcCommandSetModel;

		#endregion

		/// <summary>
		///		Chronoir Robocon Timerの現在の状態を取得します。
		/// </summary>
		public FRCTimerState NowState { get; private set; } =
			FRCTimerState.TeamSelect;

		/// <summary>
		///		MainViewModelの新しいインスタンスを生成します。
		/// </summary>
		public MainViewModel() {
			// TeamsModelのイベントハンドラに処理を登録します。
			teamsModel.PropertyChanged +=
				( sender, e ) =>
					PropertyChangedFromThis?.Invoke( this, new PropertyChangedEventArgs( e.PropertyName ) );
			teamsModel.LoadTeamsListCompleted += ( sender, e ) => {
				RedTeamIndex = BlueTeamIndex = -1;
				LoadTeamsListCompleted?.Invoke(
					this,
					new NotifyResultEventArgs<LoadTeamsListResult, bool, bool>(
						e.Result,
						// 読み込みに成功したら、チーム名選択のコンボボックスに反映させます。
						_ => {
							RedTeamIndex = 0;
							BlueTeamIndex = TeamsList.Count >= 2 ? 1 : 0;
							NotifyPropertyChanged( nameof( TeamsList ) );
							NotifyPropertyChanged( nameof( RedTeamIndex ) );
							NotifyPropertyChanged( nameof( BlueTeamIndex ) );
						},
						// 読み込みに失敗したら、初期値に設定します。
						_ => teamsModel.ResetTeamsList( _ )
					)
				);
			};
			teamsModel.ResetTeamsListCompleted +=
				( sender, e ) =>
					ResetTeamsListCompleted?.Invoke(
						this,
						new NotifyResultEventArgs<SaveTeamsListResult, bool, bool>(
							e.Result,
							// 初期化したら、チーム名選択のコンボボックスに反映させます。
							_ => {
								RedTeamIndex = 0;
								BlueTeamIndex = TeamsList.Count >= 2 ? 1 : 0;
								NotifyPropertyChanged( nameof( TeamsList ) );
								NotifyPropertyChanged( nameof( RedTeamIndex ) );
								NotifyPropertyChanged( nameof( BlueTeamIndex ) );
							},
							null
						)
					);
			teamsModel.SaveTeamsListCompleted +=
				( sender, e ) =>
					SaveTeamsListCompleted?.Invoke(
						this,
						new NotifyResultEventArgs<SaveTeamsListResult, bool, bool>(
							e.Result,
							// 保存に成功したら、チーム名選択のコンボボックスに反映させます。
							_ => {
								RedTeamIndex = 0;
								BlueTeamIndex = TeamsList.Count >= 2 ? 1 : 0;
								NotifyPropertyChanged( nameof( TeamsList ) );
								NotifyPropertyChanged( nameof( RedTeamIndex ) );
								NotifyPropertyChanged( nameof( BlueTeamIndex ) );
							},
							null
						)
					);

			timerModel.LoadSettingsCompleted +=
				( sender, e ) =>
					LoadSettingsCompleted?.Invoke(
						this,
						new NotifyResultEventArgs<LoadSettingsResult, bool, bool>(
							e.Result,
							_ => {
								NotifyPropertyChanged( nameof( DisplayTimer ) );
								message[FRCTimerState.Victory] = TimerModel.VictoryMessage;
							},
							null
						)
					);

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

			TeamsListSet = new ObservableCollection<TeamInfo>();
			AppVer = FileVersionInfo.GetVersionInfo( System.Reflection.Assembly.GetExecutingAssembly().Location );
		}

		/// <summary>
		///		チーム名リストと時間定義を読み込みます
		/// </summary>
		public void Init() {
			teamsModel.LoadTeamsList();
			timerModel.LoadSettings(
				_modifyDisplayingTimer: MotifyDisplayingTimer,
				_startSettingTime: StartSettingTime, _finishSettingTime: FinishSettingTime, _startPlayTime: StartPlayTime, _finishPlayTime: FinishPlayTime,
				_notifyLast10sec: NotifyLast10sec, _finishAutoMachineLanchStartTime: NotifyFinishAutoMachineLanchStartTime,
				_playReadySound: PlayReadySound, _playStartSound: PlayStartSound, _playLast3secSound: PlayLast3secSound, _playFinishSound: PlayFinishSound
			);
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
			get	{
				TimeSpan ts = TimeSpan.Zero;
				switch( NowState ) {
					case FRCTimerState.TeamSelect:
						ts = TimerModel.SettingTime;
						break;
					case FRCTimerState.PlayPrepairing:
						ts = TimerModel.PlayTime;
						break;
					case FRCTimerState.SettingReady:
					case FRCTimerState.PlayReady:
					case FRCTimerState.SettingTime:
					case FRCTimerState.PlayTime:
						ts = timerModel.Duration;
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
		public bool CanSelectTeam =>
			NowState == FRCTimerState.TeamSelect;


		#region TimerModelのイベント

		/// <summary>
		///		タイマーの残り時間を更新をした時に実行します。
		/// </summary>
		private void MotifyDisplayingTimer( object sender, EventArgs e ) {
			NotifyPropertyChanged( nameof( DisplayTimer ) );
		}

		/// <summary>
		///		セッティングタイムを開始時に実行します。
		/// </summary>
		private void StartSettingTime( object sender, EventArgs e ) {
			NowState = FRCTimerState.SettingTime;
			timerModel.Start( TimerType.Setting );
			NotifyFRCTimerPropertyChanged();
		}

		/// <summary>
		///		セッティングタイムを終了時に実行します。
		/// </summary>
		private void FinishSettingTime( object sender, EventArgs e ) {
			NowState = FRCTimerState.PlayPrepairing;
			DisplayMessageColor = Brushes.Lime;
			DisplayTimerColor = Brushes.White;
			IsTimerRunning = false;
			timerModel.Stop();
			NotifyFRCTimerPropertyChanged();
		}

		/// <summary>
		///		試合開始時に実行します。
		/// </summary>
		private void StartPlayTime( object sender, EventArgs e ) {
			DisplayTimerColor = TimerModel.AutoMachineLanchTimeLimit > TimeSpan.Zero ? Brushes.Aqua : Brushes.White;
			NowState = FRCTimerState.PlayTime;
			timerModel.Start( TimerType.Play );
			NotifyFRCTimerPropertyChanged();
		}

		/// <summary>
		///		試合終了時に実行します。
		/// </summary>
		private void FinishPlayTime( object sender, EventArgs e ) {
			NowState = FRCTimerState.GameSet;
			DisplayTimerColor = Brushes.White;
			IsTimerRunning = false;
			timerModel.Stop();
			NotifyFRCTimerPropertyChanged();
		}

		/// <summary>
		///		残り時間が10秒になった時に実行します。
		/// </summary>
		private void NotifyLast10sec( object sender, EventArgs e ) {
			DisplayTimerColor = Brushes.HotPink;
			NotifyPropertyChanged( nameof( DisplayTimerColor ) );
		}

		/// <summary>
		///		自動機の発進時間が終了した時に実行します。
		/// </summary>
		private void NotifyFinishAutoMachineLanchStartTime( object sender, EventArgs e ) {
			DisplayTimerColor = Brushes.White;
			NotifyPropertyChanged( nameof( DisplayTimerColor ) );
		}

		/// <summary>
		///		
		/// </summary>
		private void PlayReadySound( object sender, EventArgs e ) {
			PlaySoundEffect?.Invoke( sender, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Ready ) );
		}

		/// <summary>
		///		
		/// </summary>
		private void PlayStartSound( object sender, EventArgs e ) {
			PlaySoundEffect?.Invoke( sender, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Start ) );
		}

		/// <summary>
		///		
		/// </summary>
		private void PlayLast3secSound( object sender, EventArgs e ) {
			PlaySoundEffect?.Invoke( sender, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Last3 ) );
		}

		/// <summary>
		///		
		/// </summary>
		private void PlayFinishSound( object sender, EventArgs e ) {
			PlaySoundEffect?.Invoke( sender, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Finish ) );
		}

		#endregion

		#region イベントハンドラー

		/// <summary>
		///		チーム名リストを読み込んだ後に発生します。
		/// </summary>
		public event NotifyResultEventHandler<LoadTeamsListResult, bool, bool> LoadTeamsListCompleted;

		/// <summary>
		///		時間定義ファイをを読み込んだ後に発生します。
		/// </summary>
		public event NotifyResultEventHandler<LoadSettingsResult, bool, bool> LoadSettingsCompleted;

		/// <summary>
		///		チーム名リストを初期化した後に発生します。
		/// </summary>
		public event NotifyResultEventHandler<SaveTeamsListResult, bool, bool> ResetTeamsListCompleted;

		/// <summary>
		///		チーム名リストを保存した後に発生します。
		/// </summary>
		public event NotifyResultEventHandler<SaveTeamsListResult, bool, bool> SaveTeamsListCompleted;

		/// <summary>
		///		アプリを終了する時に発生します。
		/// </summary>
		public event ComfirmEventHandler ExitFRCTimer;

		/// <summary>
		///		確認ダイアログを表示する時に発生します。
		/// </summary>
		public event ComfirmEventHandler ComfirmAction;

		/// <summary>
		///		効果音を鳴らす時に発生します。
		/// </summary>
		public event FRCSoundEffectTypeEventHandler PlaySoundEffect;

		#endregion

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

		#region コマンド関連

		#region コマンド

		// コマンドオブジェクトは遅延初期化させます。

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
		///		セッティング開始するコマンド
		/// </summary>
		private ICommand cmdStartSetting;
		/// <summary>
		///		セッティング開始するコマンドを表します。
		/// </summary>
		public ICommand CMDStartSetting =>
			cmdStartSetting ??
			( cmdStartSetting = new ActionCommand( this, p => StartSetting() ) );

		/// <summary>
		///		セッティングを開始します。
		/// </summary>
		private void StartSetting() {
			NowState = FRCTimerState.SettingReady;
			IsTimerRunning = true;
			timerModel.Start( TimerType.SettingReady );
			NotifyFRCTimerPropertyChanged();
			//dpTimer.Start();
		}

		#endregion

		#region SkipSettingCommand

		/// <summary>
		///		セッティングスキップするコマンド
		/// </summary>
		private ICommand cmdSkipSetting;
		/// <summary>
		///		セッティングスキップするコマンドを表します。
		/// </summary>
		public ICommand CMDSkipSetting =>
			cmdSkipSetting ??
			( cmdSkipSetting = new ActionCommand(
				this,
				p => ComfirmAction?.Invoke( this, new ComfirmEventArgs( "セッティングをスキップし、試合準備画面に移りますか？", SkipSetting ) )
			) );

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
		///		試合開始するコマンド
		/// </summary>
		private ICommand cmdStartPlaying;
		/// <summary>
		///		試合開始するコマンドを表します。
		/// </summary>
		public ICommand CMDStartPlaying =>
			cmdStartPlaying ?? ( cmdStartPlaying = new ActionCommand( this, p => StartPlay() ) );

		/// <summary>
		///		試合を開始します。
		/// </summary>
		private void StartPlay() {
			NowState = FRCTimerState.PlayReady;
			IsTimerRunning = true;
			timerModel.Start( TimerType.PlayReady );
			NotifyFRCTimerPropertyChanged();
			//dpTimer.Start();
		}

		#endregion

		#region CancelCommand

		/// <summary>
		///		セッティング、試合を中止するコマンド
		/// </summary>
		private ICommand cmdCancel;
		/// <summary>
		///		セッティング、試合を中止するコマンドを表します。
		/// </summary>
		public ICommand CMDCancel =>
			cmdCancel ??
			( cmdCancel = new ActionCommand(
				this,
				p => {
					if( NowState == FRCTimerState.SettingReady || NowState == FRCTimerState.SettingTime ) {
						ComfirmAction?.Invoke( this, new ComfirmEventArgs( "セッティングを中止し、チーム選択に戻りますか？", CancelOperation ) );
					}
					else {
						ComfirmAction?.Invoke( this, new ComfirmEventArgs( "試合を中止しますか？", CancelOperation ) );
					}
				}
			) );

		/// <summary>
		///		セッティング・試合を中止します。
		/// </summary>
		private void CancelOperation() {
			// タイマーが動作している場合、停止します。
			if( IsTimerRunning ) {
				//dpTimer.Stop();
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
		///		チーム名選択に戻るコマンド
		/// </summary>
		private ICommand cmdBackToTeamSelect;
		/// <summary>
		///		チーム名選択に戻るコマンドを表します。
		/// </summary>
		public ICommand CMDBackToTeamSelect =>
			cmdBackToTeamSelect ??
			( cmdBackToTeamSelect = new ActionCommand(
				this,
				p => {
					if( NowState == FRCTimerState.PlayPrepairing ) {
						ComfirmAction?.Invoke( this, new ComfirmEventArgs( "チーム選択に戻りますか？", BackToTeamSelect ) );
					}
					else {
						BackToTeamSelect();
					}
				}
			) );

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
		///		Victory画面に移動するコマンド
		/// </summary>
		private ICommand cmdToVictory;
		/// <summary>
		///		Victory画面に移動するコマンドを表します。
		/// </summary>
		public ICommand CMDToVictory =>
			cmdToVictory ??
			( cmdToVictory = new ActionCommand(
				this,
				p => ComfirmAction?.Invoke( this, new ComfirmEventArgs( "試合終了しますか？", ChangeToVictory ) )
			) );

		/// <summary>
		///		Victory画面に移動します。
		/// </summary>
		private void ChangeToVictory() {
			if( IsTimerRunning ) {
				//dpTimer.Stop();
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
		///		設定画面に移動するコマンド
		/// </summary>
		private ICommand cmdConfiguration;
		/// <summary>
		///		設定画面に移動するコマンドを表します。
		/// </summary>
		public ICommand CMDConfiguration =>
			cmdConfiguration ?? ( cmdConfiguration = new ActionCommand( this, p => OpenFRCTimerSetting() ) );

		/// <summary>
		///		アプリの設定画面に移動します。
		/// </summary>
		private void OpenFRCTimerSetting() {
			NowState = FRCTimerState.FRCTimerSetting;
			NotifyPropertyChanged( nameof( FRCTimerIsSetting ) );
			NotifyPropertyChanged( nameof( CommanddSetList ) );
			RollbackTeamsList();
		}

		#endregion

		#region AppCloseCommand

		/// <summary>
		///		アプリ終了のコマンド
		/// </summary>
		private ICommand cmdAppClose;
		/// <summary>
		///		アプリ終了のコマンドを表します。
		/// </summary>
		public ICommand CMDAppClose =>
			cmdAppClose ??
			( cmdAppClose = new ActionCommand(
				this,
				p => ExitFRCTimer?.Invoke( this, new ComfirmEventArgs( "", Close ) )
			) );

		/// <summary>
		///		アプリを終了します。
		/// </summary>
		private void Close() {
			//dpTimer.Stop();
			timerModel.Stop();
			PlaySoundEffect?.Invoke( this, new FRCSoundEffectTypeEventArgs( FRCSoundEffectType.Stop ) );
		}

		#endregion

		#endregion

		#region アプリの設定

		/// <summary>
		///		アプリは設定中であるかどうかを表す値を取得します。
		/// </summary>
		public bool FRCTimerIsSetting =>
			NowState == FRCTimerState.FRCTimerSetting;

		/// <summary>
		///		リストビューで選択しているチーム名の位置を取得・設定します。
		/// </summary>
		public int SelectedTeam { get; set; } = -1;

		/// <summary>
		///		リストビューにバインディングするチーム名リストを取得します。
		/// </summary>
		public ObservableCollection<TeamInfo> TeamsListSet { get; private set; }

		#region コマンド

		#region アプリ下部のボタン用

		#region SaveTeamsListCommand

		/// <summary>
		///		チーム名リストを保存して戻るコマンド
		/// </summary>
		private ICommand cmdSaveTeamsList;
		/// <summary>
		///		チーム名リストを保存して戻るコマンドを表します。
		/// </summary>
		public ICommand CMDSaveTeamsList =>
			cmdSaveTeamsList ??
			( cmdSaveTeamsList = new ActionCommand(
				this,
				p => ComfirmAction?.Invoke( this, new ComfirmEventArgs( "チーム名リストの変更を保存し、アプリの設定画面を閉じますか？", SaveTeamListAndCloseSetting ) ),
				p => TeamsListSet != null && TeamsListSet.Any()
			) );

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
		///		チーム名リストを保存せずに戻るコマンド
		/// </summary>
		private ICommand cmdCloseSetting;
		/// <summary>
		///		チーム名リストを保存せずに戻るコマンドを表します。
		/// </summary>
		public ICommand CMDCloseSetting =>
			cmdCloseSetting ??
			( cmdCloseSetting = new ActionCommand(
				this,
				p => ComfirmAction?.Invoke(
						this,
						new ComfirmEventArgs(
							"アプリの設定画面を閉じますか？\n（注意：保存していない変更部分は失われます。）",
							CloseFRCSetting
						)
					)
			) );

		/// <summary>
		///		チーム名リストを保存せず、メイン画面に戻ります。
		/// </summary>
		private void CloseFRCSetting() {
			NowState = FRCTimerState.TeamSelect;
			NotifyPropertyChanged( nameof( FRCTimerIsSetting ) );
			NotifyPropertyChanged( nameof( CommanddSetList ) );
		}

		#endregion

		#region ApplyTeamsListCommand

		/// <summary>
		///		チーム名リストを保存するコマンド
		/// </summary>
		private ICommand cmdApplyTeamsList;
		/// <summary>
		///		チーム名リストを保存するコマンドを表します。
		/// </summary>
		public ICommand CMDApplyTeamsList =>
			cmdApplyTeamsList ??
			( cmdApplyTeamsList = new ActionCommand(
				this,
				p => ComfirmAction?.Invoke( this, new ComfirmEventArgs( "チーム名リストの変更を保存しますか？", SaveTeamList ) ),
				p => TeamsListSet != null && TeamsListSet.Any()
			) );

		/// <summary>
		///		チーム名リストを保存します。
		/// </summary>
		private void SaveTeamList() {
			teamsModel.SaveTeamsList( TeamsListSet );
		}

		#endregion

		#endregion

		#region 設定画面のボタン用

		#region AddTeam

		/// <summary>
		///		チーム名をリストに追加するコマンド
		/// </summary>
		private ICommand cmdAddTeam;
		/// <summary>
		///		チーム名をリストに追加するコマンドを表します。
		/// </summary>
		public ICommand CMDAddTeam =>
			cmdAddTeam ?? ( cmdAddTeam = new ActionCommand( this, p => TeamsListSetAddItem() ) );

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
				SelectedTeam = TeamsListSet.Count - 1;
				NotifyPropertyChanged( nameof( SelectedTeam ) );
			}
		}

		#endregion

		#region RenameTeam

		/// <summary>
		///		チーム名を変更するコマンド
		/// </summary>
		private ICommand cmdRenameTeam;
		/// <summary>
		///		チーム名を変更するコマンドを表します。
		/// </summary>
		public ICommand CMDRenameTeam =>
			cmdRenameTeam ??
			( cmdRenameTeam = new ActionCommand( this, p => TeamsListSetRenameItem() ) );

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
					NotifyPropertyChanged( nameof( SelectedTeam ) );
				}
			}
			catch { }
		}

		#endregion

		#region RemoveTeam

		/// <summary>
		///		チーム名をリストから削除するコマンド
		/// </summary>
		private ICommand cmdRemoveTeam;
		/// <summary>
		///		チーム名をリストから削除するコマンドを表します。
		/// </summary>
		public ICommand CMDRemoveTeam =>
			cmdRemoveTeam ?? ( cmdRemoveTeam = new ActionCommand( this, p => TeamsListSetRemoveItem() ) );

		/// <summary>
		///		選択したチームをチーム名をリストから削除します。
		/// </summary>
		private void TeamsListSetRemoveItem() {
			try {
				TeamsListSet.RemoveAt( SelectedTeam );
				NotifyPropertyChanged( nameof( SelectedTeam ) );
			}
			catch { }
		}

		#endregion

		#region MoveUpTeam

		/// <summary>
		///		チーム名を1つ上に移動するコマンド
		/// </summary>
		private ICommand cmdMoveUpTeam;
		/// <summary>
		///		チーム名を1つ上に移動するコマンドを表します。
		/// </summary>
		public ICommand CMDMoveUpTeam =>
			cmdMoveUpTeam ??
			( cmdMoveUpTeam = new ActionCommand(
				this,
				p => TeamsListSetMoveUpItem(),
				p => TeamsListSet != null && TeamsListSet.Count > 1
			) );

		/// <summary>
		///		選択したチームを1つ上に移動します。
		/// </summary>
		private void TeamsListSetMoveUpItem() {
			try {
				// リストの先頭の場合、末尾に移動します。
				TeamsListSet.Move( SelectedTeam, SelectedTeam > 0 ? SelectedTeam - 1 : TeamsListSet.Count - 1 );
				NotifyPropertyChanged( nameof( SelectedTeam ) );
			}
			catch { }
		}

		#endregion

		#region MoveDownTeam

		/// <summary>
		///		チーム名を1つ下に移動するコマンド
		/// </summary>
		private ICommand cmdMoveDownTeam;
		/// <summary>
		///		チーム名を1つ下に移動するコマンドを表します。
		/// </summary>
		public ICommand CMDMoveDownTeam =>
			cmdMoveDownTeam ??
			( cmdMoveDownTeam = new ActionCommand(
				this,
				p => TeamsListSetMoveDownItem(),
				p => TeamsListSet != null && TeamsListSet.Count > 1
			) );

		/// <summary>
		///		選択したチームを1つ下に移動します。
		/// </summary>
		private void TeamsListSetMoveDownItem() {
			try {
				// リストの末尾の場合、先頭に移動します。
				TeamsListSet.Move( SelectedTeam, SelectedTeam < TeamsListSet.Count - 1 ? SelectedTeam + 1 : 0 );
				NotifyPropertyChanged( nameof( SelectedTeam ) );
			}
			catch { }
		}

		#endregion

		#region GatherTeamByGroup

		/// <summary>
		///		グループ名ごとにソートするコマンド
		/// </summary>
		private ICommand cmdGatherTeamsByGroup;
		/// <summary>
		///		グループ名ごとにソートするコマンドを表します。
		/// </summary>
		public ICommand CMDGatherTeamsByGroup =>
			cmdGatherTeamsByGroup ??
			( cmdGatherTeamsByGroup = new ActionCommand(
				this,
				p => TeamsListSetGatherTeamByGroupItem(),
				p => TeamsListSet != null && TeamsListSet.Count > 1
			) );

		/// <summary>
		///		チーム名リストをグループ名単位でソートします。
		/// </summary>
		private void TeamsListSetGatherTeamByGroupItem() {
			// ToListメソッドで即時実行させます。
			var tmp = TeamsListSet.OrderBy( _ => _.GroupName ).ToList();
			TeamsListSet.Clear();
			foreach( var item in tmp ) {
				TeamsListSet.Add( item );
			}
			NotifyPropertyChanged( nameof( SelectedTeam ) );
		}

		#endregion

		#region RollbackTeamsList

		/// <summary>
		///		チーム名リストを元に戻すコマンド
		/// </summary>
		private ICommand cmdRollbackTeamsList;
		/// <summary>
		///		チーム名リストを元に戻すコマンドを表します。
		/// </summary>
		public ICommand CMDRollbackTeamsList =>
			cmdRollbackTeamsList ??
			( cmdRollbackTeamsList = new ActionCommand(
				this,
				p => ComfirmAction?.Invoke( this, new ComfirmEventArgs( "設定画面内のチーム名リストを変更前の状態に復元しますか？", RollbackTeamsList ) )
			) );

		/// <summary>
		///		チーム名リストをアプリの設定画面を開く前の状態に戻します。
		/// </summary>
		private void RollbackTeamsList() {
			TeamsListSet.Clear();
			foreach( var item in TeamsList ) {
				TeamsListSet.Add( item );
			}
			SelectedTeam = TeamsListSet.Count > 0 ? 0 : -1;
			NotifyPropertyChanged( nameof( SelectedTeam ) );
		}

		#endregion

		#region ResetTeamsList

		/// <summary>
		///		チーム名リストを初期化するコマンド
		/// </summary>
		private ICommand cmdResetTeamsList;
		/// <summary>
		///		チーム名リストを初期化するコマンドを表します。
		/// </summary>
		public ICommand CMDResetTeamsList =>
			cmdResetTeamsList ??
			( cmdResetTeamsList = new ActionCommand(
				this,
				p => ComfirmAction?.Invoke( this, new ComfirmEventArgs( "設定画面内のチーム名リストを初期化しますか？\n※ メイン画面に反映させたい時は、初期化後に保存します。", ResetTeamsList ) )
			) );

		/// <summary>
		///		チーム名リストを初期化します。
		/// </summary>
		private void ResetTeamsList() {
			TeamsListSet.Clear();
			for( int i = 1; i <= 5; i++ ) {
				TeamsListSet.Add( new TeamInfo { TeamName = $"Team {i}", GroupName = "Team Group" } );
			}
			SelectedTeam = 0;
			NotifyPropertyChanged( nameof( SelectedTeam ) );
		}

		#endregion

		#endregion

		#endregion

		#endregion
	}
}

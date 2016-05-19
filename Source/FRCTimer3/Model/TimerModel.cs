using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Threading;

namespace FRCTimer3 {

	/// <summary>
	///		JSONのパース用のクラスを定義します。
	/// </summary>
	[DataContract]
	class CRTJson {
		/// <summary>
		///		Victory画面に表示するメッセージを取得・設定します。
		/// </summary>
		[DataMember]
		public string VictoryMessage { get; set; }

		/// <summary>
		///		準備時間を取得・設定します。
		/// </summary>
		[DataMember]
		public double ReadyTime { get; set; }

		/// <summary>
		///		セッティングタイムを取得・設定します。
		/// </summary>
		[DataMember]
		public double SettingTime { get; set; }

		/// <summary>
		///		試合時間を取得・設定します。
		/// </summary>
		[DataMember]
		public double PlayTime { get; set; }

		/// <summary>
		///		自動機発進のタイムリミットを取得・設定します。
		/// </summary>
		[DataMember]
		public double AutoMachineLanchTimeLimit { get; set; }
	}

	/// <summary>
	///		タイマーのModelです。
	/// </summary>
	class TimerModel : NotifyPropertyChangedHelper {

		private TimerType timerType = TimerType.SettingReady;

		/// <summary>
		///		現在動作しているタイマーにおける、イベントを発生させる時間を定義します。
		/// </summary>
		private Queue<TimeSpan> timeEventQueue;

		/// <summary>
		///		指定した時間に発生させるイベントを管理します。
		/// </summary>
		private Dictionary<TimerType, Dictionary<TimeSpan, EventHandler>> timeEventManager;

		/// <summary>
		///		イベント発生時に渡すイベント引数を表します。
		/// </summary>
		private EventArgs eventArgs = new EventArgs();

		/// <summary>
		///		経過時間を管理するストップウォッチです。
		/// </summary>
		private Stopwatch stopWatch;

		/// <summary>
		///		DispatcherTimerは一定間隔ごとにイベントを発生させるクラスです。
		/// </summary>
		private DispatcherTimer dpTimer;

		/// <summary>
		///		時間定義ファイルの名前を取得します。
		/// </summary>
		public static string FileName { get; } =
			@"settings.json";

		/// <summary>
		///		Victory画面に表示するメッセージを取得します。
		/// </summary>
		public static string VictoryMessage { get; private set; }

		/// <summary>
		///		準備時間を取得します。
		/// </summary>
		public static TimeSpan ReadyTime { get; private set; }

		/// <summary>
		///		セッティング時間を取得します。
		/// </summary>
		public static TimeSpan SettingTime { get; private set; }

		/// <summary>
		///		試合時間を取得します。
		/// </summary>
		public static TimeSpan PlayTime { get; private set; }

		/// <summary>
		///		試合において、自動機発進時間のリミットを取得します。
		/// </summary>
		public static TimeSpan AutoMachineLanchTimeLimit { get; private set; }

		/// <summary>
		///		現在の経過時間を取得します。
		/// </summary>
		public TimeSpan Duration {
			get {
				TimeSpan ts = TimeSpan.Zero;
				switch( timerType ) {
					case TimerType.SettingReady:
					case TimerType.PlayReady:
						ts = TimeSpan.FromSeconds( Math.Ceiling( ( ReadyTime - stopWatch.Elapsed ).TotalSeconds ) );
						break;
					case TimerType.Setting:
						ts = SettingTime - stopWatch.Elapsed;
						break;
					case TimerType.Play:
						ts = PlayTime - stopWatch.Elapsed;
						break;
				}
				return ts;
			}
		}

		/// <summary>
		///		TimerModelクラスの新しいインスタンスを生成します。
		/// </summary>
		public TimerModel() {
			stopWatch = new Stopwatch();
			// DispatcherTimerを設定します。
			dpTimer = new DispatcherTimer( DispatcherPriority.Normal );
			dpTimer.Interval = TimeSpan.FromMilliseconds( 30.0 );   // 30msec.間隔
			dpTimer.Tick += DpTimer_Tick;
			timeEventManager = new Dictionary<TimerType, Dictionary<TimeSpan, EventHandler>>();
			timeEventManager[TimerType.SettingReady] = new Dictionary<TimeSpan, EventHandler>();
			timeEventManager[TimerType.Setting] = new Dictionary<TimeSpan, EventHandler>();
			timeEventManager[TimerType.PlayReady] = new Dictionary<TimeSpan, EventHandler>();
			timeEventManager[TimerType.Play] = new Dictionary<TimeSpan, EventHandler>();
		}

		/// <summary>
		///		DispacherTimerのイベント発生時に実行します。
		/// </summary>
		private void DpTimer_Tick( object sender, EventArgs e ) {
			if( !timeEventQueue.Any() ) {
				dpTimer.Stop();
			}
			else if( stopWatch.Elapsed >= timeEventQueue.Peek() ) {
				var item = timeEventQueue.Dequeue();
				timeEventManager[timerType][item]?.Invoke( this, e );
			}
			ModifyDisplayingTimer?.Invoke( this, e );
		}

		/// <summary>
		///		時間定義ファイル（ settings.json ）を読み込みます。
		/// </summary>
		/// <param name="_modifyDisplayingTimer">タイマーの残り時間を更新した時に実行するイベント</param>
		/// <param name="_startSettingTime">セッティングタイムを開始する時に実行するイベント</param>
		/// <param name="_finishSettingTime">セッティングタイムを終了する時に実行するイベント</param>
		/// <param name="_startPlayTime">試合を開始する時に実行するイベント</param>
		/// <param name="_finishPlayTime">試合を終了する時に実行するイベント</param>
		/// <param name="_notifyLast10sec">残り時間が10秒になった時に実行するイベント</param>
		/// <param name="_finishAutoMachineLanchStartTime">自動機発進時間が終了した時に発生するイベント</param>
		/// <param name="_playReadySound">Readyサウンドを鳴らすイベント</param>
		/// <param name="_playStartSound">開始サウンドを鳴らすイベント</param>
		/// <param name="_playLast3secSound">残り3秒のサウンドを鳴らすイベント</param>
		/// <param name="_playFinishSound">終了サウンドを鳴らすイベント</param>
		public void LoadSettings(
			EventHandler _modifyDisplayingTimer = null, EventHandler _startSettingTime = null, EventHandler _finishSettingTime = null, 
			EventHandler _startPlayTime = null, EventHandler _finishPlayTime = null,
			EventHandler _notifyLast10sec = null, EventHandler _finishAutoMachineLanchStartTime = null,
			EventHandler _playReadySound = null, EventHandler _playStartSound = null, EventHandler _playLast3secSound = null, EventHandler _playFinishSound = null
		) {

			LoadSettingsResult result = LoadSettingsResult.Succeed;

			try {
				// JSONファイルをストリーム経由で読み込みます。
				StreamReader sr = new StreamReader( FileName );
				string s = sr.ReadToEnd();
				sr.Close();
				// 正規表現でJSONファイル内のコメント「/* ～ */」を取り除きます。
				Regex jsonCommentTrimmer = new Regex( @"/\*(.*?)\*/", RegexOptions.Singleline );

				DataContractJsonSerializer json = new DataContractJsonSerializer( typeof( CRTJson ) );
				MemoryStream ms = new MemoryStream( Encoding.UTF8.GetBytes( jsonCommentTrimmer.Replace( s, "" ) ) );
				CRTJson crtJson = ( CRTJson )json.ReadObject( ms );
				ms.Close();

				// JSONをパースします。
				var rt = crtJson.ReadyTime;
				var st = crtJson.SettingTime;
				var pt = crtJson.PlayTime;

				if( crtJson.VictoryMessage == null )
					throw new SerializationException();

				// 読み取った値が範囲内であるかどうかチェックします。
				if( rt < 3.0 || rt > 30 ) {
					rt = 5.0; result = LoadSettingsResult.ValueOutOfRange;
				}
				if( st < 0.25 || st > 60.0 ) {
					st = 1.0; result = LoadSettingsResult.ValueOutOfRange;
				}
				if( pt < 0.25 || pt > 60.0 ) {
					st = 3.0; result = LoadSettingsResult.ValueOutOfRange;
				}

				VictoryMessage = crtJson.VictoryMessage;
				ReadyTime = TimeSpan.FromSeconds( rt );
				SettingTime = TimeSpan.FromMinutes( st );
				PlayTime = TimeSpan.FromMinutes( pt );
				AutoMachineLanchTimeLimit = TimeSpan.FromSeconds( crtJson.AutoMachineLanchTimeLimit );
			}
			// JSONファイルが見つからなかった時
			catch( FileNotFoundException ) {
				result = LoadSettingsResult.FileNotFound;
			}
			// JSONのパースに失敗した時
			catch( SerializationException ) {
				result = LoadSettingsResult.InvaildFormat;
			}
			// その他のエラー
			catch {
				result = LoadSettingsResult.OtherError;
			}

			if( result != LoadSettingsResult.Succeed && result != LoadSettingsResult.ValueOutOfRange ) {
				VictoryMessage = @"V-GOAL Congratulations !";
				ReadyTime = TimeSpan.FromSeconds( 5.0 );
				SettingTime = TimeSpan.FromMinutes( 1.0 );
				PlayTime = TimeSpan.FromMinutes( 3.0 );
				AutoMachineLanchTimeLimit = TimeSpan.FromSeconds( 15 );
				try {
					File.WriteAllBytes( FileName, Properties.Resources.DefaultTimeDef );
				}
				catch {
					result = LoadSettingsResult.JsonRemakeFailed;
				}
			}

			timeEventManager[TimerType.SettingReady][ReadyTime - TimeSpan.FromSeconds( 3 )] = _playReadySound;
			timeEventManager[TimerType.SettingReady][ReadyTime] = _startSettingTime;
			timeEventManager[TimerType.SettingReady][ReadyTime] += _playStartSound;
			
			timeEventManager[TimerType.Setting][SettingTime - TimeSpan.FromSeconds( 10 )] = _notifyLast10sec;
			timeEventManager[TimerType.Setting][SettingTime - TimeSpan.FromSeconds( 3 )] = _playLast3secSound;
			timeEventManager[TimerType.Setting][SettingTime] = _finishSettingTime;
			timeEventManager[TimerType.Setting][SettingTime] += _playFinishSound;
			
			timeEventManager[TimerType.PlayReady][ReadyTime - TimeSpan.FromSeconds( 3 )] = _playReadySound;
			timeEventManager[TimerType.PlayReady][ReadyTime] = _startPlayTime;
			timeEventManager[TimerType.PlayReady][ReadyTime] += _playStartSound;

			timeEventManager[TimerType.Play][AutoMachineLanchTimeLimit] = _finishAutoMachineLanchStartTime;
			timeEventManager[TimerType.Play][PlayTime - TimeSpan.FromSeconds( 10 )] = _notifyLast10sec;
			timeEventManager[TimerType.Play][PlayTime - TimeSpan.FromSeconds( 3 )] = _playLast3secSound;
			timeEventManager[TimerType.Play][PlayTime] = _finishPlayTime;
			timeEventManager[TimerType.Play][PlayTime] += _playFinishSound;

			ModifyDisplayingTimer = _modifyDisplayingTimer;

			LoadSettingsCompleted?.Invoke( this, new NotifyResultEventArgs<LoadSettingsResult, bool, bool>( result, null, null ) );
		}

		/// <summary>
		///		タイマーを開始します。
		/// </summary>
		public void Start( TimerType ttype ) {
			timerType = ttype;

			timeEventQueue = new Queue<TimeSpan>( timeEventManager[timerType].Select( _ => _.Key ) );

			// タイマーが動作していた場合、リセットします。
			if( stopWatch.IsRunning ) {
				stopWatch.Reset();
			}
			dpTimer.Start();
			stopWatch.Start();
		}

		/// <summary>
		///		タイマーを停止します。
		/// </summary>
		public void Stop() {
			if( stopWatch.IsRunning ) {
				stopWatch.Reset();
				timeEventQueue = null;
			}
			dpTimer.Stop();
		}

		/// <summary>
		///		時間定義ファイルを読み込んだ後に発生します。
		/// </summary>
		public event NotifyResultEventHandler<LoadSettingsResult, bool, bool> LoadSettingsCompleted;

		/// <summary>
		///		タイマーの残り時間を更新する時に実行します。
		/// </summary>
		private EventHandler ModifyDisplayingTimer;
	}
}

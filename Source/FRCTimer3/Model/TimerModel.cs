using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

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

		/// <summary>
		///		経過時間を管理するストップウォッチです。
		/// </summary>
		private Stopwatch sw;

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
		public TimeSpan Duration =>
			sw.Elapsed;

		/// <summary>
		///		TimerModelクラスの新しいインスタンスを生成します。
		/// </summary>
		public TimerModel() {
			sw = new Stopwatch();
		}

		/// <summary>
		///		時間定義ファイル（ settings.json ）を読み込みます。
		/// </summary>
		public void LoadSettings() {

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

			LoadSettingsCompleted?.Invoke( this, new NotifyResultEventArgs<LoadSettingsResult, bool, bool>( result, null, null ) );
		}

		/// <summary>
		///		タイマーを開始します。
		/// </summary>
		public void Start() {
			// タイマーが動作していた場合、リセットします。
			if( sw.IsRunning )
				sw.Reset();
			sw.Start();
		}

		/// <summary>
		///		タイマーを停止します。
		/// </summary>
		public void Stop() {
			if( sw.IsRunning ) {
				sw.Reset();
			}
		}

		/// <summary>
		///		時間定義ファイルを読み込んだ後に発生します。
		/// </summary>
		public event NotifyResultEventHandler<LoadSettingsResult, bool, bool> LoadSettingsCompleted;
	}
}

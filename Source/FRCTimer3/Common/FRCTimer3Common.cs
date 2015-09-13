using System;

namespace FRCTimer3 {

	/// <summary>
	///		チーム名リストファイルの読み込み結果を表します。
	/// </summary>
	enum LoadTeamsListResult {
		/// <summary>
		///		チーム名リストファイルを正しく読み込んだことを表します。
		/// </summary>
		Succeed,
		/// <summary>
		///		ファイルが見つかなかったため、正しく読み込めなかったことを表します。
		/// </summary>
		FileNotFound,
		/// <summary>
		///		チーム名リストの内容が無効なため、正しく読み込めなかったことを表します。
		/// </summary>
		InvaildList,
		/// <summary>
		///		その他のエラーが発生したことを表します。
		/// </summary>
		OtherError
	}

	/// <summary>
	///		チーム名リストファイルの書き込み結果を表します。
	/// </summary>
	enum SaveTeamsListResult {
		/// <summary>
		///		チーム名リストファイルを保存できたことを表します
		/// </summary>
		Succeed,
		/// <summary>
		///		チーム名リストファイルを保存しなかったことを表します。
		/// </summary>
		NotSaved,
		/// <summary>
		///		チーム名リストファイルを保存できなかったことを表します
		/// </summary>
		Failed,
		/// <summary>
		///		その他のエラーが発生したことを表します。
		/// </summary>
		OtherError

	}

	/// <summary>
	///		時間定義ファイルの読み込み結果を表します。
	/// </summary>
	enum LoadSettingsResult {
		/// <summary>
		///		時間定義ファイルを正しく読み込んだことを表します。
		/// </summary>
		Succeed,
		/// <summary>
		///		時間定義ファイルを正しく読み込んだが、範囲外の値を読み込んだものがあることを表します。
		/// </summary>
		ValueOutOfRange,
		/// <summary>
		///		ファイルが見つかなかったため、正しく読み込めなかったことを表します。
		/// </summary>
		FileNotFound,
		/// <summary>
		///		時間定義ファイルの内容が無効なため、正しく読み込めなかったことを表します。
		/// </summary>
		InvaildFormat,
		/// <summary>
		///		JSONファイルの再作成に失敗したことを表します。
		/// </summary>
		JsonRemakeFailed,
		/// <summary>
		///		その他のエラーが発生したことを表します。
		/// </summary>
		OtherError
	}

	/// <summary>
	///		結果情報を持つイベント引数です。
	/// </summary>
	/// <typeparam name="TResult">結果の情報の型</typeparam>
	/// <typeparam name="TSAction">成功した時に実行するメソッドに渡す引数の型</typeparam>
	/// <typeparam name="TFAction">失敗した時に実行するメソッドに渡す引数の型</typeparam>
	class NotifyResultEventArgs<TResult, TSAction, TFAction> : EventArgs {

		/// <summary>
		///		結果を表します。
		/// </summary>
		public TResult Result;

		/// <summary>
		///		成功した時に実行するメソッドを表します。
		/// </summary>
		public Action<TSAction> SucceedAction { get; private set; }

		/// <summary>
		///		失敗した時に実行するメソッドを表します。
		/// </summary>
		public Action<TFAction> FailedAction { get; private set; }

		/// <summary>
		///		NotifyResultEventArgsの新しいインスタンスを生成します。
		/// </summary>
		/// <param name="_r">結果</param>
		/// <param name="sa">成功した時に実行するメソッド</param>
		/// <param name="fa">失敗した時に実行するメソッド</param>
		public NotifyResultEventArgs( TResult _r, Action<TSAction> sa, Action<TFAction> fa ) {
			Result = _r;
			SucceedAction = sa;
			FailedAction = fa;
		}
	}

	/// <summary>
	///		結果を通知する時発生するイベントを処理します。
	/// </summary>
	delegate void NotifyResultEventHandler<TResult, TSAction, TFAction>( object sender, NotifyResultEventArgs<TResult, TSAction, TFAction> e );

	/// <summary>
	///		Chronoir Robocon Timerの状態を表します。
	/// </summary>
	enum FRCTimerState {
		/// <summary>
		///		チーム選択
		/// </summary>
		TeamSelect,
		/// <summary>
		///		セッティングタイム開始
		/// </summary>
		SettingReady,
		/// <summary>
		///		セッティングタイム
		/// </summary>
		SettingTime,
		/// <summary>
		///		セッティングタイム終了
		/// </summary>
		PlayPrepairing,
		/// <summary>
		///		ゲーム開始
		/// </summary>
		PlayReady,
		/// <summary>
		///		ゲーム中
		/// </summary>
		PlayTime,
		/// <summary>
		///		Vゴール
		/// </summary>
		Victory,
		/// <summary>
		///		ゲームセット
		/// </summary>
		GameSet,
		/// <summary>
		///		FRCTimerの設定
		/// </summary>
		FRCTimerSetting
	}

	/// <summary>
	///		Chronoir Robocon Timerで鳴らす効果音の種類を表します。
	/// </summary>
	enum FRCSoundEffectType {
		/// <summary>
		///		3、2、1、
		/// </summary>
		Ready,
		/// <summary>
		///		スタート合図
		/// </summary>
		Start,
		/// <summary>
		///		残り3秒の合図
		/// </summary>
		Last3,
		/// <summary>
		///		終了の合図
		/// </summary>
		Finish,
		/// <summary>
		///		効果音停止
		/// </summary>
		Stop
	}

	/// <summary>
	///		Chronoir Robocon Timerで鳴らす効果音の種類の情報を持つイベント引数です。
	/// </summary>
	class FRCSoundEffectTypeEventArgs : EventArgs {

		/// <summary>
		///		サウンドのインデックス値を表します。
		/// </summary>
		public FRCSoundEffectType FRCSoundEffect { get; private set; }

		/// <summary> 
		///		FRCSoundEffectTypeEventArgsの新しいインスタンスを生成します。
		/// </summary>
		/// <param name="frcset">サウンドのインデックス値</param>
		public FRCSoundEffectTypeEventArgs( FRCSoundEffectType frcset ) {
			FRCSoundEffect = frcset;
		}
	}

	/// <summary>
	///		Chronoir Robocon Timerで鳴らすイベントを処理します。
	/// </summary>
	delegate void FRCSoundEffectTypeEventHandler( object sender, FRCSoundEffectTypeEventArgs e );

	/// <summary>
	///		コールバックメソッドの情報を持つイベント引数です。
	/// </summary>
	class CallbackEventArgs : EventArgs {

		/// <summary>
		///		コールバックのメソッド
		/// </summary>
		public Action Callback { get; private set; }

		/// <summary>
		///		CallbackEventArgsの新しいインスタンスを生成します。
		/// </summary>
		/// <param name="cb">コールバックのメソッド</param>
		public CallbackEventArgs( Action cb ) {
			Callback = cb;
		}
	}

	/// <summary>
	///		イベントを処理し、コールバックを呼び出します。
	/// </summary>
	delegate void CallbackEventHandler( object sender, CallbackEventArgs e );

	/// <summary>
	///		確認メッセージとコールバックメソッドの情報を持つイベント引数です。
	/// </summary>
	class ComfirmEventArgs : EventArgs {

		/// <summary>
		///		確認メッセージ
		/// </summary>
		public string Message { get; private set; }

		/// <summary>
		///		コールバックのメソッド
		/// </summary>
		public Action Callback { get; private set; }

		/// <summary>
		///		
		/// </summary>
		/// <param name="mes"></param>
		/// <param name="cb"></param>
		public ComfirmEventArgs( string mes, Action cb ) {
			Message = mes;
			Callback = cb;
		}
	}

	/// <summary>
	///		イベントを処理し、確認メッセージを使用したり、コールバックを呼び出したりします。
	/// </summary>
	delegate void ComfirmEventHandler( object sender, ComfirmEventArgs e );
}

using System.Collections.Generic;
using System.Windows.Input;

namespace FRCTimer3 {

	/// <summary>
	///		ボタンコントロールの文字列とコマンドを格納する構造体を定義します。
	/// </summary>
	struct CommandButton {

		/// <summary>
		///		ボタンに表示する文字列を取得します。
		/// </summary>
		/// <remarks>Button.ContentやLabel.Contentにバインディングすると、アクセラレーターキーが利用できます。</remarks>
		public string CommandName { get; private set; }
		/// <summary>
		///		ボタンに関連付けるICommandインターフェースを取得します。
		/// </summary>
		public ICommand Command { get; private set; }

		/// <summary>
		///		CommandButtonクラスの新しいインスタンスを生成します。
		/// </summary>
		/// <param name="cn">ボタンに表示する文字列</param>
		/// <param name="cmd">ボタンに関連付けるコマンドインターフェース</param>
		public CommandButton( string cn, ICommand cmd ) {
			CommandName = cn; Command = cmd;
		}

	}

	/// <summary>
	///		アプリの状態とCommandButtonの関連付けたリストのModelクラスを定義します。
	/// </summary>
	class FRCCommandSetModel {

		/// <summary>
		///		アプリの状態とCommandButtonの関連付けたリストを取得します。
		/// </summary>
		public Dictionary<FRCTimerState, CommandButton[]> FRCCommandSetList { get; private set; }

		/// <summary>
		///		FRCCommandSetModelクラスの新しいインスタンスを生成します。
		/// </summary>
		/// <param name="startSetting">セッティング開始用</param>
		/// <param name="skipSetting">セッティングスキップ用</param>
		/// <param name="startPlaying">試合開始用</param>
		/// <param name="canceler">セッティング、試合中止用</param>
		/// <param name="backToTeamSelect">チーム選択に戻る用</param>
		/// <param name="vGoal">Vゴール用</param>
		/// <param name="configuration">アプリの設定用</param>
		/// <param name="appEnd">アプリの終了用</param>
		/// <param name="saveTeamsList">チーム名リストを保存して戻る用</param>
		/// <param name="closeSetting">チーム名リストを保存せずに戻る用</param>
		/// <param name="applySetting">チーム名リストを保存する用</param>
		public FRCCommandSetModel(
				CommandButton startSetting,
				CommandButton skipSetting,
				CommandButton startPlaying,
				CommandButton canceler,
				CommandButton backToTeamSelect,
				CommandButton vGoal,
				CommandButton configuration,
				CommandButton appEnd,
				CommandButton saveTeamsList,
				CommandButton closeSetting,
				CommandButton applySetting
			) {

			// アプリの状態とCommandButtonの関連付けたリストを作成します。
			FRCCommandSetList = new Dictionary<FRCTimerState, CommandButton[]> {
				[FRCTimerState.TeamSelect] = new CommandButton[4] { startSetting, skipSetting, configuration, appEnd },
				[FRCTimerState.SettingReady] = new CommandButton[2] { canceler, appEnd },
				[FRCTimerState.SettingTime] = new CommandButton[2] { canceler, appEnd },
				[FRCTimerState.PlayPrepairing] = new CommandButton[3] { startPlaying, backToTeamSelect, appEnd },
				[FRCTimerState.PlayReady] = new CommandButton[2] { canceler, appEnd },
				[FRCTimerState.PlayTime] = new CommandButton[3] { vGoal, canceler, appEnd },
				[FRCTimerState.GameSet] = new CommandButton[2] { backToTeamSelect, appEnd },
				[FRCTimerState.Victory] = new CommandButton[2] { backToTeamSelect, appEnd },
				[FRCTimerState.FRCTimerSetting] = new CommandButton[3] { saveTeamsList, closeSetting, applySetting }
			};
		}
	}
}

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FRCTimer3 {

	/// <summary>
	///		プロパティの変更通知するヘルパークラスを定義します。
	/// </summary>
	public abstract class NotifyPropertyChangedHelper : INotifyPropertyChanged {

		/// <summary>
		///		プロパティを変更した時に発生します。
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		///		PropertyChangedのイベントハンドラーを取得します。
		/// </summary>
		public PropertyChangedEventHandler PropertyChangedFromThis =>
			PropertyChanged;

		/// <summary>
		///		指定したプロパティの変更を通知します。
		/// </summary>
		/// <param name="propertyName">変更を通知するプロパティ名（ 省略時、呼び出し元のプロパティ名となります。）</param>
		protected virtual void NotifyPropertyChanged( [CallerMemberName]string propertyName = null ) {
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
		}

		/// <summary>
		///		指定したプロパティに値を設定し、そのプロパティの変更を通知します。
		/// </summary>
		/// <typeparam name="T">プロパティの型</typeparam>
		/// <param name="property">設定先のプロパティ</param>
		/// <param name="value">プロパティに設定する値</param>
		/// <param name="propertyName">プロパティの名前（ 省略時、呼び出し元のプロパティ名となります。）</param>
		protected virtual void SetProperty<T>( ref T property, T value, [CallerMemberName]string propertyName = null ) {
			property = value;
			NotifyPropertyChanged( propertyName );
		}
	}

	/// <summary>
	///		Executeメソッド、CanExecuteメソッドで実行するデリゲートを登録することができるコマンドを定義します。
	/// </summary>
	/// <remarks>UI上でボタンを押した時の動作を定義します。</remarks>
	class ActionCommand : ICommand {
		/// <summary>
		///		ViewModelの参照を表します。
		/// </summary>
		private ViewModelBase viewModel;

		/// <summary>
		///		Executeメソッドで、実行する処理のデリゲートを表します。
		/// </summary>
		private Action<object> action;

		/// <summary>
		///		CanExecuteメソッドで、実行可能かどうか判定するための式のデリゲートを表します。
		/// </summary>
		private Func<object, bool> predicate;

		/// <summary>
		///		ReadBTCInputDataCommandクラスの新しいインスタンスを生成します。
		/// </summary>
		/// <param name="vm">ViewModelの参照</param>
		/// <param name="act">Executeメソッドで、実行する処理</param>
		/// <param name="prdct">CanExecuteメソッドで、このコマンドが実行可能かどうか判定するための式</param>
		public ActionCommand( ViewModelBase vm, Action<object> act = null, Func<object, bool> prdct = null ) {
			viewModel = vm;
			action = act;
			predicate = prdct;
			// ViewModelのプロパティ変更通知と連動させます。
			viewModel.PropertyChanged +=
				( sender, e ) =>
					CanExecuteChanged?.Invoke( sender, e );
		}

		/// <summary>
		///		実行可能かどうか判定します。
		/// </summary>
		/// <param name="parameter">パラメーター</param>
		/// <returns>true：実行できます。 / false：実行できません。</returns>
		/// <remarks>バインド先のコントロールのIsEnabledに対応しています。predicateがnullの場合、常にtrueが返ります。</remarks>
		public bool CanExecute( object parameter = null ) =>
			predicate?.Invoke( parameter ) ?? true;

		/// <summary>
		///		CanExecuteが変更されたことを通知するイベントハンドラーです。
		/// </summary>
		public event EventHandler CanExecuteChanged;

		/// <summary>
		///		コマンドを実行します。
		/// </summary>
		/// <param name="parameter">コマンドのパラメーター</param>
		/// <remarks>actionがnullの場合、何もしません。</remarks>
		public void Execute( object parameter = null ) =>
			action?.Invoke( parameter );
	}

	/// <summary>
	///		ViewModelのベースを表します。
	/// </summary>
	class ViewModelBase : NotifyPropertyChangedHelper {
	}
}

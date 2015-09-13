using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Text;

namespace FRCTimer3 {

	/// <summary>
	///		チーム名とグループ名を格納する構造体です。
	/// </summary>
	public struct TeamInfo {
		/// <summary>
		///		チーム名
		/// </summary>
		public string TeamName { get; set; }
		/// <summary>
		///		グループ名
		/// </summary>
		public string GroupName { get; set; }
	}

	/// <summary>
	///		チーム名リストのModelです。
	/// </summary>
	class TeamsModel : INotifyPropertyChanged {

		/// <summary>
		///		チーム名を格納するリストです。
		/// </summary>
		public ObservableCollection<TeamInfo> Teams { get; private set; } = new ObservableCollection<TeamInfo>();

		/// <summary>
		///		チーム名リストファイルの名前です。
		/// </summary>
		public static string FileName { get; } = @"teams.xml";

		/// <summary>
		///		TeamsModelの新しいインスタンスを生成します。
		/// </summary>
		public TeamsModel() {
			BindingOperations.EnableCollectionSynchronization( Teams, new object() );
		}

		/// <summary>
		///		チーム名リスト（ terms.xml ）を読み込みます。
		/// </summary>
		/// <returns>読み込み結果</returns>
		private Task<LoadTeamsListResult> LoadTeamsList() {
			LoadTeamsListResult result = LoadTeamsListResult.Succeed;
			Teams.Clear();
			try {
				XmlDocument xd = new XmlDocument();
				xd.Load( FileName );
				XmlNodeList xn = xd.SelectNodes( "/teams/team" );

				// チーム名リストが空の時、例外をスローします。
				if( xn.Count == 0 ) {
					throw new NullReferenceException();		// このtryブロック直下のハンドラへスローされます。
				}

				// チーム名とグループ名を読み込みます。
				foreach( XmlNode xni in xn ) {
					Teams.Add( new TeamInfo {
							TeamName = xni.SelectSingleNode( "./@name" ).Value,
							GroupName = xni.SelectSingleNode( "./@group" ).Value
						}
					);
				}

			}
			// チーム名リストのファイルが見つからない時
			catch( FileNotFoundException ) {
				Teams.Clear();
				result = LoadTeamsListResult.FileNotFound;
			}
			// チーム名リストが空の時
			catch( NullReferenceException ) {
				Teams.Clear();
				result = LoadTeamsListResult.InvaildList;
			}
			// その他のエラー
			catch {
				Teams.Clear();
				result = LoadTeamsListResult.OtherError;
			}

			return Task.FromResult( result );
		}

		/// <summary>
		///		チーム名リスト（ terms.xml ）を初期化します。
		/// </summary>
		/// <returns>結果</returns>
		private SaveTeamsListResult ResetTeamsList( bool saveFileFlag ) {
			SaveTeamsListResult result = SaveTeamsListResult.NotSaved;

			try {
				Teams.Clear();
				for( int i = 1; i <= 5; i++ ) {
					Teams.Add( new TeamInfo { TeamName = $"Team {i}", GroupName = "Team Group" } );
				}

				if( saveFileFlag ) {
					try {
						File.WriteAllBytes( FileName, Properties.Resources.DefaultTeamNameList );
						result = SaveTeamsListResult.Succeed;
					}
					catch {
						result = SaveTeamsListResult.Failed;
					}
				}
			}
			catch {
				result = SaveTeamsListResult.OtherError;
			}

			return result;
		}

		/// <summary>
		///		チーム名リスト（ terms.xml ）を保存します。
		/// </summary>
		/// <param name="newTeamList">保存するチーム名リスト</param>
		/// <returns>保存結果</returns>
		private Task<SaveTeamsListResult> SaveTeamsList( IEnumerable<TeamInfo> newTeamList ) {
			SaveTeamsListResult result = SaveTeamsListResult.Succeed;
			
			try {
				// StringBuilderでXMLデータを文字列で構成し、
				// XmlDocument.LoadXmlでXMLドキュメントに変換します。
				XmlDocument xd = new XmlDocument();
				StringBuilder sb = new StringBuilder();
				sb.AppendLine( "<?xml version=\"1.0\" encoding=\"utf-8\"?>" );
				sb.AppendLine( "<!-- チーム名リスト -->" );
				sb.AppendLine( "<teams>" );
				foreach( var i in newTeamList ) {
					sb.AppendLine( $"\t<team name=\"{i.TeamName}\" group=\"{i.GroupName}\"/>" );
				}
				sb.AppendLine( "</teams>" );
				xd.LoadXml( sb.ToString() );
				xd.Save( FileName );

			}
			// XMLファイルへの保存に失敗した時
			catch {
				result = SaveTeamsListResult.Failed;
			}

			// XMLファイルに保存したら、元のチーム名リストにも反映させます。
			try {
				Teams.Clear();
				foreach( var i in newTeamList ) {
					Teams.Add( i );
				}
			}
			catch {
				result = SaveTeamsListResult.OtherError;
			}

			return Task.FromResult( result );
		}

		/// <summary>
		///		プロパティを変更した時に発生するイベントハンドラです。
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		///		チーム名リストを読み込んだ後に発生するイベントハンドラです。
		/// </summary>
		public event NotifyResultEventHandler<LoadTeamsListResult, bool, bool> LoadTeamsListCompleted;

		/// <summary>
		///		チーム名リストを初期化した後に発生するイベントハンドラです。
		/// </summary>
		public event NotifyResultEventHandler<SaveTeamsListResult, bool, bool> ResetTeamsListCompleted;

		/// <summary>
		///		チーム名リストを保存した後に発生するイベントハンドラです。
		/// </summary>
		public event NotifyResultEventHandler<SaveTeamsListResult, bool, bool> SaveTeamsListCompleted;

		/// <summary>
		///		プロパティの変更を通知します。
		/// </summary>
		/// <param name="propertyName">プロパティ名（ 省略時、呼び出し元のプロパティ名 ）</param>
		private void NotifyPropertyChanged( [CallerMemberName]string propertyName = null ) {
			// CallerMemberNameの属性を付けた文字列型変数は省略時、
			// 呼び出し元のメンバーの名前が入ります。
			// 呼び出し元ののプロパティの変更通知の場合、引数を省略することができます。
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
		}

		/// <summary>
		///		チーム名リストを読み込みます。
		/// </summary>
		public async Task LoadTeamsListAsync() {
			LoadTeamsListResult result = await LoadTeamsList();
			LoadTeamsListCompleted?.Invoke( this, new NotifyResultEventArgs<LoadTeamsListResult, bool, bool>( result, null, null ) );
		}

		/// <summary>
		///		チーム名リストをリセットします。
		/// </summary>
		public void ResetTeamsListAsync( bool saveFileFlag ) {
			SaveTeamsListResult result = ResetTeamsList( saveFileFlag );
			ResetTeamsListCompleted?.Invoke( this, new NotifyResultEventArgs<SaveTeamsListResult, bool, bool>( result, null, null ) );
		}

		/// <summary>
		///		チーム名リストをXMLファイルに保存します。
		/// </summary>
		/// <param name="newTeamList">保存するチーム名リスト</param>
		public async Task SaveTeamsListAsync( IEnumerable<TeamInfo> newTeamList ) {
			SaveTeamsListResult result = await SaveTeamsList( newTeamList );
			SaveTeamsListCompleted?.Invoke( this, new NotifyResultEventArgs<SaveTeamsListResult, bool, bool>( result, null, null ) );
		}
	}
}

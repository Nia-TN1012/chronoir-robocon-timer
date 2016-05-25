using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Data;
using System.Linq;
using System.Xml.Linq;

namespace FRCTimer3 {

	/// <summary>
	///		チーム名とグループ名を格納する構造体を定義します。
	/// </summary>
	public struct TeamInfo {
		/// <summary>
		///		チーム名を取得・設定します。
		/// </summary>
		public string TeamName { get; set; }
		/// <summary>
		///		グループ名を取得・設定します。
		/// </summary>
		public string GroupName { get; set; }
	}

	/// <summary>
	///		チーム名リストのModelクラスを定義します。
	/// </summary>
	class TeamsModel : NotifyPropertyChangedHelper {

		/// <summary>
		///		チーム名を格納するリストを取得します。
		/// </summary>
		public ObservableCollection<TeamInfo> Teams { get; private set; } =
			new ObservableCollection<TeamInfo>();

		/// <summary>
		///		チーム名リストファイルを取得します。
		/// </summary>
		public static string FileName { get; } =
			@"teams.xml";

		/// <summary>
		///		TeamsModelクラスの新しいインスタンスを生成します。
		/// </summary>
		public TeamsModel() {
			BindingOperations.EnableCollectionSynchronization( Teams, new object() );
		}

		/// <summary>
		///		チーム名リスト（ terms.xml ）を読み込みます。
		/// </summary>
		public void LoadTeamsList() {
			LoadTeamsListResult result = LoadTeamsListResult.Succeed;
			Teams.Clear();
			try {
				XElement xroot = XElement.Load( FileName );
				var xteams = xroot.Elements( "team" );

				// チーム名リストが空の時、例外をスローします。
				if( xteams.Count() == 0 ) {
					throw new NullReferenceException();		// このtryブロック直下のハンドラへスローされます。
				}

				// チーム名とグループ名を読み込みます。
				foreach( var xteam in xteams ) {
					Teams.Add( new TeamInfo {
							TeamName = xteam.Attribute( "name" ).Value,
							GroupName = xteam.Attribute( "group" ).Value
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
			catch( Exception ) {
				Teams.Clear();
				result = LoadTeamsListResult.OtherError;
			}

			LoadTeamsListCompleted?.Invoke( this, new NotifyResultEventArgs<LoadTeamsListResult, bool, bool>( result, null, null ) );
		}

		/// <summary>
		///		チーム名リスト（ terms.xml ）を初期化します。
		/// </summary>
		public void ResetTeamsList( bool saveFileFlag ) {
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

			ResetTeamsListCompleted?.Invoke( this, new NotifyResultEventArgs<SaveTeamsListResult, bool, bool>( result, null, null ) );
		}

		/// <summary>
		///		チーム名リスト（ terms.xml ）を保存します。
		/// </summary>
		/// <param name="newTeamList">保存するチーム名リスト</param>
		public void SaveTeamsList( IEnumerable<TeamInfo> newTeamList ) {
			SaveTeamsListResult result = SaveTeamsListResult.Succeed;
			
			try {
				XDocument xd = new XDocument();
				xd.Add( new XComment( "チーム名リスト" ) );
				var teams = new XElement( "teams",
					newTeamList.Select(
						team => new XElement( "team",
							new XAttribute( "name", team.TeamName ),
							new XAttribute( "group", team.GroupName )
						)
					)
				);
				xd.Add( teams );
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

			SaveTeamsListCompleted?.Invoke( this, new NotifyResultEventArgs<SaveTeamsListResult, bool, bool>( result, null, null ) );
		}

		/// <summary>
		///		チーム名リストを読み込んだ後に発生します。
		/// </summary>
		public event NotifyResultEventHandler<LoadTeamsListResult, bool, bool> LoadTeamsListCompleted;

		/// <summary>
		///		チーム名リストを初期化した後に発生します。
		/// </summary>
		public event NotifyResultEventHandler<SaveTeamsListResult, bool, bool> ResetTeamsListCompleted;

		/// <summary>
		///		チーム名リストを保存した後に発生します。
		/// </summary>
		public event NotifyResultEventHandler<SaveTeamsListResult, bool, bool> SaveTeamsListCompleted;
	}
}

/* Yet Another Forum.net
 * Copyright (C) 2003-2005 Bj�rnar Henden
 * Copyright (C) 2006-2010 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

namespace YAF.Pages
{
  // YAF.Pages
  #region Using

  using System;
  using System.Data;
  using System.Web;

  using YAF.Classes.Data;
  using YAF.Controls;
  using YAF.Core;
  using YAF.Types;
  using YAF.Types.Constants;
  using YAF.Types.Flags;
  using YAF.Types.Interfaces;
  using YAF.Utils;

  #endregion

  /// <summary>
  /// Summary description for topics.
  /// </summary>
  public partial class topics : ForumPage
  {
    #region Constants and Fields

    /// <summary>
    ///   The _show topic list selected.
    /// </summary>
    protected int _showTopicListSelected;

    /// <summary>
    ///   The last post image tooltip.
    /// </summary>
    protected string lastPostImageTT = string.Empty;

    /// <summary>
    ///   The _forum.
    /// </summary>
    private DataRow _forum;

    /// <summary>
    ///   The _forum flags.
    /// </summary>
    private ForumFlags _forumFlags;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    ///   Initializes a new instance of the <see cref = "topics" /> class. 
    ///   Overloads the topics page.
    /// </summary>
    public topics()
      : base("TOPICS")
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// The style transform func wrap.
    /// </summary>
    /// <param name="dt">
    /// The DateTable
    /// </param>
    /// <returns>
    /// The style transform wrap.
    /// </returns>
    public DataTable StyleTransformDataTable([NotNull] DataTable dt)
    {
      if (YafContext.Current.BoardSettings.UseStyledNicks)
      {
        var styleTransform = this.Get<IStyleTransform>();
        styleTransform.DecodeStyleByTable(ref dt, true, "StarterStyle", "LastUserStyle");
      }

      return dt;
    }

    #endregion

    #region Methods

    /// <summary>
    /// The get sub forum title.
    /// </summary>
    /// <returns>
    /// The get sub forum title.
    /// </returns>
    protected string GetSubForumTitle()
    {
      return this.GetTextFormatted("SUBFORUMS", this.HtmlEncode(this.PageContext.PageForumName));
    }

    /// <summary>
    /// The new topic_ click.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void NewTopic_Click([NotNull] object sender, [NotNull] EventArgs e)
    {
      if (this._forumFlags.IsLocked)
      {
        this.PageContext.AddLoadMessage(this.GetText("WARN_FORUM_LOCKED"));
        return;
      }

      if (!this.PageContext.ForumPostAccess)
      {
        YafBuildLink.AccessDenied( /*"You don't have access to post new topics in this forum."*/);
      }

      YafBuildLink.Redirect(ForumPages.postmessage, "f={0}", this.PageContext.PageForumID);
    }

    /// <summary>
    /// The initialization script for the topics page.
    /// </summary>
    /// <param name="e">
    /// The EventArgs object for the topics page.
    /// </param>
    protected override void OnInit([NotNull] EventArgs e)
    {
      this.Unload += this.topics_Unload;
      moderate1.Click += this.moderate_Click;
      moderate2.Click += this.moderate_Click;
      ShowList.SelectedIndexChanged += this.ShowList_SelectedIndexChanged;
      MarkRead.Click += this.MarkRead_Click;
      Pager.PageChange += this.Pager_PageChange;
      this.NewTopic1.Click += this.NewTopic_Click;
      this.NewTopic2.Click += this.NewTopic_Click;
      this.WatchForum.Click += this.WatchForum_Click;

      // CODEGEN: This call is required by the ASP.NET Web Form Designer.
      base.OnInit(e);
    }

    /// <summary>
    /// The page_ load.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void Page_Load([NotNull] object sender, [NotNull] EventArgs e)
    {
      YafContext.Current.Get<IYafSession>().UnreadTopics = 0;
      this.AtomFeed.AdditionalParameters =
        "f={0}".FormatWith(this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("f"));
      this.RssFeed.AdditionalParameters =
        "f={0}".FormatWith(this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("f"));
      this.MarkRead.Text = this.GetText("MARKREAD");
      this.ForumJumpHolder.Visible = this.PageContext.BoardSettings.ShowForumJump &&
                                     this.PageContext.Settings.LockedForum == 0;
      this.lastPostImageTT = this.PageContext.Localization.GetText("DEFAULT", "GO_LAST_POST");

      if (!this.IsPostBack)
      {
        // PageLinks.Clear();
        if (this.PageContext.Settings.LockedForum == 0)
        {
          this.PageLinks.AddLink(this.PageContext.BoardSettings.Name, YafBuildLink.GetLink(ForumPages.forum));
          this.PageLinks.AddLink(
            this.PageContext.PageCategoryName, 
            YafBuildLink.GetLink(ForumPages.forum, "c={0}", this.PageContext.PageCategoryID));
        }

        this.PageLinks.AddForumLinks(this.PageContext.PageForumID, true);

        this.ShowList.DataSource = StaticDataHelper.TopicTimes();
        this.ShowList.DataTextField = "TopicText";
        this.ShowList.DataValueField = "TopicValue";
        this._showTopicListSelected = (YafContext.Current.Get<IYafSession>().ShowList == -1)
                                        ? this.PageContext.BoardSettings.ShowTopicsDefault
                                        : YafContext.Current.Get<IYafSession>().ShowList;

        this.HandleWatchForum();
      }

      if (this.Request.QueryString.GetFirstOrDefault("f") == null)
      {
        YafBuildLink.AccessDenied();
      }

      if (this.PageContext.IsGuest && !this.PageContext.ForumReadAccess)
      {
        // attempt to get permission by redirecting to login...
        this.Get<IPermissions>().HandleRequest(ViewPermissions.RegisteredUsers);
      }
      else if (!this.PageContext.ForumReadAccess)
      {
        YafBuildLink.AccessDenied();
      }

      using (DataTable dt = DB.forum_list(this.PageContext.PageBoardID, this.PageContext.PageForumID))
      {
        this._forum = dt.Rows[0];
      }

      if (this._forum["RemoteURL"] != DBNull.Value)
      {
        this.Response.Clear();
        this.Response.Redirect((string)this._forum["RemoteURL"]);
      }

      this._forumFlags = new ForumFlags(this._forum["Flags"]);

      this.PageTitle.Text = this.HtmlEncode(this._forum["Name"]);

      this.BindData(); // Always because of yaf:TopicLine

      if (!this.PageContext.ForumPostAccess || (this._forumFlags.IsLocked && !this.PageContext.ForumModeratorAccess))
      {
        this.NewTopic1.Visible = false;
        this.NewTopic2.Visible = false;
      }

      if (!this.PageContext.ForumModeratorAccess)
      {
        this.moderate1.Visible = false;
        this.moderate2.Visible = false;
      }
    }

    /// <summary>
    /// The watch forum_ click.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void WatchForum_Click([NotNull] object sender, [NotNull] EventArgs e)
    {
      if (!this.PageContext.ForumReadAccess)
      {
        return;
      }

      if (this.PageContext.IsGuest)
      {
        this.PageContext.AddLoadMessage(this.GetText("WARN_LOGIN_FORUMWATCH"));
        return;
      }

      if (this.WatchForumID.InnerText == string.Empty)
      {
        DB.watchforum_add(this.PageContext.PageUserID, this.PageContext.PageForumID);

        // PageContext.AddLoadMessage(GetText("INFO_WATCH_FORUM"));
        var notification = (DialogBox)this.PageContext.CurrentForumPage.Notification;

        notification.Show(
          this.GetText("INFO_WATCH_FORUM"), 
          null, 
          DialogBox.DialogIcon.Info, 
          new DialogBox.DialogButton { Text = "Ok", CssClass = "LoginButton", }, 
          null);
      }
      else
      {
        var tmpID = this.WatchForumID.InnerText.ToType<int>();
        DB.watchforum_delete(tmpID);

        // PageContext.AddLoadMessage(GetText("INFO_UNWATCH_FORUM"));
        var notification = (DialogBox)this.PageContext.CurrentForumPage.Notification;

        notification.Show(
          this.GetText("INFO_UNWATCH_FORUM"), 
          null, 
          DialogBox.DialogIcon.Info, 
          new DialogBox.DialogButton { Text = "Ok", CssClass = "LoginButton", }, 
          null);
      }

      this.HandleWatchForum();
    }

    /// <summary>
    /// The moderate_ click.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void moderate_Click([NotNull] object sender, [NotNull] EventArgs e)
    {
      if (this.PageContext.ForumModeratorAccess)
      {
        YafBuildLink.Redirect(ForumPages.moderate, "f={0}", this.PageContext.PageForumID);
      }
    }

    /// <summary>
    /// The bind data.
    /// </summary>
    private void BindData()
    {
      DataSet ds = this.Get<IDBBroker>().BoardLayout(
        this.PageContext.PageBoardID, 
        this.PageContext.PageUserID, 
        this.PageContext.PageCategoryID, 
        this.PageContext.PageForumID);
      if (ds.Tables[MsSqlDbAccess.GetObjectName("Forum")].Rows.Count > 0)
      {
        this.ForumList.DataSource = ds.Tables[MsSqlDbAccess.GetObjectName("Forum")].Rows;
        this.SubForums.Visible = true;
      }

      this.Pager.PageSize = this.PageContext.BoardSettings.TopicsPerPage;

      // when userId is null it returns the count of all deleted messages
      int? userId = null;

      // get the userID to use for the deleted posts count...
      if (!this.PageContext.BoardSettings.ShowDeletedMessagesToAll)
      {
        // only show deleted messages that belong to this user if they are not admin/mod
        if (!this.PageContext.IsAdmin && !this.PageContext.ForumModeratorAccess)
        {
          userId = this.PageContext.PageUserID;
        }
      }

      DataTable dt =
        this.StyleTransformDataTable(
          DB.topic_list(
            this.PageContext.PageForumID, userId, 1, null, 0, 10, this.PageContext.BoardSettings.UseStyledNicks, true));

      int nPageSize = Math.Max(5, this.Pager.PageSize - dt.Rows.Count);
      this.Announcements.DataSource = dt;

      /*if ( !m_bIgnoreQueryString && Request.QueryString ["p"] != null )
			{
				// show specific page (p is 1 based)
				int tPage = (int)Security.StringToLongOrRedirect( Request.QueryString ["p"] );

				if ( tPage > 0 )
				{
					Pager.CurrentPageIndex = tPage - 1;
				}
			}*/
      int nCurrentPageIndex = this.Pager.CurrentPageIndex;

      DataTable dtTopics;
      if (this._showTopicListSelected == 0)
      {
        dtTopics =
          this.StyleTransformDataTable(
            DB.topic_list(
              this.PageContext.PageForumID, 
              userId, 
              0, 
              null, 
              nCurrentPageIndex * nPageSize, 
              nPageSize, 
              this.PageContext.BoardSettings.UseStyledNicks, 
              true));
      }
      else
      {
        DateTime date = DateTime.UtcNow;
        switch (this._showTopicListSelected)
        {
          case 1:
            date -= TimeSpan.FromDays(1);
            break;
          case 2:
            date -= TimeSpan.FromDays(2);
            break;
          case 3:
            date -= TimeSpan.FromDays(7);
            break;
          case 4:
            date -= TimeSpan.FromDays(14);
            break;
          case 5:
            date -= TimeSpan.FromDays(31);
            break;
          case 6:
            date -= TimeSpan.FromDays(2 * 31);
            break;
          case 7:
            date -= TimeSpan.FromDays(6 * 31);
            break;
          case 8:
            date -= TimeSpan.FromDays(365);
            break;
        }

        dtTopics =
          this.StyleTransformDataTable(
            DB.topic_list(
              this.PageContext.PageForumID, 
              userId, 
              0, 
              date, 
              nCurrentPageIndex * nPageSize, 
              nPageSize, 
              this.PageContext.BoardSettings.UseStyledNicks, 
              true));
      }

      int nRowCount = 0;
      if (dtTopics.Rows.Count > 0)
      {
        nRowCount = (int)dtTopics.Rows[0]["RowCount"];
      }

      int nPageCount = (nRowCount + nPageSize - 1) / nPageSize;

      this.TopicList.DataSource = dtTopics;

      this.DataBind();

      // setup the show topic list selection after data binding
      this.ShowList.SelectedIndex = this._showTopicListSelected;
      YafContext.Current.Get<IYafSession>().ShowList = this._showTopicListSelected;

      this.Pager.Count = nRowCount;
    }

    /// <summary>
    /// The handle watch forum.
    /// </summary>
    private void HandleWatchForum()
    {
      if (this.PageContext.IsGuest || !this.PageContext.ForumReadAccess)
      {
        return;
      }

      // check if this forum is being watched by this user
      using (DataTable dt = DB.watchforum_check(this.PageContext.PageUserID, this.PageContext.PageForumID))
      {
        if (dt.Rows.Count > 0)
        {
          // subscribed to this forum
          this.WatchForum.Text = this.GetText("unwatchforum");
          foreach (DataRow row in dt.Rows)
          {
            this.WatchForumID.InnerText = row["WatchForumID"].ToString();
            break;
          }
        }
        else
        {
          // not subscribed
          this.WatchForumID.InnerText = string.Empty;
          this.WatchForum.Text = this.GetText("watchforum");
        }
      }
    }

    /// <summary>
    /// The mark read_ click.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    private void MarkRead_Click([NotNull] object sender, [NotNull] EventArgs e)
    {
      YafContext.Current.Get<IYafSession>().SetForumRead(this.PageContext.PageForumID, DateTime.UtcNow);
      this.BindData();
    }

    /// <summary>
    /// The pager_ page change.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    private void Pager_PageChange([NotNull] object sender, [NotNull] EventArgs e)
    {
      this.SmartScroller1.Reset();
      this.BindData();
    }

    /// <summary>
    /// The show list_ selected index changed.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    private void ShowList_SelectedIndexChanged([NotNull] object sender, [NotNull] EventArgs e)
    {
      this._showTopicListSelected = this.ShowList.SelectedIndex;
      this.BindData();
    }

    /// <summary>
    /// The topics_ unload.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    private void topics_Unload([NotNull] object sender, [NotNull] EventArgs e)
    {
      if (YafContext.Current.Get<IYafSession>().UnreadTopics == 0)
      {
        YafContext.Current.Get<IYafSession>().SetForumRead(this.PageContext.PageForumID, DateTime.UtcNow);
      }
    }

    #endregion
  }
}
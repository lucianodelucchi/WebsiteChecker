using System;
using System.Text.RegularExpressions;
using Gtk;
using System.Net;
using System.Threading;

public partial class MainWindow: Gtk.Window
{		
	private static StatusIcon trayIcon;
	
	// timer
	private static System.Timers.Timer theTimer;
	
	public static ManualResetEvent allDone = new ManualResetEvent (false);
	
	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
		this.SetUpInterface ();
		
		theTimer = new System.Timers.Timer ();
		theTimer.Interval = WebsiteChecker.Configuration.DefaulTimerInterval;
		theTimer.Elapsed += HandleTheTimerElapsed;
	}
	
	/// <summary>
	/// Checks the URLs the user entered.
	/// </summary>
	/// <param name='urls'>
	/// The valid URLs the user entered.
	/// </param>
	private void CheckURLs (string[] urls)
	{
		foreach (var url in urls) {
			
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create (url);
			// Set method to HEAD to get only the headers in the response
			request.Method = "HEAD";
			this.imgLoading.Visible = true;
			// Make an asynchronous call to get the response passing the request as an additional parameter
			IAsyncResult result = (IAsyncResult)request.BeginGetResponse (new AsyncCallback (FinishWebRequest), request);
			// this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
			// check http://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.begingetresponse.aspx
			ThreadPool.RegisterWaitForSingleObject (result.AsyncWaitHandle, new WaitOrTimerCallback (TimeoutCallback), request, WebsiteChecker.Configuration.DefaultTimeout, true);
			allDone.WaitOne ();
		}
	}
	
	/// <summary>
	/// Function that is called asynchronously when the WebResponse has finished.
	/// </summary>
	/// <param name='result'>
	/// Result.
	/// </param>
	private void FinishWebRequest (IAsyncResult result)
	{
		HttpStatusCode status = HttpStatusCode.OK;
		WebExceptionStatus exceptionStatus = WebExceptionStatus.Success;
		
		string statusDescription = string.Empty;
		Uri url = null;
		
		try {
			// Get the request from the parameter
			HttpWebRequest request = (HttpWebRequest)result.AsyncState;
			url = request.RequestUri;
			HttpWebResponse response = (HttpWebResponse)request.EndGetResponse (result);
			status = response.StatusCode;
			response.Close ();
			
		} catch (WebException ex) {
			exceptionStatus = ex.Status;
			statusDescription = ex.Message;
			
			if (ex.Response != null) {
				status = ((HttpWebResponse)ex.Response).StatusCode;
				statusDescription = ((HttpWebResponse)ex.Response).StatusDescription;
				ex.Response.Close ();
			}
			
		}
		
		this.imgLoading.Visible = false;
		
		// Log the result in the TreeView
		this.AddURL (url, status, exceptionStatus, statusDescription);
		allDone.Set ();
	}
		
#region Auxiliary Methods
		
	/// <summary>
	/// Compares 2 URLs. Used to sort the TreeView
	/// </summary>
	/// <returns>
	/// The sort order.
	/// </returns>
	/// <param name='model'>
	/// The TreeModel.
	/// </param>
	/// <param name='tia'>
	/// First TreeIter to compare.
	/// </param>
	/// <param name='tib'>
	/// Second TreeIter to compare.
	/// </param>
	private int CompareURLs (TreeModel model, TreeIter tia, TreeIter tib)
	{
		string urlA = (string)model.GetValue (tia, 0);
		string urlB = (string)model.GetValue (tib, 0);
		
		return urlA.CompareTo (urlB);
	}
		
	/// <summary>
	/// Validates the URLs.
	/// </summary>
	/// <returns>
	/// Whether the entries are valid URLs
	/// </returns>
	/// <param name='entries'>
	/// The TextBuffer the user entered.
	/// </param>
	private bool ValidateURLs (TextBuffer entries)
	{
		bool ret = true;
		
		if (entries.CharCount == 0) {
			return false;
		}
		
		var urls = entries.Text.Trim ().Split ("\r\n".ToCharArray ());
		
		TextTag tagBold = entries.TagTable.Lookup ("bold");
		 
		string pattern = @"((https?):((//)|(\\\\))+[\w\d:#@%/;$()~_?\+-=\\\.&]*)";
		Regex rgxURL = new Regex (pattern);
		
		for (int i = 0; i < urls.Length; i++) {
			
			ret &= rgxURL.IsMatch (urls [i]);
			
			if (!rgxURL.IsMatch (urls [i])) {
				TextIter start = entries.GetIterAtLine (i);
				TextIter end = start; 
				end.ForwardToLineEnd ();
				
				entries.ApplyTag (tagBold, start, end);
			}
			
		}
		
		return ret;
	}
	
	/// <summary>
	/// Tries to find the iter index by URL.
	/// </summary>
	/// <returns>
	/// The iter index.
	/// </returns>
	/// <param name='url'>
	/// URL to search.
	/// </param>
	private int FindIterIndexByURL (string url)
	{
		int index = -1;
		
		var resultListStore = (TreeStore)this.tvResults.Model;
		
		TreeIter iter;
		
		resultListStore.GetIterFirst (out iter);
		
		for (int i = 0; i < resultListStore.IterNChildren(); i++) {
			if ((string)resultListStore.GetValue (iter, 0) == url) {
				index = i;
				break;
			}
			resultListStore.IterNext (ref iter);
		}
		
		return index;
	}
	
 	// Abort the request if the timer fires.
	private void TimeoutCallback (object state, bool timedOut)
	{ 
		if (timedOut) {
			HttpWebRequest request = state as HttpWebRequest;
			if (request != null) {
				request.Abort ();
			}
		}
	}
#endregion

#region Events
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		// Added because the icon wasn't removed when the application was closed
		trayIcon.Dispose ();
		
		theTimer.Dispose ();
		
		Application.Quit ();
		a.RetVal = true;
	}

	protected void OnBtnStartClicked (object sender, System.EventArgs e)
	{
		
		var valid = this.ValidateURLs (this.txtURLs.Buffer);
		
		if (valid) {
			
			this.ShowStop (true);
			
			// reset the timer just in case
			theTimer.Stop ();
			theTimer.Start ();
			var urls = this.txtURLs.Buffer.Text.Trim ().Split ("\r\n".ToCharArray ());
			this.CheckURLs (urls);
		}
	}
	
	protected void OnBtnStopClicked (object sender, System.EventArgs e)
	{
		theTimer.Stop ();
		this.ShowStart ();
	}
	
	/// <summary>
	/// Clears the TreeView on BtnClicked.
	/// </summary>
	protected void OnBtnClearClicked (object sender, System.EventArgs e)
	{
		theTimer.Stop ();
		
		var resultListStore = (TreeStore)this.tvResults.Model;
		resultListStore.Clear ();
		
		theTimer.Start ();
	}

	void HandleTheTimerElapsed (object sender, System.Timers.ElapsedEventArgs e)
	{
		var urls = this.txtURLs.Buffer.Text.Trim ().Split ("\r\n".ToCharArray ());
		
		this.CheckURLs (urls);
		
	}
	
	protected void OnTxtURLsKeyReleaseEvent (object o, Gtk.KeyReleaseEventArgs args)
	{
		theTimer.Stop ();
		this.ShowStart ();
	}
	
	void HandleTvResultsModelhandleRowInserted (object o, RowInsertedArgs args)
	{
		var resultListStore = (TreeStore)this.tvResults.Model;
		TreeIter parent;
		resultListStore.IterParent (out parent, args.Iter);
		
		if (WebsiteChecker.Configuration.DefaulURLRowLimit < resultListStore.IterNChildren (parent)) {
			theTimer.Stop ();
			TreeIter iterChild;
			resultListStore.IterChildren (out iterChild, parent);
			try {
				// should be done with a while, but I was getting an error/exception 
				// and was not being caught in the catch
				for (int i = 0; i < 5; i++) {
					resultListStore.Remove (ref iterChild);
				}
			} catch (Exception ex) {
				
			}
			
			theTimer.Start ();
		}
	
	}
	
#endregion
	
#region Interface Related
		
	/// <summary>
	/// Sets up interface.
	/// </summary>
	protected void SetUpInterface ()
	{	
		// Set up TreeView
		this.tvResults.AppendColumn ("Status", new CellRendererText (), "text", 0);
		this.tvResults.AppendColumn ("Timestamp", new CellRendererText (), "text", 1);
		
		// Set Up TreeStore
		TreeStore resultListStore = new TreeStore (typeof(string), typeof(string));
		resultListStore.SetSortFunc (0, CompareURLs);
		resultListStore.SetSortColumnId (0, SortType.Ascending);
		
		// Bind the TreeStore to the TreeView
		this.tvResults.Model = resultListStore;
		
		// Create a Bold and Red TextTag
		TextTag tagBold = new TextTag ("bold"); 
		tagBold.Weight = Pango.Weight.Bold;
		tagBold.Foreground = "red";
		// Add the TextTag to the TextView
		this.txtURLs.Buffer.TagTable.Add (tagBold);
		
		// Creation of the StatusIcon
		trayIcon = StatusIcon.NewFromStock (Stock.DialogQuestion);
		trayIcon.Visible = true;
 
		// Show/Hide the window (even from the Panel/Taskbar) when the TrayIcon has been clicked.
		trayIcon.Activate += delegate {
			this.Visible = !this.Visible; };
		// Show a pop up menu when the icon has been right clicked.
		//trayIcon.PopupMenu += OnTrayIconPopup;
 
		// A Tooltip for the Icon
		trayIcon.Tooltip = "Website Checker";
		
		// Loader icon
		Gdk.PixbufAnimation pba = Gdk.PixbufAnimation.LoadFromResource ("WebsiteChecker.ajax-loader.gif");
		this.imgLoading.PixbufAnimation = pba;
		
		this.tvResults.Model.RowInserted += HandleTvResultsModelhandleRowInserted;
	}


	
	/// <summary>
	/// Logs the URL in the TreeView.
	/// </summary>
	/// <param name='url'>
	/// URL to log.
	/// </param>
	/// <param name='status'>
	/// Status/Description to log against the URL.
	/// </param>
	private void AddURL (Uri url, HttpStatusCode status, WebExceptionStatus exceptionStatus, string statusDescription)
	{
		var resultListStore = (TreeStore)this.tvResults.Model;
		TreeIter iter;

		// try to get the index of the URL in the TreeView
		var index = this.FindIterIndexByURL (url.AbsoluteUri);
		
		// decide whether to append the status to an existing URL or to create a new line for the URL
		if (-1 != index) {
			resultListStore.GetIterFromString (out iter, index.ToString ());
		} else {
			iter = resultListStore.AppendValues (url.AbsoluteUri);		
		}
		
		if (status.ToString () == statusDescription.Replace (" ", "")) {
			statusDescription = string.Empty;
		}
		
		resultListStore.AppendValues (iter, string.Format ("{0} {1} {2}", status.ToString (), exceptionStatus.ToString (), statusDescription).Trim (), DateTime.Now.ToString ());
		this.tvResults.Model = resultListStore;
		
	}
	
	private void ShowStop (bool show)
	{
		this.btnStop.Visible = show;
		this.btnStart.Visible = !this.btnStop.Visible;
	}
	
	private void ShowStart ()
	{
		this.ShowStop (false);
	}
	
#endregion
}

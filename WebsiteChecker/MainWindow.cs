using System;
using System.Text.RegularExpressions;
using Gtk;
using System.Net;

public partial class MainWindow: Gtk.Window
{	
	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
		this.SetUpInterface ();	
	}
	
	protected void SetUpInterface ()
	{	
		
		this.tvResults.AppendColumn ("Status", new CellRendererText (), "text", 0);
		this.tvResults.AppendColumn ("Timestamp", new CellRendererText (), "text", 1);
		
		TreeStore resultListStore = new TreeStore (typeof(string), typeof(string));
		
		this.tvResults.Model = resultListStore;
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected void OnBtnStartClicked (object sender, System.EventArgs e)
	{
		var valid = this.ValidateURLs (this.txtURLs.Buffer);
		
		if (valid) {
			this.CheckURLs (this.txtURLs.Buffer);
		}
	}
	
	private bool ValidateURLs (TextBuffer entries)
	{
		bool ret = true;
		
		if (entries.CharCount == 0) {
			return false;
		}
		
		var urls = entries.Text.Trim ().Split ("\r\n".ToCharArray ());
		
		TextTag tagBold = new TextTag ("bold"); 
		tagBold.Weight = Pango.Weight.Bold;
		tagBold.Foreground = "red";
		entries.TagTable.Add (tagBold);
		
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
	
	private void CheckURLs (TextBuffer entries)
	{
		var urls = entries.Text.Trim ().Split ("\r\n".ToCharArray ());
		
		string status = string.Empty;
		string statusDescription = string.Empty;
		
		HttpWebResponse response = null;
		
		foreach (var url in urls) {
			
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create (url);
			
			try {
				
				response = (HttpWebResponse)request.GetResponse ();
				status = response.StatusCode.ToString ();
				
			} catch (WebException ex) {
				status = string.Empty;
				statusDescription = ex.Message;
			} finally { 
				if (null != response) {
					response.Close ();
				}
			}
				
			this.AddURL (url, string.Format ("{0} {1}", status, statusDescription).Trim());
		}
	}
	
	private void AddURL (string url, string status)
	{
		var resultListStore = (TreeStore)this.tvResults.Model;
		TreeIter iter;
		
		var index = this.FindIterIndexByURL (url);
		
		if (-1 != index) {
			resultListStore.GetIterFromString (out iter, index.ToString ());
		} else {
			iter = resultListStore.AppendValues (url);		
		}
		
		resultListStore.AppendValues (iter, status, DateTime.Now.ToString ());
		this.tvResults.Model = resultListStore;
	}
	
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
}

/******************************************************************************\
 * IceChat 9 Internet Relay Chat Client
 *
 * Copyright (C) 2014 Paul Vanderzee <snerf@icechat.net>
 *                                    <www.icechat.net> 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 *
 * Please consult the LICENSE.txt file included with this project for
 * more details
 *
\******************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace IceChat
{
	public partial class IceInputBox : System.Windows.Forms.TextBox
	{
		private System.ComponentModel.Container components = null;

		//Nick complete variables
        private int _nickNumber = -1;
        private string _partialNick;
        private List<NickList.Nick> _nickCompleteNames;

		internal delegate void SendCommand(object sender, string data);
		internal event SendCommand OnCommand;

        private delegate void ScrollWindowDelegate(bool scrollup);
        private delegate void ScrollConsoleWindowDelegate(bool scrollup);

        private delegate void ScrollWindowPageDelegate(bool scrollup);
        private delegate void ScrollConsoleWindowPageDelegate(bool scrollup);

        private InputPanel parent;

        public IceInputBox() { }

        public IceInputBox(InputPanel _parent)
		{
			InitializeComponent();

            this.MouseWheel += new MouseEventHandler(OnMouseWheel);

            _nickCompleteNames = new List<NickList.Nick>();

            this.parent = _parent;
		}
        
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            //120 -- scroll up
            //see which control has focus in the main program
            if (FormMain.Instance.CurrentWindowStyle != IceTabPage.WindowType.Console)
            {
                if (FormMain.Instance.CurrentWindow != null)
                    ScrollWindow(e.Delta > 0);
            }
            else
            {
                //make a scroll window for the console
                //find the current window for the console
                ScrollConsoleWindow(e.Delta > 0);                
            }
        }

        private void ScrollConsoleWindow(bool scrollUp)
        {
            if (this.InvokeRequired)
            {
                ScrollConsoleWindowDelegate s = new ScrollConsoleWindowDelegate(ScrollConsoleWindow);
                this.Invoke(s, new object[] { scrollUp });
            }
            else
                FormMain.Instance.ChannelBar.GetTabPage("Console").CurrentConsoleWindow().ScrollWindow(scrollUp);

        }

        private void ScrollWindow(bool scrollUp)
        {
            if (this.InvokeRequired)
            {
                ScrollWindowDelegate s = new ScrollWindowDelegate(ScrollWindow);
                this.Invoke(s, new object[] { scrollUp });
            }
            else
            {
                if (FormMain.Instance.CurrentWindowStyle != IceTabPage.WindowType.ChannelList)
                {
                    if (FormMain.Instance.CurrentWindowStyle == IceTabPage.WindowType.Channel)
                    {
                        //check if mousewheel is hovering over nicklist
                        if (FormMain.Instance.NickList.MouseHasFocus)
                        {
                            FormMain.Instance.NickList.ScrollWindow(scrollUp);
                            return;
                        }
                    }
                    FormMain.Instance.CurrentWindow.TextWindow.ScrollWindow(scrollUp);
                }
            }
        }

        private void ScrollConsoleWindowPage(bool scrollUp)
        {
            if (this.InvokeRequired)
            {
                ScrollConsoleWindowPageDelegate s = new ScrollConsoleWindowPageDelegate(ScrollConsoleWindowPage);
                this.Invoke(s, new object[] { scrollUp });
            }
            else
                FormMain.Instance.ChannelBar.GetTabPage("Console").CurrentConsoleWindow().ScrollWindowPage(scrollUp);

        }

        private void ScrollWindowPage(bool scrollUp)
        {
            if (this.InvokeRequired)
            {
                ScrollWindowPageDelegate s = new ScrollWindowPageDelegate(ScrollWindowPage);
                this.Invoke(s, new object[] { scrollUp });
            }
            else
            {
                IceTabPage currentWindow;
                if (parent.Parent.Name =="FormMain")
                    currentWindow = FormMain.Instance.CurrentWindow;
                else
                    currentWindow = (IceTabPage) parent.Parent;

                if (currentWindow.WindowStyle != IceTabPage.WindowType.ChannelList)
                    currentWindow.TextWindow.ScrollWindowPage(scrollUp);
            }
        }


		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
					components.Dispose();
			}
			base.Dispose( disposing );
			
            //_buffer = null;
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// IRCInputBox
			// 
			this.Size = new System.Drawing.Size(272, 20);

		}
				
		#endregion
		
		protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, System.Windows.Forms.Keys keyData)
		{
            if ((keyData == (Keys.Control | Keys.V)) || keyData == (Keys.Shift | Keys.Insert))
            {
                string data = Clipboard.GetText(TextDataFormat.UnicodeText);
                string[] lines = data.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    if (base.Multiline == true)
                        this.AppendText(data);
                    else
                        OnCommand(this, data);
                    return true;
                }
            }

            if (keyData == Keys.Tab)
            {
                NickComplete();
                return true;
            }
            else
                _nickNumber = -1;

            return false;
        }
        
        private void NickComplete()
        {
            string afterComplete = FormMain.Instance.IceChatOptions.NickCompleteAfter.Replace("&#x3;", ((char)3).ToString()).Replace("&#x2;", ((char)2).ToString());
            
            if (FormMain.Instance.CurrentWindowStyle == IceTabPage.WindowType.Console)
            {
                //tab complete in Console, just send current nick
                if (parent.CurrentConnection != null)
                {
                    this.Text += parent.CurrentConnection.ServerSetting.CurrentNickName;
                    this.SelectionStart = this.Text.Length;
                }
            }
            else if (FormMain.Instance.CurrentWindowStyle == IceTabPage.WindowType.Channel)
            {
                if (this.Text.Length == 0)
                    return;

                string boxText = this.Text;
                if (boxText.EndsWith(afterComplete) && afterComplete.Length > 0)
                {
                    boxText = boxText.Substring(0, boxText.Length - afterComplete.Length);        
                }

                //get the partial nick
                if (boxText.IndexOf(' ') == -1)
                    _partialNick = boxText;
                else
                    _partialNick = boxText.Substring(boxText.LastIndexOf(' ') + 1);

                if (_partialNick.Length == 0)
                    return;

                //get the current window
                //if this is FormMain - it is not docked 

                if (Array.IndexOf(FormMain.Instance.CurrentWindow.Connection.ServerSetting.ChannelTypes, _partialNick[0]) != -1)
                {
                    //channel name complete
                    this.Text = this.Text.Substring(0, this.Text.Length - _partialNick.Length) + FormMain.Instance.CurrentWindow.TabCaption;
                    this.SelectionStart = this.Text.Length;
                    this._nickNumber = -1;
                    return;
                }
                
                if (_nickNumber == -1)
                {
                    _nickCompleteNames.Clear();

                    foreach (User u in FormMain.Instance.CurrentWindow.Nicks.Values)
                    {
                        if (u.NickName.Length > _partialNick.Length)
                        {
                            if (u.NickName.Substring(0, _partialNick.Length).ToLower() == _partialNick.ToLower())
                            {
                                NickList.Nick n = new NickList.Nick();
                                n.nick = u.NickName;
                                n.Level = u.Level;
                                _nickCompleteNames.Add(n);
                            }
                        }
                    }
                    if (_nickCompleteNames.Count == 0)
                        return;
                    
                    _nickCompleteNames.Sort();

                    _nickNumber = 0;
                }
                else
                {
                    if (_nickCompleteNames.Count == 0)
                    {
                        _nickNumber = -1;
                        return;
                    }
                    
                    _nickNumber++;
                    if ( _nickNumber > (_nickCompleteNames.Count - 1))
                        _nickNumber = 0;
                }

                this.Text = boxText.Substring(0, boxText.Length - _partialNick.Length) + _nickCompleteNames[_nickNumber] + afterComplete;
                this.SelectionStart = this.Text.Length;

            }
            else if (FormMain.Instance.CurrentWindowStyle == IceTabPage.WindowType.Query || FormMain.Instance.CurrentWindowStyle == IceTabPage.WindowType.DCCChat)
            {
                string boxText = this.Text;
                if (boxText.EndsWith(afterComplete) && afterComplete.Length > 0)
                {
                    boxText = boxText.Substring(0, boxText.Length - afterComplete.Length);
                }

                if (boxText.IndexOf(' ') == -1)
                    _partialNick = boxText;
                else
                    _partialNick = boxText.Substring(boxText.LastIndexOf(' ') + 1);
 
                if (_partialNick.Length == 0)
                    this.Text += FormMain.Instance.CurrentWindow.TabCaption;
                else
                    this.Text = boxText.Substring(0, this.Text.Length - _partialNick.Length) + FormMain.Instance.CurrentWindow.TabCaption + afterComplete;
                
                this.SelectionStart = this.Text.Length;

            }
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("key up:" + e.Alt + ":" + e.Modifiers);
            //key up:False:None

            if (e.Alt)
            {
                //show the menu bar if we are in a FormWindow
                if (parent.Parent.Name == "IceTabPage")
                {
                    FormWindow fw = (FormWindow)parent.Parent.Parent;
                    System.Diagnostics.Debug.WriteLine(fw.MainMenu.Visible);
                    if (fw.MainMenu.Visible)
                        fw.MainMenu.Visible = false;
                    else
                        fw.MainMenu.Visible = true;

                }
            }
        }
		protected override void OnKeyDown(KeyEventArgs e)
		{			
            if (e.Modifiers == Keys.Control)
			{
				if (e.KeyCode == Keys.K)
				{
                    base.SelectedText = "\x0003";
					e.Handled=true;
				}
				else if (e.KeyCode == Keys.B)
				{
					base.SelectedText = "\x0002";
					e.Handled=true;
				}
				else if (e.KeyCode == Keys.U)
				{
                    base.SelectedText = "\x001F";
					e.Handled=true;
				}
                else if (e.KeyCode == Keys.R)
                {
                    base.SelectedText = "\x0016";
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.I)
                {
                    base.SelectedText = "\x001D";
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.O)
                {
                    base.SelectedText = "\x000F";
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.D)
                {
                    FormMain.Instance.debugWindowToolStripMenuItem.PerformClick();
                    e.Handled = true;                
                }
                else if (e.KeyCode == Keys.S)
                {
                    FormMain.Instance.iceChatEditorToolStripMenuItem.PerformClick();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.P)
                {
                    FormMain.Instance.iceChatSettingsToolStripMenuItem.PerformClick();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.W)
                {
                    FormMain.Instance.closeCurrentWindowToolStripMenuItem.PerformClick();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.F)
                {
                    FormMain.Instance.fontSettingsToolStripMenuItem.PerformClick();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.G)
                {
                    FormMain.Instance.iceChatColorsToolStripMenuItem.PerformClick();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Tab)
                {
                    int nextIndex = FormMain.Instance.ChannelBar.TabCount == FormMain.Instance.ChannelBar.SelectedIndex + 1 ? 0 : FormMain.Instance.ChannelBar.SelectedIndex + 1;
                    FormMain.Instance.ChannelBar.SelectTab(FormMain.Instance.ChannelBar.TabPages[nextIndex]);
                    FormMain.Instance.ServerTree.SelectTab(FormMain.Instance.ChannelBar.TabPages[nextIndex], false);
                    return;
                }
                else if (e.KeyCode == Keys.PageUp)
                {
                    int nextIndex = FormMain.Instance.ChannelBar.TabCount == FormMain.Instance.ChannelBar.SelectedIndex + 1 ? 0 : FormMain.Instance.ChannelBar.SelectedIndex + 1;
                    FormMain.Instance.ChannelBar.SelectTab(FormMain.Instance.ChannelBar.TabPages[nextIndex]);
                    FormMain.Instance.ServerTree.SelectTab(FormMain.Instance.ChannelBar.TabPages[nextIndex], false);
                    return;
                }
                else if (e.KeyCode == Keys.PageDown)
                {
                    int prevIndex = FormMain.Instance.ChannelBar.SelectedIndex == 0 ? FormMain.Instance.ChannelBar.TabCount - 1 : FormMain.Instance.ChannelBar.SelectedIndex - 1;
                    FormMain.Instance.ChannelBar.SelectTab(FormMain.Instance.ChannelBar.TabPages[prevIndex]);
                    FormMain.Instance.ServerTree.SelectTab(FormMain.Instance.ChannelBar.TabPages[prevIndex], false);
                    return;
                }
                else if (e.KeyCode == Keys.Back)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    //check if in the middle of a sentence or not (or in the middle of the last word)

                    if (base.Text.Length > 0)
                    {
                        int prevSpace = base.Text.LastIndexOf(' ');
                        int nextSpace = base.Text.IndexOf(' ', base.SelectionStart);

                        if (nextSpace == -1)
                        {
                            //last word - remove it
                            if (prevSpace == -1)
                                base.Text = "";
                            else
                            {
                                base.Text = base.Text.Substring(0, prevSpace);
                                base.SelectionStart = base.Text.Length;
                            }
                        }
                        else
                        {
                            //in the middle.. split it
                            string before = base.Text.Substring(0, nextSpace);
                            string after = base.Text.Substring(nextSpace);
                            int lastSpace = before.LastIndexOf(' ');
                            //check if 1st word or not
                            if (lastSpace == -1)
                            {
                                base.Text = after;
                            }
                            else
                            {
                                base.Text = before.Substring(0, lastSpace) + after;
                                base.SelectionStart = before.Substring(0, lastSpace).Length;
                            }
                        }
                    }
                    return;
                }

                if (base.Multiline == true)
                {
                    if (e.KeyCode == Keys.Up)
                    {
                        e.Handled = true;

                        if (parent.CurrentHistoryItem <= 0)
                        {
                            if (parent.Buffer.Count == 1)
                                parent.CurrentHistoryItem = 1;
                            else
                                return;                        }

                        if ((parent.CurrentHistoryItem != parent.Buffer.Count - 1) || (base.Text.ToString() == parent.Buffer[parent.Buffer.Count - 1]))
                        {
                            parent.CurrentHistoryItem--;
                        }
                        else
                        {
                            parent.CurrentHistoryItem = parent.Buffer.Count - 1;
                        }

                        if (parent.CurrentHistoryItem > -1)
                        {
                            base.Text = parent.Buffer[parent.CurrentHistoryItem];
                            base.SelectionStart = base.Text.Length;
                        }
                        return;
                        
                    }

                    if (e.KeyCode == Keys.Down)
                    {
                        //DOWN Key
                        e.Handled = true;

                        if (parent.CurrentHistoryItem >= parent.Buffer.Count - 1)
                        {
                            parent.CurrentHistoryItem = parent.Buffer.Count - 1;
                            base.Text = "";
                            return;
                        }
                        else if (parent.CurrentHistoryItem == -1)
                        {
                            base.Text = "";
                            return;
                        }

                        parent.CurrentHistoryItem++;
                        base.Text = parent.Buffer[parent.CurrentHistoryItem];
                        base.SelectionStart = base.Text.Length;
                        return;
                    }


                    if (e.KeyCode == Keys.A)
                    {
                        //Select all
                        base.SelectionStart = 0;
                        base.SelectionLength = base.Text.Length;

                        e.Handled = true;
                        e.SuppressKeyPress = true;

                    }
                }
			}


            if (e.KeyCode == Keys.Tab && (e.KeyData & Keys.Control) != Keys.None)
            {
                bool forward = (e.KeyData & Keys.Shift) == Keys.None;
                if (!forward)
                {
                    int prevIndex = FormMain.Instance.ChannelBar.SelectedIndex == 0 ? FormMain.Instance.ChannelBar.TabCount - 1 : FormMain.Instance.ChannelBar.SelectedIndex - 1;
                    FormMain.Instance.ChannelBar.SelectTab(FormMain.Instance.ChannelBar.TabPages[prevIndex]);
                    FormMain.Instance.ServerTree.SelectTab(FormMain.Instance.ChannelBar.TabPages[prevIndex], false);
                    return;
                }
            }

			//code below is for the single line Inputbox
			//UP Key
            if (base.Multiline == false)
            {
                if (e.KeyCode == Keys.Up)
                {
                    e.Handled = true;

                    if (parent.CurrentHistoryItem <= 0)
                    {
                        if (parent.Buffer.Count == 1)
                            parent.CurrentHistoryItem = 1;
                        else
                            return;
                    }

                    if ((parent.CurrentHistoryItem != parent.Buffer.Count - 1) || (base.Text.ToString() == parent.Buffer[parent.Buffer.Count - 1]))
                    {
                        parent.CurrentHistoryItem--;
                    }
                    else
                    {
                        parent.CurrentHistoryItem = parent.Buffer.Count - 1;
                    }

                    if (parent.CurrentHistoryItem > -1)
                    {
                        base.Text = parent.Buffer[parent.CurrentHistoryItem];
                        base.SelectionStart = base.Text.Length;
                    }
                    return;
                }

                if (e.KeyCode == Keys.Down)
                {
                    //DOWN Key
                    e.Handled = true;

                    if (parent.CurrentHistoryItem >= parent.Buffer.Count - 1)
                    {
                        parent.CurrentHistoryItem = parent.Buffer.Count - 1;
                        base.Text = "";
                        return;
                    }
                    else if (parent.CurrentHistoryItem == -1)
                    {
                        base.Text = "";
                        return;
                    }

                    parent.CurrentHistoryItem++;
                    base.Text = parent.Buffer[parent.CurrentHistoryItem];
                    base.SelectionStart = base.Text.Length;
                    return;
                }
            }

            if (e.KeyCode == Keys.PageDown)
            {
                //scroll window down one page
                IceTabPage currentWindow;
                if (parent.Parent.Name == "FormMain")
                    currentWindow = FormMain.Instance.CurrentWindow;
                else
                    currentWindow = (IceTabPage)parent.Parent;

                if (currentWindow.WindowStyle != IceTabPage.WindowType.Console)
                {
                    if (currentWindow != null)
                        ScrollWindowPage(false);
                }
                else
                {
                    //make a scroll window for the console
                    //find the current window for the console
                    ScrollConsoleWindowPage(false);
                }
            }

            if (e.KeyCode == Keys.PageUp)
            {
                //scroll window down one page
                if (FormMain.Instance.CurrentWindowStyle != IceTabPage.WindowType.Console)
                {
                    if (FormMain.Instance.CurrentWindow != null)
                        ScrollWindowPage(true);
                }
                else
                {
                    //make a scroll window for the console
                    //find the current window for the console
                    ScrollConsoleWindowPage(true);
                }
            }

            if (e.KeyCode == Keys.F3)
            {
                e.Handled = true;

            }

            if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                //show or hide the wide text panel
                if (this.Parent.Name == "inputPanel")
                {
                    ((InputPanel)this.Parent).ShowWideTextPanel = !((InputPanel)this.Parent).ShowWideTextPanel;
                    FormMain.Instance.IceChatOptions.ShowMultilineEditbox = ((InputPanel)this.Parent).ShowWideTextPanel;
                }
                else
                {
                    ((InputPanel)this.Parent.Parent).ShowWideTextPanel = !((InputPanel)this.Parent.Parent).ShowWideTextPanel;
                    FormMain.Instance.IceChatOptions.ShowMultilineEditbox = ((InputPanel)this.Parent.Parent).ShowWideTextPanel;
                }
                return;
            }

            if (e.KeyCode == Keys.Escape)
			{
				e.Handled = true;				
				base.Text = "";
				return;
			}

            if (base.Multiline == true)
            {
                if (e.KeyCode == Keys.Enter && e.Modifiers != Keys.Shift)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
    
                    if (base.Text.Length > 0)
                    {
                        parent.SendButtonClick();
                    }
                    return;
                }
            }
			base.OnKeyDown (e);			
		
		}
        
        internal void OnEnterKey()
        {
            OnKeyPress(new KeyPressEventArgs((char)13));
        }

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			string command = base.Text;

            bool ctrlKeyUsed = false;
            if (base.Multiline == false)
            {
                if (e.KeyChar == (char)10)
                {
                    if (command.Length > 0)
                    {
                        ctrlKeyUsed = true;
                    }
                    else
                        return;
                }

                if (e.KeyChar == (char)13 || ctrlKeyUsed)
                {
                    if (command.Length == 0)
                    {
                        return;
                    }

                    //add the text to the _buffer
                    addToBuffer(command);

                    //fire event for server command
                    if (OnCommand != null)
                    {
                        if (ctrlKeyUsed)
                            command = "/say " + command;

                        OnCommand(this, command);
                    }

                    //clear the text box
                    base.Text = "";
                    e.Handled = true;

                }
                else
                {
                    base.OnKeyPress(e);
                }
            }
            else
            {
                base.OnKeyPress(e);
            }
		}
		
		internal void addToBuffer(string data)
		{
			//add text to back _buffer
			if (data.Length == 0) return;

            //check for maximum back _buffer history here
			//remove 1st item if exceeded size
			if (parent.Buffer.Count > parent.MaxBufferSize)
                parent.Buffer.Remove(parent.Buffer[0]);
			
			//check what the last value that was added, no need to duplicate
            if (parent.Buffer.Count > 0)
                if (parent.Buffer[parent.Buffer.Count - 1].ToString() == data)
                    return;

            parent.Buffer.Add(data);
            parent.CurrentHistoryItem = parent.Buffer.Count - 1;
		}

	}
}

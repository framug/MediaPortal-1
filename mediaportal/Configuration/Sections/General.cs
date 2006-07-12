#region Copyright (C) 2005-2006 Team MediaPortal

/* 
 *	Copyright (C) 2005-2006 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Globalization;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using MediaPortal.Util;
using System.Runtime.InteropServices;
using MediaPortal.Utils.Services;

#pragma warning disable 108

namespace MediaPortal.Configuration.Sections
{
  public class General : MediaPortal.Configuration.SectionSettings
  {
    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    public static extern int SendMessageTimeout(
      IntPtr hwnd,
      IntPtr msg,
      IntPtr wParam,
      IntPtr lParam,
      IntPtr fuFlags,
      IntPtr uTimeout,
      out IntPtr lpdwResult);

    const int WM_SETTINGCHANGE = 0x1A;
    const int SMTO_ABORTIFHUNG = 0x2;
    const int HWND_BROADCAST = 0xFFFF;

    const string LanguageDirectory = @"language\";
    private MediaPortal.UserInterface.Controls.MPGroupBox mpGroupBox1;
    private MediaPortal.UserInterface.Controls.MPComboBox languageComboBox;
    private MediaPortal.UserInterface.Controls.MPLabel label2;
    private MediaPortal.UserInterface.Controls.MPGroupBox groupBox1;
    private System.Windows.Forms.CheckedListBox settingsCheckedListBox;
    private MediaPortal.UserInterface.Controls.MPGroupBox groupBox2;
    private System.Windows.Forms.NumericUpDown numericUpDown1;
    private MediaPortal.UserInterface.Controls.MPLabel label1;
    private MediaPortal.UserInterface.Controls.MPLabel label3;
    private System.ComponentModel.IContainer components = null;
    protected ILog _log;

    public General()
      : this("General")
    {
    }

    public General(string name)
      : base(name)
    {
      ServiceProvider services = GlobalServiceProvider.Instance;
      _log = services.Get<ILog>();

      // This call is required by the Windows Form Designer.
      InitializeComponent();

      //
      // Populate comboboxes
      //
      LoadLanguages();
    }

    private void LoadLanguages()
    {
      // Get system language
      string strLongLanguage = CultureInfo.CurrentCulture.EnglishName;
      int iTrimIndex = strLongLanguage.IndexOf(" ", 0, strLongLanguage.Length);
      string strShortLanguage = strLongLanguage.Substring(0, iTrimIndex);

      bool bExactLanguageFound = false;
      if (Directory.Exists(LanguageDirectory))
      {
        string[] folders = Directory.GetDirectories(LanguageDirectory, "*.*");

        foreach (string folder in folders)
        {
          string fileName = folder.Substring(@"language\".Length);

          //
          // Exclude cvs folder
          //
          if (fileName.ToLower() != "cvs")
          {
            if (fileName.Length > 0)
            {
              fileName = fileName.Substring(0, 1).ToUpper() + fileName.Substring(1);
              languageComboBox.Items.Add(fileName);

              // Check language file to user region language
              if (fileName.ToLower() == strLongLanguage.ToLower())
              {
                languageComboBox.Text = fileName;
                bExactLanguageFound = true;
              }
              else if (!bExactLanguageFound && (fileName.ToLower() == strShortLanguage.ToLower()))
              {
                languageComboBox.Text = fileName;
              }
            }
          }
        }
      }

      if (languageComboBox.Text == "")
      {
        languageComboBox.Text = "English";
      }
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (components != null)
        {
          components.Dispose();
        }
      }
      base.Dispose(disposing);
    }

    string[][] sectionEntries = new string[][] { 
      new string[] { "general", "startfullscreen", "false" },
      new string[] { "general", "minimizeonstartup", "false" },
      new string[] { "general", "minimizeonexit", "false" },
      new string[] { "general", "autohidemouse", "true" },
			new string[] { "general", "mousesupport", "true" }, 
      new string[] { "general", "hideextensions", "true" },
      new string[] { "general", "animations", "true" },
			new string[] { "general", "autostart", "false" },
			new string[] { "general", "baloontips", "false" },
			new string[] { "general", "dblclickasrightclick", "false" },
			new string[] { "general", "hidetaskbar", "true" },
			new string[] { "general", "alwaysontop", "true" },
			new string[] { "general", "exclusivemode", "true" },
			new string[] { "general", "enableguisounds", "true" },
			new string[] { "general", "screensaver", "false" },
      new string[] { "general", "turnoffmonitor", "false" },
			new string[] { "general", "startbasichome", "false" },
      new string[] { "general", "turnmonitoronafterresume", "false" },
      new string[] { "general", "allowfocus", "false" }};


    /// <summary>
    /// 
    /// </summary>
    public override void LoadSettings()
    {
      using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings("MediaPortal.xml"))
      {
        //
        // Load general settings
        //
        for (int index = 0; index < sectionEntries.Length; index++)
        {
          string[] currentSection = sectionEntries[index];
          settingsCheckedListBox.SetItemChecked(index, xmlreader.GetValueAsBool(currentSection[0], currentSection[1], bool.Parse(currentSection[2])));
        }

        //
        // Set language
        //
        languageComboBox.Text = xmlreader.GetValueAsString("skin", "language", languageComboBox.Text);
        //numericUpDown1.Value=xmlreader.GetValueAsInt("vmr9OSDSkin","alphaValue",10);

        // Allow Focus
        using (RegistryKey subkey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
          settingsCheckedListBox.SetItemChecked(17, ((int)subkey.GetValue("ForegroundLockTimeout", 2000000) == 0));
      }
    }

    public override void SaveSettings()
    {
      using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings("MediaPortal.xml"))
      {
        //
        // Load general settings
        //
        for (int index = 0; index < sectionEntries.Length - 1; index++)  // Leave out last setting (focus)!
        {
          string[] currentSection = sectionEntries[index];
          xmlwriter.SetValueAsBool(currentSection[0], currentSection[1], settingsCheckedListBox.GetItemChecked(index));
        }

        //
        // Set language
        string prevLanguage = xmlwriter.GetValueAsString("skin", "language", "English");
        string skin = xmlwriter.GetValueAsString("skin", "name", "mce");
        if (prevLanguage != languageComboBox.Text)
          MediaPortal.Util.Utils.DeleteFiles(@"skin\" + skin + @"\fonts", "*");

        xmlwriter.SetValue("skin", "language", languageComboBox.Text);

        //xmlwriter.SetValue("vmr9OSDSkin","alphaValue",numericUpDown1.Value);
      }

      try
      {
        if (settingsCheckedListBox.GetItemChecked(7))
        {
          string fileName = String.Format("\"{0}\"", System.IO.Path.GetFullPath("mediaportal.exe"));
          using (RegistryKey subkey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            subkey.SetValue("MediaPortal", fileName);
        }
        else
          using (RegistryKey subkey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            subkey.DeleteValue("MediaPortal", false);

        Int32 iValue = 1;
        if (settingsCheckedListBox.GetItemChecked(8))
          iValue = 0;

        using (RegistryKey subkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", true))
          subkey.SetValue("EnableBalloonTips", iValue);

        if (settingsCheckedListBox.GetItemChecked(17))
          using (RegistryKey subkey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            subkey.SetValue("ForegroundLockTimeout", 0);

        // Allow Focus
        using (RegistryKey subkey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
        {
          bool focusChecked = ((int)subkey.GetValue("ForegroundLockTimeout", 200000) == 0);

          if (focusChecked != settingsCheckedListBox.GetItemChecked(17))
            if (settingsCheckedListBox.GetItemChecked(17))
              subkey.SetValue("ForegroundLockTimeout", 0);
            else
              subkey.SetValue("ForegroundLockTimeout", 200000);
        }

        IntPtr result = IntPtr.Zero;
        SendMessageTimeout((IntPtr)HWND_BROADCAST, (IntPtr)WM_SETTINGCHANGE, IntPtr.Zero, Marshal.StringToBSTR(String.Empty), (IntPtr)SMTO_ABORTIFHUNG, (IntPtr)3, out result);
      }
      catch (Exception ex)
      {
        _log.Info("Exception: {0}", ex.Message);
        _log.Info("Exception: {0}", ex);
        _log.Info("Exception: {0}", ex.StackTrace);
      }
    }

    #region Designer generated code
    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.mpGroupBox1 = new MediaPortal.UserInterface.Controls.MPGroupBox();
      this.languageComboBox = new MediaPortal.UserInterface.Controls.MPComboBox();
      this.label2 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.groupBox1 = new MediaPortal.UserInterface.Controls.MPGroupBox();
      this.settingsCheckedListBox = new System.Windows.Forms.CheckedListBox();
      this.groupBox2 = new MediaPortal.UserInterface.Controls.MPGroupBox();
      this.label1 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
      this.label3 = new MediaPortal.UserInterface.Controls.MPLabel();
      this.mpGroupBox1.SuspendLayout();
      this.groupBox1.SuspendLayout();
      this.groupBox2.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
      this.SuspendLayout();
      // 
      // mpGroupBox1
      // 
      this.mpGroupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.mpGroupBox1.Controls.Add(this.languageComboBox);
      this.mpGroupBox1.Controls.Add(this.label2);
      this.mpGroupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.mpGroupBox1.Location = new System.Drawing.Point(0, 0);
      this.mpGroupBox1.Name = "mpGroupBox1";
      this.mpGroupBox1.Size = new System.Drawing.Size(472, 56);
      this.mpGroupBox1.TabIndex = 0;
      this.mpGroupBox1.TabStop = false;
      this.mpGroupBox1.Text = "Language Settings";
      // 
      // languageComboBox
      // 
      this.languageComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.languageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.languageComboBox.Location = new System.Drawing.Point(168, 20);
      this.languageComboBox.Name = "languageComboBox";
      this.languageComboBox.Size = new System.Drawing.Size(288, 21);
      this.languageComboBox.TabIndex = 1;
      // 
      // label2
      // 
      this.label2.Location = new System.Drawing.Point(16, 24);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(96, 16);
      this.label2.TabIndex = 0;
      this.label2.Text = "Display language:";
      // 
      // groupBox1
      // 
      this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.groupBox1.Controls.Add(this.settingsCheckedListBox);
      this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.groupBox1.Location = new System.Drawing.Point(0, 64);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(472, 320);
      this.groupBox1.TabIndex = 1;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "General Settings";
      // 
      // settingsCheckedListBox
      // 
      this.settingsCheckedListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.settingsCheckedListBox.CheckOnClick = true;
      this.settingsCheckedListBox.Items.AddRange(new object[] {
            "Start MediaPortal in fullscreen mode",
            "Minimize to tray on start up",
            "Minimize to tray on GUI exit",
            "Autohide mouse cursor in fullscreen mode when idle",
            "Show special mouse controls (scrollbars, etc)",
            "Dont show file extensions like .mp3, .avi, .mpg,...",
            "Enable animations",
            "Autostart MediaPortal when windows starts",
            "Disable Windows XP balloon tips",
            "Use mouse left double click as right click",
            "Hide taskbar in fullscreen mode",
            "MediaPortal always on top",
            "Use Exclusive DirectX Mode for fullscreen tv/video",
            "Enable GUI sound effects",
            "Blank screen in fullscreen mode when MediaPortal is idle",
            "Turn off monitor when blanking screen",
            "Start with basic home screen",
            "Turn monitor/tv on when resuming from standby",
            "Allow MediaPortal (and other apps) to gain focus (per-user setting - needs reboot" +
                "!)"});
      this.settingsCheckedListBox.Location = new System.Drawing.Point(16, 24);
      this.settingsCheckedListBox.Name = "settingsCheckedListBox";
      this.settingsCheckedListBox.Size = new System.Drawing.Size(440, 274);
      this.settingsCheckedListBox.TabIndex = 0;
      // 
      // groupBox2
      // 
      this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.groupBox2.Controls.Add(this.label1);
      this.groupBox2.Controls.Add(this.numericUpDown1);
      this.groupBox2.Controls.Add(this.label3);
      this.groupBox2.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.groupBox2.Location = new System.Drawing.Point(0, 368);
      this.groupBox2.Name = "groupBox2";
      this.groupBox2.Size = new System.Drawing.Size(472, 56);
      this.groupBox2.TabIndex = 2;
      this.groupBox2.TabStop = false;
      this.groupBox2.Text = "VMR9 OSD Settings";
      this.groupBox2.Visible = false;
      // 
      // label1
      // 
      this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.label1.Location = new System.Drawing.Point(16, 24);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(96, 16);
      this.label1.TabIndex = 0;
      this.label1.Text = "OSD Alpha level:";
      this.label1.Visible = false;
      // 
      // numericUpDown1
      // 
      this.numericUpDown1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.numericUpDown1.Location = new System.Drawing.Point(168, 20);
      this.numericUpDown1.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
      this.numericUpDown1.Name = "numericUpDown1";
      this.numericUpDown1.Size = new System.Drawing.Size(168, 20);
      this.numericUpDown1.TabIndex = 1;
      this.numericUpDown1.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
      this.numericUpDown1.Visible = false;
      // 
      // label3
      // 
      this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.label3.Location = new System.Drawing.Point(328, 24);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(128, 16);
      this.label3.TabIndex = 2;
      this.label3.Text = "(10 = solid, 0 = invisible)";
      this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
      this.label3.Visible = false;
      // 
      // General
      // 
      this.BackColor = System.Drawing.SystemColors.Control;
      this.Controls.Add(this.groupBox2);
      this.Controls.Add(this.groupBox1);
      this.Controls.Add(this.mpGroupBox1);
      this.Name = "General";
      this.Size = new System.Drawing.Size(472, 408);
      this.mpGroupBox1.ResumeLayout(false);
      this.groupBox1.ResumeLayout(false);
      this.groupBox2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
      this.ResumeLayout(false);

    }
    #endregion
  }
}


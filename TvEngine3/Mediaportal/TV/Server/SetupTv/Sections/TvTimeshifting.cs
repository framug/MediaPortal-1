#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Windows.Forms;
using Mediaportal.TV.Server.SetupControls;
using Mediaportal.TV.Server.TVControl.ServiceAgents;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;

namespace Mediaportal.TV.Server.SetupTV.Sections
{
  public partial class TvTimeshifting : SectionSettings
  {
    private string _bufferLocation;
    private int _bufferFileSize;
    private int _bufferFileCount;
    private int _bufferFileCountMaximum;
    private int _tunerLimit;
    private int _parkTimeLimit;

    public TvTimeshifting(ServerConfigurationChangedEventHandler handler)
      : base("Time-shifting", handler)
    {
      InitializeComponent();
    }

    public override void OnSectionActivated()
    {
      this.LogDebug("time-shifting: activating");

      _bufferLocation = ServiceAgents.Instance.SettingServiceAgent.GetValue("timeShiftBufferFolder", string.Empty);
      _bufferFileSize = ServiceAgents.Instance.SettingServiceAgent.GetValue("timeShiftBufferFileSize", 256);
      _bufferFileCount = ServiceAgents.Instance.SettingServiceAgent.GetValue("timeShiftBufferFileCount", 6);
      _bufferFileCountMaximum = ServiceAgents.Instance.SettingServiceAgent.GetValue("timeShiftBufferFileCountMaximum", 20);
      textBoxBufferLocation.Text = _bufferLocation;
      numericUpDownBufferFileSize.Value = _bufferFileSize;
      numericUpDownBufferFileCount.Value = _bufferFileCount;
      numericUpDownBufferFileCountMaximum.Value = _bufferFileCountMaximum;
      UpdateBufferTimeEstimates();
      this.LogDebug("  buffer...");
      this.LogDebug("    folder         = {0}", textBoxBufferLocation.Text);
      this.LogDebug("    file size      = {0} MB", numericUpDownBufferFileSize.Value);
      this.LogDebug("    file count     = {0}", numericUpDownBufferFileCount.Value);
      this.LogDebug("    max file count = {0}", numericUpDownBufferFileCountMaximum.Value);

      _tunerLimit = ServiceAgents.Instance.SettingServiceAgent.GetValue("timeShiftTunerLimit", 3);
      _parkTimeLimit = ServiceAgents.Instance.SettingServiceAgent.GetValue("parkedStreamTimeout", 10);
      numericUpDownTunerLimit.Value = _tunerLimit;
      numericUpDownParkTimeLimit.Value = _parkTimeLimit;
      this.LogDebug("  tuner limit      = {0}", numericUpDownTunerLimit.Value);
      this.LogDebug("  park time limit  = {0} m", numericUpDownParkTimeLimit.Value);

      base.OnSectionActivated();
    }

    public override void OnSectionDeActivated()
    {
      this.LogDebug("time-shifting: deactivating");

      bool needReload = false;
      if (!string.Equals(_bufferLocation, textBoxBufferLocation.Text))
      {
        this.LogInfo("time-shifting: buffer location changed from {0} to {1}", _bufferLocation, textBoxBufferLocation.Text);
        ServiceAgents.Instance.SettingServiceAgent.SaveValue("timeShiftBufferFolder", textBoxBufferLocation.Text);
        needReload = true;
      }
      if (_bufferFileSize != numericUpDownBufferFileSize.Value)
      {
        this.LogInfo("time-shifting: buffer file size changed from {0} to {1} MB", _bufferFileSize, numericUpDownBufferFileSize.Value);
        ServiceAgents.Instance.SettingServiceAgent.SaveValue("timeShiftBufferFileSize", (int)numericUpDownBufferFileSize.Value);
        needReload = true;
      }
      if (_bufferFileCount != numericUpDownBufferFileCount.Value)
      {
        this.LogInfo("time-shifting: buffer file count changed from {0} to {1}", _bufferFileCount, numericUpDownBufferFileCount.Value);
        ServiceAgents.Instance.SettingServiceAgent.SaveValue("timeShiftBufferFileCount", (int)numericUpDownBufferFileCount.Value);
        needReload = true;
      }
      if (_bufferFileCountMaximum != numericUpDownBufferFileCountMaximum.Value)
      {
        this.LogInfo("time-shifting: buffer maximum file count changed from {0} to {1}", _bufferFileCountMaximum, numericUpDownBufferFileCountMaximum.Value);
        ServiceAgents.Instance.SettingServiceAgent.SaveValue("timeShiftBufferFileCountMaximum", (int)numericUpDownBufferFileCountMaximum.Value);
        needReload = true;
      }

      if (_tunerLimit != numericUpDownTunerLimit.Value)
      {
        this.LogInfo("time-shifting: tuner limit changed from {0} to {1}", _tunerLimit, numericUpDownTunerLimit.Value);
        ServiceAgents.Instance.SettingServiceAgent.SaveValue("timeShiftTunerLimit", (int)numericUpDownTunerLimit.Value);
        needReload = true;
      }
      if (_parkTimeLimit != numericUpDownParkTimeLimit.Value)
      {
        this.LogInfo("time-shifting: park time limit changed from {0} to {1} minutes", _parkTimeLimit, numericUpDownParkTimeLimit.Value);
        ServiceAgents.Instance.SettingServiceAgent.SaveValue("parkedStreamTimeout", (int)numericUpDownParkTimeLimit.Value);
        needReload = true;
      }

      if (needReload)
      {
        OnServerConfigurationChanged(this, false, true, null);
      }

      base.OnSectionDeActivated();
    }

    private void buttonBufferLocationBrowse_Click(object sender, EventArgs e)
    {
      FolderBrowserDialog dlg = new FolderBrowserDialog();
      dlg.SelectedPath = textBoxBufferLocation.Text;
      dlg.Description = "Select a folder for the time-shift buffer.";
      dlg.ShowNewFolderButton = true;
      if (dlg.ShowDialog() == DialogResult.OK)
      {
        textBoxBufferLocation.Text = dlg.SelectedPath;
      }
    }

    private void numericUpDownBufferFileSize_ValueChanged(object sender, EventArgs e)
    {
      UpdateBufferTimeEstimates();
    }

    private void numericUpDownBufferFileCount_ValueChanged(object sender, EventArgs e)
    {
      UpdateBufferTimeEstimates();
    }

    private void numericUpDownBufferFileCountMaximum_ValueChanged(object sender, EventArgs e)
    {
      UpdateBufferTimeEstimates();
    }

    private void UpdateBufferTimeEstimates()
    {
      labelBufferFileCountDescription.Text = string.Format("=> {0} GB (approx. {1} minutes SD or {2} minutes HD)",
                                              Math.Round(numericUpDownBufferFileSize.Value * numericUpDownBufferFileCount.Value / 1000, 2),
                                              Math.Round(numericUpDownBufferFileSize.Value * numericUpDownBufferFileCount.Value / 375000, 1),   // SD = ~3 Mb/s
                                              Math.Round(numericUpDownBufferFileSize.Value * numericUpDownBufferFileCount.Value / 1250000, 1)); // HD = ~10 Mb/s
      labelBufferFileCountMaximumDescription.Text = string.Format("=> {0} GB (approx. {1} minutes SD or {2} minutes HD)",
                                                      Math.Round(numericUpDownBufferFileSize.Value * numericUpDownBufferFileCountMaximum.Value / 1000, 2),
                                                      Math.Round(numericUpDownBufferFileSize.Value * numericUpDownBufferFileCountMaximum.Value / 375000, 1),    // SD = ~3 Mb/s
                                                      Math.Round(numericUpDownBufferFileSize.Value * numericUpDownBufferFileCountMaximum.Value / 1250000, 1));  // HD = ~10 Mb/s
    }
  }
}
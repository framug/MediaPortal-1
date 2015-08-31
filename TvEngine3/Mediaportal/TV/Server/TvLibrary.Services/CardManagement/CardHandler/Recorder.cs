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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVLibrary.Interfaces;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Tuner.Enum;
using Mediaportal.TV.Server.TVService.Interfaces.CardHandler;
using Mediaportal.TV.Server.TVService.Interfaces.Enums;
using Mediaportal.TV.Server.TVService.Interfaces.Services;
using Microsoft.Win32;
using ISubChannel = Mediaportal.TV.Server.TVLibrary.Interfaces.Tuner.ISubChannel;

namespace Mediaportal.TV.Server.TVLibrary.CardManagement.CardHandler
{
  public class Recorder : TimeShifterBase, IRecorder
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Recording"/> class.
    /// </summary>
    /// <param name="cardHandler">The card handler.</param>
    public Recorder(ITvCardHandler cardHandler)
      : base(cardHandler, "recordingFolder")
    {
    }

    public override void ReloadConfiguration()
    {
      this.LogDebug("recorder: reload configuration");
      base.ReloadConfiguration();
    }

    protected override string GetDefaultFolder()
    {
      string wmcRecordingFolder = null;
      try
      {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\MediaCenter\Service\Recording"))
        {
          if (key != null)
          {
            wmcRecordingFolder = key.GetValue("RecordPath").ToString();
            key.Close();
            key.Dispose();
          }
        }
      }
      catch
      {
      }

      if (!string.IsNullOrEmpty(wmcRecordingFolder))
      {
        return wmcRecordingFolder;
      }

      if (Environment.OSVersion.Version.Major >= 6) // Vista or later
      {
        return Path.Combine(Environment.GetEnvironmentVariable("PUBLIC"), "Recorded TV");
      }
      return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Shared Documents\\Recorded TV");
    }

    protected override void AudioVideoEventHandler(PidType pidType)
    {
      this.LogDebug("Recorder audioVideoEventHandler {0}", pidType);

      // we are only interested in video and audio PIDs
      if (pidType == PidType.Audio)
      {
        _eventAudio.Set();
      }

      if (pidType == PidType.Video)
      {
        _eventVideo.Set();
      }
    }

    /// <summary>
    /// Starts recording.
    /// </summary>
    /// <param name="user">User</param>
    /// <param name="fileName">Name of the recording file.</param>
    /// <returns></returns>
    public TvResult Start(ref IUser user, ref string fileName)
    {
      TvResult result = TvResult.UnknownError;
      try
      {
#if DEBUG
        if (File.Exists(@"\failrec_" + _cardHandler.Card.TunerId))
        {
          throw new Exception("failed rec. on purpose");
        }
#endif
        if (IsTuneCancelled())
        {
          result = TvResult.TuneCancelled;
          return result;
        }

        _eventTimeshift.Reset();
        if (_cardHandler.Card.IsEnabled)
        {                              
          _cardHandler.UserManagement.RefreshUser(ref user);
          int timeshiftingSubChannel = _cardHandler.UserManagement.GetTimeshiftingSubChannel(user.Name);
          ISubChannel subchannel = GetSubChannel(timeshiftingSubChannel);
          if (subchannel != null)
          {
            _subchannel = subchannel;

            // Ensure valid and unique file names are used.
            fileName = SanitiseFileName(fileName);
            fileName = Path.Combine(_folder, fileName);
            string directory = Path.GetDirectoryName(fileName);
            bool directoryExists = Directory.Exists(directory);
            if (!directoryExists)
            {
              try
              {
                Directory.CreateDirectory(directory);
                directoryExists = true;
              }
              catch (Exception ex)
              {
                this.LogError(ex, "recorder: failed to create recording folder, folder = {1}", directory);
                result = TvResult.NoFreeDiskSpace;
              }
            }

            if (directoryExists)
            {
              string baseFileName = fileName;
              int i = 1;
              while (File.Exists(fileName + ".ts"))
              {
                fileName = string.Format("{0}_{1}", baseFileName, i++);
              }
              fileName = fileName + ".ts";

              // fix mantis 0002807: A/V detection for recordings is not working correctly 
              // reset the events ONLY before attaching the observer, at a later position it can already miss the a/v callback.
              if (IsTuneCancelled())
              {
                result = TvResult.TuneCancelled;
                return result;
              }
              _eventVideo.Reset();
              _eventAudio.Reset();
              this.LogDebug("Recorder.start add audioVideoEventHandler");
              AttachAudioVideoEventHandler(subchannel);

              this.LogDebug("card: StartRecording {0} {1}", _cardHandler.Card.TunerId, fileName);
              bool recStarted = subchannel.StartRecording(fileName);
              if (recStarted)
              {
                fileName = subchannel.RecordingFileName;
                _cardHandler.UserManagement.SetOwnerSubChannel(timeshiftingSubChannel, user.Name);

                bool isScrambled;
                if (WaitForFile(ref user, out isScrambled))
                {
                  result = TvResult.Succeeded;
                }
                else
                {
                  DetachAudioVideoEventHandler(subchannel);
                  result = GetFailedTvResult(isScrambled);
                }
              }
            }
          }          
        }
        else
        {
          result = TvResult.CardIsDisabled;
        }
        if (result == TvResult.Succeeded)
        {
          StartTimeShiftingEpgGrabber(user);
        }
      }
      catch (Exception ex)
      {
        this.LogError(ex);
        result = TvResult.UnknownError;
      }
      finally
      {
        _eventTimeshift.Set();
        _cancelled = false;
        if (result != TvResult.Succeeded)
        {
          HandleFailedRecording(ref user, fileName);
        }
      }
      return result;
    }

    private static string SanitiseFileName(string fileName)
    {
      if (string.IsNullOrEmpty(fileName))
      {
        Log.Warn("recorder: recording file name is not set");
        return "Unknown";
      }

      fileName = fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
      string[] originalParts = fileName.Split(Path.DirectorySeparatorChar);
      List<string> parts = new List<string>(originalParts.Length);
      for (int i = 0; i < originalParts.Length; i++)
      {
        string part = originalParts[i];
        foreach (char c in Path.GetInvalidFileNameChars())
        {
          part = part.Replace(c, '_');
        }

        while (true)
        {
          string temp = part;

          // Remove trailing dots, and trim.
          if (part[part.Length - 1] == '.')
          {
            part = part.Substring(0, part.Length - 1);
          }
          part = part.Trim();

          if (string.Equals(temp, part))
          {
            break;
          }
        }

        if (!string.IsNullOrEmpty(part))
        {
          parts.Add(part);
        }
      }

      return Path.Combine(parts.ToArray());
    }

    private void HandleFailedRecording(ref IUser user, string fileName)
    {
      this.LogDebug("card: Recording failed! {0} {1}", _cardHandler.Card.TunerId, fileName);
      Stop(ref user);
      _cardHandler.UserManagement.RemoveUser(user, _cardHandler.UserManagement.GetTimeshiftingChannelId(user.Name));

      try
      {
        File.Delete(fileName);

        // Delete the containing folder if it's empty and not the base
        // recording folder.
        string folderName = Path.GetDirectoryName(fileName);
        if (string.Equals(folderName, _folder))
        {
          return;
        }

        DirectoryInfo info = new DirectoryInfo(folderName);
        if (info != null && info.GetFiles().Length == 0 && info.GetDirectories().Length == 0)
        {
          Directory.Delete(folderName);
        }
      }
      catch { }
    }

    /// <summary>
    /// Stops recording.
    /// </summary>
    /// <param name="user">User</param>
    /// <returns></returns>
    public bool Stop(ref IUser user)
    {
      bool stop = false;
      try
      {
        if (_cardHandler.Card.IsEnabled)
        {
          this.LogDebug("card: StopRecording card={0}, user={1}", _cardHandler.Card.TunerId, user.Name);
          if (user.UserType == UserType.Scheduler)
          {
            stop = StopRecording(ref user);
            /*if (stop)
            {
              SetContextOwnerToNextRecUser(context);
            }*/
          }
        }
      }
      catch (Exception ex)
      {
        this.LogError(ex);
      }
      return stop;
    }

    private bool StopRecording(ref IUser user)
    {
      bool stop = false;
      var recentSubChannelId = _cardHandler.UserManagement.GetRecentSubChannelId(user.Name);
      user = _cardHandler.UserManagement.GetUserCopy(recentSubChannelId);
      ISubChannel subchannel = GetSubChannel(recentSubChannelId);
      if (subchannel != null)
      {
        subchannel.StopRecording();
        _cardHandler.Card.FreeSubChannel(recentSubChannelId);
        if (subchannel.IsTimeShifting == false || _cardHandler.UserManagement.UsersCount() <= 1)
        {
          _cardHandler.UserManagement.RemoveUser(user, _cardHandler.UserManagement.GetTimeshiftingChannelId(user.Name));
        }
        if (_cardHandler.IsIdle)
        {
          StopTimeShiftingEpgGrabber();
        }
        stop = true;
      }
      else
      {
        this.LogDebug("card: StopRecording subchannel null, skipping");
      }
      return stop;
    }

    private void SetContextOwnerToNextRecUser(ITvCardContext context)
    {
      //todo gibman - is this even needed, as its taken care of in usermanagement ?
      /*
      IDictionary<string, IUser> users = context.Users;
      foreach (IUser user in users.Values)
      {
        ITvSubChannel subchannel = GetSubChannel(_cardHandler.UserManagement.GetSubChannelIdByChannelId(user.Name, idChannel));
        if (subchannel != null)
        {
          if (subchannel.IsRecording)
          {
            this.LogDebug("card: StopRecording setting new context owner on user '{0}'", user.Name);
            context.Owner = user;
            break;
          }
        }
      }*/
    }

    /// <summary>
    /// Gets a value indicating whether this card is recording.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this card is recording; otherwise, <c>false</c>.
    /// </value>
    public bool IsAnySubChannelRecording
    {
      get
      {
        IDictionary<string, IUser> users = _cardHandler.UserManagement.UsersCopy;
        if (users.Values.Select(user => (IUser) user.Clone()).Any(userCopy => IsRecording(userCopy.Name)))
        {
          return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Returns if the card is recording or not
    /// </summary>
    /// <param name="userName"> </param>
    /// <returns>true when card is recording otherwise false</returns>
    public bool IsRecording(string userName)
    {
      bool isRecording = false;
      try
      {
        var subchannel = GetSubChannel(userName, _cardHandler.UserManagement.GetRecentChannelId(userName));
        if (subchannel != null)
        {
          isRecording = subchannel.IsRecording;
        }
      }
      catch (Exception ex)
      {
        this.LogError(ex);
      }
      return isRecording;
    }

    /// <summary>
    /// Returns the current filename used for recording
    /// </summary>
    /// <param name="userName"> </param>
    /// <returns>filename or null when not recording</returns>
    public string FileName(string userName)
    {
      string recordingFileName = "";
      try
      {
        ISubChannel subchannel = GetSubChannel(userName, _cardHandler.UserManagement.GetRecentChannelId(userName));
        if (subchannel != null)
        {
          recordingFileName = subchannel.RecordingFileName;
        }
      }
      catch (Exception ex)
      {
        this.LogError(ex);
      }
      return recordingFileName;
    }

    /// <summary>
    /// returns the date/time when recording has been started for the card specified
    /// </summary>
    /// <param name="userName"> </param>
    /// <returns>DateTime containg the date/time when recording was started</returns>
    public DateTime RecordingStarted(string userName)
    {
      DateTime recordingStarted = DateTime.MinValue;
      try
      {
        ISubChannel subchannel = GetSubChannel(userName, _cardHandler.UserManagement.GetRecentChannelId(userName));
        if (subchannel != null)
        {
          recordingStarted = subchannel.RecordingStarted;
        }
      }
      catch (Exception ex)
      {
        this.LogError(ex);
      }
      return recordingStarted;
    }
  }
}
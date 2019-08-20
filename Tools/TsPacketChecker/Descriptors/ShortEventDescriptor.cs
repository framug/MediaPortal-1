﻿using System;

namespace WindowsApplication13
{
  internal class ShortEventDescriptor : Descriptor
  {
    #region Private Fields
    private int _lastIndex = -1;


    #endregion
    #region Constructor
    public ShortEventDescriptor()
    {
    }
    #endregion
    #region Properties
    #endregion
    #region Overrides
    public override int Index
    {
      get
      {
        if (_lastIndex == -1)
          throw (new InvalidOperationException("Short Event Descriptor: Index requested before block processed"));
        return (_lastIndex);
      }
    }

    internal override void Process(byte[] buffer, int index)
    {
      _lastIndex = index;
      try
      {

        _lastIndex = index + DescriptorLength;
      }
      catch (IndexOutOfRangeException)
      {
        throw (new ArgumentOutOfRangeException("The Short Event Descriptor message is short"));
      }
    }
    #endregion
  }
}
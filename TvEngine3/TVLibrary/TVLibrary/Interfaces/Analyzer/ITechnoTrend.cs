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
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using DirectShowLib;

namespace TvLibrary.Interfaces.Analyzer
{
  [ComVisible(true), ComImport,
 Guid("B0AB5587-DCEC-49f4-B1AA-06EF58DBF1D3"),
   InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface ITechnoTrend
  {
    [PreserveSig]
    int SetTunerFilter(IBaseFilter tunerFilter);
    [PreserveSig]
    int IsTechnoTrend(ref bool yesNo);
    [PreserveSig]
    int IsCamReady(ref bool yesNo);
    [PreserveSig]
    int SetAntennaPower(bool onOff);
    [PreserveSig]
    int SetDisEqc(short diseqcType, short hiband, short vertical);
    [PreserveSig]
    int DescrambleService(short serviceId, ref bool succeeded);
  };
}

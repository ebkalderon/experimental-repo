/**********************************************************************************
 * VirtualDesktop.cs - A desktop creation/deletion/switching class for Windows.   *
 * Copyright (c) 2013 Eyal Kalderon                                               *
 *                                                                                *
 * The MIT License (MIT)                                                          *
 *                                                                                *
 * Permission is hereby granted, free of charge, to any person obtaining a copy   *
 * of this software and associated documentation files (the "Software"), to deal  *
 * in the Software without restriction, including without limitation the rights   *
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell      *
 * copies of the Software, and to permit persons to whom the Software is          *
 * furnished to do so, subject to the following conditions:                       *
 *                                                                                *
 * The above copyright notice and this permission notice shall be included in     *
 * all copies or substantial portions of the Software.                            *
 *                                                                                *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR     *
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,       *
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE    *
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER         *
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  *
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN      *
 * THE SOFTWARE.                                                                  *
 **********************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
 
public class VirtualDesktop : IDisposable
{
    // These security descriptors below are required to let us manipulate the desktop objects.
    internal enum DESKTOP_ACCESS_MASK : uint {
        DESKTOP_NONE = 0,
        DESKTOP_READOBJECTS = 0x0001,
        DESKTOP_CREATEWINDOW = 0x0002,
        DESKTOP_CREATEMENU = 0x0004,
        DESKTOP_HOOKCONTROL = 0x0008,
        DESKTOP_JOURNALRECORD = 0x0010,
        DESKTOP_JOURNALPLAYBACK = 0x0020,
        DESKTOP_ENUMERATE = 0x0040,
        DESKTOP_WRITEOBJECTS = 0x0080,
        DESKTOP_SWITCHDESKTOP = 0x0100,
 
        EVERYTHING = (DESKTOP_READOBJECTS | DESKTOP_CREATEWINDOW | DESKTOP_CREATEMENU |
                      DESKTOP_HOOKCONTROL | DESKTOP_JOURNALRECORD | DESKTOP_JOURNALPLAYBACK |
                      DESKTOP_ENUMERATE | DESKTOP_WRITEOBJECTS | DESKTOP_SWITCHDESKTOP),
    }
 
    #region Variables
    public IntPtr DesktopPtr;     // This will point to the current desktop we are using.
    private string _sMyDesk;      // This will hold the name for the desktop object we created.
    IntPtr _hOrigDesktop;         // This will remember the very first desktop we spawned on.
    #endregion
    
    #region DLL Definitions
    [DllImport("user32.dll", EntryPoint = "CloseDesktop", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseDesktop(IntPtr handle);
     
    [DllImport("user32.dll")]
    private static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice, IntPtr pDevmode,
                                               int dwFlags, long dwDesiredAccess, IntPtr lpsa);
     
    [DllImport("kernel32.dll")]
    public static extern int GetCurrentThreadId();
     
    [DllImport("user32.dll")]
    public static extern IntPtr GetThreadDesktop(int dwThreadId);
     
    [DllImport("user32.dll")]
    public static extern bool SetThreadDesktop(IntPtr hDesktop);
     
    [DllImport("user32.dll")]
    private static extern bool SwitchDesktop(IntPtr hDesktop);
    #endregion
    
    #region Disposal Methods
    // 1) Switch to the desktop we were on before.
    public void Dispose() {
        SwitchToOrginal();
        ((IDisposable)this).Dispose();
    }
     
    // 2) Delete our custom one.
    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            CloseDesktop(DesktopPtr);
        }
    }
     
    // 3) ... flush!
    void IDisposable.Dispose() {
        Dispose(true);
        
        // This takes the already destroyed desktop off the finalization queue so the GC
        // doesn’t call the finalization code twice.
        GC.SuppressFinalize(this);
    }
    #endregion
    
    #region Methods
    public IntPtr GetCurrentDesktopPtr()
    {
        return GetThreadDesktop(GetCurrentThreadId());
    }
     
    private IntPtr OpenDesktop()
    {
        return CreateDesktop(_sMyDesk, IntPtr.Zero, IntPtr.Zero,
                             0, (long)DESKTOP_ACCESS_MASK.EVERYTHING, IntPtr.Zero);
    }
     
    public void ShowDesktop() {
        SetThreadDesktop(DesktopPtr);
        SwitchDesktop(DesktopPtr);
    }
     
    public void SwitchToOrginal() {
        SwitchDesktop(_hOrigDesktop);
        SetThreadDesktop(_hOrigDesktop);
    }
    #endregion

    #region Constructors
	  public VirtualDesktop()
	  {
	    	_sMyDesk = "";
	  }

	  public VirtualDesktop(string sDesktopName)
	  {
		   _hOrigDesktop = GetCurrentDesktopPtr();
		   _sMyDesk = sDesktopName;
		   DesktopPtr = LaunchDesktop();
	  }
    #endregion
}

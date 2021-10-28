﻿// <copyright file="GameProcess.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

namespace GameHelper
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using Coroutine;
    using GameHelper.CoroutineEvents;
    using GameHelper.Utils;
    using GameOffsets;

    /// <summary>
    /// Allows process manipulation. It uses the (time/event based) co-routines
    /// to continuously monitor and open a process with the specific name. It exposes
    /// variables/events for the caller to use.
    ///
    /// Base class OnControllerReady is only triggered when all static addresses are found.
    ///
    /// Limitation: This class will not open a game process if multiple processes match
    /// the name because it does not know which process to select.
    /// </summary>
    public class GameProcess
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameProcess"/> class.
        /// </summary>
        internal GameProcess()
        {
            CoroutineHandler.Start(this.FindAndOpen());
            CoroutineHandler.Start(this.FindStaticAddresses());
        }

        /// <summary>
        /// Gets the Pid of the game or zero in case game isn't running..
        /// </summary>
        public uint Pid
        {
            get
            {
                try
                {
                    return (uint)this.Information.Id;
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the game is foreground or not.
        /// </summary>
        public bool Foreground { get; private set; } = false;

        /// <summary>
        /// Gets the game size and position with respect to the monitor screen.
        /// </summary>
        public Rectangle WindowArea { get; private set; } = Rectangle.Empty;

        /// <summary>
        /// Gets the Base Address of the game.
        /// </summary>
        internal IntPtr Address
        {
            get
            {
                try
                {
                    var reader = this.Handle;
                    if (reader != null && !reader.IsClosed && !reader.IsInvalid)
                    {
                        return this.Information.MainModule.BaseAddress;
                    }

                    return IntPtr.Zero;
                }
                catch (Exception)
                {
                    return IntPtr.Zero;
                }
            }

            private set
            {
            }
        }

        /// <summary>
        /// Gets the event which is triggered when GameProcess
        /// has found all the static offset patterns.
        /// </summary>
        internal Event OnStaticAddressFound { get; private set; } = new Event();

        /// <summary>
        /// Gets the static addresses (along with their names) found in the GameProcess
        /// based on the GameOffsets.StaticOffsets file.
        /// </summary>
        internal Dictionary<string, IntPtr> StaticAddresses { get; private set; } =
            new Dictionary<string, IntPtr>();

        /// <summary>
        /// Gets the game diagnostics information.
        /// </summary>
        internal Process Information { get; private set; } = null;

        /// <summary>
        /// Gets the game handle.
        /// </summary>
        internal SafeMemoryHandle Handle { get; private set; } = null;

        /// <summary>
        /// Closes the handle for the game and releases all the resources.
        /// </summary>
        /// <param name="monitorForNewGame">
        /// Set to true if caller wants to start monitoring for new game process after closing.
        /// </param>
        internal void Close(bool monitorForNewGame = true)
        {
            CoroutineHandler.RaiseEvent(GameHelperEvents.OnClose);
            this.WindowArea = Rectangle.Empty;
            this.Foreground = false;
            this.Handle?.Dispose();
            this.Information?.Close();
            if (monitorForNewGame)
            {
                CoroutineHandler.Start(this.FindAndOpen());
            }
        }
        /// <summary>
        /// Bind the list of processes from the list of processes running on the system
        /// </summary>
        public int curr_poe_process_count { get; private set; }
        List<Process> _pa = new List<Process>(); //list of simultaneously running OE processes;
      
        /// based on the GameOffsets.GameProcessName class.
        /// <returns>
        /// co-routine IWait.
        /// </returns>
        private IEnumerator<Wait> FindAndOpen() {
            while (true) {
                yield return new Wait(1d);
                _pa.Clear();
                foreach (var process in Process.GetProcesses()) {
                    if (GameProcessDetails.ProcessName.TryGetValue(process.ProcessName, out var windowTitle)) {
                        if (process.MainWindowTitle.ToLower() == windowTitle) {
                            _pa.Add(process);
                        }
                    }
                }
                curr_poe_process_count = _pa.Count;
                if (_pa.Count == 0) {
                    //do you check it somewhere and react to it?
                } else if (_pa.Count == 1) {
                    this.Information = _pa[0];
                    if (this.Open()) {
                        break;
                    }
                } else { //_pa.Count == 2
                    var cpi = Core.GHSettings.curr_poe_index;
                    Debug.Assert(cpi == 0 || cpi == 1);
                    this.Information = _pa[Core.GHSettings.curr_poe_index];
                    if (this.Open()) {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Monitors the game process for changes.
        /// </summary>
        /// <returns>co-routine IWait.</returns>
        private IEnumerator<Wait> Monitor()
        {
            while (true)
            {
                // Have to check MainWindowHandle because
                // sometime HasExited returns false even when game isn't running..
                if (this.Information.HasExited ||
                    this.Information.MainWindowHandle.ToInt64() <= 0x00)
                {
                    this.Close();
                    break;
                }
                else
                {
                    this.UpdateIsForeground();
                    this.UpdateWindowRectangle();
                }

                yield return new Wait(1d);
            }
        }

        /// <summary>
        /// Finds the static addresses in the GameProcess based on the
        /// GameOffsets.StaticOffsetsPatterns file.
        /// </summary>
        /// <returns>co-routine IWait.</returns>
        private IEnumerator<Wait> FindStaticAddresses()
        {
            while (true)
            {
                yield return new Wait(GameHelperEvents.OnOpened);
                var baseAddress = this.Information.MainModule.BaseAddress;
                var procSize = this.Information.MainModule.ModuleMemorySize;
                var patternsInfo = PatternFinder.Find(this.Handle, baseAddress, procSize);
                foreach (var patternInfo in patternsInfo)
                {
                    int offsetDataValue = this.Handle.ReadMemory<int>(baseAddress + patternInfo.Value);
                    IntPtr address = baseAddress + patternInfo.Value + offsetDataValue + 0x04;
                    this.StaticAddresses[patternInfo.Key] = address;
                }

                CoroutineHandler.RaiseEvent(this.OnStaticAddressFound);
            }
        }

        /// <summary>
        /// Opens the handle for the game process.
        /// </summary>
        private bool Open()
        {
            this.Handle = new SafeMemoryHandle(this.Information.Id);
            if (this.Handle.IsInvalid)
            {
                return false;
            }

            Core.CoroutinesRegistrar.Add(CoroutineHandler.Start(
                this.Monitor(), "[GameProcess] Monitoring Game Process"));
            CoroutineHandler.RaiseEvent(GameHelperEvents.OnOpened);
            return true;
        }

        /// <summary>
        /// Updates the Foreground Property of the GameProcess class.
        /// </summary>
        private void UpdateIsForeground()
        {
            bool foreground = GetForegroundWindow() == this.Information.MainWindowHandle;
            if (foreground != this.Foreground)
            {
                this.Foreground = foreground;
                CoroutineHandler.RaiseEvent(GameHelperEvents.OnForegroundChanged);
            }
        }

        /// <summary>
        /// Gets the game process window area with reference to the monitor screen.
        /// </summary>
        private void UpdateWindowRectangle()
        {
            GetClientRect(this.Information.MainWindowHandle, out var size);
            ClientToScreen(this.Information.MainWindowHandle, out var pos);
            Rectangle sizePos = size.ToRectangle(pos);
            if (sizePos != this.WindowArea && sizePos.Size != Size.Empty)
            {
                this.WindowArea = sizePos;
                CoroutineHandler.RaiseEvent(GameHelperEvents.OnMoved);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, out Point lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            private readonly int left;
            private readonly int top;
            private readonly int right;
            private readonly int bottom;

            internal Rectangle ToRectangle(Point point)
            {
                return new Rectangle(point.X, point.Y, this.right - this.left, this.bottom - this.top);
            }
        }
    }
}

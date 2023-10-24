using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Copy_Paster
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize your application class
            CopyPaster copyPaster = new CopyPaster();

            // Hook into the Clipboard's ClipboardChanged event
            ClipboardMonitor.Start(copyPaster.OnClipboardChanged);
            // Rest of your code remains unchanged
            Application.Run(copyPaster);
        }

        public class CopyPaster : ApplicationContext
        {
            private NotifyIcon trayIcon;
            private const int DISPLAY_TEXT_ID = 3;
            private const int HISTORY_SIZE = 5;

            private List<string> clipboardHistory = new List<string>();

            public CopyPaster()
            {
                // Initialize Tray Icon
                trayIcon = new NotifyIcon()
                {
                    Icon = new System.Drawing.Icon("AppIcon.ico"),
                    ContextMenu = new ContextMenu(),
                    Visible = true
                };

                // Add menu items for clipboard history
                for (int i = 0; i < HISTORY_SIZE; i++)
                {
                    trayIcon.ContextMenu.MenuItems.Add($"History Slot {i + 1}", ShowHistoryItem);
                }

                // Add "Exit" menu item
                trayIcon.ContextMenu.MenuItems.Add(new MenuItem("Exit", Exit));

                // Register a global hotkey for copying
                HotkeyManager.RegisterHotKey(IntPtr.Zero, DISPLAY_TEXT_ID, 0x0003 /*Ctrl+Alt*/, (uint)Keys.C);

            }

            void ShowHistoryItem(object sender, EventArgs e)
            {
                // Retrieve the index of the clicked history item
                int index = trayIcon.ContextMenu.MenuItems.IndexOf((MenuItem)sender);

                // Display the corresponding history entry
                if (index < clipboardHistory.Count)
                {
                    MessageBox.Show($"History Slot {index + 1}: {clipboardHistory[index]}", "Clipboard History");
                }
            }

            void Exit(object sender, EventArgs e)
            {
                // Unregister the hotkey, hide tray icon, and stop monitoring clipboard
                HotkeyManager.UnregisterHotKey(IntPtr.Zero, DISPLAY_TEXT_ID);
                trayIcon.Visible = false;
                ClipboardMonitor.Stop();

                // Exit the application
                Application.Exit();
            }

            public void OnClipboardChanged(object sender, EventArgs e)
            {
                // Handle clipboard changes here
                string clipboardData = Clipboard.GetText();
                Console.WriteLine("Clipboard Content Changed: " + clipboardData);

                // Add the new entry to the clipboard history
                clipboardHistory.Add(clipboardData);

                // Remove the oldest entry if the history size exceeds the limit
                if (clipboardHistory.Count > HISTORY_SIZE)
                {
                    clipboardHistory.RemoveAt(0);
                }

                // Update the context menu with the latest clipboard history
                UpdateContextMenu();
            }

            private void UpdateContextMenu()
            {
                // Update the context menu items with clipboard history
                for (int i = 0; i < HISTORY_SIZE; i++)
                {
                    if (i < clipboardHistory.Count)
                    {
                        // Update the text of the menu item
                        trayIcon.ContextMenu.MenuItems[i].Text = $"History Slot {i + 1}: {clipboardHistory[i]}";
                    }
                    else
                    {
                        // If no history entry, clear the text
                        trayIcon.ContextMenu.MenuItems[i].Text = $"History Slot {i + 1}";
                    }
                }
            }
        }

        public class HotkeyManager
        {
            [DllImport("user32.dll")]
            public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

            [DllImport("user32.dll")]
            public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        }

        // Helper class to monitor clipboard changes
        public class ClipboardMonitor
        {
            public delegate void ClipboardChangedEventHandler(object sender, EventArgs e);
            public static event ClipboardChangedEventHandler ClipboardChanged;

            private static IntPtr nextClipboardViewer;

            // Windows messages constants
            private const int WM_DRAWCLIPBOARD = 0x0308;
            private const int WM_CHANGECBCHAIN = 0x030D;

            [DllImport("User32.dll")]
            protected static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

            public static void Start(ClipboardChangedEventHandler clipboardChangedHandler)
            {
                // Register the event handler
                ClipboardChanged += clipboardChangedHandler;

                // Start monitoring the clipboard
                nextClipboardViewer = SetClipboardViewer(IntPtr.Zero);
            }

            public static void Stop()
            {
                // Unregister the event handler
                ClipboardChanged = null;

                // Stop monitoring the clipboard
                ChangeClipboardChain(nextClipboardViewer, IntPtr.Zero);
            }

            public static void OnClipboardChanged()
            {
                // Invoke the event when the clipboard changes
                ClipboardChanged?.Invoke(null, EventArgs.Empty);
            }

            // Handle the WM_DRAWCLIPBOARD message to detect clipboard changes
            public static IntPtr HandleClipboardMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == WM_DRAWCLIPBOARD)
                {
                    // Clipboard content has changed
                    OnClipboardChanged();
                    handled = true;
                }
                else if (msg == WM_CHANGECBCHAIN)
                {
                    // The clipboard chain is changing
                    if (wParam == nextClipboardViewer)
                    {
                        // If the next viewer in the chain is closing, repair the chain
                        nextClipboardViewer = lParam;
                    }
                    else if (nextClipboardViewer != IntPtr.Zero)
                    {
                        // Forward the message to the next viewer in the chain
                        SendMessage(nextClipboardViewer, msg, wParam, lParam);
                    }
                    handled = true;
                }

                return IntPtr.Zero;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WK.Libraries.SharpClipboardNS;

namespace Copy_Paster
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize your application class
            CopyPaster copyPaster = new CopyPaster();

            // Rest of your code remains unchanged
            Application.Run(copyPaster);
        }

        public class CopyPaster : ApplicationContext
        {
            private NotifyIcon trayIcon;
            private const int DISPLAY_TEXT_ID = 3;
            private const int HISTORY_SIZE = 5;

            private List<string> clipboardHistory = new List<string>();
            private SharpClipboard clipboard;
            private string lastClipboardContent = string.Empty;

            // Set the maximum number of characters to display in each slot
            int maxCharacters = 25;  // Adjust this value as needed

            public CopyPaster()
            {
                // Initialize SharpClipboard
                clipboard = new SharpClipboard();
                clipboard.ObserveLastEntry = false;
                clipboard.ClipboardChanged += ClipboardChanged;

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
                    trayIcon.ContextMenu.MenuItems.Add($"Slot {i + 1}", ShowHistoryItem);
                }

                // Add "Exit" menu item
                trayIcon.ContextMenu.MenuItems.Add(new MenuItem("Exit", Exit));

                // Register a global hotkey for copying
                HotkeyManager.RegisterHotKey(IntPtr.Zero, DISPLAY_TEXT_ID, 0x0003 /*Ctrl+Alt*/, (uint)Keys.C);
            }


            private void ClipboardChanged(object sender, SharpClipboard.ClipboardChangedEventArgs e)
            {
                // Handle clipboard changes here
                if (e.ContentType == SharpClipboard.ContentTypes.Text)
                {
                    // Get the cut/copied text.
                    string clipboardData = clipboard.ClipboardText;

                    // Check if the clipboard content has changed
                    if (clipboardData != lastClipboardContent)
                    {
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

                        // Update the last clipboard content
                        lastClipboardContent = clipboardData;
                    }
                }
            }


            private void ShowHistoryItem(object sender, EventArgs e)
            {
                // Retrieve the index of the clicked history item
                int index = trayIcon.ContextMenu.MenuItems.IndexOf((MenuItem)sender);

                // Display the corresponding history entry
                if (index < clipboardHistory.Count)
                {
                    MessageBox.Show($"Slot {index + 1}: {clipboardHistory[index]}", "Clipboard History");
                    clipboard.StopMonitoring();
                    Clipboard.SetText(clipboardHistory[index]);
                    clipboard = new SharpClipboard();
                    clipboard.ObserveLastEntry = false;
                    clipboard.StartMonitoring();
                }
            }

            private void Exit(object sender, EventArgs e)
            {
                // Unregister the hotkey, hide tray icon, and stop monitoring clipboard
                HotkeyManager.UnregisterHotKey(IntPtr.Zero, DISPLAY_TEXT_ID);
                trayIcon.Visible = false;

                // Dispose of the SharpClipboard instance
                clipboard.Dispose();

                // Exit the application
                Application.Exit();
            }

            private void UpdateContextMenu()
            {


                // Update the context menu items with clipboard history
                for (int i = 0; i < HISTORY_SIZE; i++)
                {
                    if (i < clipboardHistory.Count)
                    {
                        // Get the text for the current history entry
                        string entryText = clipboardHistory[i];

                        // Truncate the text if it exceeds the maximum number of characters
                        string truncatedText = entryText.Length > maxCharacters
                            ? "..." + entryText.Substring(entryText.Length - maxCharacters)
                            : entryText;

                        // Update the text of the menu item
                        trayIcon.ContextMenu.MenuItems[i].Text = $"Slot {i + 1}: {truncatedText}";
                    }
                    else
                    {
                        // If no history entry, clear the text
                        trayIcon.ContextMenu.MenuItems[i].Text = $"Slot {i + 1}";
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
    }
}
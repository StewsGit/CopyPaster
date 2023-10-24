using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class HotkeyManager
{
    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const int WM_HOTKEY = 0x0312;
}

public class ClipboardListener
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
}

public class YourForm : Form
{
    const int COPY_HOTKEY_ID = 1;
    const int PASTE_HOTKEY_ID = 2;

    private IntPtr hookId = IntPtr.Zero;

    public YourForm()
    {
        hookId = SetHook(KeyboardHookCallback);
        ClipboardListener.AddClipboardFormatListener(this.Handle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnhookWindowsHookEx(hookId);
            ClipboardListener.RemoveClipboardFormatListener(this.Handle);
        }
        base.Dispose(disposing);
    }

    private IntPtr SetHook(HookProc proc)
    {
        using (ProcessModule curModule = Process.GetCurrentProcess().MainModule)
        {
            return SetWindowsHookEx(13, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)0x0100) // WM_KEYDOWN
        {
            int vkCode = Marshal.ReadInt32(lParam);

            if (ModifierKeys == Keys.Control)
            {
                if (vkCode == (int)Keys.C)
                {
                    // Handle Ctrl+C (copy) event
                    string clipboardData = Clipboard.GetText();
                    // Save the data to your storage (e.g., a list of slots)
                }
                else if (vkCode == (int)Keys.V)
                {
                    // Handle Ctrl+V (paste) event
                    // Retrieve data from your storage and set it to the clipboard
                    Clipboard.SetText("Pasted data here");
                    // Simulate a Ctrl+V press
                    SendKeys.Send("^(v)");
                }
            }
        }

        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == HotkeyManager.WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();

            if (id == COPY_HOTKEY_ID)
            {
                // Capture and save clipboard data
                string clipboardData = Clipboard.GetText();
                // Save the data to your storage (e.g., a list of slots)
            }
            else if (id == PASTE_HOTKEY_ID)
            {
                // Retrieve and paste clipboard data
                // Retrieve data from your storage and set it to the clipboard
                Clipboard.SetText("Pasted data here");
                // Simulate a Ctrl+V press
                SendKeys.Send("^(v)");
            }
        }

        base.WndProc(ref m);
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        // Register hotkeys when the form loads
        HotkeyManager.RegisterHotKey(this.Handle, COPY_HOTKEY_ID, 0x0002 /*Ctrl*/, (uint)Keys.C);
        HotkeyManager.RegisterHotKey(this.Handle, PASTE_HOTKEY_ID, 0x0002 /*Ctrl*/, (uint)Keys.V);
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        // Unregister hotkeys when the form is closing
        HotkeyManager.UnregisterHotKey(this.Handle, COPY_HOTKEY_ID);
        HotkeyManager.UnregisterHotKey(this.Handle, PASTE_HOTKEY_ID);
    }

    #region WinAPI Declarations

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    #endregion
}

using System.Runtime.InteropServices;

namespace Showcase.Handlers;

internal sealed unsafe partial class Window
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint WindowProcedure(nint hwnd, uint message, nuint wparam, nint lparam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Size;
        public uint Style;
        public nint Procedure;
        public int ClassExtra;
        public int WindowExtra;
        public nint Instance;
        public nint Icon;
        public nint Cursor;
        public nint Background;
        public string? Menu;
        public string Name;
        public nint SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public nint Hwnd;
        public uint Id;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public int X;
        public int Y;
        public uint Private;
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandleW(string? name);

    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WindowClass wc);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClassW(string name, nint instance);

    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(uint ex, string name, string title, uint style, int x, int y, int w, int h, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32")]
    private static extern nint DefWindowProcW(nint hwnd, uint message, nuint wparam, nint lparam);

    [DllImport("user32")]
    private static extern bool DestroyWindow(nint hwnd);

    [DllImport("user32")]
    private static extern bool PeekMessageW(out Message message, nint hwnd, uint min, uint max, uint remove);

    [DllImport("user32")]
    private static extern bool TranslateMessage(in Message message);

    [DllImport("user32")]
    private static extern nint DispatchMessageW(in Message message);

    [DllImport("user32")]
    private static extern bool WaitMessage();

    [DllImport("user32")]
    private static extern nint LoadCursorW(nint instance, nint name);

    [DllImport("user32")]
    private static extern bool AdjustWindowRectEx(ref Rect rect, uint style, bool menu, uint ex);

    [DllImport("user32")]
    private static extern bool SetProcessDpiAwarenessContext(nint context);

    [DllImport("user32")]
    private static extern uint GetDpiForWindow(nint hwnd);

    [DllImport("user32")]
    private static extern nint SetCapture(nint hwnd);

    [DllImport("user32")]
    private static extern bool ReleaseCapture();
}

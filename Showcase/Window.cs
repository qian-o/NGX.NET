using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using ImGuiNET;

namespace Showcase;

internal sealed unsafe class Window : IDisposable
{
    public nint Handle
    {
        get; private set;
    }
    public nint Instance { get; } = GetModuleHandleW(null);
    public int Width { get; private set; } = 1600;
    public int Height { get; private set; } = 900;
    public bool Closed
    {
        get; private set;
    }
    public bool Looking
    {
        get; private set;
    }
    public Vector2 MouseDelta
    {
        get; private set;
    }
    public float DpiScale => GetDpiForWindow(Handle) / 96f;
    public Action? BeforeWindowChange;
    public uint LatencyPingMessage;
    public bool LatencyPing;
    private readonly bool[] keys = new bool[256];
    private readonly WindowProcedure procedure;
    private Vector2 mouse;
    private bool hasMouse;
    private ExceptionDispatchInfo? callbackError;

    public Window()
    {
        SetProcessDpiAwarenessContext(-4);
        procedure = ProcessMessage;
        WindowClass wc = new()
        {
            Size = (uint)Marshal.SizeOf<WindowClass>(),
            Procedure = Marshal.GetFunctionPointerForDelegate(procedure),
            Instance = Instance,
            Cursor = LoadCursorW(0, 32512),
            Name = "StreamlineShowcase"
        };
        if (RegisterClassExW(ref wc) == 0)
        {
            throw new Win32Exception();
        }

        Rect rectangle = new()
        {
            Right = Width,
            Bottom = Height
        };
        AdjustWindowRectEx(ref rectangle, 0x00CF0000, false, 0);
        Handle = CreateWindowExW(0, wc.Name, "Streamline.NET Showcase", 0x10CF0000, unchecked((int)0x80000000), unchecked((int)0x80000000), rectangle.Right - rectangle.Left, rectangle.Bottom - rectangle.Top, 0, 0, Instance, 0);
        if (Handle == 0)
        {
            throw new Win32Exception();
        }

        callbackError?.Throw();
    }

    public bool Down(int key) => keys[key];
    public void Pump()
    {
        MouseDelta = Vector2.Zero;
        while (PeekMessageW(out Message message, 0, 0, 0, 1))
        {
            if (message.Id == 0x12)
            {
                Closed = true;
            }

            TranslateMessage(in message);
            DispatchMessageW(in message);
            callbackError?.Throw();
        }
    }
    public void Wait() => WaitMessage();

    private nint ProcessMessage(nint hwnd, uint message, nuint wparam, nint lparam)
    {
        try
        {
            return HandleMessage(hwnd, message, wparam, lparam);
        }
        catch (Exception error)
        {
            callbackError = ExceptionDispatchInfo.Capture(error);
            return 0;
        }
    }

    private nint HandleMessage(nint hwnd, uint message, nuint wparam, nint lparam)
    {
        if (message == LatencyPingMessage && LatencyPingMessage != 0)
        {
            LatencyPing = true;
            return 0;
        }
        ImGuiIOPtr io = ImGui.GetIO();
        switch (message)
        {
            case 0x0010:
                BeforeWindowChange?.Invoke();
                Closed = true;
                return 0;
            case 0x0112:
                if ((wparam & 0xFFF0) is 0xF020 or 0xF030 or 0xF120)
                {
                    BeforeWindowChange?.Invoke();
                }

                break;
            case 0x0231:
                BeforeWindowChange?.Invoke();
                break;
            case 0x0005:
                Width = wparam == 1 ? 0 : (int)((long)lparam & 0xFFFF);
                Height = wparam == 1 ? 0 : (int)(((long)lparam >> 16) & 0xFFFF);
                return 0;
            case 0x0200:
                Vector2 current = new((short)((long)lparam & 0xFFFF), (short)(((long)lparam >> 16) & 0xFFFF));
                if (hasMouse && Looking)
                {
                    MouseDelta += current - mouse;
                }

                mouse = current;
                hasMouse = true;
                io.AddMousePosEvent(current.X, current.Y);
                return 0;
            case 0x0201:
            case 0x0202:
                io.AddMouseButtonEvent(0, message == 0x0201);
                return 0;
            case 0x0204:
                Looking = !io.WantCaptureMouse;
                if (Looking)
                {
                    SetCapture(hwnd);
                }

                io.AddMouseButtonEvent(1, true);
                return 0;
            case 0x0205:
                Looking = false;
                ReleaseCapture();
                io.AddMouseButtonEvent(1, false);
                return 0;
            case 0x020A:
                io.AddMouseWheelEvent(0, (short)(wparam >> 16) / 120f);
                return 0;
            case 0x0100:
            case 0x0101:
                if (wparam < 256)
                {
                    keys[(int)wparam] = message == 0x0100;
                }

                ImGuiKey key = Key((int)wparam);
                if (key != ImGuiKey.None)
                {
                    io.AddKeyEvent(key, message == 0x0100);
                }

                return 0;
            case 0x0102:
                io.AddInputCharacter((uint)wparam);
                return 0;
            case 0x0007:
                io.AddFocusEvent(true);
                return 0;
            case 0x0008:
                Array.Clear(keys);
                Looking = false;
                ReleaseCapture();
                io.AddFocusEvent(false);
                return 0;
        }
        return DefWindowProcW(hwnd, message, wparam, lparam);
    }

    private static ImGuiKey Key(int key) => key switch
    {
        >= 'A' and <= 'Z' => ImGuiKey.A + key - 'A',
        >= '0' and <= '9' => ImGuiKey._0 + key - '0',
        112 => ImGuiKey.F1,
        9 => ImGuiKey.Tab,
        13 => ImGuiKey.Enter,
        27 => ImGuiKey.Escape,
        32 => ImGuiKey.Space,
        37 => ImGuiKey.LeftArrow,
        38 => ImGuiKey.UpArrow,
        39 => ImGuiKey.RightArrow,
        40 => ImGuiKey.DownArrow,
        16 => ImGuiKey.ModShift,
        17 => ImGuiKey.ModCtrl,
        18 => ImGuiKey.ModAlt,
        8 => ImGuiKey.Backspace,
        46 => ImGuiKey.Delete,
        _ => ImGuiKey.None
    };

    public void Dispose()
    {
        if (Handle != 0)
        {
            DestroyWindow(Handle);
            Handle = 0;
        }
        UnregisterClassW("StreamlineShowcase", Instance);
        GC.KeepAlive(procedure);
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate nint WindowProcedure(nint hwnd, uint message, nuint wparam, nint lparam);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Size, Style;
        public nint Procedure;
        public int ClassExtra, WindowExtra;
        public nint Instance, Icon, Cursor, Background;
        public string? Menu;
        public string Name;
        public nint SmallIcon;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public nint Hwnd; public uint Id; public nuint WParam; public nint LParam; public uint Time; public int X, Y; public uint Private;
    }
    [DllImport("kernel32", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandleW(string? name);
    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClassExW(ref WindowClass wc);
    [DllImport("user32", CharSet = CharSet.Unicode)] private static extern bool UnregisterClassW(string name, nint instance);
    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern nint CreateWindowExW(uint ex, string name, string title, uint style, int x, int y, int w, int h, nint parent, nint menu, nint instance, nint parameter);
    [DllImport("user32")] private static extern nint DefWindowProcW(nint hwnd, uint message, nuint wparam, nint lparam);
    [DllImport("user32")] private static extern bool DestroyWindow(nint hwnd);
    [DllImport("user32")] private static extern bool PeekMessageW(out Message message, nint hwnd, uint min, uint max, uint remove);
    [DllImport("user32")] private static extern bool TranslateMessage(in Message message);
    [DllImport("user32")] private static extern nint DispatchMessageW(in Message message);
    [DllImport("user32")] private static extern bool WaitMessage();
    [DllImport("user32")] private static extern nint LoadCursorW(nint instance, nint name);
    [DllImport("user32")] private static extern bool AdjustWindowRectEx(ref Rect rect, uint style, bool menu, uint ex);
    [DllImport("user32")] private static extern bool SetProcessDpiAwarenessContext(nint context);
    [DllImport("user32")] private static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32")] private static extern nint SetCapture(nint hwnd);
    [DllImport("user32")] private static extern bool ReleaseCapture();
}

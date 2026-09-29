using System.ComponentModel;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace Showcase.Handlers;

internal sealed unsafe partial class Window : IDisposable
{
    public nint Handle { get; private set; }

    public nint Instance { get; } = GetModuleHandleW(null);

    public int Width { get; private set; } = 1600;

    public int Height { get; private set; } = 900;

    public bool Closed { get; private set; }

    public bool Looking { get; private set; }

    public Vector2 MouseDelta { get; private set; }

    public float DpiScale => GetDpiForWindow(Handle) / 96f;

    // Ordinary Settings navigation focus must not consume the camera's movement keys.
    public bool KeyboardCaptured => ImGui.GetIO().WantTextInput || ImGui.IsPopupOpen(string.Empty, ImGuiPopupFlags.AnyPopup);

    public Action? BeforeWindowChange;
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
            Name = "NGXShowcase"
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
        Handle = CreateWindowExW(0, wc.Name, "NGX.NET Showcase", 0x10CF0000, unchecked((int)0x80000000), unchecked((int)0x80000000), rectangle.Right - rectangle.Left, rectangle.Bottom - rectangle.Top, 0, 0, Instance, 0);

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

        UnregisterClassW("NGXShowcase", Instance);
        GC.KeepAlive(procedure);
    }
}

using System.Numerics;
using System.Runtime.ExceptionServices;
using ImGuiNET;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Key = Silk.NET.Input.Key;
using MouseButton = Silk.NET.Input.MouseButton;
using SilkWindow = Silk.NET.Windowing.Window;

namespace Showcase.Handlers;

internal sealed unsafe class Window : IDisposable
{
    public IWindow SurfaceWindow { get; }

    public nint Handle => disposed ? 0 : SurfaceWindow.Native?.Win32?.Hwnd ?? 0;

    public int Width { get; private set; }

    public int Height { get; private set; }

    public bool Closed { get; private set; }

    public bool Looking => lookMouse is not null;

    public Vector2 MouseDelta { get; private set; }

    public float DpiScale { get; private set; } = 1;

    // Ordinary Settings navigation focus must not consume the camera's movement keys.
    public bool KeyboardCaptured => ImGui.GetIO().WantTextInput || ImGui.IsPopupOpen(string.Empty, ImGuiPopupFlags.AnyPopup);

    public Action? BeforeWindowChange;

    private readonly IInputContext input;
    private readonly Glfw? glfw;
    private readonly HashSet<Key> keys = [];
    private IMouse? lookMouse;
    private CursorMode previousCursorMode;
    private Vector2 mouse;
    private Vector2 pendingMouseDelta;
    private bool hasMouse;
    private bool focused = true;
    private bool disposed;
    private ExceptionDispatchInfo? callbackError;

    public Window()
    {
        SurfaceWindow = SilkWindow.Create(WindowOptions.Default with
        {
            Size = new(1600, 900),
            API = GraphicsAPI.None,
            Title = "NGX.NET Showcase",
            ShouldSwapAutomatically = false,
            VSync = false,
            UpdatesPerSecond = 0,
            FramesPerSecond = 0
        });

        try
        {
            SurfaceWindow.FramebufferResize += _ => Callback(Resize);
            SurfaceWindow.StateChanged += _ => Callback(Resize);
            SurfaceWindow.Closing += () => Callback(Close);
            SurfaceWindow.FocusChanged += value => Callback(() => Focus(value));
            SurfaceWindow.Initialize();

            if (SurfaceWindow.Handle == 0)
            {
                throw new InvalidOperationException("Silk.NET could not create the native window.");
            }

            if (SurfaceWindow.Monitor is IMonitor monitor)
            {
                SurfaceWindow.Center(monitor);
            }

            input = SurfaceWindow.CreateInput();

            if (SurfaceWindow.Native?.Glfw is not null)
            {
                glfw = Glfw.GetApi();
            }

            foreach (IKeyboard keyboard in input.Keyboards)
            {
                keyboard.KeyDown += (_, key, _) => Callback(() => KeyChanged(key, true));
                keyboard.KeyUp += (_, key, _) => Callback(() => KeyChanged(key, false));
                keyboard.KeyChar += (_, value) => Callback(() => ImGui.GetIO().AddInputCharacterUTF16(value));
            }

            foreach (IMouse pointer in input.Mice)
            {
                pointer.MouseDown += (device, button) => Callback(() => MouseButtonChanged(device, button, true));
                pointer.MouseUp += (device, button) => Callback(() => MouseButtonChanged(device, button, false));
                pointer.MouseMove += (device, position) => Callback(() => MouseMoved(device, position));
                pointer.Scroll += (_, wheel) => Callback(() => ImGui.GetIO().AddMouseWheelEvent(wheel.X, wheel.Y));
            }

            UpdateMetrics();
            callbackError?.Throw();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public bool Down(Key key) => keys.Contains(key);

    public void Pump()
    {
        callbackError?.Throw();
        SurfaceWindow.DoEvents();
        SurfaceWindow.DoUpdate();
        UpdateMetrics();
        MouseDelta = pendingMouseDelta;
        pendingMouseDelta = Vector2.Zero;
        Closed |= SurfaceWindow.IsClosing;
        callbackError?.Throw();
    }

    public void Wait()
    {
        callbackError?.Throw();

        if (Closed)
        {
            return;
        }

        // DoEvents waits through the window backend while minimized. The next
        // Pump publishes any accumulated input after the window wakes.
        SurfaceWindow.IsEventDriven = true;

        try
        {
            SurfaceWindow.DoEvents();
        }
        finally
        {
            SurfaceWindow.IsEventDriven = false;
        }

        callbackError?.Throw();
    }

    private void Callback(Action action)
    {
        try
        {
            action();
        }
        catch (Exception error)
        {
            callbackError ??= ExceptionDispatchInfo.Capture(error);
        }
    }

    private void Resize()
    {
        BeforeWindowChange?.Invoke();
        UpdateMetrics();
    }

    private void Close()
    {
        Closed = true;
        BeforeWindowChange?.Invoke();
    }

    private void UpdateMetrics()
    {
        Vector2D<int> size = SurfaceWindow.FramebufferSize;
        bool minimized = SurfaceWindow.WindowState == WindowState.Minimized;
        Width = minimized ? 0 : size.X;
        Height = minimized ? 0 : size.Y;

        if (glfw is not null && SurfaceWindow.Monitor is IMonitor monitor)
        {
            Silk.NET.GLFW.Monitor** monitors = glfw.GetMonitors(out int count);

            if (monitor.Index >= 0 && monitor.Index < count)
            {
                glfw.GetMonitorContentScale(monitors[monitor.Index], out float scale, out _);
                DpiScale = scale;
            }
        }
        else if (SurfaceWindow.Size.X > 0 && size.X > 0)
        {
            DpiScale = (float)size.X / SurfaceWindow.Size.X;
        }
    }

    private void Focus(bool value)
    {
        focused = value;
        hasMouse = false;
        ImGuiIOPtr io = ImGui.GetIO();

        if (!value)
        {
            keys.Clear();
            EndLooking();
            MouseDelta = Vector2.Zero;
            pendingMouseDelta = Vector2.Zero;
            io.ClearInputKeys();
            io.ClearInputMouse();
        }

        io.AddFocusEvent(value);
    }

    private void KeyChanged(Key key, bool pressed)
    {
        if (pressed && focused)
        {
            keys.Add(key);
        }
        else
        {
            keys.Remove(key);
        }

        ImGuiIOPtr io = ImGui.GetIO();
        io.AddKeyEvent(ImGuiKey.ModShift, Down(Key.ShiftLeft) || Down(Key.ShiftRight));
        io.AddKeyEvent(ImGuiKey.ModCtrl, Down(Key.ControlLeft) || Down(Key.ControlRight));
        io.AddKeyEvent(ImGuiKey.ModAlt, Down(Key.AltLeft) || Down(Key.AltRight));
        io.AddKeyEvent(ImGuiKey.ModSuper, Down(Key.SuperLeft) || Down(Key.SuperRight));
        ImGuiKey translated = TranslateKey(key);

        if (translated != ImGuiKey.None)
        {
            io.AddKeyEvent(translated, pressed && focused);
        }
    }

    private void MouseButtonChanged(IMouse device, MouseButton button, bool pressed)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        if (button == MouseButton.Right)
        {
            if (pressed && focused && !io.WantCaptureMouse && !Looking)
            {
                previousCursorMode = device.Cursor.CursorMode;
                lookMouse = device;
                device.Cursor.CursorMode = CursorMode.Disabled;
                hasMouse = false;
            }
            else if (!pressed)
            {
                EndLooking();
            }
        }

        if (button is >= MouseButton.Left and <= MouseButton.Button5)
        {
            io.AddMouseButtonEvent((int)button, pressed && focused);
        }
    }

    private void MouseMoved(IMouse device, Vector2 position)
    {
        Vector2D<int> size = SurfaceWindow.Size;
        Vector2D<int> framebuffer = SurfaceWindow.FramebufferSize;
        Vector2 scale = size.X > 0 && size.Y > 0 ? new((float)framebuffer.X / size.X, (float)framebuffer.Y / size.Y) : Vector2.One;

        if (hasMouse && ReferenceEquals(device, lookMouse))
        {
            pendingMouseDelta += (position - mouse) * scale;
        }

        mouse = position;
        hasMouse = true;
        Vector2 pixels = position * scale;
        ImGui.GetIO().AddMousePosEvent(pixels.X, pixels.Y);
    }

    private void EndLooking()
    {
        IMouse? captured = lookMouse;
        lookMouse = null;
        hasMouse = false;

        if (captured is not null)
        {
            captured.Cursor.CursorMode = previousCursorMode;
        }
    }

    private static ImGuiKey TranslateKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => ImGuiKey.A + (key - Key.A),
        >= Key.Number0 and <= Key.Number9 => ImGuiKey._0 + (key - Key.Number0),
        >= Key.F1 and <= Key.F24 => ImGuiKey.F1 + (key - Key.F1),
        >= Key.Keypad0 and <= Key.Keypad9 => ImGuiKey.Keypad0 + (key - Key.Keypad0),
        Key.Tab => ImGuiKey.Tab,
        Key.Enter => ImGuiKey.Enter,
        Key.Escape => ImGuiKey.Escape,
        Key.Space => ImGuiKey.Space,
        Key.Left => ImGuiKey.LeftArrow,
        Key.Right => ImGuiKey.RightArrow,
        Key.Up => ImGuiKey.UpArrow,
        Key.Down => ImGuiKey.DownArrow,
        Key.PageUp => ImGuiKey.PageUp,
        Key.PageDown => ImGuiKey.PageDown,
        Key.Home => ImGuiKey.Home,
        Key.End => ImGuiKey.End,
        Key.Insert => ImGuiKey.Insert,
        Key.Delete => ImGuiKey.Delete,
        Key.Backspace => ImGuiKey.Backspace,
        Key.Apostrophe => ImGuiKey.Apostrophe,
        Key.Comma => ImGuiKey.Comma,
        Key.Minus => ImGuiKey.Minus,
        Key.Period => ImGuiKey.Period,
        Key.Slash => ImGuiKey.Slash,
        Key.Semicolon => ImGuiKey.Semicolon,
        Key.Equal => ImGuiKey.Equal,
        Key.LeftBracket => ImGuiKey.LeftBracket,
        Key.BackSlash => ImGuiKey.Backslash,
        Key.RightBracket => ImGuiKey.RightBracket,
        Key.GraveAccent => ImGuiKey.GraveAccent,
        Key.CapsLock => ImGuiKey.CapsLock,
        Key.ScrollLock => ImGuiKey.ScrollLock,
        Key.NumLock => ImGuiKey.NumLock,
        Key.PrintScreen => ImGuiKey.PrintScreen,
        Key.Pause => ImGuiKey.Pause,
        Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
        Key.KeypadDivide => ImGuiKey.KeypadDivide,
        Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
        Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
        Key.KeypadAdd => ImGuiKey.KeypadAdd,
        Key.KeypadEnter => ImGuiKey.KeypadEnter,
        Key.KeypadEqual => ImGuiKey.KeypadEqual,
        Key.ShiftLeft => ImGuiKey.LeftShift,
        Key.ShiftRight => ImGuiKey.RightShift,
        Key.ControlLeft => ImGuiKey.LeftCtrl,
        Key.ControlRight => ImGuiKey.RightCtrl,
        Key.AltLeft => ImGuiKey.LeftAlt,
        Key.AltRight => ImGuiKey.RightAlt,
        Key.SuperLeft => ImGuiKey.LeftSuper,
        Key.SuperRight => ImGuiKey.RightSuper,
        Key.Menu => ImGuiKey.Menu,
        _ => ImGuiKey.None
    };

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Closed = true;
        BeforeWindowChange = null;

        try
        {
            EndLooking();
        }
        finally
        {
            try
            {
                input?.Dispose();
            }
            finally
            {
                try
                {
                    SurfaceWindow.Dispose();
                }
                finally
                {
                    glfw?.Dispose();
                }
            }
        }
    }
}

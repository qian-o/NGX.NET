using System.Numerics;
using System.Runtime.ExceptionServices;
using Hexa.NET.ImGui;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Showcase.Handlers;

internal sealed class InputHandler : IDisposable
{
    private readonly IWindow window;
    private readonly IKeyboard[] keyboards;
    private readonly IMouse[] mice;
    private readonly HashSet<(IKeyboard Keyboard, Key Key)> keys = [];
    private readonly HashSet<(IMouse Mouse, MouseButton Button)> buttons = [];
    private IMouse? lookMouse;
    private CursorMode previousCursorMode;
    private Vector2 mouse;
    private Vector2 pendingMouseDelta;
    private bool hasMouse;
    private bool focused = true;
    private bool disposed;
    private ExceptionDispatchInfo? callbackError;

    public bool Looking => lookMouse is not null;

    public Vector2 MouseDelta { get; private set; }

    // Settings navigation must not consume the camera's movement keys.
    public bool KeyboardCaptured => ImGui.GetIO().WantTextInput || ImGui.IsPopupOpen(string.Empty, ImGuiPopupFlags.AnyPopup);

    public InputHandler(IWindow window, IInputContext input)
    {
        this.window = window;
        keyboards = [.. input.Keyboards];
        mice = [.. input.Mice];
        window.FocusChanged += OnFocusChanged;

        foreach (IKeyboard keyboard in keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
            keyboard.KeyChar += OnKeyChar;
        }

        foreach (IMouse pointer in mice)
        {
            pointer.MouseDown += OnMouseDown;
            pointer.MouseUp += OnMouseUp;
            pointer.MouseMove += OnMouseMove;
            pointer.Scroll += OnScroll;
        }
    }

    public bool Down(Key key) => keys.Any(pressed => pressed.Key == key);

    public void Update()
    {
        callbackError?.Throw();
        MouseDelta = pendingMouseDelta;
        pendingMouseDelta = Vector2.Zero;
    }

    private void OnFocusChanged(bool value) => Callback(() => Focus(value));

    private void OnKeyDown(IKeyboard keyboard, Key key, int scanCode) => Callback(() => KeyChanged(keyboard, key, true));

    private void OnKeyUp(IKeyboard keyboard, Key key, int scanCode) => Callback(() => KeyChanged(keyboard, key, false));

    private void OnKeyChar(IKeyboard keyboard, char value) => Callback(() => ImGui.GetIO().AddInputCharacterUTF16(value));

    private void OnMouseDown(IMouse mouse, MouseButton button) => Callback(() => MouseButtonChanged(mouse, button, true));

    private void OnMouseUp(IMouse mouse, MouseButton button) => Callback(() => MouseButtonChanged(mouse, button, false));

    private void OnMouseMove(IMouse mouse, Vector2 position) => Callback(() => MouseMoved(mouse, position));

    private void OnScroll(IMouse mouse, ScrollWheel wheel) => Callback(() => ImGui.GetIO().AddMouseWheelEvent(wheel.X, wheel.Y));

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

    private void Focus(bool value)
    {
        focused = value;
        hasMouse = false;
        ImGuiIOPtr io = ImGui.GetIO();

        if (!value)
        {
            keys.Clear();
            buttons.Clear();
            EndLooking();
            MouseDelta = Vector2.Zero;
            pendingMouseDelta = Vector2.Zero;
            io.ClearInputKeys();
            io.ClearInputMouse();
        }

        io.AddFocusEvent(value);
    }

    private void KeyChanged(IKeyboard keyboard, Key key, bool pressed)
    {
        if (pressed && focused)
        {
            keys.Add((keyboard, key));
        }
        else
        {
            keys.Remove((keyboard, key));
        }

        ImGuiIOPtr io = ImGui.GetIO();
        io.AddKeyEvent(ImGuiKey.ModShift, Down(Key.ShiftLeft) || Down(Key.ShiftRight));
        io.AddKeyEvent(ImGuiKey.ModCtrl, Down(Key.ControlLeft) || Down(Key.ControlRight));
        io.AddKeyEvent(ImGuiKey.ModAlt, Down(Key.AltLeft) || Down(Key.AltRight));
        io.AddKeyEvent(ImGuiKey.ModSuper, Down(Key.SuperLeft) || Down(Key.SuperRight));
        ImGuiKey translated = TranslateKey(key);

        if (translated != ImGuiKey.None)
        {
            io.AddKeyEvent(translated, Down(key));
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
            else if (!pressed && ReferenceEquals(device, lookMouse))
            {
                EndLooking();
            }
        }

        if (button is >= MouseButton.Left and <= MouseButton.Button5)
        {
            if (pressed && focused)
            {
                buttons.Add((device, button));
            }
            else
            {
                buttons.Remove((device, button));
            }

            io.AddMouseButtonEvent((int)button, buttons.Any(held => held.Button == button));
        }
    }

    private void MouseMoved(IMouse device, Vector2 position)
    {
        if (ReferenceEquals(device, lookMouse))
        {
            if (hasMouse)
            {
                pendingMouseDelta += position - mouse;
            }

            mouse = position;
            hasMouse = true;
        }

        ImGui.GetIO().AddMousePosEvent(position.X, position.Y);
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
        >= Key.Number0 and <= Key.Number9 => ImGuiKey.Key0 + (key - Key.Number0),
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
        window.FocusChanged -= OnFocusChanged;

        foreach (IKeyboard keyboard in keyboards)
        {
            keyboard.KeyDown -= OnKeyDown;
            keyboard.KeyUp -= OnKeyUp;
            keyboard.KeyChar -= OnKeyChar;
        }

        foreach (IMouse mouse in mice)
        {
            mouse.MouseDown -= OnMouseDown;
            mouse.MouseUp -= OnMouseUp;
            mouse.MouseMove -= OnMouseMove;
            mouse.Scroll -= OnScroll;
        }

        keys.Clear();
        buttons.Clear();
        MouseDelta = Vector2.Zero;
        pendingMouseDelta = Vector2.Zero;
        EndLooking();
    }
}

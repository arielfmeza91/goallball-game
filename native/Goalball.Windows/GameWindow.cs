using System.Diagnostics;
using Goalball.Core;
namespace Goalball.Windows;

public sealed class GameWindow : Form
{
    private readonly Match match = new();
    private readonly Nvda nvda = new();
    private GameAudio? audio;
    private readonly Court court;
    private readonly Panel menu = new() { Dock = DockStyle.Fill };
    private readonly Label nvdaStatus = new() { AutoSize = true, AccessibleName = "Estado de NVDA" };
    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 40, TextAlign = ContentAlignment.MiddleCenter, AccessibleName = "Último anuncio del partido" };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Stopwatch watch = Stopwatch.StartNew();
    private readonly HashSet<Keys> held = new();
    private double lastTime;
    private bool warned, muteVoice, fullscreen, modal, resourcesDisposed;
    private FormWindowState previousState;
    private FormBorderStyle previousBorder;
    private readonly ListBox choices = new() { AccessibleName = "Menú principal", Width = 420, Height = 230, IntegralHeight = false };
    public GameWindow()
    {
        Text = "Goalball Nativo NVDA 3.0"; ClientSize = new(1150, 760); MinimumSize = new(800, 580);
        StartPosition = FormStartPosition.CenterScreen; BackColor = SystemColors.Control; ForeColor = SystemColors.ControlText;
        Font = SystemFonts.MessageBoxFont; KeyPreview = true;
        court = new Court(match) { Dock = DockStyle.Fill, AccessibleName = "Partido de goalball", AccessibleDescription = "F1 ayuda. Escape menú. H marcador. Espacio lanza o bloquea." };
        Controls.Add(court); Controls.Add(status); Controls.Add(menu);
        var layout = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Padding = new Padding(20) };
        layout.Controls.Add(new Label { Text = "Goalball Nativo NVDA 3.0", AutoSize = true, Font = new Font(Font.FontFamily, 16, FontStyle.Bold), Margin = new Padding(0, 0, 0, 12) });
        layout.Controls.Add(new Label { Text = "Menú principal. Flechas para elegir; Enter para aceptar.", AutoSize = true, Margin = new Padding(0, 0, 0, 12) });
        layout.Controls.Add(choices); layout.Controls.Add(nvdaStatus);
        var choose = new Button { Text = "Aceptar", Width = 120, Height = 32, UseVisualStyleBackColor = true };
        choose.Click += (_, _) => ChooseMenu(); layout.Controls.Add(choose); AcceptButton = choose;
        choices.DoubleClick += (_, _) => ChooseMenu();
        choices.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; ChooseMenu(); } };
        menu.Controls.Add(layout);
        void CenterMenu() => layout.Location = new(Math.Max(0, (menu.Width - layout.Width) / 2), Math.Max(0, (menu.Height - layout.Height) / 2));
        menu.Resize += (_, _) => CenterMenu();
        SetMenuChoices();
        Shown += (_, _) => { CenterMenu(); choices.Focus(); TryAudio(); timer.Start(); nvda.Speak("Goalball nativo, versión tres. Menú principal. Flechas para elegir y Enter para aceptar."); };
        KeyDown += OnKeyDown; KeyUp += OnKeyUp;
        Deactivate += (_, _) => { held.Clear(); if (!menu.Visible && !modal) { match.Pause(); audio?.StopEffects(); ShowMenu(); Drain(); } };
        timer.Tick += (_, _) => TickGame();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !resourcesDisposed) { resourcesDisposed = true; timer.Stop(); timer.Dispose(); audio?.Dispose(); nvda.Dispose(); }
        base.Dispose(disposing);
    }
    internal string VerifyNativeMenu()
    {
        var className = new System.Text.StringBuilder(256);
        if (GetClassName(choices.Handle, className, 256) == 0 || !className.ToString().Contains("LISTBOX", StringComparison.OrdinalIgnoreCase)) throw new Exception("No se creó la lista nativa de Windows.");
        if (choices.AccessibilityObject.Role != AccessibleRole.List || choices.Items.Count != 7) throw new Exception("Menú accesible incorrecto.");
        return "PASS: menú Windows nativo, clase " + className + ", rol accesible List, siete opciones.";
    }
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, System.Text.StringBuilder name, int max);
    private void SetMenuChoices()
    {
        choices.Items.Clear();
        if (match.Phase is Phase.Pause or Phase.Break) choices.Items.Add("Continuar partido");
        choices.Items.AddRange(["Nuevo partido", "Entrenamiento", "Opciones", "Ayuda y controles", "Comprobar NVDA", "Acerca de esta versión", "Salir"]);
        choices.SelectedIndex = 0;
    }
    private void ChooseMenu()
    {
        switch (choices.SelectedItem?.ToString())
        {
            case "Continuar partido": Continue(); break;
            case "Nuevo partido": NewGame(false); break;
            case "Entrenamiento": NewGame(true); break;
            case "Opciones": ShowSettings(); choices.Focus(); break;
            case "Ayuda y controles": ShowHelp(); choices.Focus(); break;
            case "Comprobar NVDA": CheckNvda(); break;
            case "Acerca de esta versión": ShowAbout(); break;
            case "Salir": Close(); break;
        }
    }
    private void ShowAbout() => MessageBox.Show(this, "Goalball Nativo NVDA 3.0\nAplicación Windows nativa: WinForms y GDI+.\nEjecutable: GoalballNativoNVDA.exe\nAnuncios mediante NVDA Controller Client.\nSin HTML, navegador, WebView ni Electron.", "Acerca de Goalball", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private void TryAudio()
    {
        try { audio ??= new GameAudio(); }
        catch (Exception ex) when (ex is NAudio.MmException or IOException or InvalidOperationException)
        { MessageBox.Show(this, "No se pudo abrir el sonido. Conecta una salida de audio y reinicia el juego.\n" + ex.Message, "Sonido no disponible", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void NewGame(bool practice)
    {
        if (audio == null) { TryAudio(); if (audio == null) return; }
        match.Reset(practice); match.Start(); AcceptButton = null; menu.Visible = false; status.Visible = true; court.Focus(); lastTime = watch.Elapsed.TotalSeconds; Drain();
    }
    private void Continue()
    {
        AcceptButton = null; menu.Visible = false; match.Resume(); held.Clear(); court.Focus(); lastTime = watch.Elapsed.TotalSeconds; Drain();
    }
    private void ShowMenu()
    {
        held.Clear(); SetMenuChoices(); menu.Visible = true; menu.BringToFront(); choices.Focus();
    }
    private void CheckNvda() => nvda.Speak("NVDA conectado. Las respuestas del juego usan tu voz y configuración de NVDA.", true);
    private void Tell(string message, bool urgent = false) { status.Text = message; if (!muteVoice) nvda.Speak(message, urgent); }
    private void Drain()
    {
        while (match.Events.TryDequeue(out var e)) { if (e.Sound.Length > 0) audio?.Play(e.Sound, gain: e.Sound == "goal" ? .65 : .8); if (e.Text.Length > 0) Tell(e.Text, e.Critical); }
    }
    private void TickGame()
    {
        double now = watch.Elapsed.TotalSeconds, dt = Math.Min(.1, now - lastTime); lastTime = now;
        if (nvdaStatus.Text != nvda.Status) nvdaStatus.Text = nvda.Status;
        if (menu.Visible) return;
        if (held.Contains(Keys.Left)) match.Move(-1, dt); if (held.Contains(Keys.Right)) match.Move(1, dt);
        if (held.Contains(Keys.A)) match.SetAim(match.Aim - dt * 4); if (held.Contains(Keys.D)) match.SetAim(match.Aim + dt * 4);
        match.Step(dt);
        audio?.BallPosition(match.Phase == Phase.Play ? match.Ball : null);
        if (match.Phase == Phase.Play && match.Owner == 0 && match.Ball == null && match.Hold >= 7 && !warned) { Tell("Quedan tres segundos.", true); warned = true; }
        if (match.Hold < 1) warned = false;
        Drain(); court.Invalidate();
    }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F3) { if (!menu.Visible) { match.Pause(); ShowMenu(); } ShowAbout(); e.Handled = true; return; }
        if (e.KeyCode == Keys.F2) { CheckNvda(); e.Handled = true; return; }
        if (e.KeyCode == Keys.F11) { ToggleFullscreen(); e.Handled = true; return; }
        if (menu.Visible) return;
        e.Handled = true;
        if (!held.Add(e.KeyCode)) return;
        switch (e.KeyCode)
        {
            case Keys.D1: case Keys.NumPad1: match.Select(0); break;
            case Keys.D2: case Keys.NumPad2: match.Select(1); break;
            case Keys.D3: case Keys.NumPad3: match.Select(2); break;
            case Keys.Space: match.Action(); break;
            case Keys.Enter: if (match.Phase == Phase.Break) match.Resume(); else if (match.Phase == Phase.Finished) ShowMenu(); break;
            case Keys.Escape: case Keys.P: match.Pause(); audio?.StopEffects(); ShowMenu(); break;
            case Keys.H: Tell(match.Report(), true); break;
            case Keys.M: court.Blindfold = !court.Blindfold; Tell(court.Blindfold ? "Antifaz activado." : "Antifaz desactivado."); break;
            case Keys.V: muteVoice = !muteVoice; if (!muteVoice) Tell("Anuncios de NVDA activados.", true); break;
            case Keys.Up: match.SetPower(match.Power + .1); Tell($"Potencia {match.Power * 100:0} por ciento."); break;
            case Keys.Down: match.SetPower(match.Power - .1); Tell($"Potencia {match.Power * 100:0} por ciento."); break;
            case Keys.T: match.Timeout(); break;
            case Keys.S: match.Substitute(); break;
            case Keys.F1: match.Pause(); ShowMenu(); ShowHelp(); break;
            case Keys.F5: Referee(); break;
            case Keys.F6: match.Style = (ThrowStyle)(((int)match.Style + 1) % 4); Tell("Tipo de tiro: " + new[] { "legal", "balón alto", "balón largo", "balón corto" }[(int)match.Style]); break;
        }
        Drain();
    }
    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        held.Remove(e.KeyCode); if (menu.Visible) return;
        if (e.KeyCode is Keys.Left or Keys.Right) Tell($"Defensor {match.Selected + 1}, posición {match.Players[0][match.Selected]:0.0} metros.");
        if (e.KeyCode is Keys.A or Keys.D) Tell($"Destino del tiro: {match.Aim:0.0} metros.");
    }
    private void ToggleFullscreen()
    {
        if (!fullscreen) { previousState = WindowState; previousBorder = FormBorderStyle; FormBorderStyle = FormBorderStyle.None; WindowState = FormWindowState.Maximized; }
        else { FormBorderStyle = previousBorder; WindowState = previousState; }
        fullscreen = !fullscreen;
    }
    private void ShowSettings()
    {
        using var dialog = new Form { Text = "Opciones de Goalball", ClientSize = new(510, 260), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var blind = new CheckBox { Text = "Antifaz: ocultar pista", Checked = court.Blindfold, Left = 20, Top = 20, Width = 400 };
        var voice = new CheckBox { Text = "Anuncios de partido con NVDA", Checked = !muteVoice, Left = 20, Top = 65, Width = 440 };
        var volume = new TrackBar { AccessibleName = "Volumen de efectos de sonido", Left = 20, Top = 135, Width = 450, Minimum = 0, Maximum = 100, Value = (int)((audio?.Volume ?? .7f) * 100), TickFrequency = 10 };
        var accept = new Button { Text = "Aceptar", Left = 345, Top = 205, Width = 120, DialogResult = DialogResult.OK };
        dialog.Controls.AddRange([blind, voice, new Label { Text = "Volumen de efectos", AutoSize = true, Left = 20, Top = 110 }, volume, accept]); dialog.AcceptButton = accept;
        if (dialog.ShowDialog(this) == DialogResult.OK) { court.Blindfold = blind.Checked; muteVoice = !voice.Checked; if (audio != null) audio.Volume = volume.Value / 100f; }
    }
    private void ShowHelp()
    {
        using var dialog = new Form { Text = "Ayuda y controles", ClientSize = new(740, 550), StartPosition = FormStartPosition.CenterParent };
        var text = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, AccessibleName = "Manual de Goalball Sonoro", Text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "LEEME.txt")) };
        var close = new Button { Text = "Cerrar", Dock = DockStyle.Bottom, Height = 42, DialogResult = DialogResult.OK }; dialog.Controls.Add(text); dialog.Controls.Add(close); dialog.CancelButton = close;
        dialog.Shown += (_, _) => text.Focus(); dialog.ShowDialog(this);
    }
    private void Referee()
    {
        if (match.Phase != Phase.Play) return;
        match.Pause(); held.Clear(); audio?.StopEffects();
        using var dialog = new Form { Text = "Arbitraje de entrenamiento", ClientSize = new(460, 200), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent };
        var list = new ComboBox { AccessibleName = "Falta de tu equipo", DropDownStyle = ComboBoxStyle.DropDownList, Left = 20, Top = 30, Width = 420 };
        list.Items.AddRange(["Antifaces", "Defensa ilegal", "Ruido", "Retraso personal", "Retraso del equipo", "Conducta antideportiva", "Instrucciones ilegales del banquillo"]); list.SelectedIndex = 0;
        var apply = new Button { Text = "Aplicar penalti", DialogResult = DialogResult.OK, Left = 20, Top = 120, Width = 200 };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Left = 240, Top = 120, Width = 200 };
        dialog.Controls.AddRange([list, apply, cancel]); dialog.AcceptButton = apply; dialog.CancelButton = cancel;
        modal = true; DialogResult result; try { result = dialog.ShowDialog(this); } finally { modal = false; } match.Resume(); if (result == DialogResult.OK) match.Violation(0, list.Text); court.Focus(); Drain();
    }
}

public sealed class Court(Match match) : Control
{
    [System.ComponentModel.DefaultValue(false)]
    public bool Blindfold { get; set; }
    protected override bool IsInputKey(Keys keyData) => true;
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; g.Clear(Color.FromArgb(9, 19, 30));
        string period = new[] { "", "PRIMERA MITAD", "SEGUNDA MITAD", "PRÓRROGA 1", "PRÓRROGA 2", "LANZAMIENTOS EXTRA" }[match.Period];
        using var heading = new Font("Segoe UI", 24, FontStyle.Bold); using var normal = new Font("Segoe UI", 12);
        g.DrawString($"TU EQUIPO  {match.Score[0]}          {Math.Floor(match.Clock / 60):00}:{Math.Floor(match.Clock % 60):00}          {match.Score[1]}  RIVAL", heading, Brushes.White, 35, 24);
        g.DrawString(period + "   ·   " + (match.Phase == Phase.Timeout ? $"Tiempo muerto: {Math.Ceiling(match.Wait)} s" : match.PenaltyOffender != null ? "PENALTI" : match.Phase == Phase.Finished ? "FIN DEL PARTIDO · ENTER vuelve al menú" : match.Phase == Phase.Break ? "DESCANSO · ENTER continúa" : match.Ball != null ? "BALÓN EN MOVIMIENTO" : $"Posesión {(match.Owner == 0 ? "tuya" : "rival")} · {Math.Max(0, 10 - match.Hold):0.0} s"), normal, Brushes.LightGray, 35, 70);
        if (Blindfold) { g.DrawString("ANTIFAZ ACTIVADO\nEscucha · Localiza · Bloquea", heading, Brushes.MediumAquamarine, 80, Height / 2); return; }
        float left = 45, top = 115, width = Math.Max(100, Width - 90), height = Math.Max(100, Height - 180);
        float X(double y) => left + (float)(y / 18) * width;
        float Y(double x) => top + (float)(x / 9) * height;
        using var courtBrush = new SolidBrush(Color.FromArgb(24, 63, 75)); using var line = new Pen(Color.MediumAquamarine, 2);
        g.FillRectangle(courtBrush, left, top, width, height); g.DrawRectangle(line, left, top, width, height);
        foreach (double y in new double[] { 3, 6, 9, 12, 15 }) g.DrawLine(line, X(y), top, X(y), top + height);
        foreach (double x in new double[] { 3, 6 }) { g.DrawLine(line, X(0), Y(x), X(3), Y(x)); g.DrawLine(line, X(15), Y(x), X(18), Y(x)); }
        for (int team = 0; team < 2; team++) for (int i = 0; i < 3; i++)
        {
            if (match.PenaltyOffender == team && i != match.PenaltyDefender) continue;
            float x = X(team == 0 ? 1.6 : 16.4), y = Y(match.Players[team][i]); float radius = team == 0 && match.Dives[i] > 0 ? height / 9 * 1.35f : 16;
            g.FillEllipse(team == 0 ? Brushes.MediumAquamarine : Brushes.SandyBrown, x - 16, y - radius, 32, radius * 2);
            if (team == 0 && i == match.Selected) g.DrawEllipse(Pens.White, x - 22, y - 22, 44, 44);
            g.DrawString((i + 1).ToString(), normal, Brushes.Black, x - 6, y - 11);
        }
        if (match.Ball is { } b) g.FillEllipse(Brushes.LightGoldenrodYellow, X(b.Y) - 9, Y(b.X) - 9, 18, 18);
        if (match.Owner == 0 && match.Ball == null) { using var aim = new Pen(Color.FromArgb(120, Color.MediumAquamarine)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }; g.DrawLine(aim, X(2), Y(match.Players[0][match.Selected]), X(16), Y(match.Aim)); }
        g.DrawString($"ESPACIO lanzar / bloquear    H marcador    ESC menú    F1 ayuda    F11 pantalla completa\nDefensor {match.Selected + 1}    Potencia {match.Power * 100:0}%    Destino {match.Aim:0.0} m", normal, Brushes.LightGray, 35, top + height + 12);
    }
    protected override void OnCreateControl() { base.OnCreateControl(); SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true); TabStop = true; AccessibleRole = AccessibleRole.Pane; }
}

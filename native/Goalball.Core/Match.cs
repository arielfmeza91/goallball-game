namespace Goalball.Core;

public enum Phase { Ready, Play, Pause, Break, Timeout, Finished }
public enum ThrowStyle { Legal, High, Long, Short }
public record GameEvent(string Text, string Sound = "", bool Critical = false);
public sealed class Ball(double x, double y, double target, double speed, int team)
{
    public double X = x, Y = y, FromX = x, Target = target, Speed = speed, Travel;
    public int Team = team;
}
public sealed class Match
{
    private readonly Func<double> random;
    public Match(Func<double>? random = null) { this.random = random ?? Random.Shared.NextDouble; Reset(); }
    public int[] Score { get; private set; } = [0, 0];
    public int Period { get; private set; }
    public double Clock { get; set; }
    public Phase Phase { get; private set; }
    public int Owner { get; set; }
    public double Hold { get; private set; }
    public Ball? Ball { get; private set; }
    public double[][] Players { get; private set; } = [[1.5, 4.5, 7.5], [1.5, 4.5, 7.5]];
    public double[] Dives { get; private set; } = [0, 0, 0];
    public int Selected { get; private set; } = 1;
    public double Aim { get; private set; } = 4.5;
    public double Power { get; private set; } = .65;
    public ThrowStyle Style { get; set; }
    public int? PenaltyOffender { get; private set; }
    public string PenaltyReason { get; private set; } = "";
    public int PenaltyDefender { get; private set; }
    public double Wait { get; private set; }
    public bool Practice { get; private set; }
    public int[] Timeouts { get; private set; } = [0, 0];
    public int[] Subs { get; private set; } = [0, 0];
    private int[] halfTimeouts = [0, 0], halfSubs = [0, 0], extraTimeouts = [0, 0], extraSubs = [0, 0];
    public int[] ShootCount { get; private set; } = [0, 0];
    public int[] ShootGoals { get; private set; } = [0, 0];
    private double aiWait;
    private Phase pausedPhase = Phase.Play;
    public Queue<GameEvent> Events { get; } = new();
    public void Say(string text, string sound = "", bool critical = false) => Events.Enqueue(new(text, sound, critical));
    public void Reset(bool practice = false)
    {
        Practice = practice; Score = [0, 0]; Period = 1; Clock = practice ? 300 : 720;
        Phase = Phase.Ready; Owner = 0; Hold = 0; Ball = null;
        Players = [[1.5, 4.5, 7.5], [1.5, 4.5, 7.5]]; Dives = [0, 0, 0]; Selected = 1; Aim = 4.5; Power = .65;
        Timeouts = [0, 0]; Subs = [0, 0]; halfTimeouts = [0, 0]; halfSubs = [0, 0]; extraTimeouts = [0, 0]; extraSubs = [0, 0];
        ShootCount = [0, 0]; ShootGoals = [0, 0]; PenaltyOffender = null; Wait = 0; aiWait = 2; Style = ThrowStyle.Legal;
        Events.Clear();
    }
    public void Start() { if (Phase == Phase.Ready) { Phase = Phase.Play; Say("Silencio. Play. Balón de tu equipo.", "whistle", true); } }
    public void Pause() { if (Phase is Phase.Play or Phase.Timeout) { pausedPhase = Phase; Phase = Phase.Pause; Say("Juego pausado.", critical: true); } }
    public void Resume()
    {
        if (Phase == Phase.Pause) { Phase = pausedPhase; Say("Play."); }
        else if (Phase == Phase.Break) { Phase = Phase.Play; Hold = 0; Owner = Period == 2 ? 1 : 0; aiWait = 2; Say("Play."); }
    }
    public void Select(int player)
    {
        if (PenaltyOffender == 0 && player != PenaltyDefender) { Say("El penalti lo defiende el jugador sancionado."); return; }
        Selected = Math.Clamp(player, 0, 2); Say($"Defensor {Selected + 1}.");
    }
    public void Move(double d, double dt) { if (Phase == Phase.Play) Players[0][Selected] = Math.Clamp(Players[0][Selected] + d * dt * 4, .5, 8.5); }
    public void SetAim(double x) => Aim = Math.Clamp(x, 0, 9);
    public void SetPower(double x) => Power = Math.Clamp(x, .1, 1);
    public void Dive()
    {
        if (Phase == Phase.Play && Owner == 1) { Dives[Selected] = .65; Say("", "dive"); }
    }
    public void Violation(int team, string reason)
    {
        Ball = null; Hold = 0; PenaltyOffender = team; PenaltyDefender = team == 0 ? Selected : 1;
        PenaltyReason = reason; Owner = 1 - team; aiWait = 2.6;
        Say($"Penalti: {reason}. Lanza {(team == 0 ? "el rival" : "tu equipo")}. Un único defensor.", "whistle", true);
    }
    public bool Throw(int team, double target, double power, ThrowStyle style = ThrowStyle.Legal)
    {
        if (Phase != Phase.Play || Owner != team || Ball != null) return false;
        if (style == ThrowStyle.High) { Violation(team, "balón alto"); return false; }
        if (style == ThrowStyle.Long) { Violation(team, "balón largo"); return false; }
        if (style == ThrowStyle.Short) { PenaltyOffender = null; Owner = 1 - team; Hold = 0; aiWait = 2; Say("Balón corto. Cambio de posesión.", "whistle", true); return false; }
        int player = team == 0 ? Selected : 1;
        Ball = new(Players[team][player], team == 0 ? 2 : 16, Math.Clamp(target, -1, 10), 7 + Math.Clamp(power, 0, 1) * 9, team);
        Hold = 0; Say(team == 0 ? "Lanzamiento." : "Lanza el rival.", "throw"); return true;
    }
    public void Action() { if (Owner == 0 && Ball == null) Throw(0, Aim, Power, Style); else Dive(); }
    public bool Timeout(int team = 0)
    {
        if (Phase != Phase.Play || Ball != null || PenaltyOffender != null) { Say("Espera a que el balón esté controlado."); return false; }
        bool extra = Period > 2;
        if (extra ? extraTimeouts[team] >= 1 : Timeouts[team] >= 4 || halfTimeouts[team] >= 3) { Say("No quedan tiempos muertos."); return false; }
        Timeouts[team]++; halfTimeouts[team]++; if (extra) extraTimeouts[team]++;
        Phase = Phase.Timeout; Wait = 45; Say("Tiempo muerto. Cuarenta y cinco segundos.", "whistle", true); return true;
    }
    public bool Substitute(int team = 0)
    {
        if (Phase != Phase.Play || Ball != null || PenaltyOffender != null) { Say("Espera a que el balón esté controlado."); return false; }
        bool extra = Period > 2;
        if (extra ? extraSubs[team] >= 1 : Subs[team] >= 4 || halfSubs[team] >= 3) { Say("No quedan sustituciones."); return false; }
        Subs[team]++; halfSubs[team]++; if (extra) extraSubs[team]++; Say("Sustitución completada.", "whistle"); return true;
    }
    private void Finish()
    {
        Phase = Phase.Finished;
        Say($"Fin del partido. Tu equipo {Score[0]}, rival {Score[1]}. " + (Score[0] == Score[1] ? "Empate." : Score[0] > Score[1] ? "Gana tu equipo." : "Gana el rival."), "whistle", true);
    }
    private void NextPeriod()
    {
        if (Practice || (Period == 2 && Score[0] != Score[1])) { Finish(); return; }
        if (Period == 4) { Period = 5; Clock = 0; Phase = Phase.Play; PenaltyOffender = 1; PenaltyDefender = 1; Owner = 0; Hold = 0; Say("Lanzamientos extra. Tres por equipo; después muerte súbita.", "whistle", true); return; }
        Period++; Clock = Period <= 2 ? 720 : 180; halfTimeouts = [0, 0]; halfSubs = [0, 0]; Phase = Phase.Break; Ball = null;
        Wait = Period == 4 ? 0 : 180;
        Say(Period == 2 ? "Descanso. Tres minutos. Enter para continuar." : Period == 3 ? "Empate. Prórroga: dos mitades de tres minutos, gol de oro. Enter para continuar." : "Cambio de campo. Enter para continuar.", "whistle", true);
    }
    public void Resolve(bool goal)
    {
        if (Ball == null) return;
        int team = Ball.Team; Ball = null;
        if (goal) { Score[team]++; Say($"Gol. Tu equipo {Score[0]}, rival {Score[1]}.", "goal", true); }
        else Say("Balón controlado.", "save");
        if (Period == 5)
        {
            ShootCount[team]++; if (goal) ShootGoals[team]++;
            int a = ShootCount[0], b = ShootCount[1], ga = ShootGoals[0], gb = ShootGoals[1];
            if ((a <= 3 && b <= 3 && (ga > gb + (3 - b) || gb > ga + (3 - a))) || (a == b && a >= 3 && ga != gb)) { Finish(); return; }
            Owner = 1 - team; PenaltyOffender = team; PenaltyDefender = team == 0 ? Selected : 1;
        }
        else
        {
            PenaltyOffender = null; Owner = 1 - team;
            if (goal && (Math.Abs(Score[0] - Score[1]) >= 10 || Period >= 3)) { Finish(); return; }
        }
        Hold = 0; aiWait = 2.5;
    }
    public void Step(double dt)
    {
        dt = Math.Clamp(dt, 0, .1);
        if (Phase == Phase.Timeout) { Wait -= dt; if (Wait <= .000001) { Phase = Phase.Play; Say("Fin del tiempo muerto. Play."); } return; }
        if (Phase != Phase.Play) return;
        for (int i = 0; i < 3; i++) Dives[i] = Math.Max(0, Dives[i] - dt);
        if (PenaltyOffender == null && Period < 5) { Clock = Math.Max(0, Clock - dt); if (Clock == 0) { NextPeriod(); return; } }
        if (Ball == null)
        {
            Hold += dt;
            if (Hold >= 10) { Violation(Owner, "diez segundos"); return; }
            if (Owner == 1) { aiWait -= dt; if (aiWait <= 0) Throw(1, .5 + random() * 8, .4 + random() * .55); }
            return;
        }
        var ball = Ball; ball.Travel += dt * ball.Speed; ball.Y += dt * ball.Speed * (ball.Team == 0 ? 1 : -1);
        ball.X = ball.FromX + (ball.Target - ball.FromX) * Math.Clamp(ball.Travel / 14, 0, 1);
        if (ball.X < 0 || ball.X > 9) { Ball = null; PenaltyOffender = null; Owner = 1 - ball.Team; Hold = 0; aiWait = 2; Say("Fuera. Cambio de posesión.", "whistle", true); return; }
        if (!(ball.Team == 0 ? ball.Y >= 16 : ball.Y <= 2)) return;
        int defense = 1 - ball.Team; bool saved = false;
        for (int i = 0; i < 3; i++)
        {
            if (PenaltyOffender != null && i != PenaltyDefender) continue;
            double reach = defense == 1 ? (PenaltyOffender != null ? .7 : .95) : Dives[i] > 0 ? 1.35 : .32;
            if (Math.Abs(Players[defense][i] - ball.X) < reach && (defense == 0 || random() < .72)) { saved = true; break; }
        }
        Resolve(!saved);
    }
    public string Report() => $"Tu equipo {Score[0]}, rival {Score[1]}. {Math.Ceiling(Clock)} segundos. " +
        $"Defensor {Selected + 1}, posición {Players[0][Selected]:0.0} metros. Destino {Aim:0.0} metros. Potencia {Power * 100:0} por ciento. " +
        (Ball != null ? $"Balón a {(Ball.X < 3 ? "la izquierda" : Ball.X > 6 ? "la derecha" : "el centro")}." : $"Posesión {(Owner == 0 ? "tuya" : "rival")}. Quedan {Math.Max(0, 10 - Hold):0} segundos para lanzar.");
}

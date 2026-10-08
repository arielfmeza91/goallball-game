using Goalball.Core;
int passed = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
void Test(string name, Action action) { action(); Console.WriteLine("PASS: " + name); passed++; }
Match Play() { var m = new Match(() => .99); m.Start(); return m; }
void Tick(Match m, int n) { for (int i = 0; i < n; i++) m.Step(.1); }
Test("partido reglamentario", () => { var m = Play(); Check(m.Clock == 720 && m.Phase == Phase.Play && m.Players[0].Length == 3, "inicio"); });
Test("diez segundos, penalti y reloj detenido", () => { var m = Play(); Tick(m, 101); Check(m.PenaltyOffender == 0 && m.Owner == 1, "penalti"); double c = m.Clock; m.Step(.1); Check(c == m.Clock, "reloj"); });
Test("balón alto y largo", () => { foreach (var s in new[] { ThrowStyle.High, ThrowStyle.Long }) { var m = Play(); m.Throw(0, 4.5, .6, s); Check(m.PenaltyOffender == 0 && m.Owner == 1, "falta"); } });
Test("balón corto cambia posesión", () => { var m = Play(); m.Throw(0, 4.5, .6, ThrowStyle.Short); Check(m.Owner == 1 && m.PenaltyOffender == null, "corto"); });
Test("diferencia de diez termina partido", () => { var m = Play(); m.Score[0] = 9; m.Throw(0, 4.5, 1); Tick(m, 30); Check(m.Score[0] == 10 && m.Phase == Phase.Finished, "fin"); });
Test("bloqueo activo", () => { var m = Play(); m.Owner = 1; m.Throw(1, 4.5, .6); m.Ball!.Y = 2.2; m.Ball.Travel = 13.8; m.Dive(); m.Step(.1); Check(m.Score[1] == 0 && m.Owner == 0, "bloqueo"); });
Test("fuera cambia posesión", () => { var m = Play(); m.Throw(0, 10, 1); Tick(m, 15); Check(m.Score[0] == 0 && m.Owner == 1, "fuera"); });
Test("segunda mitad y prórroga con gol de oro", () => { var m = Play(); m.Clock = .01; m.Step(.1); Check(m.Period == 2 && m.Phase == Phase.Break, "descanso"); m.Resume(); m.Clock = .01; m.Step(.1); Check(m.Period == 3 && m.Clock == 180, "prórroga"); m.Resume(); m.Throw(0, 4.5, 1); Tick(m, 30); Check(m.Phase == Phase.Finished, "oro"); });
Test("45 segundos y máximo tres tiempos por mitad", () => { var m = Play(); for (int i = 0; i < 3; i++) { Check(m.Timeout(), "timeout"); double c = m.Clock; while (m.Phase == Phase.Timeout) m.Step(.1); Check(m.Clock == c, "reloj timeout"); } Check(!m.Timeout(), "límite"); });
Test("máximo cuatro tiempos por partido", () => { var m = Play(); for (int i = 0; i < 3; i++) { m.Timeout(); while (m.Phase == Phase.Timeout) m.Step(.1); } m.Clock = .01; m.Step(.1); m.Resume(); Check(m.Timeout(), "cuarto"); while (m.Phase == Phase.Timeout) m.Step(.1); Check(!m.Timeout(), "quinto"); });
Test("sustituciones y pausa", () => { var m = Play(); for (int i = 0; i < 3; i++) Check(m.Substitute(), "sustitución"); Check(!m.Substitute(), "límite"); m.Pause(); Tick(m, 20); Check(m.Clock == 720 && m.Hold == 0, "pausa"); m.Resume(); Check(m.Phase == Phase.Play, "continuar"); });
Test("un tiempo adicional para toda la prórroga", () => { var m = Play(); m.Clock = .01; m.Step(.1); m.Resume(); m.Clock = .01; m.Step(.1); m.Resume(); Check(m.Timeout(), "tiempo extra"); while (m.Phase == Phase.Timeout) m.Step(.1); m.Clock = .01; m.Step(.1); m.Resume(); Check(m.Period == 4 && !m.Timeout(), "límite prórroga"); });
Test("lanzamientos extra alternados y ventaja insalvable", () => { var m = Play(); for (int i = 0; i < 4; i++) { m.Clock = .01; m.Step(.1); m.Resume(); } Check(m.Period == 5 && m.PenaltyOffender == 1, "extra"); for (int i = 0; i < 4; i++) { int team = i % 2; m.Throw(team, 4.5, .6); m.Resolve(team == 0); } Check(m.Phase == Phase.Finished && m.ShootCount[0] == 2 && m.ShootCount[1] == 2, "ganador"); });
Test("penalti fija al defensor sancionado", () => { var m = Play(); m.Select(0); m.Violation(0, "antifaces"); m.Select(2); Check(m.Selected == 0 && m.PenaltyDefender == 0, "defensor"); });
Test("límites de posición, potencia y puntería", () => { var m = Play(); m.Move(-1, 100); m.SetAim(100); m.SetPower(-1); Check(m.Players[0][1] == .5 && m.Aim == 9 && m.Power == .1, "límites"); });
Test("entrenamiento termina tras cinco minutos", () => { var m = Play(); m.Reset(true); m.Start(); m.Clock = .01; m.Step(.1); Check(m.Phase == Phase.Finished, "entrenamiento"); });
Test("pausa conserva un tiempo muerto en curso", () => { var m = Play(); m.Timeout(); m.Step(.1); double wait = m.Wait; m.Pause(); Tick(m, 20); Check(m.Wait == wait, "pausa tiempo"); m.Resume(); Check(m.Phase == Phase.Timeout, "reanudar tiempo"); });
Test("audio continuo se mantiene entre lecturas", () => { var r = new RollingAudio([.25f, -.5f, .8f, -.2f]); r.Position(4.5, 2, 8, 1); var a = new float[20]; Check(r.Read(a, 0, 20) == 20 && r.Read(a, 0, 20) == 20, "flujo"); });
Test("panorama izquierdo y derecho", () => { var r = new RollingAudio([1f, 1f]); var a = new float[2]; r.Position(0, 2, 8, 1); r.Read(a, 0, 2); Check(a[0] > .5f && a[1] == 0, "izquierda"); r.Position(9, 2, 8, 1); r.Read(a, 0, 2); Check(a[1] > .5f && Math.Abs(a[0]) < .00001f, "derecha"); });
Test("distancia atenúa y control detiene el balón", () => { var r = new RollingAudio([1f, 1f]); var a = new float[2]; r.Position(4.5, 2, 8, 1); r.Read(a, 0, 2); float near = a[0]; r.Position(4.5, 16, 8, 1); r.Read(a, 0, 2); Check(a[0] < near, "distancia"); r.Active = false; Check(r.Read(a, 0, 2) == 0, "control"); });
Console.WriteLine($"{passed} pruebas ejecutadas y aprobadas.");

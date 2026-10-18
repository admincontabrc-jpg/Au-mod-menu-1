// Among Us v19.0.0 — Mod Menu Completo PT-BR
// Runtime: IL2CPP | Plataforma: Android ARM64
// Offsets extraídos de dump.cs v19.0.0 (global-metadata v31)
//
// Dependências:
//   BepInEx 6.x IL2CPP  — carregador de mods para Unity IL2CPP
//   Il2CppInterop        — bridge gerenciado→IL2CPP
//   UnityEngine.CoreModule (via Il2Cpp strip)
//
// Como buildar:
//   1. Clone BepInEx IL2CPP template
//   2. Adicione este arquivo em src/
//   3. dotnet build -c Release
//   4. Coloque o .dll em BepInEx/plugins/
//   5. Repache o APK com APKTool + sign com apksigner

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace AUModMenu
{
    // ── OFFSETS v19.0.0 ─────────────────────────────────────────────────────
    // Todos extraídos do dump.cs gerado pelo Il2CppDumper
    public static class Offsets
    {
        // PlayerControl
        public const long MurderPlayer       = 0x21D8DA0;  // void MurderPlayer(PlayerControl, MurderResultFlags)
        public const long RpcMurderPlayer    = 0x21D8558;  // void RpcMurderPlayer(PlayerControl, bool)
        public const long Exiled             = 0x21D5F58;  // void Exiled()
        public const long SetKillTimer       = 0x21D1D40;  // void SetKillTimer(float)
        public const long RpcSetRole         = 0x21DD560;  // void RpcSetRole(RoleTypes, bool)
        public const long AdjustLighting     = 0x21D260C;  // void AdjustLighting()

        // LightSource
        public const long SetViewDistance    = 0x223DE44;  // void SetViewDistance(float)

        // InnerNetClient / AmongUsClient
        public const long KickPlayer         = 0x231A550;  // void KickPlayer(int clientId, bool ban)
    }

    // ── ENUMS ────────────────────────────────────────────────────────────────
    public enum RoleTypes : ushort
    {
        Crewmate     = 0,
        Impostor     = 1,
        Scientist    = 2,
        Engineer     = 3,
        GuardianAngel= 4,
        Shapeshifter = 5,
        CrewmateGhost= 6,
        ImpostorGhost= 7,
        Noisemaker   = 8,
        Phantom      = 9,
        Tracker      = 10,
        Detective    = 12,
        Viper        = 18,
        Judge        = 19,
        SpiritGuide  = 21,
    }

    // ── PLUGIN ENTRY ─────────────────────────────────────────────────────────
    [BepInPlugin("br.modmenu.aumod", "AU Mod Menu PT-BR", "1.0.0")]
    public class ModMenuPlugin : BasePlugin
    {
        public static ModMenuPlugin Instance;
        private Harmony _harmony;

        public override void Load()
        {
            Instance = this;
            _harmony = new Harmony("br.modmenu.aumod");
            _harmony.PatchAll();
            AddComponent<ModMenuBehaviour>();
            Log.LogInfo("[ModMenu] carregado — v19.0.0");
        }
    }

    // ── GUI PRINCIPAL ─────────────────────────────────────────────────────────
    public class ModMenuBehaviour : MonoBehaviour
    {
        // ── estado da UI ──────────────────────────────────────────────────────
        private bool   _aberto        = false;
        private int    _aba           = 0;           // 0=Jogadores 1=Local 2=Visual 3=Anfitrião 4=Config
        private string[] _abas        = { "👥 Jogadores", "🗺 Local", "👁 Visual", "👑 Anfitrião", "⚙ Config" };
        private Vector2  _scroll      = Vector2.zero;
        private Rect     _janela      = new Rect(30, 30, 440, 600);

        // ── estado visual ─────────────────────────────────────────────────────
        private bool  _espJogadores   = false;
        private bool  _espLinhas      = false;
        private float _visao          = 1.0f;        // multiplicador (1.0 = normal, 5.0 = mapa todo)
        private bool  _zoom           = false;
        private float _zoomFator      = 1.0f;

        // ── estado anfitrião ──────────────────────────────────────────────────
        private bool  _cooldownZero   = false;
        private bool  _sempreImpostor = false;
        private int   _jogadorSelecionado = -1;      // índice em PlayerControl.AllPlayerControls
        private int   _roleSelecionada    = 1;       // índice no enum RoleTypes
        private string[] _roleNomes = {
            "Tripulante","Impostor","Cientista","Engenheiro",
            "Anjo da Guarda","Metamorfo","Fantasma Trip.","Fantasma Imp.",
            "Gerador Ruído","Fantasma","Rastreador","Detetive","Víbora","Juíz","Guia Espiritual"
        };
        private RoleTypes[] _roleValues = {
            RoleTypes.Crewmate, RoleTypes.Impostor, RoleTypes.Scientist, RoleTypes.Engineer,
            RoleTypes.GuardianAngel, RoleTypes.Shapeshifter, RoleTypes.CrewmateGhost, RoleTypes.ImpostorGhost,
            RoleTypes.Noisemaker, RoleTypes.Phantom, RoleTypes.Tracker, RoleTypes.Detective,
            RoleTypes.Viper, RoleTypes.Judge, RoleTypes.SpiritGuide
        };

        // ── login via token ───────────────────────────────────────────────────
        private string _tokenInput    = "";
        private bool   _autenticado   = false;
        private bool   _mostrarLogin  = true;

        // ── estilo ────────────────────────────────────────────────────────────
        private GUIStyle _estiloJanela;
        private GUIStyle _estiloTitulo;
        private GUIStyle _estiloAba;
        private GUIStyle _estiloAbaAtiva;
        private GUIStyle _estiloBotao;
        private GUIStyle _estiloToggle;
        private GUIStyle _estiloSliderLabel;
        private bool     _estiloIniciado = false;

        private void IniciarEstilo()
        {
            if (_estiloIniciado) return;
            _estiloIniciado = true;

            var fundo = CriarTextura(new Color(0.08f, 0.08f, 0.12f, 0.97f));
            var fundoAba = CriarTextura(new Color(0.15f, 0.10f, 0.30f, 1f));
            var fundoAbaAtiva = CriarTextura(new Color(0.45f, 0.10f, 0.80f, 1f));
            var fundoBotao = CriarTextura(new Color(0.25f, 0.10f, 0.55f, 1f));
            var fundoBotaoHover = CriarTextura(new Color(0.55f, 0.15f, 0.85f, 1f));

            _estiloJanela = new GUIStyle(GUI.skin.window)
            {
                normal   = { background = fundo, textColor = Color.white },
                fontSize = 15,
                fontStyle= FontStyle.Bold,
                padding  = new RectOffset(8, 8, 30, 8)
            };

            _estiloTitulo = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 18,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.75f, 0.4f, 1f) },
                alignment = TextAnchor.MiddleCenter
            };

            _estiloAba = new GUIStyle(GUI.skin.button)
            {
                normal   = { background = fundoAba,      textColor = new Color(0.8f, 0.8f, 0.8f) },
                hover    = { background = fundoAbaAtiva, textColor = Color.white },
                active   = { background = fundoAbaAtiva, textColor = Color.white },
                fontSize = 11,
                fontStyle= FontStyle.Bold,
                padding  = new RectOffset(4, 4, 6, 6)
            };

            _estiloAbaAtiva = new GUIStyle(_estiloAba)
            {
                normal = { background = fundoAbaAtiva, textColor = Color.white }
            };

            _estiloBotao = new GUIStyle(GUI.skin.button)
            {
                normal  = { background = fundoBotao,      textColor = Color.white },
                hover   = { background = fundoBotaoHover, textColor = Color.white },
                active  = { background = fundoBotao,      textColor = Color.white },
                fontSize= 13,
                fontStyle= FontStyle.Bold,
                padding = new RectOffset(6, 6, 8, 8)
            };

            _estiloToggle = new GUIStyle(GUI.skin.toggle)
            {
                normal  = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                active  = { textColor = Color.white },
                fontSize= 13
            };

            _estiloSliderLabel = new GUIStyle(GUI.skin.label)
            {
                normal  = { textColor = new Color(0.8f, 0.6f, 1f) },
                fontSize= 12
            };
        }

        private Texture2D CriarTextura(Color cor)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, cor);
            t.Apply();
            return t;
        }

        private void Update()
        {
            // toggle: toque com 4 dedos OU tecla F1 no PC
            if (Input.GetKeyDown(KeyCode.F1) || (Input.touchCount == 4 && TouchPhase.Began == Input.GetTouch(0).phase))
                _aberto = !_aberto;

            if (!_autenticado) return;

            // ESP linhas — desenhado no Update via GL
            if (_espLinhas) DesenharLinhasESP();

            // zoom via Camera.main
            if (_zoom && Camera.main != null)
                Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, _zoomFator * 3f, Time.deltaTime * 5f);

            // cooldown zero
            if (_cooldownZero)
            {
                var local = PlayerControl.LocalPlayer;
                if (local != null)
                    local.SetKillTimer(0f);
            }

            // visão
            AplicarVisao();
        }

        private void AplicarVisao()
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null) return;
            var ls = local.lightSource;
            if (ls == null) return;
            ls.SetViewDistance(_visao * 1.5f);
        }

        private void DesenharLinhasESP()
        {
            if (PlayerControl.AllPlayerControls == null) return;
            var local = PlayerControl.LocalPlayer;
            if (local == null) return;

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p == local || p.Data == null) continue;
                var origem = Camera.main.WorldToScreenPoint(local.transform.position);
                var dest   = Camera.main.WorldToScreenPoint(p.transform.position);
                origem.y   = Screen.height - origem.y;
                dest.y     = Screen.height - dest.y;
                // GL.Lines desenhado via OnGUI abaixo
            }
        }

        private void OnGUI()
        {
            IniciarEstilo();

            // tela de login
            if (_mostrarLogin && !_autenticado)
            {
                DesenharLogin();
                return;
            }

            if (!_aberto) return;

            _janela = GUI.Window(0, _janela, DesenharJanela, "", _estiloJanela);
        }

        // ── LOGIN ─────────────────────────────────────────────────────────────
        private void DesenharLogin()
        {
            var w = 420f;
            var h = 260f;
            var rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(rect, "");

            GUILayout.BeginArea(rect);
            GUILayout.Space(20);
            GUILayout.Label("🔐  AU Mod Menu — Login", _estiloTitulo);
            GUILayout.Space(12);
            GUILayout.Label("Cole seu token de conta Innersloth:", _estiloSliderLabel);
            GUILayout.Space(6);
            _tokenInput = GUILayout.TextField(_tokenInput, 600, GUILayout.Height(36));
            GUILayout.Space(12);

            if (GUILayout.Button("Entrar", _estiloBotao, GUILayout.Height(40)))
            {
                // validação mínima: token JWT tem 3 partes separadas por ponto
                if (_tokenInput.Split('.').Length == 3)
                {
                    _autenticado   = true;
                    _mostrarLogin  = false;
                }
                else
                {
                    _tokenInput = "❌ Token inválido — tente novamente";
                }
            }
            GUILayout.Space(8);
            GUILayout.Label("Onde encontrar: Settings → Conta → Copiar token", _estiloSliderLabel);
            GUILayout.EndArea();
        }

        // ── JANELA PRINCIPAL ──────────────────────────────────────────────────
        private void DesenharJanela(int id)
        {
            GUILayout.Label("⚡  AU Mod Menu  v19.0.0", _estiloTitulo);
            GUILayout.Space(4);

            // abas
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _abas.Length; i++)
            {
                var estilo = (i == _aba) ? _estiloAbaAtiva : _estiloAba;
                if (GUILayout.Button(_abas[i], estilo))
                    _aba = i;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            _scroll = GUILayout.BeginScrollView(_scroll);

            switch (_aba)
            {
                case 0: AbaJogadores();  break;
                case 1: AbaLocal();      break;
                case 2: AbaVisual();     break;
                case 3: AbaAnfitriao(); break;
                case 4: AbaConfig();     break;
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ABA 0 — JOGADORES
        // ═══════════════════════════════════════════════════════════════════════
        private void AbaJogadores()
        {
            if (PlayerControl.AllPlayerControls == null || PlayerControl.AllPlayerControls.Count == 0)
            {
                GUILayout.Label("⚠ Nenhum jogador encontrado (partida não iniciada?)", _estiloSliderLabel);
                return;
            }

            var players = PlayerControl.AllPlayerControls;

            GUILayout.Label("Selecione um jogador:", _estiloSliderLabel);
            GUILayout.Space(4);

            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p.Data == null) continue;

                bool selecionado = (_jogadorSelecionado == i);
                string nome  = p.Data.PlayerName ?? $"Jogador {i}";
                string label = $"{(selecionado ? "▶ " : "   ")}{nome}  [{p.PlayerId}]";

                if (GUILayout.Button(label, selecionado ? _estiloAbaAtiva : _estiloAba))
                    _jogadorSelecionado = i;
            }

            if (_jogadorSelecionado < 0 || _jogadorSelecionado >= players.Count)
            {
                GUILayout.Space(8);
                GUILayout.Label("← Selecione um jogador acima", _estiloSliderLabel);
                return;
            }

            var alvo = players[_jogadorSelecionado];
            if (alvo == null) return;

            GUILayout.Space(10);
            GUILayout.Label($"Ações — {alvo.Data?.PlayerName ?? "?"}", _estiloTitulo);
            GUILayout.Space(4);

            // ── KILL ──────────────────────────────────────────────────────────
            if (GUILayout.Button("💀  Matar jogador", _estiloBotao))
            {
                var local = PlayerControl.LocalPlayer;
                if (local != null)
                    local.RpcMurderPlayer(alvo, true);
            }

            // ── EXILAR (equivalente a expulsão) ───────────────────────────────
            if (GUILayout.Button("🚀  Exilar (expulsar sem votação)", _estiloBotao))
                alvo.Exiled();

            // ── TIRAR DO DUTO (sem ser engenheiro) ───────────────────────────
            if (GUILayout.Button("🌀  Tirar do duto", _estiloBotao))
            {
                if (alvo.inVent)
                {
                    // força saída do vent via NetworkTransform + Exited state
                    alvo.MyPhysics?.RpcBootFromVent(0);
                }
            }

            // ── SETAR ROLE ────────────────────────────────────────────────────
            GUILayout.Space(6);
            GUILayout.Label("Setar função:", _estiloSliderLabel);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("◀", _estiloBotao, GUILayout.Width(36)))
                _roleSelecionada = (_roleSelecionada - 1 + _roleNomes.Length) % _roleNomes.Length;
            GUILayout.Label(_roleNomes[_roleSelecionada], _estiloTitulo, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("▶", _estiloBotao, GUILayout.Width(36)))
                _roleSelecionada = (_roleSelecionada + 1) % _roleNomes.Length;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("✅  Aplicar função", _estiloBotao))
                alvo.RpcSetRole((global::RoleTypes)_roleValues[_roleSelecionada], true);

            // ── KICK / BAN ────────────────────────────────────────────────────
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("👢  Kickar", _estiloBotao))
                AmongUsClient.Instance?.KickPlayer(alvo.PlayerId, false);
            if (GUILayout.Button("🔨  Banir", _estiloBotao))
                AmongUsClient.Instance?.KickPlayer(alvo.PlayerId, true);
            GUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ABA 1 — LOCAL
        // ═══════════════════════════════════════════════════════════════════════
        private void AbaLocal()
        {
            GUILayout.Label("Teleporte", _estiloTitulo);
            GUILayout.Space(4);

            var local = PlayerControl.LocalPlayer;
            if (local == null)
            {
                GUILayout.Label("⚠ LocalPlayer não encontrado", _estiloSliderLabel);
                return;
            }

            GUILayout.Label($"Posição atual: {local.transform.position.x:F1}, {local.transform.position.y:F1}", _estiloSliderLabel);
            GUILayout.Space(6);

            // pontos de teleporte por mapa (The Skeld — posições comuns)
            var pontos = new Dictionary<string, Vector2>
            {
                { "🚀 Cafeteria",      new Vector2(-2.0f,  2.6f) },
                { "🔧 Elétrica",       new Vector2(9.0f,  -9.0f) },
                { "⚙ Motor (Esq.)",   new Vector2(-15.0f,-9.0f) },
                { "🛡 Escudo",         new Vector2( 9.0f,  2.0f) },
                { "🗺 Nav.",           new Vector2(19.0f,  3.0f) },
                { "💊 Medbay",        new Vector2(-7.0f,  0.0f) },
                { "🔒 Segurança",     new Vector2( 0.5f, -6.0f) },
                { "🛠 Admin",         new Vector2( 4.0f,  8.0f) },
                { "📡 Comm.",         new Vector2(-6.0f, -5.0f) },
                { "🌡 Armazen.",      new Vector2(-13.0f,-12.0f) },
            };

            foreach (var kv in pontos)
            {
                if (GUILayout.Button(kv.Key, _estiloBotao))
                {
                    local.NetTransform?.RpcSnapTo(kv.Value);
                    local.transform.position = new Vector3(kv.Value.x, kv.Value.y, local.transform.position.z);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ABA 2 — VISUAL / ESP
        // ═══════════════════════════════════════════════════════════════════════
        private void AbaVisual()
        {
            GUILayout.Label("Visual / ESP", _estiloTitulo);
            GUILayout.Space(6);

            // ESP jogadores (linhas)
            bool espAntes = _espLinhas;
            _espLinhas = GUILayout.Toggle(_espLinhas, "  🔴  ESP Jogadores (linhas no mapa)", _estiloToggle);
            GUILayout.Space(4);

            // ESP nomes sempre visíveis
            _espJogadores = GUILayout.Toggle(_espJogadores, "  📛  ESP Nomes (sempre visíveis)", _estiloToggle);
            GUILayout.Space(8);

            // visão
            GUILayout.Label($"👁  Visão do jogador: {_visao:F1}x  {(_visao >= 4.9f ? "(visão total 🗺)" : "")}", _estiloSliderLabel);
            _visao = GUILayout.HorizontalSlider(_visao, 0.5f, 5.0f);
            if (GUILayout.Button("Aplicar visão", _estiloBotao))
                AplicarVisao();

            GUILayout.Space(8);

            // zoom
            _zoom = GUILayout.Toggle(_zoom, "  🔍  Zoom (câmera afasta)", _estiloToggle);
            if (_zoom)
            {
                GUILayout.Label($"Nível do zoom: {_zoomFator:F1}x", _estiloSliderLabel);
                _zoomFator = GUILayout.HorizontalSlider(_zoomFator, 1.0f, 6.0f);
            }

            // ESP overlay — desenhado aqui via GUI quando ativo
            if (_espJogadores && Event.current.type == EventType.Repaint)
                DesenharESPNomes();
        }

        private void DesenharESPNomes()
        {
            if (PlayerControl.AllPlayerControls == null) return;
            var local = PlayerControl.LocalPlayer;

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null) continue;
                if (Camera.main == null) continue;

                var sp = Camera.main.WorldToScreenPoint(p.transform.position + Vector3.up * 0.5f);
                if (sp.z < 0) continue;

                sp.y = Screen.height - sp.y;
                bool ehLocal = (p == local);
                bool ehMorto = p.Data.IsDead;

                var cor = ehMorto ? Color.gray : (ehLocal ? Color.green : Color.red);
                GUI.color = cor;
                GUI.Label(new Rect(sp.x - 40, sp.y - 20, 100, 20), p.Data.PlayerName ?? "?");
                GUI.color = Color.white;
            }

            // linhas ESP via GUI.DrawTexture linha a linha
            if (_espLinhas && local != null)
            {
                var origemSP = Camera.main.WorldToScreenPoint(local.transform.position);
                origemSP.y  = Screen.height - origemSP.y;

                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p == null || p == local) continue;
                    var destSP = Camera.main.WorldToScreenPoint(p.transform.position);
                    destSP.y   = Screen.height - destSP.y;
                    DesenharLinha(new Vector2(origemSP.x, origemSP.y), new Vector2(destSP.x, destSP.y), Color.red, 1.5f);
                }
            }
        }

        private void DesenharLinha(Vector2 a, Vector2 b, Color cor, float espessura)
        {
            var saved = GUI.color;
            GUI.color = cor;
            var diff   = b - a;
            var angulo = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angulo, a);
            GUI.DrawTexture(new Rect(a.x, a.y - espessura / 2f, diff.magnitude, espessura), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color  = saved;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ABA 3 — ANFITRIÃO
        // ═══════════════════════════════════════════════════════════════════════
        private void AbaAnfitriao()
        {
            GUILayout.Label("Controles de Anfitrião", _estiloTitulo);
            GUILayout.Space(6);

            bool ehHost = AmongUsClient.Instance != null &&
                          AmongUsClient.Instance.AmHost();

            if (!ehHost)
            {
                GUILayout.Label("⚠ Você não é o anfitrião desta sala.\nAlgumas funções podem não funcionar.", _estiloSliderLabel);
                GUILayout.Space(6);
            }

            // cooldown zero
            _cooldownZero = GUILayout.Toggle(_cooldownZero, "  ⚡  Cooldown de matar = 0s", _estiloToggle);
            GUILayout.Space(4);

            // sempre impostor
            bool siAntes = _sempreImpostor;
            _sempreImpostor = GUILayout.Toggle(_sempreImpostor, "  👺  Sempre Impostor", _estiloToggle);
            if (_sempreImpostor && !siAntes)
            {
                var local = PlayerControl.LocalPlayer;
                if (local != null)
                    local.RpcSetRole((global::RoleTypes)RoleTypes.Impostor, true);
            }

            GUILayout.Space(8);
            GUILayout.Label("Ações em massa:", _estiloSliderLabel);

            // matar todos
            if (GUILayout.Button("💀  Matar todos os jogadores", _estiloBotao))
            {
                var local = PlayerControl.LocalPlayer;
                if (local != null && PlayerControl.AllPlayerControls != null)
                    foreach (var p in PlayerControl.AllPlayerControls)
                        if (p != null && p != local && !p.Data.IsDead)
                            local.RpcMurderPlayer(p, true);
            }

            // kickar todos
            if (GUILayout.Button("👢  Kickar todos", _estiloBotao))
            {
                if (PlayerControl.AllPlayerControls != null)
                    foreach (var p in PlayerControl.AllPlayerControls)
                        if (p != null && p != PlayerControl.LocalPlayer)
                            AmongUsClient.Instance?.KickPlayer(p.PlayerId, false);
            }

            // banir todos
            if (GUILayout.Button("🔨  Banir todos", _estiloBotao))
            {
                if (PlayerControl.AllPlayerControls != null)
                    foreach (var p in PlayerControl.AllPlayerControls)
                        if (p != null && p != PlayerControl.LocalPlayer)
                            AmongUsClient.Instance?.KickPlayer(p.PlayerId, true);
            }

            GUILayout.Space(8);
            GUILayout.Label("Forçar funções de todos:", _estiloSliderLabel);

            // grid de roles
            GUILayout.BeginHorizontal();
            int col = 0;
            for (int i = 0; i < _roleNomes.Length; i++)
            {
                if (col == 2) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); col = 0; }
                int idx = i;
                if (GUILayout.Button(_roleNomes[idx], _estiloBotao))
                    ForcarFuncaoTodos(_roleValues[idx]);
                col++;
            }
            GUILayout.EndHorizontal();
        }

        private void ForcarFuncaoTodos(RoleTypes role)
        {
            if (PlayerControl.AllPlayerControls == null) return;
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null)
                    p.RpcSetRole((global::RoleTypes)role, true);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ABA 4 — CONFIGURAÇÕES
        // ═══════════════════════════════════════════════════════════════════════
        private void AbaConfig()
        {
            GUILayout.Label("Configurações", _estiloTitulo);
            GUILayout.Space(6);

            GUILayout.Label("Posição do menu:", _estiloSliderLabel);
            GUILayout.Label("X", _estiloSliderLabel);
            _janela.x = GUILayout.HorizontalSlider(_janela.x, 0, Screen.width - _janela.width);
            GUILayout.Label("Y", _estiloSliderLabel);
            _janela.y = GUILayout.HorizontalSlider(_janela.y, 0, Screen.height - _janela.height);

            GUILayout.Space(8);
            GUILayout.Label("Token atual:", _estiloSliderLabel);
            GUILayout.Label(_tokenInput.Length > 20 ? _tokenInput.Substring(0, 20) + "..." : _tokenInput, _estiloSliderLabel);

            if (GUILayout.Button("🔓  Sair / trocar conta", _estiloBotao))
            {
                _autenticado  = false;
                _mostrarLogin = true;
                _tokenInput   = "";
            }

            GUILayout.Space(8);
            GUILayout.Label("Atalho: F1 (PC) / 4 dedos (mobile)", _estiloSliderLabel);
            GUILayout.Label($"Versão: v19.0.0 | Build: 2026.9.29", _estiloSliderLabel);
        }
    }

    // ── HARMONY PATCH — cooldown zero permanente ──────────────────────────────
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
    public static class PatchKillTimer
    {
        static bool Prefix(PlayerControl __instance, ref float time)
        {
            // se o toggle tiver ativo, força o timer pra 0 antes do método original
            // checamos via singleton da behaviour
            return true; // deixa o Update do ModMenuBehaviour fazer o set
        }
    }
}

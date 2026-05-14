using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ludo.Ads;
using Ludo.Core;

namespace Ludo.Local
{
    /// <summary>
    /// Self-bootstrapping Ludo game. Builds the entire UI procedurally so no
    /// scene art / prefabs / textures are required. Just open the project and
    /// hit Build &amp; Run.
    ///
    /// Game modes (offline only):
    ///   - vs Bot (you = Red, three bots).
    ///   - 2 Player local (Red &amp; Yellow on same device).
    ///   - 4 Player local (all four seats).
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        // ---------- bootstrap ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (FindObjectOfType<GameRoot>() != null) return;
            var go = new GameObject("LudoGameRoot");
            DontDestroyOnLoad(go);
            go.AddComponent<GameRoot>();
        }

        // ---------- screens ----------

        private enum ScreenKind { Menu, Game, Result }
        private ScreenKind _screen;

        private Canvas _canvas;
        private GameObject _menuRoot;
        private GameObject _gameRoot;
        private GameObject _resultRoot;

        // gameplay state
        private LocalLudoEngine _engine;
        private LocalLudoEngine.PlayerInfo[] _players;
        private GameObject _boardRoot;
        private RectTransform _boardRect;
        private float _cellSize;
        private GameObject[][] _tokenViews;     // [seat][index]
        private Image[,] _cellImages;           // [col,row]
        private Text _statusText;
        private Text _diceText;
        private Button _rollBtn;
        private Text _resultText;
        private bool _busy;

        private static Sprite _circleSprite;
        private static Sprite _squareSprite;

        // ---------- lifecycle ----------

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            BuildSprites();
            BuildCanvas();
            ShowMenu();
        }

        // ---------- sprite helpers ----------

        private static void BuildSprites()
        {
            if (_squareSprite == null)
            {
                var tex = Texture2D.whiteTexture;
                _squareSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            if (_circleSprite == null)
            {
                int s = 64;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                var px = new Color32[s * s];
                float r = s * 0.5f - 1f;
                Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    if (d <= r - 1.0f) px[y * s + x] = new Color32(255, 255, 255, 255);
                    else if (d <= r) px[y * s + x] = new Color32(255, 255, 255, (byte)(255 * (r - d)));
                    else px[y * s + x] = new Color32(0, 0, 0, 0);
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _circleSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            }
        }

        // ---------- canvas / common UI helpers ----------

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem required for buttons
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                DontDestroyOnLoad(es);
            }
        }

        private static GameObject UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image AddImage(GameObject go, Color c, Sprite sprite = null)
        {
            var img = go.AddComponent<Image>();
            img.color = c;
            img.sprite = sprite ?? _squareSprite;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        private static Text AddText(GameObject go, string text, int size, Color c, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Button MakeButton(Transform parent, string label, Vector2 size, Vector2 anchored, Color bg, System.Action onClick)
        {
            var go = UI(label, parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchored;

            var img = go.AddComponent<Image>();
            img.color = bg;
            img.sprite = _squareSprite;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bg;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.2f);
            colors.selectedColor = bg;
            colors.disabledColor = new Color(bg.r, bg.g, bg.b, 0.4f);
            btn.colors = colors;

            var labelGO = UI("Label", go.transform);
            var lrt = (RectTransform)labelGO.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            AddText(labelGO, label, 48, Color.white);

            btn.onClick.AddListener(() => { try { onClick?.Invoke(); } catch (System.Exception e) { Debug.LogError(e); } });
            return btn;
        }

        // ============================================================
        // MENU SCREEN
        // ============================================================

        private void ShowMenu()
        {
            _screen = ScreenKind.Menu;
            DestroyIfNotNull(ref _gameRoot);
            DestroyIfNotNull(ref _resultRoot);
            DestroyIfNotNull(ref _menuRoot);

            _menuRoot = UI("Menu", _canvas.transform);
            var rt = (RectTransform)_menuRoot.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            AddImage(_menuRoot, new Color(0.10f, 0.13f, 0.18f));

            // title
            var titleGO = UI("Title", _menuRoot.transform);
            var trt = (RectTransform)titleGO.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(900, 200);
            trt.anchoredPosition = new Vector2(0, 600);
            AddText(titleGO, "LUDO", 200, Color.white);

            var subGO = UI("Sub", _menuRoot.transform);
            var srt = (RectTransform)subGO.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(900, 80);
            srt.anchoredPosition = new Vector2(0, 460);
            AddText(subGO, "Choose a mode", 48, new Color(1f, 1f, 1f, 0.7f));

            MakeButton(_menuRoot.transform, "Play vs Bot",     new Vector2(700, 160), new Vector2(0,  220), new Color(0.92f, 0.20f, 0.22f), () => StartMatch(MakePlayers(1)));
            MakeButton(_menuRoot.transform, "2 Players Local", new Vector2(700, 160), new Vector2(0,    0), new Color(0.20f, 0.72f, 0.30f), () => StartMatch(MakePlayers(2)));
            MakeButton(_menuRoot.transform, "4 Players Local", new Vector2(700, 160), new Vector2(0, -220), new Color(0.18f, 0.45f, 0.92f), () => StartMatch(MakePlayers(4)));

            var creditGO = UI("Credit", _menuRoot.transform);
            var crt = (RectTransform)creditGO.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0);
            crt.sizeDelta = new Vector2(900, 60);
            crt.anchoredPosition = new Vector2(0, 80);
            AddText(creditGO, "Tap a token to move it on your turn", 36, new Color(1f, 1f, 1f, 0.5f));

            // Banner ad on the menu (no-op until AdMob SDK is imported + ENABLE_ADMOB defined)
            if (ServiceLocator.TryGet<AdManager>(out var ad)) ad.ShowBanner();
        }

        private LocalLudoEngine.PlayerInfo[] MakePlayers(int mode)
        {
            // mode: 1 = vs Bot (you Red, 3 bots), 2 = local 2P (Red+Yellow), 4 = local 4P
            if (mode == 1)
            {
                return new[]
                {
                    new LocalLudoEngine.PlayerInfo { seat = 0, name = "You",    isBot = false },
                    new LocalLudoEngine.PlayerInfo { seat = 1, name = "Bot G",  isBot = true },
                    new LocalLudoEngine.PlayerInfo { seat = 2, name = "Bot Y",  isBot = true },
                    new LocalLudoEngine.PlayerInfo { seat = 3, name = "Bot B",  isBot = true },
                };
            }
            if (mode == 2)
            {
                return new[]
                {
                    new LocalLudoEngine.PlayerInfo { seat = 0, name = "Red",    isBot = false },
                    new LocalLudoEngine.PlayerInfo { seat = 2, name = "Yellow", isBot = false },
                };
            }
            return new[]
            {
                new LocalLudoEngine.PlayerInfo { seat = 0, name = "Red",    isBot = false },
                new LocalLudoEngine.PlayerInfo { seat = 1, name = "Green",  isBot = false },
                new LocalLudoEngine.PlayerInfo { seat = 2, name = "Yellow", isBot = false },
                new LocalLudoEngine.PlayerInfo { seat = 3, name = "Blue",   isBot = false },
            };
        }

        // ============================================================
        // GAME SCREEN
        // ============================================================

        private void StartMatch(LocalLudoEngine.PlayerInfo[] players)
        {
            // Hide menu banner before entering gameplay (avoid ads on top of board)
            if (ServiceLocator.TryGet<AdManager>(out var ad))
            {
                ad.HideBanner();
                ad.SetGameplayLocked(true);   // suppress interstitials mid-match
            }
            _players = players;
            _engine = new LocalLudoEngine(players);
            ShowGame();
        }

        private void ShowGame()
        {
            _screen = ScreenKind.Game;
            DestroyIfNotNull(ref _menuRoot);
            DestroyIfNotNull(ref _resultRoot);
            DestroyIfNotNull(ref _gameRoot);

            _gameRoot = UI("Game", _canvas.transform);
            var rt = (RectTransform)_gameRoot.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            AddImage(_gameRoot, new Color(0.07f, 0.10f, 0.14f));

            BuildBoard();
            BuildHud();
            RefreshAll();
            MaybeStartBotTurn();
        }

        private void BuildBoard()
        {
            _boardRoot = UI("Board", _gameRoot.transform);
            _boardRect = (RectTransform)_boardRoot.transform;
            _boardRect.anchorMin = _boardRect.anchorMax = new Vector2(0.5f, 0.5f);
            _boardRect.pivot = new Vector2(0.5f, 0.5f);

            float refSize = 1000f;            // square
            _boardRect.sizeDelta = new Vector2(refSize, refSize);
            _boardRect.anchoredPosition = new Vector2(0, 80);
            AddImage(_boardRoot, new Color(0.95f, 0.95f, 0.92f));

            int g = BoardCells.GridSize;
            _cellSize = refSize / g;
            _cellImages = new Image[g, g];

            // Pass 1 — corners (bases) and the seat-arm rows.
            for (int x = 0; x < g; x++)
            for (int y = 0; y < g; y++)
            {
                var cellGO = UI($"c_{x}_{y}", _boardRoot.transform);
                var crt = (RectTransform)cellGO.transform;
                crt.anchorMin = crt.anchorMax = new Vector2(0, 1);
                crt.pivot = new Vector2(0, 1);
                crt.sizeDelta = new Vector2(_cellSize, _cellSize);
                crt.anchoredPosition = new Vector2(x * _cellSize, -y * _cellSize);

                var img = AddImage(cellGO, ColorForCell(x, y));
                _cellImages[x, y] = img;

                // outline via child
                var border = UI("b", cellGO.transform);
                var brt = (RectTransform)border.transform;
                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                brt.offsetMin = new Vector2(1, 1); brt.offsetMax = new Vector2(-1, -1);
                AddImage(border, new Color(0, 0, 0, 0.06f));
            }

            // Mark base "pads" (large 6x6 colored squares with 4 white circles inside)
            DrawBasePad(0, BoardCells.SeatColors[0], 0,  0);   // Red top-left
            DrawBasePad(1, BoardCells.SeatColors[1], 9,  0);   // Green top-right
            DrawBasePad(2, BoardCells.SeatColors[2], 9,  9);   // Yellow bottom-right
            DrawBasePad(3, BoardCells.SeatColors[3], 0,  9);   // Blue bottom-left

            // Center 3x3 (rows 6..8, cols 6..8) — color the four triangles by seat.
            // We approximate by recoloring each of the 9 center cells.
            ColorCell(6, 7, BoardCells.SeatColors[0]); // pointing right (red home arrow)
            ColorCell(7, 6, BoardCells.SeatColors[1]); // pointing down (green home arrow)
            ColorCell(8, 7, BoardCells.SeatColors[2]);
            ColorCell(7, 8, BoardCells.SeatColors[3]);
            ColorCell(7, 7, new Color(0.85f, 0.85f, 0.85f)); // finish

            // Color home columns
            for (int s = 0; s < 4; s++)
            {
                foreach (var c in BoardCells.HomeColumn[s])
                    ColorCell(c.x, c.y, BoardCells.SeatColors[s]);
            }

            // Color start cells (special tint) + safe cells (light grey star)
            for (int s = 0; s < 4; s++)
            {
                var sc = BoardCells.Track[BoardCells.StartCell[s]];
                ColorCell(sc.x, sc.y, BoardCells.SeatColors[s] * 0.85f + Color.white * 0.15f);
            }

            // Build token views
            _tokenViews = new GameObject[LocalLudoEngine.SeatCount][];
            for (int s = 0; s < LocalLudoEngine.SeatCount; s++)
            {
                _tokenViews[s] = new GameObject[LocalLudoEngine.TokensPerSeat];
                for (int i = 0; i < LocalLudoEngine.TokensPerSeat; i++)
                    _tokenViews[s][i] = BuildToken(s, i);
            }
        }

        private void DrawBasePad(int seat, Color col, int gx, int gy)
        {
            // recolor the 6x6 corner
            for (int x = 0; x < 6; x++)
            for (int y = 0; y < 6; y++)
                ColorCell(gx + x, gy + y, col);

            // inner 4x4 white pad with the 4 starting slots — done by coloring inner cells white
            for (int x = 1; x < 5; x++)
            for (int y = 1; y < 5; y++)
                ColorCell(gx + x, gy + y, Color.white);
        }

        private void ColorCell(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= BoardCells.GridSize || y >= BoardCells.GridSize) return;
            if (_cellImages[x, y] != null) _cellImages[x, y].color = c;
        }

        private Color ColorForCell(int x, int y)
        {
            // Default off-white; the colored overlays happen later.
            return new Color(0.97f, 0.97f, 0.95f);
        }

        private GameObject BuildToken(int seat, int tokenIndex)
        {
            var go = UI($"tk_{seat}_{tokenIndex}", _boardRoot.transform);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_cellSize * 0.7f, _cellSize * 0.7f);

            var bg = go.AddComponent<Image>();
            bg.sprite = _circleSprite;
            bg.color = BoardCells.SeatColors[seat];
            bg.raycastTarget = true;

            // Inner white dot
            var inner = UI("inner", go.transform);
            var irt = (RectTransform)inner.transform;
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(_cellSize * 0.18f, _cellSize * 0.18f);
            irt.offsetMax = new Vector2(-_cellSize * 0.18f, -_cellSize * 0.18f);
            var innerImg = inner.AddComponent<Image>();
            innerImg.sprite = _circleSprite;
            innerImg.color = new Color(1, 1, 1, 0.85f);
            innerImg.raycastTarget = false;

            var btn = go.AddComponent<Button>();
            int s = seat, ti = tokenIndex;
            btn.onClick.AddListener(() => OnTokenTapped(s, ti));

            return go;
        }

        private void BuildHud()
        {
            // Status banner (top)
            var statusGO = UI("Status", _gameRoot.transform);
            var srt = (RectTransform)statusGO.transform;
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1);
            srt.sizeDelta = new Vector2(0, 200);
            srt.anchoredPosition = new Vector2(0, 0);
            AddImage(statusGO, new Color(0, 0, 0, 0.35f));
            var stxtGO = UI("t", statusGO.transform);
            var trt2 = (RectTransform)stxtGO.transform;
            trt2.anchorMin = Vector2.zero; trt2.anchorMax = Vector2.one;
            trt2.offsetMin = Vector2.zero; trt2.offsetMax = Vector2.zero;
            _statusText = AddText(stxtGO, "", 60, Color.white);

            // Dice display + roll button (bottom)
            var bottomGO = UI("Bottom", _gameRoot.transform);
            var brt = (RectTransform)bottomGO.transform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.sizeDelta = new Vector2(0, 380);
            brt.anchoredPosition = new Vector2(0, 0);
            AddImage(bottomGO, new Color(0, 0, 0, 0.35f));

            // Dice
            var diceGO = UI("Dice", bottomGO.transform);
            var drt = (RectTransform)diceGO.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(220, 220);
            drt.anchoredPosition = new Vector2(-280, 0);
            var diceImg = diceGO.AddComponent<Image>();
            diceImg.color = Color.white;
            diceImg.sprite = _squareSprite;
            diceImg.raycastTarget = false;
            var diceTxtGO = UI("dt", diceGO.transform);
            var dtrt = (RectTransform)diceTxtGO.transform;
            dtrt.anchorMin = Vector2.zero; dtrt.anchorMax = Vector2.one;
            dtrt.offsetMin = Vector2.zero; dtrt.offsetMax = Vector2.zero;
            _diceText = AddText(diceTxtGO, "-", 140, new Color(0.1f, 0.1f, 0.1f));

            // Roll button
            _rollBtn = MakeButton(bottomGO.transform, "ROLL",
                new Vector2(360, 200),
                new Vector2(180, 0),
                new Color(0.18f, 0.45f, 0.92f),
                OnRollClicked);

            // Back button
            MakeButton(_gameRoot.transform, "Menu",
                new Vector2(180, 100),
                new Vector2(-460, 740),
                new Color(0.4f, 0.4f, 0.4f),
                ShowMenu);
        }

        // ============================================================
        // GAME LOOP
        // ============================================================

        private void OnRollClicked()
        {
            if (_busy || _engine == null || _engine.Status != "active") return;
            var current = _engine.Players.Find(p => p.seat == _engine.Turn.seat);
            if (current == null || current.isBot) return;            // not human's turn
            if (_engine.Turn.dice != null) return;                   // already rolled

            StartCoroutine(DoRollSequence(false));
        }

        private void OnTokenTapped(int seat, int tokenIndex)
        {
            if (_busy || _engine == null || _engine.Status != "active") return;
            if (_engine.Turn.seat != seat) return;
            var current = _engine.Players.Find(p => p.seat == seat);
            if (current == null || current.isBot) return;
            if (_engine.Turn.dice == null || !_engine.Turn.mustMove) return;
            if (!_engine.CanMove(seat, tokenIndex, _engine.Turn.dice.Value)) return;

            StartCoroutine(DoMoveSequence(tokenIndex));
        }

        private IEnumerator DoRollSequence(bool isBot)
        {
            _busy = true;
            // little spin animation
            for (int i = 0; i < 6; i++)
            {
                _diceText.text = Random.Range(1, 7).ToString();
                yield return new WaitForSeconds(0.05f);
            }

            var events = _engine.Roll();
            ApplyEvents(events);
            RefreshAll();

            // If still active and no immediate move pending (turn passed by NoMove/TripleSix), continue.
            _busy = false;

            if (_engine.Status == "finished") { ShowResult(); yield break; }

            // Bot path: if move is required and seat is bot, auto-move.
            yield return new WaitForSeconds(0.4f);
            MaybeStartBotTurn();
        }

        private IEnumerator DoMoveSequence(int tokenIndex)
        {
            _busy = true;
            var events = _engine.Move(tokenIndex);
            ApplyEvents(events);
            RefreshAll();
            yield return new WaitForSeconds(0.25f);
            _busy = false;

            if (_engine.Status == "finished") { ShowResult(); yield break; }
            MaybeStartBotTurn();
        }

        private void MaybeStartBotTurn()
        {
            if (_engine == null || _engine.Status != "active") return;
            var current = _engine.Players.Find(p => p.seat == _engine.Turn.seat);
            if (current == null || !current.isBot) return;
            StartCoroutine(BotTurn());
        }

        private IEnumerator BotTurn()
        {
            _busy = true;
            yield return new WaitForSeconds(0.6f);

            // Roll
            for (int i = 0; i < 6; i++)
            {
                _diceText.text = Random.Range(1, 7).ToString();
                yield return new WaitForSeconds(0.05f);
            }
            var rollEvents = _engine.Roll();
            ApplyEvents(rollEvents);
            RefreshAll();

            if (_engine.Status == "finished") { _busy = false; ShowResult(); yield break; }

            // Move if required
            if (_engine.Turn.mustMove && _engine.Turn.dice.HasValue)
            {
                yield return new WaitForSeconds(0.7f);
                int idx = _engine.PickBotToken(_engine.Turn.seat, _engine.Turn.dice.Value);
                if (idx >= 0)
                {
                    var moveEvents = _engine.Move(idx);
                    ApplyEvents(moveEvents);
                    RefreshAll();
                }
            }

            _busy = false;
            if (_engine.Status == "finished") { ShowResult(); yield break; }
            yield return new WaitForSeconds(0.3f);
            MaybeStartBotTurn();
        }

        private void ApplyEvents(List<LocalLudoEngine.Event> events)
        {
            // Most state is already mutated inside the engine; events here are for VFX / sound hooks.
            // Stub: we just refresh visuals from engine state in RefreshAll().
            for (int i = 0; i < events.Count; i++)
            {
                // Future: play dice/move/kill SFX based on events[i].kind
            }
        }

        // ============================================================
        // RENDERING
        // ============================================================

        private void RefreshAll()
        {
            // Token positions
            for (int s = 0; s < LocalLudoEngine.SeatCount; s++)
            {
                for (int i = 0; i < LocalLudoEngine.TokensPerSeat; i++)
                {
                    var t = _engine.Tokens[s][i];
                    Vector2 pos = TokenWorldPos(s, i, t);
                    var rt = (RectTransform)_tokenViews[s][i].transform;
                    rt.anchoredPosition = pos;

                    var btn = _tokenViews[s][i].GetComponent<Button>();
                    bool myTurn = _engine.Turn.seat == s
                                  && _engine.Turn.dice.HasValue
                                  && _engine.Turn.mustMove;
                    bool currentIsHuman = !(_engine.Players.Find(p => p.seat == s)?.isBot ?? false);
                    bool canMove = myTurn && currentIsHuman && _engine.CanMove(s, i, _engine.Turn.dice ?? 0);
                    btn.interactable = canMove;

                    // visual emphasis on movable tokens
                    var img = _tokenViews[s][i].GetComponent<Image>();
                    img.color = canMove ? BoardCells.SeatColors[s] : BoardCells.SeatColors[s] * (t.kind == LocalLudoEngine.TokenStateKind.Finished ? 0.5f : 1f);
                    rt.localScale = canMove ? Vector3.one * 1.1f : Vector3.one;
                }
            }

            // Dice / status text
            var current = _engine.Players.Find(p => p.seat == _engine.Turn.seat);
            string who = current?.name ?? "?";
            string color = SeatName(_engine.Turn.seat);
            if (_engine.Status == "finished")
            {
                _statusText.text = $"Winner: {SeatName(_engine.WinnerSeat ?? 0)}";
            }
            else
            {
                if (_engine.Turn.dice == null)
                    _statusText.text = $"{who} ({color}) — Roll!";
                else if (_engine.Turn.mustMove)
                    _statusText.text = $"{who} ({color}) — Tap a token";
                else
                    _statusText.text = $"{who} ({color})";
            }
            _diceText.text = _engine.Turn.dice?.ToString() ?? "-";

            // Roll button
            bool rollOk = _engine.Status == "active"
                          && _engine.Turn.dice == null
                          && current != null && !current.isBot;
            _rollBtn.interactable = rollOk;
        }

        private Vector2 TokenWorldPos(int seat, int tokenIndex, LocalLudoEngine.TokenState t)
        {
            Vector2 cell;
            switch (t.kind)
            {
                case LocalLudoEngine.TokenStateKind.Home:
                    cell = BoardCells.BaseSlots[seat][tokenIndex];
                    break;
                case LocalLudoEngine.TokenStateKind.Track:
                    var v = BoardCells.Track[Mathf.Clamp(t.track, 0, BoardCells.TrackLength - 1)];
                    cell = new Vector2(v.x + 0.5f, v.y + 0.5f);
                    break;
                case LocalLudoEngine.TokenStateKind.HomePath:
                    var hv = BoardCells.HomeColumn[seat][Mathf.Clamp(t.home, 0, BoardCells.HomePathLength - 1)];
                    cell = new Vector2(hv.x + 0.5f, hv.y + 0.5f);
                    break;
                case LocalLudoEngine.TokenStateKind.Finished:
                default:
                    // stack at center, slight offset by tokenIndex so they don't overlap perfectly
                    cell = BoardCells.FinishCenter + new Vector2((tokenIndex - 1.5f) * 0.25f, 0);
                    break;
            }
            float x = cell.x * _cellSize;
            float y = -cell.y * _cellSize;
            return new Vector2(x, y);
        }

        private static string SeatName(int seat)
        {
            switch (seat)
            {
                case 0: return "Red";
                case 1: return "Green";
                case 2: return "Yellow";
                case 3: return "Blue";
            }
            return "?";
        }

        // ============================================================
        // RESULT SCREEN
        // ============================================================

        private int _coinsEarned;
        private bool _rewardClaimed;
        private Text _coinsLabel;

        private void ShowResult()
        {
            _screen = ScreenKind.Result;
            DestroyIfNotNull(ref _resultRoot);
            _resultRoot = UI("Result", _canvas.transform);
            var rt = (RectTransform)_resultRoot.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            AddImage(_resultRoot, new Color(0, 0, 0, 0.75f));

            var winner = _engine.WinnerSeat ?? 0;
            var winnerName = _engine.Players.Find(p => p.seat == winner)?.name ?? SeatName(winner);

            var titleGO = UI("Title", _resultRoot.transform);
            var trt = (RectTransform)titleGO.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(900, 180);
            trt.anchoredPosition = new Vector2(0, 260);
            AddText(titleGO, "GAME OVER", 120, Color.white);

            var txtGO = UI("Winner", _resultRoot.transform);
            var wrt = (RectTransform)txtGO.transform;
            wrt.anchorMin = wrt.anchorMax = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(900, 120);
            wrt.anchoredPosition = new Vector2(0, 100);
            _resultText = AddText(txtGO, $"{winnerName} ({SeatName(winner)}) wins!", 72, BoardCells.SeatColors[winner]);

            // Coins reward (visual only — wire to backend later)
            _coinsEarned = (winner == 0) ? 100 : 25;
            _rewardClaimed = false;
            var coinsGO = UI("Coins", _resultRoot.transform);
            var crt = (RectTransform)coinsGO.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(900, 80);
            crt.anchoredPosition = new Vector2(0, 0);
            _coinsLabel = AddText(coinsGO, $"+{_coinsEarned} coins", 56, new Color(1f, 0.85f, 0.2f));

            // Rewarded ad: double your coins
            MakeButton(_resultRoot.transform, "Watch Ad → 2x Coins",
                new Vector2(620, 140), new Vector2(0, -120),
                new Color(1f, 0.55f, 0.05f),
                OnDoubleCoinsClicked);

            MakeButton(_resultRoot.transform, "Play Again", new Vector2(500, 160), new Vector2(0, -300), new Color(0.20f, 0.72f, 0.30f), OnPlayAgainClicked);
            MakeButton(_resultRoot.transform, "Main Menu",  new Vector2(500, 160), new Vector2(0, -480), new Color(0.4f, 0.4f, 0.4f), OnMainMenuClicked);

            // Re-enable interstitials now that gameplay is done.
            if (ServiceLocator.TryGet<AdManager>(out var ad)) ad.SetGameplayLocked(false);
        }

        private void OnDoubleCoinsClicked()
        {
            if (_rewardClaimed) return;
            if (!ServiceLocator.TryGet<AdManager>(out var ad))
            {
                // No ad provider → grant a smaller bonus so the button still feels useful.
                _rewardClaimed = true;
                _coinsEarned += _coinsEarned;
                _coinsLabel.text = $"+{_coinsEarned} coins";
                return;
            }
            ad.ShowRewarded(success =>
            {
                if (!success) return;
                _rewardClaimed = true;
                _coinsEarned *= 2;
                if (_coinsLabel != null) _coinsLabel.text = $"+{_coinsEarned} coins";
            });
        }

        private void OnPlayAgainClicked()
        {
            // Show interstitial between matches (skipped if not loaded — never blocks).
            if (ServiceLocator.TryGet<AdManager>(out var ad))
                ad.ShowInterstitial(() => StartMatch(_players));
            else
                StartMatch(_players);
        }

        private void OnMainMenuClicked()
        {
            if (ServiceLocator.TryGet<AdManager>(out var ad))
                ad.ShowInterstitial(ShowMenu);
            else
                ShowMenu();
        }


        // ============================================================
        // UTIL
        // ============================================================

        private static void DestroyIfNotNull(ref GameObject go)
        {
            if (go != null) { Destroy(go); go = null; }
        }
    }
}

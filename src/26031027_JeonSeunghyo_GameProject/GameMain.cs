// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vortice.Mathematics;
using Vortice.DirectWrite;

class GameMain : G2AppBase
{
	public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
	public override string GameName => GameGlobal.GameName;

	enum GameScene  { Title, Play }
	enum Phase      { Watching, Choosing, Result, StageClear, AllClear }
	enum PlayerAnim { Idle, Run, Attack }

	// ── 화면을 가로지르는 몬스터 ─────────────────────────────────────────────
	class FlightMob
	{
		public int   TypeIdx;
		public float SX, SY, EX, EY;
		public float PathLen, NX, NY, PerpX, PerpY;
		public float Speed, WaveAmp, WaveFreq;
		public float SpawnAt, Elapsed;
		public bool  Active, Exited;
		public int   Frame; public float AnimT;
		public bool  FlipX;
		public float Scale = 1.0f;
	}

	// ── 몬스터 종류 (0=고블린, 1=눈알, 2=버섯, 3=해골) ──────────────────────
	static readonly string[] sFlyPath = {
		"resource/image/enemy/monsters_creatures_fantasy/goblin/Run.png",
		"resource/image/enemy/monsters_creatures_fantasy/flying_eye/Flight.png",
		"resource/image/enemy/monsters_creatures_fantasy/mushroom/Run.png",
		"resource/image/enemy/monsters_creatures_fantasy/skeleton/Walk.png",
	};
	static readonly string[] sIdlePath = {
		"resource/image/enemy/monsters_creatures_fantasy/goblin/Idle.png",
		"resource/image/enemy/monsters_creatures_fantasy/flying_eye/Flight.png",
		"resource/image/enemy/monsters_creatures_fantasy/mushroom/Idle.png",
		"resource/image/enemy/monsters_creatures_fantasy/skeleton/Idle.png",
	};
	static readonly string[] sDeadPath = {
		"resource/image/enemy/monsters_creatures_fantasy/goblin/Death.png",
		"resource/image/enemy/monsters_creatures_fantasy/flying_eye/Death.png",
		"resource/image/enemy/monsters_creatures_fantasy/mushroom/Death.png",
		"resource/image/enemy/monsters_creatures_fantasy/skeleton/Death.png",
	};
	static readonly int[] sFlyFrames  = { 8, 8, 8, 4 };
	static readonly int[] sIdleFrames = { 4, 8, 4, 4 };
	static readonly string[] sNames   = { "고블린", "눈알", "버섯", "해골" };

	// Shadow 트릭 (L→R, 경로 1280px 기준)
	const float ShadowLargeSpd = 420f;  // 대형: spawnAt+3.05s 탈출
	const float ShadowSmallSpd = 370f;  // 소형: spawnAt+3.56s 탈출 (뒤에 숨음)
	// FastFinish 트릭
	const float FFSlowSpd  = 175f;      // 느린 몬스터: +7.31s 탈출
	const float FFFastSpd  = 720f;      // 빠른 몬스터: 느린 것보다 0.3s 뒤 탈출
	const float FFPathLen  = 1280f;

	// 플레이어 시작 위치
	const float PX0 = 70f, PY0 = 490f;

	// ── 텍스처 ────────────────────────────────────────────────────────────────
	G2Texture?[] _mobFly  = new G2Texture?[4];
	G2Texture?[] _mobIdle = new G2Texture?[4];
	G2Texture?[] _mobDead = new G2Texture?[4];

	G2Texture? _titleBg, _playBg1, _playBg2;
	G2Texture? _btnNewGame, _btnContinue;
	G2Texture? _playerIdle, _playerRun, _playerAttack;
	G2Texture? _btnSettings, _uiFrame;
	G2Texture? _btnMainMenu, _btnGameOptions, _btnQuitGame;

	// ── 폰트 & 사운드 ─────────────────────────────────────────────────────────
	G2Font? _titleFont, _hudFont, _bigFont, _nameFont;
	G2AudioSound? _titleBgm, _battleBgm, _btnClick;

	// ── 입력 ─────────────────────────────────────────────────────────────────
	bool _prevClick, _prevSpace, _showSettings;
	GameScene _scene = GameScene.Title;

	// ── 게임 상태 ─────────────────────────────────────────────────────────────
	Phase _phase = Phase.Watching;
	int   _round = 1, _lives = 3, _stage = 1;

	readonly List<FlightMob> _mobs = new();
	float _roundTimer     = 0f;
	int   _lastExitedType = -1;
	readonly List<int> _appearedOrder = new();

	// 선택 화면
	int[]  _choices     = Array.Empty<int>();
	int    _choiceFrame = 0;
	float  _choiceAnimT = 0f;

	// 결과
	bool  _resultCorrect  = false;
	float _resultTimer    = 0f;
	int   _deadChoiceIdx  = -1;
	int   _deadFrame      = 0;
	float _deadAnimT      = 0f;

	// 플레이어 상태머신
	PlayerAnim _pAnim   = PlayerAnim.Idle;
	float _pX  = PX0, _pY  = PY0;   // 현재 위치 (중심)
	float _pTX = PX0, _pTY = PY0;   // 목표 위치
	bool  _pFaceRight = true;
	int   _pFrame = 0;
	float _pAnimT = 0f;
	int   _pClickIdx   = -1;          // 클릭한 선택지 인덱스
	bool  _pClickRight = false;       // 클릭한 선택지가 정답인가

	// 암전
	bool  _black; float _blackTimer;
	static readonly Color4 BgDark  = new(0.03f, 0.02f, 0.08f, 1f);
	static readonly Color4 BgBlack = new(0f, 0f, 0f, 1f);

	readonly Random _rng = new();

	// ═══════════════════════════════════════════════════════════════════════════
	protected override void Initialize()
	{
		for (int i = 0; i < 4; i++)
		{
			_mobFly[i]  = new G2Texture(sFlyPath[i]);
			_mobIdle[i] = new G2Texture(sIdlePath[i]);
			_mobDead[i] = new G2Texture(sDeadPath[i]);
		}

		_titleBg   = new G2Texture("resource/image/background/title_bg.png");
		_playBg1   = new G2Texture("resource/image/background/battle_forest.png");
		_playBg2   = new G2Texture("resource/image/background/battle_dungeon.png");
		_btnNewGame  = new G2Texture("resource/image/ui_menu/game_menu/1x/Asset 7 - 1080p.png");
		_btnContinue = new G2Texture("resource/image/ui_menu/game_menu/1x/Asset 12 - 1080p.png");

		string pBase = "resource/image/player/colour1/nooutline/120x80_pngsheets/";
		_playerIdle   = new G2Texture(pBase + "_Idle.png");
		_playerRun    = new G2Texture(pBase + "_Run.png");
		_playerAttack = new G2Texture(pBase + "_Attack.png");

		_btnSettings    = new G2Texture("resource/image/ui_menu/game_menu/1x/Asset 3 - 1080p.png");
		_uiFrame        = new G2Texture("resource/image/ui_menu/game_menu/1x/Asset 1 - 1080p.png");
		_btnMainMenu    = new G2Texture("resource/image/ui_menu/game_menu/1x/Asset 11 - 1080p.png");
		_btnGameOptions = new G2Texture("resource/image/ui_menu/game_menu/1x/Asset 13 - 1080p.png");
		_btnQuitGame    = new G2Texture("resource/image/ui_menu/game_menu/1x/Asset 5 - 1080p.png");

		_titleFont = new G2Font("Arial", 72, FontWeight.Heavy, Vortice.DirectWrite.FontStyle.Normal, TextAlignment.Center, ParagraphAlignment.Center);
		_hudFont   = new G2Font("Arial", 20, FontWeight.Bold,  Vortice.DirectWrite.FontStyle.Normal, TextAlignment.Leading, ParagraphAlignment.Near);
		_bigFont   = new G2Font("Arial", 40, FontWeight.Heavy, Vortice.DirectWrite.FontStyle.Normal, TextAlignment.Center, ParagraphAlignment.Center);
		_nameFont  = new G2Font("Arial", 16, FontWeight.Bold,  Vortice.DirectWrite.FontStyle.Normal, TextAlignment.Center, ParagraphAlignment.Center);

		_titleBgm  = new G2AudioSound("resource/audio/title_bgm.wav");
		_battleBgm = new G2AudioSound("resource/audio/battle_bgm.wav");
		_btnClick  = new G2AudioSound("resource/audio/btn_click.wav");

		this.ClearColor = BgDark;
		_titleBgm!.Play(true);
	}

	// ═══════════════════════════════════════════════════════════════════════════
	protected override void Update()
	{
		float dt = (float)DeltaTime;

		if (_black) { _blackTimer -= dt; if (_blackTimer <= 0f) DoReset(); return; }

		bool down  = Input.IsButtonDown(System.Windows.Forms.MouseButtons.Left);
		bool click = down && !_prevClick;
		_prevClick = down;

		bool spDown = Input.IsKeyDown(System.Windows.Forms.Keys.Space);
		bool space  = spDown && !_prevSpace;
		_prevSpace  = spDown;

		if (_scene == GameScene.Title)
		{
			if (click || space) { _btnClick!.Play(); StartGame(); }
			return;
		}

		if (_phase == Phase.StageClear)
		{
			if (click || space) { _stage = 2; _round = 1; BeginRound(); }
			return;
		}
		if (_phase == Phase.AllClear)
		{
			if (click || space) { GoToTitle(); }
			return;
		}

		// 플레이어 이동/애니 (항상 업데이트)
		TickPlayer(dt);

		// 세팅 버튼 (플레이어가 Idle일 때만)
		if (click && _pAnim == PlayerAnim.Idle)
		{
			float mx = Input.MousePosition.X, my = Input.MousePosition.Y;
			if (mx >= 855 && mx <= 945 && my >= 8 && my <= 40) { _btnClick!.Play(); _showSettings = !_showSettings; return; }
			if (_showSettings)
			{
				if (mx >= 340 && mx <= 620 && my >= 230 && my <= 276) { _btnClick!.Play(); GoToTitle(); return; }
				if (mx >= 340 && mx <= 620 && my >= 390 && my <= 444) { _btnClick!.Play(); Close(); }
				return;
			}
		}

		switch (_phase)
		{
			case Phase.Watching: TickWatching(dt);        break;
			case Phase.Choosing: TickChoosing(dt, click); break;
			case Phase.Result:   TickResult(dt);           break;
		}
	}

	// ── 게임 제어 ─────────────────────────────────────────────────────────────
	void StartGame()
	{
		_round = 1; _lives = 3; _stage = 1;
		_black = false; this.ClearColor = BgDark;
		_titleBgm!.Stop(); _battleBgm!.Play(true);
		_scene = GameScene.Play;
		BeginRound();
	}

	void GoToTitle()
	{
		_battleBgm!.Stop(); _titleBgm!.Play(true);
		_scene = GameScene.Title; _showSettings = false; _mobs.Clear();
	}

	void DoReset()
	{
		_black = false; this.ClearColor = BgDark;
		_round = 1; _lives = 3;
		BeginRound();
	}

	void BeginRound()
	{
		// 플레이어 시작 위치로 리셋
		_pAnim = PlayerAnim.Idle; _pX = PX0; _pY = PY0;
		_pFrame = 0; _pAnimT = 0f; _pFaceRight = true;
		_pClickIdx = -1;

		_phase = Phase.Watching;
		if (_stage == 1) GenerateWaveS1();
		else             GenerateWaveS2();
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// ── Stage 1 웨이브 ────────────────────────────────────────────────────────
	void GenerateWaveS1()
	{
		Reset();
		int numTypes = Math.Min(_round + 1, 4);
		var types = Enumerable.Range(0, 4).OrderBy(_ => _rng.Next()).Take(numTypes).ToArray();

		float baseSpd = 360f + _round * 40f;
		float spawnT  = 0.3f;

		foreach (int type in types)
		{
			float scale = 1.0f + (float)_rng.NextDouble() * 0.7f; // 1.0-1.7
			AddMob(type, spawnT, _rng.Next(6), baseSpd + _rng.Next(-60, 80),
			       _rng.Next(0, 3) * 70f, 1f + _rng.Next(0, 3), scale);
			spawnT += 0.5f + (float)_rng.NextDouble() * 0.8f;

			if (_round >= 3 && type != types.Last() && _rng.NextDouble() < 0.45)
			{
				AddMob(type, spawnT, (_rng.Next(6) + 2) % 6,
				       baseSpd + _rng.Next(0, 100), 60f, 2f,
				       0.85f + (float)_rng.NextDouble() * 0.7f);
				spawnT += 0.35f + (float)_rng.NextDouble() * 0.5f;
			}
		}

		if (_round == 5)
		{
			foreach (var m in _mobs) m.Speed = Math.Min(m.Speed * 1.35f, 800f);
			for (int i = 0; i < 2; i++)
			{
				AddMob(types[_rng.Next(types.Length - 1)], spawnT,
				       _rng.Next(6), 700f + _rng.Next(80), 90f, 3f, 1.1f);
				spawnT += 0.25f + (float)_rng.NextDouble() * 0.3f;
			}
		}
	}

	// ── Stage 2 웨이브 ────────────────────────────────────────────────────────
	void GenerateWaveS2()
	{
		Reset();
		int numTypes = Math.Min(_round + 1, 4);
		var types = Enumerable.Range(0, 4).OrderBy(_ => _rng.Next()).Take(numTypes).ToArray();

		float baseSpd = 400f + _round * 30f;
		float spawnT  = 0.3f;

		int normalCnt = Math.Max(0, numTypes - 2);
		for (int i = 0; i < normalCnt; i++)
		{
			float scale = 0.7f + (float)_rng.NextDouble() * 1.1f;
			AddMob(types[i], spawnT, _rng.Next(6), baseSpd + _rng.Next(-80, 80), 50f, 2f, scale);
			spawnT += 0.8f + (float)_rng.NextDouble() * 0.6f;
		}

		int tA = types[Math.Max(0, numTypes - 2)];
		int tB = types[numTypes - 1];

		switch (_round)
		{
			case 1:
				AddMobFixed(tA, spawnT,        -160f, Rnd(160,400), 1120f, Rnd(160,400), 430f, 0f,  0f,  1.9f);
				AddMobFixed(tB, spawnT + 0.7f, -160f, Rnd(160,400), 1120f, Rnd(160,400), 340f, 50f, 1.5f, 0.55f);
				break;
			case 2:
				AddShadowTrick(tA, tB, spawnT);
				break;
			case 3:
				spawnT = 0.3f;
				AddFastFinishTrick(tA, tB, spawnT);
				break;
			case 4:
				spawnT = 0.3f;
				AddFastFinishTrick(types[0], types[1], spawnT);
				float afterFF4 = spawnT + FFPathLen / FFSlowSpd + 1.5f;
				AddShadowTrick(types[2], types[3], afterFF4);
				break;
			case 5:
				spawnT = 0.3f;
				AddFastFinishTrick(types[0], types[1], spawnT);
				float midT = spawnT + FFPathLen / FFSlowSpd * 0.4f;
				AddMob(types[2], midT,        _rng.Next(6), 650f, 80f, 3f, 1.2f);
				AddMob(types[2], midT + 0.6f, _rng.Next(6), 620f, 70f, 2f, 0.8f);
				float afterFF5 = spawnT + FFPathLen / FFSlowSpd + 1.5f;
				AddShadowTrick(types[2], types[3], afterFF5);
				break;
		}
	}

	// ── Shadow 기믹 ───────────────────────────────────────────────────────────
	void AddShadowTrick(int tA, int tB, float spawnAt)
	{
		float sy = Rnd(160, 400);
		AddMobFixed(tA, spawnAt,        -160f, sy,      1120f, sy,      ShadowLargeSpd, 0f, 0f, 2.3f);
		AddMobFixed(tB, spawnAt + 0.1f, -160f, sy + 12f, 1120f, sy + 12f, ShadowSmallSpd, 0f, 0f, 0.4f);
	}

	// ── FastFinish 기믹 ───────────────────────────────────────────────────────
	void AddFastFinishTrick(int tA, int tB, float spawnAt)
	{
		float sy  = Rnd(150, 430);
		float sy2 = sy + Rnd(-50, 50);
		float bSpawnAt = spawnAt + FFPathLen / FFSlowSpd + 0.3f - FFPathLen / FFFastSpd;
		AddMobFixed(tA, spawnAt,  -160f, sy,  1120f, sy,  FFSlowSpd, 0f,  0f,  1.25f);
		AddMobFixed(tB, bSpawnAt, -160f, sy2, 1120f, sy2, FFFastSpd, 30f, 2f,  0.8f);
	}

	// ── 공통 헬퍼 ────────────────────────────────────────────────────────────
	int  Rnd(int lo, int hi) => _rng.Next(lo, hi);
	void Reset() { _mobs.Clear(); _appearedOrder.Clear(); _lastExitedType = -1; _roundTimer = 0f; }

	void AddMob(int typeIdx, float spawnAt, int pathId, float speed,
	            float wAmp, float wFreq, float scale = 1.0f)
	{
		float sx, sy, ex, ey;
		int rY = Rnd(80, 520), rX = Rnd(80, 880);
		switch (pathId % 6)
		{
			case 0:  sx=-160f; sy=rY;          ex=1120f; ey=rY+Rnd(-120,120); break;
			case 1:  sx=1120f; sy=rY;          ex=-160f; ey=rY+Rnd(-120,120); break;
			case 2:  sx=rX;    sy=-160f;        ex=rX+Rnd(-120,120); ey=800f; break;
			case 3:  sx=-160f; sy=Rnd(20,160); ex=1120f; ey=Rnd(440,700); break;
			case 4:  sx=1120f; sy=Rnd(20,160); ex=-160f; ey=Rnd(440,700); break;
			default: sx=rX;    sy=800f;         ex=rX+Rnd(-120,120); ey=-160f; break;
		}
		AddMobFixed(typeIdx, spawnAt, sx, sy, ex, ey, speed, wAmp, wFreq, scale);
	}

	void AddMobFixed(int typeIdx, float spawnAt, float sx, float sy,
	                 float ex, float ey, float speed, float wAmp, float wFreq, float scale = 1.0f)
	{
		float dx = ex - sx, dy = ey - sy;
		float len = MathF.Sqrt(dx * dx + dy * dy);
		if (len < 1f) len = 1f;
		float nx = dx / len, ny = dy / len;
		_mobs.Add(new FlightMob {
			TypeIdx=typeIdx, SX=sx, SY=sy, EX=ex, EY=ey,
			PathLen=len, NX=nx, NY=ny, PerpX=-ny, PerpY=nx,
			Speed=speed, WaveAmp=wAmp, WaveFreq=wFreq,
			SpawnAt=spawnAt, FlipX=nx < -0.3f, Scale=scale,
		});
	}

	// ── Watching ──────────────────────────────────────────────────────────────
	void TickWatching(float dt)
	{
		_roundTimer += dt;

		foreach (var mob in _mobs)
		{
			if (mob.Exited) continue;
			if (!mob.Active) { if (_roundTimer < mob.SpawnAt) continue; mob.Active = true; }

			mob.Elapsed += dt;
			if (mob.Elapsed * mob.Speed / mob.PathLen >= 1.0f)
			{
				mob.Exited = true;
				_lastExitedType = mob.TypeIdx;
				_appearedOrder.Remove(mob.TypeIdx);
				_appearedOrder.Add(mob.TypeIdx);
				continue;
			}

			mob.AnimT += dt;
			if (mob.AnimT >= 0.1f) { mob.AnimT = 0f; mob.Frame = (mob.Frame + 1) % sFlyFrames[mob.TypeIdx]; }
		}

		if (_mobs.Count > 0 && _mobs.All(m => m.Exited))
		{
			_choices = _appearedOrder.OrderBy(_ => _rng.Next()).ToArray();
			_choiceFrame = 0; _choiceAnimT = 0f;
			_deadChoiceIdx = -1; _deadFrame = 0;
			_phase = Phase.Choosing;
		}
	}

	// ── Choosing ──────────────────────────────────────────────────────────────
	void TickChoosing(float dt, bool click)
	{
		_choiceAnimT += dt;
		if (_choiceAnimT >= 1f / 6f) { _choiceAnimT = 0f; _choiceFrame++; }

		// 플레이어가 이미 움직이는 중이면 입력 무시
		if (_pAnim != PlayerAnim.Idle) return;
		if (!click) return;

		float mx = Input.MousePosition.X, my = Input.MousePosition.Y;
		var rects = GetChoiceRects();

		for (int i = 0; i < _choices.Length; i++)
		{
			var r = rects[i];
			if (mx < r.X || mx > r.X + r.Width || my < r.Y || my > r.Y + r.Height) continue;

			_btnClick!.Play();
			_pClickIdx   = i;
			_pClickRight = (_choices[i] == _lastExitedType);

			// 아이콘 바닥 중앙으로 달려가기
			_pTX = r.X + r.Width * 0.5f;
			_pTY = r.Y + r.Height - 20f;
			_pFaceRight = (_pTX >= _pX);
			_pAnim  = PlayerAnim.Run;
			_pFrame = 0; _pAnimT = 0f;
			return;
		}
	}

	Rect[] GetChoiceRects()
	{
		int n = _choices.Length, size = 160, gap = 30;
		int total = n * size + (n - 1) * gap;
		int startX = (960 - total) / 2;
		var rects = new Rect[n];
		for (int i = 0; i < n; i++)
			rects[i] = new Rect(startX + i * (size + gap), 240, size, size);
		return rects;
	}

	// ── 플레이어 상태머신 ─────────────────────────────────────────────────────
	void TickPlayer(float dt)
	{
		switch (_pAnim)
		{
			case PlayerAnim.Idle:
				_pAnimT += dt;
				if (_pAnimT >= 1f / 10f) { _pAnimT = 0f; _pFrame = (_pFrame + 1) % 10; }
				break;

			case PlayerAnim.Run:
				// 이동 애니 (Run: 10프레임)
				_pAnimT += dt;
				if (_pAnimT >= 1f / 12f) { _pAnimT = 0f; _pFrame = (_pFrame + 1) % 10; }

				// 목표까지 이동
				float dx = _pTX - _pX, dy = _pTY - _pY;
				float dist = MathF.Sqrt(dx * dx + dy * dy);
				if (dist < 8f)
				{
					_pX = _pTX; _pY = _pTY;
					_pAnim = PlayerAnim.Attack;
					_pFrame = 0; _pAnimT = 0f;

					// 결과 확정
					_resultCorrect = _pClickRight;
					if (_resultCorrect)
					{
						_deadChoiceIdx = _pClickIdx; _deadFrame = 0; _deadAnimT = 0f;
						_resultTimer = 2.0f;
					}
					else
					{
						_lives--;
						_resultTimer = 1.6f;
					}
					_phase = Phase.Result;
				}
				else
				{
					float spd = 700f * dt;
					_pX += dx / dist * spd;
					_pY += dy / dist * spd;
				}
				break;

			case PlayerAnim.Attack:
				// Attack: 4프레임, 1회만
				_pAnimT += dt;
				if (_pAnimT >= 1f / 8f)
				{
					_pAnimT = 0f;
					if (_pFrame < 3) _pFrame++;
					else { _pAnim = PlayerAnim.Idle; _pFrame = 0; }
				}
				break;
		}
	}

	// ── Result ────────────────────────────────────────────────────────────────
	void TickResult(float dt)
	{
		// 죽는 애니 진행
		if (_deadChoiceIdx >= 0)
		{
			_deadAnimT += dt;
			if (_deadAnimT >= 1f / 8f) { _deadAnimT = 0f; if (_deadFrame < 3) _deadFrame++; }
		}

		_resultTimer -= dt;
		if (_resultTimer > 0f) return;

		if (!_resultCorrect && _lives <= 0)
		{
			_black = true; _blackTimer = 1.5f; this.ClearColor = BgBlack; return;
		}

		if (_resultCorrect)
		{
			_round++;
			if (_round > 5)
			{
				_phase = (_stage == 1) ? Phase.StageClear : Phase.AllClear;
				return;
			}
		}
		BeginRound();
	}

	// ═══════════════════════════════════════════════════════════════════════════
	protected override void Render()
	{
		if (_black) return;

		if (_scene == GameScene.Title)
		{
			_titleBg!.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 192, 108));
			_titleFont!.DrawText("IDLE QUEST", new Rect(0, 120, 960, 160), new Color4(1f, 0.88f, 0.2f, 1f));
			_btnNewGame!.Draw(new Rect(281, 400, 398, 70),  new Rect(0, 0, 199, 35));
			_btnContinue!.Draw(new Rect(302, 490, 356, 72), new Rect(0, 0, 178, 36));
			return;
		}

		(_stage == 1 ? _playBg1 : _playBg2)!.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 1536, 1024));

		if (_phase == Phase.StageClear)
		{
			_titleFont!.DrawText("STAGE 1 CLEAR!", new Rect(0, 160, 960, 160), new Color4(0.3f, 1f, 0.5f, 1f));
			_bigFont!.DrawText("STAGE 2 진입", new Rect(0, 340, 960, 60), new Color4(1f, 0.7f, 0.2f, 1f));
			_hudFont!.DrawText("클릭 또는 Space", new Rect(380, 420, 300, 28), new Color4(1f, 1f, 1f, 0.7f));
			RenderHUD(); return;
		}
		if (_phase == Phase.AllClear)
		{
			_titleFont!.DrawText("ALL CLEAR!", new Rect(0, 180, 960, 160), new Color4(1f, 0.9f, 0.1f, 1f));
			_hudFont!.DrawText("클릭 또는 Space 로 타이틀 복귀", new Rect(300, 410, 500, 30), new Color4(1f, 1f, 1f, 0.7f));
			RenderHUD(); return;
		}

		// ── 날아다니는 몬스터 (진행도 오름차순 → 앞선 몬스터가 위에 그려짐) ──
		var drawOrder = _mobs
			.Where(m => m.Active && !m.Exited)
			.OrderBy(m => m.Elapsed * m.Speed / m.PathLen);

		foreach (var mob in drawOrder)
		{
			float t  = mob.Elapsed * mob.Speed / mob.PathLen;
			float bx = mob.SX + mob.NX * mob.PathLen * t;
			float by = mob.SY + mob.NY * mob.PathLen * t;
			float wave = MathF.Sin(t * mob.WaveFreq * MathF.PI * 2f) * mob.WaveAmp;
			float cx = bx + mob.PerpX * wave;
			float cy = by + mob.PerpY * wave;

			int half = (int)(110 * mob.Scale);   // 기본 220px
			var dest = new Rect((int)(cx - half), (int)(cy - half), half * 2, half * 2);
			var src  = new Rect(mob.Frame * 150, 0, 150, 150);

			if (mob.FlipX)
			{
				this.RenderTarget.Transform = Matrix3x2.CreateScale(-1f, 1f, new Vector2(cx, 0));
				_mobFly[mob.TypeIdx]!.Draw(dest, src);
				this.RenderTarget.Transform = Matrix3x2.Identity;
			}
			else
			{
				_mobFly[mob.TypeIdx]!.Draw(dest, src);
			}
		}

		// ── 선택지 아이콘 ──────────────────────────────────────────────────────
		if (_phase == Phase.Choosing || _phase == Phase.Result)
		{
			_bigFont!.DrawText("마지막 몬스터는?", new Rect(0, 178, 960, 52),
				new Color4(1f, 0.95f, 0.3f, 1f));

			var rects = GetChoiceRects();
			for (int i = 0; i < _choices.Length; i++)
			{
				var r    = rects[i];
				int type = _choices[i];

				if (_phase == Phase.Result && _resultCorrect && i == _deadChoiceIdx)
					_mobDead[type]!.Draw(r, new Rect(_deadFrame * 150, 0, 150, 150));
				else
					_mobIdle[type]!.Draw(r, new Rect(_choiceFrame % sIdleFrames[type] * 150, 0, 150, 150));

				_nameFont!.DrawText(sNames[type],
					new Rect(r.X, r.Y + r.Height + 4, r.Width, 22),
					new Color4(1f, 1f, 1f, 0.9f));
			}

			if (_phase == Phase.Result)
			{
				var col = _resultCorrect ? new Color4(0.2f, 1f, 0.4f, 1f) : new Color4(1f, 0.3f, 0.3f, 1f);
				_bigFont!.DrawText(_resultCorrect ? "정확해요!" : "틀렸습니다!", new Rect(0, 470, 960, 55), col);
				if (!_resultCorrect && _lastExitedType >= 0)
					_nameFont!.DrawText($"정답: {sNames[_lastExitedType]}",
						new Rect(380, 530, 300, 22), new Color4(1f, 0.9f, 0.3f, 0.9f));
			}
		}

		if (_phase == Phase.Watching)
			_bigFont!.DrawText(_stage == 2 ? "집중하세요!" : "잘 보세요!",
				new Rect(0, 12, 960, 50), new Color4(1f, 1f, 1f, 0.85f));

		// ── 플레이어 (위치 추적) ───────────────────────────────────────────────
		RenderPlayer();
		RenderHUD();
	}

	void RenderPlayer()
	{
		G2Texture pTex = _pAnim switch {
			PlayerAnim.Run    => _playerRun!,
			PlayerAnim.Attack => _playerAttack!,
			_                 => _playerIdle!,
		};
		var pDest = new Rect((int)(_pX - 135), (int)(_pY - 90), 270, 180);
		var pSrc  = new Rect(_pFrame * 120, 0, 120, 80);

		if (!_pFaceRight)
		{
			this.RenderTarget.Transform = Matrix3x2.CreateScale(-1f, 1f, new Vector2(_pX, 0));
			pTex.Draw(pDest, pSrc);
			this.RenderTarget.Transform = Matrix3x2.Identity;
		}
		else
		{
			pTex.Draw(pDest, pSrc);
		}
	}

	void RenderHUD()
	{
		string rLabel = _round == 5 ? "Round 5  [BOSS]" : $"Round {_round} / 5";
		string sLabel = $"STAGE {_stage}";
		var    rCol   = _round == 5 ? new Color4(1f, 0.3f, 0.3f, 1f) : new Color4(1f, 0.88f, 0.2f, 1f);
		var    sCol   = _stage == 2  ? new Color4(0.6f, 0.4f, 1f, 1f) : new Color4(0.5f, 0.9f, 1f, 1f);

		_hudFont!.DrawText(sLabel,             new Rect(840, 570, 120, 24), sCol);
		_hudFont!.DrawText(rLabel,             new Rect(750, 596, 210, 24), rCol);
		_hudFont!.DrawText($"Lives  {_lives}", new Rect(850, 618, 140, 20), new Color4(1f, 0.5f, 0.5f, 1f));

		_btnSettings!.Draw(new Rect(855, 8, 90, 32), new Rect(0, 0, 136, 49));
		if (_showSettings)
		{
			_uiFrame!.Draw(new Rect(280, 80, 400, 462), new Rect(0, 0, 739, 853));
			_btnMainMenu!.Draw(new Rect(340, 230, 280, 46),   new Rect(0, 0, 220, 36));
			_btnGameOptions!.Draw(new Rect(340, 320, 280, 48), new Rect(0, 0, 264, 45));
			_btnQuitGame!.Draw(new Rect(340, 390, 280, 54),   new Rect(0, 0, 224, 43));
		}
	}

	// ═══════════════════════════════════════════════════════════════════════════
	public override void Dispose()
	{
		for (int i = 0; i < 4; i++) { _mobFly[i]?.Dispose(); _mobIdle[i]?.Dispose(); _mobDead[i]?.Dispose(); }
		_titleBg?.Dispose();    _playBg1?.Dispose();   _playBg2?.Dispose();
		_btnNewGame?.Dispose(); _btnContinue?.Dispose();
		_playerIdle?.Dispose(); _playerRun?.Dispose();  _playerAttack?.Dispose();
		_btnSettings?.Dispose(); _uiFrame?.Dispose();
		_btnMainMenu?.Dispose(); _btnGameOptions?.Dispose(); _btnQuitGame?.Dispose();
		_titleFont?.Dispose(); _hudFont?.Dispose(); _bigFont?.Dispose(); _nameFont?.Dispose();
		_titleBgm?.Dispose();  _battleBgm?.Dispose(); _btnClick?.Dispose();
		base.Dispose();
	}
}

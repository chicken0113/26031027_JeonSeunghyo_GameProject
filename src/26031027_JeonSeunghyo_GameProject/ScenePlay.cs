// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using Vortice.Mathematics;
using Vortice.DirectWrite;

class ScenePlay
{
	static readonly string[] TexturePaths =
	{
		"resource/image/enemy/monsters_creatures_fantasy/goblin/Run.png",
		"resource/image/enemy/monsters_creatures_fantasy/mushroom/Run.png",
		"resource/image/enemy/monsters_creatures_fantasy/flying_eye/Flight.png",
		"resource/image/enemy/monsters_creatures_fantasy/skeleton/Walk.png",
	};
	static readonly int[] FrameCounts = { 8, 8, 8, 4 };

	G2Texture?   _bg;
	G2Texture?[] _tex      = new G2Texture?[4];
	G2Font?      _timerFont;
	G2Font?      _hintFont;

	float        _timeLeft      = 10f;
	float        _spawnTimer;
	float        _spawnInterval = 1.0f;
	int          _lastMonster;
	bool         _anyExited;
	readonly Random _rng = new();

	class MonsterInst
	{
		public int   Type;
		public float X, Y, Speed, Frame;
	}
	readonly List<MonsterInst> _monsters = new();

	public void Initialize()
	{
		string[] bgs =
		{
			"resource/image/background/battle_forest.png",
			"resource/image/background/battle_dungeon.png",
			"resource/image/background/battle_cave.png",
		};
		_bg = new G2Texture(bgs[_rng.Next(3)]);

		for (int i = 0; i < 4; i++)
			_tex[i] = new G2Texture(TexturePaths[i]);

		_timerFont = new G2Font("Arial", 56, FontWeight.Heavy,  Vortice.DirectWrite.FontStyle.Normal,
		                        TextAlignment.Center, ParagraphAlignment.Near);
		_hintFont  = new G2Font("Arial", 24, FontWeight.Normal, Vortice.DirectWrite.FontStyle.Normal,
		                        TextAlignment.Center, ParagraphAlignment.Near);

		G2AppBase.Instance!.ClearColor = new Color4(0f, 0f, 0f, 1f);
		Spawn();
	}

	void Spawn()
	{
		int   type = _rng.Next(4);
		float spd  = 160f + (float)(_rng.NextDouble() * 140);  // 160–300 px/s
		float y    = 280f + (float)(_rng.NextDouble() * 80 - 40); // 240–360
		_monsters.Add(new MonsterInst { Type = type, X = -160f, Y = y, Speed = spd });
	}

	// -1 = 계속 진행, >= 0 = 마지막 몬스터 인덱스 (End 씬으로 전환)
	public int Update(float dt)
	{
		_timeLeft    -= dt;
		_spawnTimer  += dt;

		if (_spawnTimer >= _spawnInterval)
		{
			_spawnTimer    = 0;
			_spawnInterval = 0.8f + (float)(_rng.NextDouble() * 1.4);
			if (_monsters.Count < 5) Spawn();
		}

		var toRemove = new List<MonsterInst>();
		foreach (var m in _monsters)
		{
			m.X     += m.Speed * dt;
			m.Frame  = (m.Frame + 8f * dt) % FrameCounts[m.Type];

			if (m.X > 1020)
			{
				_lastMonster = m.Type;
				_anyExited   = true;
				toRemove.Add(m);
			}
		}
		foreach (var m in toRemove) _monsters.Remove(m);

		if (_timeLeft <= 0)
		{
			if (!_anyExited && _monsters.Count > 0)
				_lastMonster = _monsters[^1].Type;
			return _lastMonster;
		}
		return -1;
	}

	public void Render()
	{
		_bg!.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 1536, 1024));

		foreach (var m in _monsters)
		{
			int fi  = (int)m.Frame;
			var src = new Rect(fi * 150, 0, 150, 150);
			var dst = new Rect(m.X - 80, m.Y - 80, 160, 160);
			_tex[m.Type]!.Draw(dst, src);
		}

		float tl = MathF.Max(0, _timeLeft);
		_timerFont!.DrawText($"{(int)MathF.Ceiling(tl)}",
		    new Rect(0, 8, 960, 70), new Color4(1.0f, 0.3f, 0.3f, 1.0f));

		if (_timeLeft > 7.5f)
			_hintFont!.DrawText("마지막으로 지나간 몬스터를 기억하세요!",
			    new Rect(0, 592, 960, 48), new Color4(0.9f, 0.9f, 0.5f, 1.0f));
	}

	public void Dispose()
	{
		_bg?.Dispose();
		for (int i = 0; i < 4; i++) _tex[i]?.Dispose();
		_timerFont?.Dispose();
		_hintFont?.Dispose();
	}
}

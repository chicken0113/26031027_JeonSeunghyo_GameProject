// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using Vortice.Mathematics;
using Vortice.DirectWrite;

class SceneEnd
{
	static readonly string[] TexturePaths =
	{
		"resource/image/enemy/monsters_creatures_fantasy/goblin/Idle.png",
		"resource/image/enemy/monsters_creatures_fantasy/mushroom/Idle.png",
		"resource/image/enemy/monsters_creatures_fantasy/flying_eye/Flight.png",
		"resource/image/enemy/monsters_creatures_fantasy/skeleton/Idle.png",
	};
	static readonly string[] Names      = { "고블린", "버섯인간", "비행눈알", "해골전사" };
	static readonly int[]    FrameCounts = { 4, 4, 8, 4 };

	readonly int  _correct;
	bool          _answered;
	bool          _wasCorrect;
	float         _resultTimer;
	float         _animTimer;
	bool          _prevDown;

	G2Texture?[] _tex = new G2Texture?[4];
	G2Font?      _questionFont;
	G2Font?      _nameFont;
	G2Font?      _resultFont;

	public SceneEnd(int correct) => _correct = correct;

	public void Initialize()
	{
		for (int i = 0; i < 4; i++)
			_tex[i] = new G2Texture(TexturePaths[i]);

		_questionFont = new G2Font("Arial", 34, FontWeight.Bold,   Vortice.DirectWrite.FontStyle.Normal,
		                           TextAlignment.Center, ParagraphAlignment.Center);
		_nameFont     = new G2Font("Arial", 24, FontWeight.Normal, Vortice.DirectWrite.FontStyle.Normal,
		                           TextAlignment.Center, ParagraphAlignment.Center);
		_resultFont   = new G2Font("Arial", 60, FontWeight.Heavy,  Vortice.DirectWrite.FontStyle.Normal,
		                           TextAlignment.Center, ParagraphAlignment.Center);

		G2AppBase.Instance!.ClearColor = new Color4(0.04f, 0.04f, 0.12f, 1.0f);
	}

	// true 반환 시 Title 씬으로 전환
	public bool Update(float dt)
	{
		_animTimer += dt;

		if (_answered)
		{
			_resultTimer += dt;
			return _resultTimer >= 2.5f;
		}

		var  input = G2AppBase.Instance!.Input;
		bool down  = input.IsButtonDown(System.Windows.Forms.MouseButtons.Left);
		bool click = down && !_prevDown;
		_prevDown = down;

		if (click)
		{
			var p = input.MousePosition;
			for (int i = 0; i < 4; i++)
			{
				var r = GetSpriteRect(i);
				if (p.X >= r.X && p.X <= r.X + r.Width &&
				    p.Y >= r.Y && p.Y <= r.Y + r.Height)
				{
					_answered   = true;
					_wasCorrect = (i == _correct);
					break;
				}
			}
		}
		return false;
	}

	// 4개 초상화를 화면 중앙에 배치 (160×160, 간격 30)
	Rect GetSpriteRect(int i)
	{
		float startX = (960f - 4 * 160f - 3 * 30f) / 2f; // = 115
		return new Rect(startX + i * 190f, 220, 160, 160);
	}

	public void Render()
	{
		_questionFont!.DrawText("마지막으로 지나간 몬스터는?",
		    new Rect(0, 100, 960, 80), new Color4(1.0f, 0.9f, 0.3f, 1.0f));

		for (int i = 0; i < 4; i++)
		{
			var sprite = GetSpriteRect(i);
			int fi     = (int)(_animTimer * 6) % FrameCounts[i];
			var src    = new Rect(fi * 150, 0, 150, 150);
			_tex[i]!.Draw(sprite, src);

			_nameFont!.DrawText(Names[i],
			    new Rect(sprite.X, sprite.Y + 170, 160, 40),
			    new Color4(1.0f, 1.0f, 1.0f, 1.0f));
		}

		if (_answered)
		{
			var    color = _wasCorrect
			    ? new Color4(0.2f, 1.0f, 0.3f, 1.0f)
			    : new Color4(1.0f, 0.25f, 0.25f, 1.0f);
			string msg   = _wasCorrect ? "정답!" : $"오답!  정답: {Names[_correct]}";

			_resultFont!.DrawText(msg,
			    new Rect(0, 445, 960, 120), color);
		}
		else
		{
			_nameFont!.DrawText("클릭하여 선택",
			    new Rect(0, 580, 960, 50), new Color4(0.6f, 0.6f, 0.8f, 1.0f));
		}
	}

	public void Dispose()
	{
		for (int i = 0; i < 4; i++) _tex[i]?.Dispose();
		_questionFont?.Dispose();
		_nameFont?.Dispose();
		_resultFont?.Dispose();
	}
}

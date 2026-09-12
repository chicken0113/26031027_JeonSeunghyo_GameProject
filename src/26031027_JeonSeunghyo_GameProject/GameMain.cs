// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Vortice.Mathematics;
using Vortice.DirectWrite;

class GameMain : G2AppBase
{
	public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
	public override string GameName => GameGlobal.GameName;

	G2Texture? _titleBg;
	G2Texture? _playBg;
	G2Texture? _btnNewGame;
	G2Texture? _player;
	G2Texture? _monster;
	G2Font?    _titleFont;
	bool       _prevClick;
	bool       _started;

	protected override void Initialize()
	{
		_titleBg    = new G2Texture("resource/image/background/title_bg.png");
		_playBg     = new G2Texture("resource/image/background/battle_forest.png");
		_btnNewGame = new G2Texture("resource/image/ui_menu/game_menu/1x/Asset 7 - 1080p.png");
		_player     = new G2Texture("resource/image/player/colour1/nooutline/120x80_pngsheets/_Idle.png");
		_monster    = new G2Texture("resource/image/enemy/monsters_creatures_fantasy/goblin/Idle.png");
		_titleFont  = new G2Font("Arial", 72, FontWeight.Heavy, Vortice.DirectWrite.FontStyle.Normal,
		                         TextAlignment.Center, ParagraphAlignment.Center);

		this.ClearColor = new Color4(0.05f, 0.05f, 0.15f, 1.0f);
	}

	protected override void Update()
	{
		if (_started) return;

		bool down  = Input.IsButtonDown(System.Windows.Forms.MouseButtons.Left);
		bool click = down && !_prevClick;
		_prevClick = down;

		if (click)
			_started = true;
	}

	protected override void Render()
	{
		if (_started)
		{
			_playBg!.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 1536, 1024));

			// 플레이어 (좌측) - Idle 10프레임, 프레임 크기 120×80, 발 위치 y=490
			int pFrame = (int)(TotalTime * 10) % 10;
			_player!.Draw(new Rect(60, 250, 360, 240), new Rect(pFrame * 120, 0, 120, 80));

			// 몬스터 (우측) - 고블린 Idle 4프레임, 좌우반전
			int mFrame = (int)(TotalTime * 6) % 4;
			float cx = 620 + 160f;
			RenderTarget.Transform = Matrix3x2.CreateScale(-1f, 1f, new System.Numerics.Vector2(cx, 0));
			_monster!.Draw(new Rect(620, 260, 320, 320), new Rect(mFrame * 150, 0, 150, 150));
			RenderTarget.Transform = Matrix3x2.Identity;

			return;
		}

		_titleBg!.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 192, 108));

		_titleFont!.DrawText("IDLE QUEST",
		    new Rect(0, 120, 960, 160), new Color4(1.0f, 0.88f, 0.2f, 1.0f));

		// NEW GAME 버튼 (원본 199×35 → 2배 확대, 화면 중앙)
		_btnNewGame!.Draw(new Rect(281, 400, 398, 70), new Rect(0, 0, 199, 35));
	}

	public override void Dispose()
	{
		_titleBg?.Dispose();
		_playBg?.Dispose();
		_btnNewGame?.Dispose();
		_player?.Dispose();
		_monster?.Dispose();
		_titleFont?.Dispose();
		base.Dispose();
	}
}

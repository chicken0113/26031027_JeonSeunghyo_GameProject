// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using Vortice.Mathematics;
using Vortice.DirectWrite;

class GameMain : G2AppBase
{
	public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
	public override string GameName => GameGlobal.GameName;

	G2Texture? _titleBg;
	G2Font?    _titleFont;
	G2Font?    _btnFont;
	bool       _prevClick;
	bool       _started;

	protected override void Initialize()
	{
		_titleBg   = new G2Texture("resource/image/background/title_bg.png");
		_titleFont = new G2Font("Arial", 72, FontWeight.Heavy,  Vortice.DirectWrite.FontStyle.Normal,
		                        TextAlignment.Center, ParagraphAlignment.Center);
		_btnFont   = new G2Font("Arial", 38, FontWeight.Normal, Vortice.DirectWrite.FontStyle.Normal,
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
		{
			var p = Input.MousePosition;
			if (p.X >= 330 && p.X <= 630 && p.Y >= 395 && p.Y <= 455)
				_started = true;
		}
	}

	protected override void Render()
	{
		_titleBg!.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 192, 108));

		_titleFont!.DrawText("IDLE QUEST",
		    new Rect(0, 120, 960, 160), new Color4(1.0f, 0.88f, 0.2f, 1.0f));

		_btnFont!.DrawText("[ START ]",
		    new Rect(0, 395, 960, 65), new Color4(1.0f, 1.0f, 1.0f, 1.0f));
	}

	public override void Dispose()
	{
		_titleBg?.Dispose();
		_titleFont?.Dispose();
		_btnFont?.Dispose();
		base.Dispose();
	}
}

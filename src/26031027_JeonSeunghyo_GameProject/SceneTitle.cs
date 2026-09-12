// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using Vortice.Mathematics;
using Vortice.DirectWrite;

class SceneTitle
{
	G2Texture? _bg;
	G2Font?    _titleFont;
	G2Font?    _startFont;
	G2Font?    _hintFont;
	bool       _prevDown;

	public void Initialize()
	{
		_bg        = new G2Texture("resource/image/background/title_bg.png");
		_titleFont = new G2Font("Arial", 72, FontWeight.Heavy,  Vortice.DirectWrite.FontStyle.Normal,
		                        TextAlignment.Center, ParagraphAlignment.Center);
		_startFont = new G2Font("Arial", 38, FontWeight.Bold,   Vortice.DirectWrite.FontStyle.Normal,
		                        TextAlignment.Center, ParagraphAlignment.Center);
		_hintFont  = new G2Font("Arial", 22, FontWeight.Normal, Vortice.DirectWrite.FontStyle.Normal,
		                        TextAlignment.Center, ParagraphAlignment.Center);

		G2AppBase.Instance!.ClearColor = new Color4(0.05f, 0.05f, 0.15f, 1.0f);
	}

	// true 반환 시 Play 씬으로 전환
	public bool Update(float dt)
	{
		var  input = G2AppBase.Instance!.Input;
		bool down  = input.IsButtonDown(System.Windows.Forms.MouseButtons.Left);
		bool click = down && !_prevDown;
		_prevDown = down;

		if (click)
		{
			var p = input.MousePosition;
			if (p.X >= 330 && p.X <= 630 && p.Y >= 395 && p.Y <= 455)
				return true;
		}
		return false;
	}

	public void Render()
	{
		_bg!.Draw(new Rect(0, 0, 960, 640), new Rect(0, 0, 192, 108));

		_titleFont!.DrawText("IDLE QUEST",
		    new Rect(0, 120, 960, 160), new Color4(1.0f, 0.88f, 0.2f, 1.0f));

		_startFont!.DrawText("[ START ]",
		    new Rect(0, 395, 960, 65), new Color4(1.0f, 1.0f, 1.0f, 1.0f));

		_hintFont!.DrawText("마우스 클릭으로 시작",
		    new Rect(0, 490, 960, 50), new Color4(0.7f, 0.7f, 0.9f, 1.0f));
	}

	public void Dispose()
	{
		_bg?.Dispose();
		_titleFont?.Dispose();
		_startFont?.Dispose();
		_hintFont?.Dispose();
	}
}

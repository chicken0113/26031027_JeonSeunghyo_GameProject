// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

class GameMain : G2AppBase
{
	public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
	public override string GameName => GameGlobal.GameName;

	enum Scene { Title, Play, End }
	Scene _scene;
	SceneTitle? _title;
	ScenePlay?  _play;
	SceneEnd?   _end;

	protected override void Initialize()
	{
		_scene = Scene.Title;
		_title = new SceneTitle();
		_title.Initialize();
	}

	protected override void Update()
	{
		switch (_scene)
		{
			case Scene.Title:
				if (_title!.Update((float)DeltaTime))
				{
					_title.Dispose(); _title = null;
					_play = new ScenePlay();
					_play.Initialize();
					_scene = Scene.Play;
				}
				break;

			case Scene.Play:
				int result = _play!.Update((float)DeltaTime);
				if (result >= 0)
				{
					_play.Dispose(); _play = null;
					_end = new SceneEnd(result);
					_end.Initialize();
					_scene = Scene.End;
				}
				break;

			case Scene.End:
				if (_end!.Update((float)DeltaTime))
				{
					_end.Dispose(); _end = null;
					_title = new SceneTitle();
					_title.Initialize();
					_scene = Scene.Title;
				}
				break;
		}
	}

	protected override void Render()
	{
		switch (_scene)
		{
			case Scene.Title: _title!.Render(); break;
			case Scene.Play:  _play!.Render();  break;
			case Scene.End:   _end!.Render();   break;
		}
	}

	public override void Dispose()
	{
		_title?.Dispose();
		_play?.Dispose();
		_end?.Dispose();
		base.Dispose();
	}
}

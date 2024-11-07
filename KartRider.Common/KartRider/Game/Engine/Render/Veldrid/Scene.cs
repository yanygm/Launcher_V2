using System.Collections.Generic;

namespace KartLibrary.Game.Engine.Render.Veldrid;

public class Scene
{
    private Camera _camera;

    private List<IRenderable> _renderables = new List<IRenderable>();

    public Camera Camera => _camera;

    public void AddRenderable(IRenderable renderable)
    {
        _renderables.Add(renderable);
    }
}
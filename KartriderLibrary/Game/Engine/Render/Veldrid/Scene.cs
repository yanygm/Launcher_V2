using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Render.Veldrid
{
    public class Scene
    {
        private Camera _camera;
        private List<IRenderable> _renderables = new List<IRenderable>();

        public Camera Camera => _camera;

        public Scene()
        {

        }

        public void AddRenderable(IRenderable renderable)
        {
            _renderables.Add(renderable);
        }
    }
}

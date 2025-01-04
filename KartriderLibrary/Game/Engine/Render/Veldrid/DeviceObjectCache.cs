using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KartLibrary.Game.Engine.Render.Veldrid
{
    public class DeviceObjectCache
    {
        private HashSet<string> _usedNames = new HashSet<string>();
        private Dictionary<string, Shader[]> _shaders = new Dictionary<string, Shader[]>();
        private Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
        private Dictionary<string, VertexLayoutDescription> _vertexLayoutDescs = new Dictionary<string, VertexLayoutDescription>();

        public Shader[]? GetShaders(string name)
        {
            if (!_shaders.ContainsKey(name))
                return null;
            return _shaders[name];
        }

        public Texture? GetTexture(string name)
        {
            if (!_textures.ContainsKey(name))
                return null;
            return _textures[name];
        }

        public void AddShaders(string name, Shader[] shaders)
        {
            if (_usedNames.Contains(name))
                throw new InvalidOperationException($"There are exist shaders called \"{name}\".");
            lock(_shaders)
                lock (_usedNames)
                {
                    _usedNames.Add(name);
                    _shaders.Add(name, shaders);
                }
        }

        public void AddTexture(string name, Texture texture)
        {
            if (_usedNames.Contains(name))
                throw new InvalidOperationException($"There are exist texture called \"{name}\".");
            lock (_textures)
                lock (_usedNames)
                {
                    _usedNames.Add(name);
                    _textures.Add(name, texture);
                }
        }

        public bool ContainsShaders(string name) => _shaders.ContainsKey(name);

        public bool ContainsTexture(string name) => _textures.ContainsKey(name);

        public void RemoveShaders(string name)
        {
            if (!_shaders.ContainsKey(name))
                throw new InvalidOperationException($"There are no any shaders called \"{name}\".");
            _shaders.Remove(name);
        }

        public void RemovTexture(string name)
        {
            if (!_textures.ContainsKey(name))
                throw new InvalidOperationException($"There are no any texture called \"{name}\".");
            _textures.Remove(name);
        }
    }
}

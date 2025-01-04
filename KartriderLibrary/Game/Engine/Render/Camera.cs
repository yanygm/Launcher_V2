using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Engine.Render
{
    public class Camera
    {
        private Vector3 _cameraPos;

        private Vector3 _cameraTarget;

        private Vector3 _cameraUp;

        private Matrix4x4 _viewMatrix;

        private Matrix4x4 _projectionMatrix;

        private float _fieldOfView = MathF.PI / 2f;

        private bool _modified = true;

        private Vector2 _sceneSize = new Vector2(1, 1);

        private bool _lockToTarget = false;

        public Vector3 CameraPosition
        {
            get => _cameraPos;
            set
            {
                _cameraPos = value;
                _modified = true;
            }
        }

        public Vector3 CameraTarget
        {
            get => _cameraTarget;
            set
            {
                _cameraTarget = value;
                _modified = true;
            }
        }

        public Vector3 CameraDirection
        {
            get => Vector3.Normalize(_cameraPos - _cameraTarget);
            set
            {
                if (_lockToTarget)
                {
                    Vector3 camToTarget = _cameraTarget - _cameraPos;
                    float len = camToTarget.Length();
                    _cameraPos = _cameraTarget + len * Vector3.Normalize(value);
                }
                else
                    _cameraTarget = _cameraPos - value;
                _modified = true;
            }
        }

        public Vector3 CameraUp
        {
            get => _cameraUp;
            set
            {
                _cameraUp = Vector3.Normalize(value);
                _modified = true;
            }
        }

        public Matrix4x4 ViewMatrix => _viewMatrix;

        public Matrix4x4 ProjectionMatrix => _projectionMatrix;

        public float FieldOfView
        {
            get => _fieldOfView;
            set
            {
                if (value >= MathF.PI)
                    _fieldOfView = MathF.PI - 0.25f;
                else if (value <= 0)
                    _fieldOfView = 0.25f;
                else
                    _fieldOfView = value;
                _modified = true;
            }
        }

        public bool IsModified => _modified;

        public bool LockToTarget
        {
            get => _lockToTarget;
            set
            {
                _lockToTarget = value;
            }
        }

        public Camera()
        {
            
        }

        public void UpdateMatrixes()
        {
            if( _modified)
            {
                _viewMatrix = Matrix4x4.CreateLookAt(_cameraPos, _cameraTarget, _cameraUp);
                _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(_fieldOfView, _sceneSize.X / _sceneSize.Y , 1f, 5000f);
                _modified = false;
            }
        }

        public void UpdateSceneSize(int width, int height) 
        {
            _sceneSize = new Vector2(width, height);
        }
    }
}

using System;
using System.Numerics;

namespace KartLibrary.Game.Engine.Render;

public class Camera
{
    private Vector3 _cameraPos;

    private Vector3 _cameraTarget;

    private Vector3 _cameraUp;

    private Matrix4x4 _viewMatrix;

    private Matrix4x4 _projectionMatrix;

    private float _fieldOfView = (float)Math.PI / 2f;

    private bool _modified = true;

    private Vector2 _sceneSize = new Vector2(1f, 1f);

    private bool _lockToTarget;

    public Vector3 CameraPosition
    {
        get
        {
            return _cameraPos;
        }
        set
        {
            _cameraPos = value;
            _modified = true;
        }
    }

    public Vector3 CameraTarget
    {
        get
        {
            return _cameraTarget;
        }
        set
        {
            _cameraTarget = value;
            _modified = true;
        }
    }

    public Vector3 CameraDirection
    {
        get
        {
            return Vector3.Normalize(_cameraPos - _cameraTarget);
        }
        set
        {
            if (_lockToTarget)
            {
                float num = (_cameraTarget - _cameraPos).Length();
                _cameraPos = _cameraTarget + num * Vector3.Normalize(value);
            }
            else
            {
                _cameraTarget = _cameraPos - value;
            }

            _modified = true;
        }
    }

    public Vector3 CameraUp
    {
        get
        {
            return _cameraUp;
        }
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
        get
        {
            return _fieldOfView;
        }
        set
        {
            if (value >= (float)Math.PI)
            {
                _fieldOfView = 2.89159274f;
            }
            else if (value <= 0f)
            {
                _fieldOfView = 0.25f;
            }
            else
            {
                _fieldOfView = value;
            }

            _modified = true;
        }
    }

    public bool IsModified => _modified;

    public bool LockToTarget
    {
        get
        {
            return _lockToTarget;
        }
        set
        {
            _lockToTarget = value;
        }
    }

    public void UpdateMatrixes()
    {
        if (_modified)
        {
            _viewMatrix = Matrix4x4.CreateLookAt(_cameraPos, _cameraTarget, _cameraUp);
            _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(_fieldOfView, _sceneSize.X / _sceneSize.Y, 1f, 5000f);
            _modified = false;
        }
    }

    public void UpdateSceneSize(int width, int height)
    {
        _sceneSize = new Vector2(width, height);
    }
}
using UnityEngine;
using System.Collections;
using System.IO;


public static class ResUtil
{
    public static ResourceRequest LoadAsync(string _path)
    {
        ResourceRequest _request = Resources.LoadAsync(_path);
        return _request;
    }

    public static T Load<T>(string _path) where T : Object
    {
        if (string.IsNullOrEmpty(_path) == true)
        {
            Logger.Error("Load Path == null");
            return null;
        }


        T _res = Resources.Load<T>(_path);
        if (null == _res)
        {
            Logger.Error($"ResUtil::Load() Load Failed : {_path}");
            return null;
        }
        return _res;
    }

    public static GameObject Create(string _path, Transform _parent, bool _isInit = true)
    {
        GameObject _res = Load<GameObject>(_path);
        if (null == _res)
            return null;

        GameObject _obj = GameObject.Instantiate<GameObject>(_res);
        if (null != _parent)
        {
            if( true == _isInit)
            {
                Attach(_obj.transform, _parent);
            }
            else
            {
                Attach_Local(_res.transform, _obj.transform, _parent);
            }
        }
        return _obj;
    }

    public static T Create<T>(string _path, Transform _parent, bool _isInit = true) where T : Component
    {
        GameObject _res = Load<GameObject>(_path);
        if (null == _res)
            return null;

        GameObject _obj = GameObject.Instantiate<GameObject>(_res);
        if (null != _parent)
        {
            if (true == _isInit)
            {
                Attach(_obj.transform, _parent);
            }
            else
            {
                Attach_Local(_res.transform, _obj.transform, _parent);
            }
        }
        T _component = _obj.GetComponent<T>();
        if(_component == null )
        {
            Logger.Error($"ResUtil::TCreate() Create no Component : {_path} ");
            GameObject.Destroy(_obj);
            return null;
        }
        return _component;
    }

    static public void Attach( Transform _self, Transform _parent )
    {
        if (null == _self)
        {
            Logger.Error("ResUtil::Attach() null == self");
            return;
        }

        if (null == _parent)
        {
            Logger.Error("ResUtil::Attach() null == parent");
            return;
        }

        _self.transform.SetParent(_parent);
        _self.transform.localPosition = Vector3.zero;
        _self.transform.localRotation = Quaternion.identity;
        _self.transform.localScale = Vector3.one;
    }

    static public void Attach_Local(Transform _org, Transform _self, Transform _parent)
    {
        if (null == _self)
        {
            Logger.Error("ResUtil::Attach() null == self");
            return;
        }

        if (null == _parent)
        {
            Logger.Error("ResUtil::Attach() null == parent");
            return;
        }

        _self.transform.SetParent(_parent);
        _self.transform.localPosition = _org.localPosition;
        _self.transform.localRotation = _org.localRotation;
        _self.transform.localScale = _org.localScale;
    }


    public static void SetAllLayer(Transform _transform, int _layer)
    {
        if( null == _transform )
        {
            Logger.Error("ResUtil::SetAllLayer() Transform == null ");
            return;
        }
        _transform.gameObject.layer = _layer;
        for (int i = 0; i < _transform.childCount; ++i)
        {
            SetAllLayer(_transform.GetChild(i), _layer);
        }
    }


    public static GameObject Create(GameObject _prefab, Transform _parent)
    {
        if (null == _prefab)
        {
            Logger.Error("ResUtil::Create() null == prefab");
            return null;
        }

        GameObject _obj = GameObject.Instantiate<GameObject>(_prefab);
        Attach(_obj.transform, _parent);
        return _obj;
    }

    public static T Create<T>(T _prefab, Transform _parent) where T : Component
    {
        if (null == _prefab)
        {
            Logger.Error("ResUtil::Create() null == prefab");
            return null;
        }

        T _obj = GameObject.Instantiate<T>(_prefab);
        Attach(_obj.transform, _parent);
        return _obj;
    }
}

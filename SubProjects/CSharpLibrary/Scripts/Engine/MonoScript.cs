using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class MonoScript {
    ///////////////////////////////////////////////////////////////////////////////////////////
    /// objects
    ///////////////////////////////////////////////////////////////////////////////////////////

    /// Behaviorの生成
    public void CreateBehavior(int _entityId, string _name, ECSGroup _ecsGroup) {
        if (!_ecsGroup) {
            return;
        }

        name_ = _name;
        ecsGroup = _ecsGroup;
        entity = ecsGroup.GetEntity(_entityId);

    }


    /// この behavior が所属するECSGroup
    public ECSGroup ecsGroup {
        get; internal set;
    }

    private string name_;
    public bool enable = true;

    public Entity entity {
        get; internal set;
    }

    public Transform transform {
        get {
            if (entity == null) {
                return null;
            }

            if (entity.transform == null) {
                return null;
            }

            return entity.transform;
        }
    }



    ///////////////////////////////////////////////////////////////////////////////////////////
    /// methods
    ///////////////////////////////////////////////////////////////////////////////////////////

    public virtual void Awake() { }
    public virtual void Initialize() { }
    public virtual void Update() { }
    public virtual void OnDestroy() { }

    public virtual void OnCollisionEnter(Entity collision) { }
    public virtual void OnCollisionExit(Entity collision) { }
    public virtual void OnCollisionStay(Entity collision) { }

    ///////////////////////////////////////////////////////////////////////////////////////////
    /// operators
    ///////////////////////////////////////////////////////////////////////////////////////////
    public static implicit operator bool(MonoScript _monoBehavior) {
        return _monoBehavior != null;
    }

}


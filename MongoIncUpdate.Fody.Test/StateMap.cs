﻿using System.Collections;
using MongoDB.Driver;
using MongoIncUpdate.Fody.Test;

namespace MongoIncUpdate.Fody.Test;

public class StateMap<K, V> : Dictionary<K, V>, IDiffUpdateable where K : notnull
{
    private readonly HashSet<K> _dirtySet = new();

    public V? this[K key]
    {
        get => base[key];
        set
        {
            MarkDirty(key);
            base[key] = value;
        }
    }


    public void BuildUpdate<T>(List<UpdateDefinition<T>> defs, string? key)
    {
        var update = Builders<T>.Update;

        // defs.Add(update.Set(IDiffUpdateable.MakeKey($"k_{k}", key), default(V)));
        //后修改

        foreach (var k in _dirtySet)
        {
            if (base.ContainsKey(k) && base[k] != null)
            {
                defs.Add(update.Set(IDiffUpdateable.MakeKey($"k_{k}", key), base[k]));
            }
            else
            {
                defs.Add(update.Unset(IDiffUpdateable.MakeKey($"k_{k}", key)));
            }
        }

        foreach (var (k, v) in this)
        {
            //这里的已经处理过了
            if (_dirtySet.Contains(k)) continue;

            //null 这种值类型add或者[]修改,会被_dirtyMap捕获
            if (v == null) continue;

            //这种类型只能通过add或者[]修改,会被_dirtyMap捕获
            if (IDiffUpdateable.IsDirectType(v.GetType())) continue;


            if (v is IDiffUpdateable v1)
                v1.BuildUpdate(defs, IDiffUpdateable.MakeKey($"k_{k}", key));
            else
                defs.Add(update.Set(IDiffUpdateable.MakeKey($"k_{k}", key), v));
        }

        _dirtySet.Clear();
    }

    //CleanDirties 必须是public。否则IDiffUpdateable调用不到这里
    public void CleanDirties() //直接写入时候才调用这个,提高性能
    {
        foreach (var (_, v) in this)
        {
            if (v is IDiffUpdateable df) df.CleanDirties(); //清除脏标记
        }

        _dirtySet.Clear();
    }

    public new bool Remove(K key)
    {
        MarkDirty(key);
        return base.Remove(key);
    }

    public new void Add(K key, V v)
    {
        MarkDirty(key);
        base.Add(key, v);
    }

    public new bool TryAdd(K key, V v)
    {
        MarkDirty(key);
        return base.TryAdd(key, v);
    }

    public new void Clear()
    {
        foreach (var (k, _) in this) MarkDirty(k);

        base.Clear();
    }

    private void MarkDirty(K key)
    {
        _dirtySet.Add(key);
    }

    #region 这些字段没什么用 仅仅为了编译过。

    BitArray IDiffUpdateable.Dirties { get; set; } = new(0);
    Dictionary<int, IPropertyCallAdapter> IDiffUpdateable.IdxMapping { get; set; } = new();
    Dictionary<string, int> IDiffUpdateable.NameMapping { get; set; } = new();

    #endregion
}
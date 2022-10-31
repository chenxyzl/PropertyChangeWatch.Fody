﻿using AssemblyToProcess;
using MongoDB.Bson.Serialization.Attributes;

namespace Example;

public enum MyEnum
{
    A,
    B
}

public class TestMyEnum
{
    [BsonSerializer(typeof(DictionarySerializer<
        Dictionary<string, MyEnum>,
        EnumStringSerializer<MyEnum>,
        EnumStringSerializer<MyEnum>>))]
    public Dictionary<MyEnum, MyEnum> FeatureSettings { get; set; }
}

[MongoIncUpdate]
public class Inner2
{
    //多层嵌套 任意测试了  
    public int I { get; set; }
}

[MongoIncUpdate]
public class Inner1
{
    //测试嵌套的dictionary的引用类型嵌套
    [BsonSerializer(typeof(StateMapSerializer<string, Inner2>))]
    public StateMap<string, Inner2> Dic1 { get; set; }
}

[MongoIncUpdate]
public class Item //: IDiffUpdateable
{
    //id
    [BsonId] public int Id { get; set; }

    //string类型带attr
    [BsonElement("RealName")] public string Name { get; set; }

    //测试引用类型
    public Inner1 Inner1 { get; set; }

    //测试dictionary的值类型
    [BsonSerializer(typeof(StateMapSerializer<string, int>))]
    public StateMap<string, int> Dic1 { get; set; }

    //测试dictionary的引用类型嵌套
    [BsonSerializer(typeof(StateMapSerializer<string, Inner1>))]
    public StateMap<string, Inner1> Dic2 { get; set; }
}
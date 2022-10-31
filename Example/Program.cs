﻿using MongoDB.Bson;
using MongoDB.Driver;

namespace Example;

static class ItemInUpdate
{
    public static async Task SaveIm(this Item self, IMongoCollection<Item> collection)
    {
        var diffUpdateable = self as IDiffUpdateable;
        var defs = new List<UpdateDefinition<Item>>();
        diffUpdateable?.BuildUpdate(defs, "");
        if (defs.Count == 0) return;
        var setter = Builders<Item>.Update.Combine(defs);
        var filter = Builders<Item>.Filter.Eq("_id", self.Id);
        await collection.UpdateOneAsync(filter, setter, new UpdateOptions { IsUpsert = true });
        Console.WriteLine($"update data count:{defs.Count}");
    }
}

public sealed class Program
{
    public async Task Run()
    {
        /*
         * 所有的动作基于BsonDocument操作就对了
         */
        //数据库连接，格式为mongodb://账号:密码@服务器地址:端口/数据库名
        var connectionString = "mongodb://admin:123456@127.0.0.1:27017/test?authSource=admin";
        var mongoClient = new MongoClient(connectionString);
        var db = mongoClient.GetDatabase(new MongoUrlBuilder(connectionString).DatabaseName);
        var cc = db.GetCollection<Item>(nameof(Item));


        //构造查询条件 建议以模型的方式插入数据，这样子字段类型是可控的
        const int id = 1;
        var filter = Builders<Item>.Filter.Eq("_id", id);
        //查询老数据
        var beforList = cc.Find(filter).ToList();
        for (var i = 0; i < beforList.Count; i++) Console.WriteLine($"查询结果0 {i}: " + beforList[i].ToJson());

        //修改数据
        //已经在new时候修改了
        var item = new Item { Id = id, Name = "newName1" };
        item.Inner1 = new Inner1 { Dic1 = new StateMap<string, Inner2> { { "0", new Inner2 { I = 0 } } } };
        item.Dic1 = new StateMap<int, int> { { 1, 1 }, { 2, 2 } };
        item.Dic1.Add(3, 3);
        item.Dic1.TryAdd(4, 4);
        //测试初始添加
        item.Dic2 = new StateMap<string, Inner1>
            { { "5", new Inner1 { Dic1 = new StateMap<string, Inner2> { { "5", new Inner2 { I = 5, } } } } } };
        // item.Dic2 = new StateMap<string, int>{ { "5", 5} };
        // // 测试后续添加
        // item.Dic2.Add("6", new Inner1 { Dic1 = new StateMap<string, Inner2>() });
        // item.Dic2.TryGetValue("6", out var item1);
        // item1?.Dic1.Add("6", new Inner2 { I = 6 });

        //保存数据
        await item.SaveIm(cc);

        //查询数据
        var cc1List = cc.Find(filter).ToList();
        for (var i = 0; i < cc1List.Count; i++) Console.WriteLine($"查询结果1 {i}: " + cc1List[i].ToJson());

        //测试修改值类型
        if (item.Dic1.TryGetValue(4, out _)) item.Dic1[4] = 44;
        item.Name = "newName2";
        
        //修改2级数据
        item.Inner1.Dic1["0"].I = 100;
        item.Dic1[1] = 11;
        item.Dic2["5"].Dic1["5"].I = 55;
        // item.Dic2["5"] = 55;

        //测试修改引用类型
        // if (item.Dic2.TryGetValue("6", out var inner1))
        // {
        //     inner1.Dic1["6"] = new Inner2 { I = 66 };
        // }

        //保存数据
        await item.SaveIm(cc);
        //查询数据
        var cc1List2 = cc.Find(filter).ToList();
        for (var i = 0; i < cc1List2.Count; i++) Console.WriteLine($"查询结果2 {i}: " + cc1List2[i].ToJson());

        //测试删除数据
        item.Dic1.Remove(4);
        // item.Dic2.Remove("6");

        //保存数据
        await item.SaveIm(cc);
        //查询数据
        var cc1List3 = cc.Find(filter).ToList();
        for (var i = 0; i < cc1List3.Count; i++) Console.WriteLine($"查询结果2 {i}: " + cc1List3[i].ToJson());
    }

    private static async Task Main(string[] args)
    {
        var a = new Program();
        await a.Run();
    }
}
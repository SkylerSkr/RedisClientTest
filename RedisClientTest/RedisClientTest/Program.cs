using ServiceStack;
using ServiceStack.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RedisClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            #region 注册 破解6000次
            var licenseUtils = typeof(LicenseUtils);
            var members = licenseUtils.FindMembers(MemberTypes.All, BindingFlags.NonPublic | BindingFlags.Static, null, null);
            Type activatedLicenseType = null;
            foreach (var memberInfo in members)
            {
                if (memberInfo.Name.Equals("__activatedLicense", StringComparison.OrdinalIgnoreCase) && memberInfo is FieldInfo fieldInfo)
                    activatedLicenseType = fieldInfo.FieldType;
            }

            if (activatedLicenseType != null)
            {
                var licenseKey = new LicenseKey
                {
                    Expiry = DateTime.Today.AddYears(100),
                    Ref = "ServiceStack",
                    Name = "Enterprise",
                    Type = LicenseType.Enterprise
                };

                var constructor = activatedLicenseType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(LicenseKey) }, null);
                if (constructor != null)
                {
                    var activatedLicense = constructor.Invoke(new object[] { licenseKey });
                    var activatedLicenseField = licenseUtils.GetField("__activatedLicense", BindingFlags.NonPublic | BindingFlags.Static);
                    if (activatedLicenseField != null)
                        activatedLicenseField.SetValue(null, activatedLicense);
                }

                Console.WriteLine(licenseKey.ToJson());
            }
            #endregion

            //创建比较器
            var comparator = TaskComparator.GetInstance(1000);

            //ServiceStack.Redis
            RedisClient redisClient = new RedisClient("192.168.8.204", 6379, "superrd", 0);
            //StackExchange.Redis
            IConnectionMultiplexer proxy = ConnectionMultiplexer.Connect("192.168.8.204:6379,password=superrd,DefaultDatabase=0");
            IDatabase db = proxy.GetDatabase();


            //string千次读写对比
            Console.WriteLine("string千次读写对比");
            comparator.Comparator(() => { redisClient.Set("S1", "AAA"); }, () => { db.StringSet("A2", "AAA"); });
            comparator.Comparator(()=> { redisClient.Set("S1", "AAA",new TimeSpan(1,0,0));  },()=> { db.StringSet("A2", "AAA", new TimeSpan(1, 0, 0)); });
            comparator.Comparator(() => { redisClient.Get<string>("A1"); }, () => { db.StringGet("A2"); });

            //hash千次读写对比
            Console.WriteLine("hash千次读写对比");
            comparator.Comparator(()=> { redisClient.SetEntryInHash("H1","id","xxx"); },()=> { db.HashSet("H2", "id", "xxx"); });
            comparator.Comparator(()=> { redisClient.GetAllEntriesFromHash("H1"); },()=> { db.HashGet("H2", "id"); });

            //list千次读写对比
            Console.WriteLine("list千次读写对比");
            comparator.Comparator(() => { redisClient.PushItemToList("L1","1"); }, () => { db.ListLeftPush("L2","1"); });
            comparator.Comparator(()=> { redisClient.PopItemFromList("L1"); },()=>db.ListLeftPop("L1"));

            //set千次读写对比
            Console.WriteLine("set千次读写对比");
            comparator.Comparator(()=> { redisClient.AddItemToSet("Set1", "xxx"); },()=> { db.SetAdd("Set2", "xxx"); });
            comparator.Comparator(() => { redisClient.GetSetCount("Set1"); }, () => { db.SetLength("Set2"); });

            //SortedSet千次读写对比
            Console.WriteLine("SortedSet千次读写对比");
            comparator.Comparator(()=> { redisClient.AddItemToSortedSet("SS1", "item1", 33); },()=> { db.SortedSetAdd("SS2", "item1", 33); });
            comparator.Comparator(()=> { redisClient.GetSortedSetCount("SS1"); },()=> { db.SortedSetLength("SS2"); });

            //Geo千次读写对比
            Console.WriteLine("Geo千次读写对比");
            comparator.Comparator(()=> { redisClient.GeoAdd("city", 33.33, 33.33, "shanghai"); },()=> { db.GeoAdd("city", 33.34, 33.34, "beijing"); });
            comparator.Comparator(()=> { redisClient.FindGeoResultsInRadius("city","beijing", 333, "m"); },()=> { db.GeoRadius("city","beijing",333,GeoUnit.Meters); });

            Console.ReadKey();
        }
    }
}

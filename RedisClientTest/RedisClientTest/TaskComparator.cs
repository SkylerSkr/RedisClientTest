using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisClientTest
{
    public class TaskComparator
    {
        /// <summary>
        /// 同步锁
        /// </summary>
        private static readonly object obj = new object();
        public static TaskComparator comparator;
        private int count;

        private TaskComparator(int count) {
            this.Count = count;
        }

        public int Count { get => count; set => count = value; }

        public static TaskComparator GetInstance(int count=1)
        {
            if (comparator == null)
            {
                lock (obj)
                {
                    comparator = new TaskComparator(count);
                }
            }
            return comparator;
        }

        public void Comparator(Action action1,Action action2)
        {
            if (comparator != null)
            {
                var time1 = TaskTimeConsuming(action1).TotalSeconds;
                var time2 = TaskTimeConsuming(action2).TotalSeconds;
                Console.WriteLine("前者耗时: {0} 秒，后者耗时: {1} 秒，前者用时少于后者: {2}", time1, time2, ((time2 - time1) / time1) * 100 + "%");
            }
        }

        private TimeSpan TaskTimeConsuming(Action action)
        {
            var time0 = DateTime.Now;
            for (int i = 0; i < Count; i++)
            {
                action.Invoke();
            }
            var time1 = DateTime.Now;
            return time1 - time0;
        }
    }
}

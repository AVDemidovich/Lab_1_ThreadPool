using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace СПП_Lab_1_ThreadPool
{
    public class Task
    {
        private Action work;
        private bool isRunned;


        public Task(Action work)
        {
            this.work = work;
        }

        public void Execute()
        {
            lock (this)
            {
                isRunned = true;
            }
            work();
        }

        public bool IsRunned
        {
            get
            {
                return isRunned;
            }
        }
    }
}

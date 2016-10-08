using System;
using System.Linq;
using System.Threading;
using System.IO;

namespace СПП_Lab_1_ThreadPool
{
    public class TaskInfo
    {
        private FileInfo info;

        public FileInfo getFileInfo()
        {
            return info;
        }

        public TaskInfo(FileInfo info)
        {
            this.info = info;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            /*int nWorkerThreads;
            int nCompletionThreads;
            ThreadPool.GetMaxThreads(out nWorkerThreads, out nCompletionThreads);
            Console.WriteLine("Максимальное количество потоков: " + nWorkerThreads
                + "\nПотоков ввода-вывода доступно: " + nCompletionThreads);*/

            FixedThreadPool pool = new FixedThreadPool(1);
            for (int i = 0; i < 1; i++)
            {
                Thread.Sleep(300);
                Guid g = Guid.NewGuid();
                bool added = pool.Execute(new Task(() =>
                {
                    Thread.Sleep(1000);
                    Console.WriteLine(Mod10Check("4 0 1 2 8 8 8 8 8 8 8 8 1 8 8 1") ? "Да" : "Нет");
                    Console.WriteLine(luhn(4012888888881881) ? "Да" : "Нет");
                }));
                Console.WriteLine(i + ": " + added + "(" + g.ToString() + ")");
            }
            pool.Stop();

            //CopyFiles();
            //SecondTask();
            //Console.WriteLine(Mod10Check("4 0 1 2 8 8 8 8 8 8 8 8 1 8 8 1") ? "Да" : "Нет");
            //Console.WriteLine(luhn(4012888888881881) ? "Да" : "Нет");
            Console.ReadLine();
        }

        // First Task

        static void CopyFiles()
        {

            DirectoryInfo dr = new DirectoryInfo("E:\\test");
            foreach (FileInfo fi in dr.GetFiles("*.txt"))
            {
                TaskInfo ti = new TaskInfo(fi);
                ThreadPool.QueueUserWorkItem((new WaitCallback(CopyFile)), ti);
            }
        }

        private static void CopyFile(object stateInfo)
        {
            TaskInfo ti = (TaskInfo)stateInfo;
            FileInfo fi = ti.getFileInfo();
            fi.CopyTo(@"E:\test2\" + fi.Name, true);
            Console.WriteLine("файл {0}, выполнение внутри потока из пула {1}", fi.Name, Thread.CurrentThread.ManagedThreadId);
            //Thread.Sleep(1000);
        }

        // Second Task

        static void SecondTask()
        {
            DirectoryInfo dr = new DirectoryInfo("E:\\test");
            foreach (FileInfo fi in dr.GetFiles("*.txt"))
            {
                TaskInfo ti = new TaskInfo(fi);
                ThreadPool.QueueUserWorkItem((new WaitCallback(getFiles)), ti);
            }
        }

        static void getFiles(object stateInfo)
        {
            TaskInfo ti = (TaskInfo)stateInfo;
            FileInfo fi = ti.getFileInfo();
            SplitFile(fi);
        }

        private static void SplitFile(FileInfo fi)
        {
            long _file_length = 1;
            using (FileStream _from_stream = new FileStream("E:\\test\\" + fi.Name, FileMode.Open))
            {
                long _file_count = _from_stream.Length / _file_length;
                for (int i = 0; i < _file_count; i++)
                    using (FileStream _to_stream = new FileStream(string.Format("E:\\test\\{0}_{1}.dat", fi.Name, i),
                        FileMode.OpenOrCreate))
                    {
                        long _byte_counter = _file_length;
                        while (_from_stream.CanRead && _byte_counter > 0)
                        {
                            _byte_counter--;
                            _to_stream.WriteByte((byte)_from_stream.ReadByte());
                        }
                    }
            }
        }

        //third task
        static bool Mod10Check(string creditCardNumber)
        {

            if (string.IsNullOrEmpty(creditCardNumber))
            {
                return false;
            }


            int sumOfDigits = creditCardNumber.Where((e) => e >= '0' && e <= '9')
                            .Reverse()
                            .Select((e, i) => ((int)e - 48) * (i % 2 == 0 ? 1 : 2))
                            .Sum((e) => e / 10 + e % 10);



            return sumOfDigits % 10 == 0;
        }

        static bool luhn(long n)
        {
            long nextdigit, sum = 0;
            bool alt = false;
            while (n != 0)
            {
                nextdigit = n % 10;
                if (alt)
                {
                    nextdigit *= 2;
                    nextdigit -= (nextdigit > 9) ? 9 : 0;
                }
                sum += nextdigit;
                alt = !alt;
                n /= 10;
            }
            return (sum % 10 == 0);
        }
    }
}
